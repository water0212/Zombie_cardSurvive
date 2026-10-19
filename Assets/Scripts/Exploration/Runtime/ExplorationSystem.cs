using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Exploration.Data;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Exploration.Runtime
{
    public class ExplorationSystem : MonoBehaviour
    {
        private static ExplorationSystem activeInstance;

        [SerializeField] private CardInventory inventory;
        [SerializeField] private List<ExplorationRegionData> regionPool = new List<ExplorationRegionData>();

        private readonly List<ExplorationRegionData> currentOffers = new List<ExplorationRegionData>();
        private readonly Dictionary<string, RegionRuntime> regionRuntimes =
            new Dictionary<string, RegionRuntime>(StringComparer.Ordinal);
        private CardController currentCardController;

        public event Action ExplorationChanged;
        public event Action RegionStatesReset;
        public event Action<RegionRuntime> RegionStateChanged;

        public ExplorationFlowState State { get; private set; } = ExplorationFlowState.Hidden;
        public ExplorationCardData CurrentExplorationCard { get; private set; }
        public IReadOnlyList<ExplorationRegionData> CurrentOffers => currentOffers;
        public IReadOnlyDictionary<string, RegionRuntime> RegionRuntimes => regionRuntimes;
        public ExplorationRegionData SelectedRegion { get; private set; }
        public ExplorationResult LastResult { get; private set; }
        public bool IsRunInitialized { get; private set; }

        private void Awake()
        {
            activeInstance = this;
            ResolveSceneReferences();
        }

        private void OnEnable()
        {
            activeInstance = this;
        }

        private void OnDisable()
        {
            if (ReferenceEquals(activeInstance, this))
            {
                activeInstance = null;
            }
        }

        private void OnDestroy()
        {
            DetachRegionRuntimeEvents();
            if (ReferenceEquals(activeInstance, this))
            {
                activeInstance = null;
            }
        }

        public static bool BeginExploration(ExplorationCardData explorationCard, CardController cardController)
        {
            ExplorationSystem system = activeInstance != null ? activeInstance : FindObjectOfType<ExplorationSystem>();
            if (system == null)
            {
                Debug.LogWarning("Exploration skipped: no ExplorationSystem found in the scene.");
                return false;
            }

            return system.BeginExplorationInternal(explorationCard, cardController);
        }

        [ContextMenu("Hide Exploration")]
        public void Hide()
        {
            ResetFlowState(false);
            NotifyChanged();
        }

        [ContextMenu("Initialize Exploration Run")]
        public void InitializeRun()
        {
            DetachRegionRuntimeEvents();
            regionRuntimes.Clear();
            ResetFlowState(true);

            for (int index = 0; index < regionPool.Count; index++)
            {
                ExplorationRegionData region = regionPool[index];
                if (region == null)
                {
                    Debug.LogWarning($"Exploration region at index {index} was skipped because the entry is null.", this);
                    continue;
                }

                string regionId = region.RegionId?.Trim();
                if (string.IsNullOrWhiteSpace(regionId))
                {
                    Debug.LogWarning(
                        $"Exploration region '{GetRegionLabel(region)}' was skipped because RegionId is missing.",
                        region);
                    continue;
                }

                if (regionRuntimes.ContainsKey(regionId))
                {
                    Debug.LogWarning(
                        $"Exploration region '{GetRegionLabel(region)}' was skipped because RegionId '{regionId}' is duplicated. The first entry was kept.",
                        region);
                    continue;
                }

                RegionRuntime runtime = new RegionRuntime(region);
                runtime.Changed += HandleRegionRuntimeChanged;
                regionRuntimes.Add(regionId, runtime);
            }

            IsRunInitialized = true;
            if (regionRuntimes.Count == 0)
            {
                Debug.LogWarning("Exploration run initialized without any valid regions.", this);
            }

            RegionStatesReset?.Invoke();
            NotifyChanged();
        }

        public bool TryGetRegionRuntime(string regionId, out RegionRuntime runtime)
        {
            runtime = null;
            if (string.IsNullOrWhiteSpace(regionId))
            {
                return false;
            }

            return regionRuntimes.TryGetValue(regionId.Trim(), out runtime);
        }

        public RegionRuntime GetRegionRuntime(string regionId)
        {
            return TryGetRegionRuntime(regionId, out RegionRuntime runtime) ? runtime : null;
        }

        public bool TryRestockRegionPack(string regionId, string packId, int amount)
        {
            return TryGetRegionRuntime(regionId, out RegionRuntime runtime)
                && runtime.TryRestockPack(packId, amount);
        }

        public bool TryRestockRegionZombies(string regionId, int amount)
        {
            return TryGetRegionRuntime(regionId, out RegionRuntime runtime)
                && runtime.TryRestockZombie(amount);
        }

        public int ResolveDailyHeatDecay()
        {
            int changedRegionCount = 0;
            foreach (RegionRuntime runtime in regionRuntimes.Values)
            {
                if (runtime.ResolveDailyHeatDecay())
                {
                    changedRegionCount++;
                }
            }

            return changedRegionCount;
        }

        public bool ChooseRegion(int index)
        {
            if (State != ExplorationFlowState.ChoosingRegion)
            {
                return false;
            }

            if (index < 0 || index >= currentOffers.Count)
            {
                return false;
            }

            SelectedRegion = currentOffers[index];
            LastResult = ResolveRegion(SelectedRegion, currentCardController);
            State = ExplorationFlowState.Result;
            NotifyChanged();
            return true;
        }

        private bool BeginExplorationInternal(ExplorationCardData explorationCard, CardController cardController)
        {
            if (explorationCard == null)
            {
                Debug.LogWarning("Exploration skipped: exploration card is null.");
                return false;
            }

            ResolveSceneReferences();

            if (!IsRunInitialized)
            {
                InitializeRun();
            }

            CurrentExplorationCard = explorationCard;
            currentCardController = cardController;
            SelectedRegion = null;
            LastResult = null;
            currentOffers.Clear();
            currentOffers.AddRange(SelectRegionOffers(explorationCard));

            if (currentOffers.Count == 0)
            {
                LastResult = new ExplorationResult();
                LastResult.AddMessage("目前沒有尚有物資的可探索地區。");
                State = ExplorationFlowState.Result;
                Debug.LogWarning("Exploration has no valid region offers with remaining supplies.");
            }
            else
            {
                State = ExplorationFlowState.ChoosingRegion;
            }

            NotifyChanged();
            return true;
        }

        private List<ExplorationRegionData> SelectRegionOffers(ExplorationCardData explorationCard)
        {
            List<WeightedRegion> candidates = BuildWeightedCandidates(explorationCard);
            List<ExplorationRegionData> offers = new List<ExplorationRegionData>();
            int offerCount = explorationCard.RegionOfferCount;

            while (offers.Count < offerCount && candidates.Count > 0)
            {
                int selectedIndex = SelectWeightedRegionIndex(candidates);
                if (selectedIndex < 0)
                {
                    break;
                }

                offers.Add(candidates[selectedIndex].Region);
                candidates.RemoveAt(selectedIndex);
            }

            if (offers.Count < offerCount)
            {
                Debug.LogWarning($"Exploration only found {offers.Count} region offers, expected {offerCount}.");
            }

            return offers;
        }

        private List<WeightedRegion> BuildWeightedCandidates(ExplorationCardData explorationCard)
        {
            List<WeightedRegion> candidates = new List<WeightedRegion>();
            foreach (ExplorationRegionData region in regionPool)
            {
                if (region == null)
                {
                    continue;
                }

                float multiplier = explorationCard.GetRegionTierMultiplier(region.RegionTier);
                float finalWeight = region.BaseOfferWeight * multiplier;
                if (finalWeight <= 0f
                    || !TryGetRegionRuntime(region.RegionId, out RegionRuntime runtime)
                    || !ReferenceEquals(runtime.Data, region)
                    || !runtime.HasDrawablePacks)
                {
                    continue;
                }

                candidates.Add(new WeightedRegion(region, finalWeight));
            }

            return candidates;
        }

        private static int SelectWeightedRegionIndex(IReadOnlyList<WeightedRegion> candidates)
        {
            float totalWeight = 0f;
            foreach (WeightedRegion candidate in candidates)
            {
                totalWeight += Mathf.Max(0f, candidate.Weight);
            }

            if (totalWeight <= 0f)
            {
                return -1;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += Mathf.Max(0f, candidates[i].Weight);
                if (roll <= cumulative)
                {
                    return i;
                }
            }

            return candidates.Count - 1;
        }

        private ExplorationResult ResolveRegion(ExplorationRegionData region, CardController cardController)
        {
            ExplorationResult result = new ExplorationResult();
            if (region == null)
            {
                result.AddMessage("探索失敗：地區資料不存在。");
                return result;
            }

            if (!TryGetRegionRuntime(region.RegionId, out RegionRuntime runtime)
                || !ReferenceEquals(runtime.Data, region))
            {
                result.AddMessage("探索失敗：找不到本局地區狀態。");
                Debug.LogWarning($"Region '{GetRegionLabel(region)}' has no matching RegionRuntime.", region);
                return result;
            }

            ResolveCardPackDrop(runtime, cardController, result);
            ResolveRegionRisks(runtime, cardController, result);
            runtime.RecordExploration();
            return result;
        }

        private void ResolveCardPackDrop(RegionRuntime region, CardController cardController, ExplorationResult result)
        {
            if (!region.TryDrawPack(out RegionPackRuntime selectedPack) || selectedPack.Data == null)
            {
                result.AddMessage("沒有取得卡包。");
                Debug.LogWarning($"Region '{GetRegionLabel(region.Data)}' has no drawable card packs.", region.Data);
                return;
            }

            OpenCardPack(region, selectedPack.Data, cardController, result);
            if (!region.HasRemainingPacks)
            {
                result.AddMessage($"地區「{GetRegionLabel(region.Data)}」的物資已枯竭。");
            }
        }

        private void OpenCardPack(
            RegionRuntime region,
            CardPackData cardPack,
            CardController cardController,
            ExplorationResult result)
        {
            if (cardPack == null)
            {
                return;
            }

            result.AddOpenedPack(cardPack);
            List<CardBase> generatedCards = cardPack.GenerateCards();
            if (generatedCards.Count == 0)
            {
                result.AddMessage($"卡包「{GetPackName(cardPack)}」沒有可取得卡牌。");
                Debug.LogWarning($"Card pack '{cardPack.name}' generated no cards. Check its card pool.");
                return;
            }

            int skippedZombiePollution = 0;
            foreach (CardBase card in generatedCards)
            {
                if (card == null)
                {
                    continue;
                }

                if (cardPack.IsZombiePollution(card) && card is ZombieCardData zombieCard)
                {
                    if (region.RemainingZombieCount <= 0)
                    {
                        skippedZombiePollution++;
                        continue;
                    }

                    TryAddRegionalZombieCardToDiscard(region, cardController, zombieCard, result);
                    continue;
                }

                if (inventory != null)
                {
                    inventory.AddCard(card);
                    result.AddStoredCard(card);
                }
                else
                {
                    result.AddMessage($"未加入倉庫：{GetCardName(card)}（場景缺少 CardInventory）。");
                    Debug.LogWarning("Exploration could not add card to inventory because CardInventory is missing.");
                }
            }

            if (skippedZombiePollution > 0)
            {
                result.AddMessage($"當地殭屍已清空，略過 {skippedZombiePollution} 張污染卡。");
            }
        }

        private void ResolveRegionRisks(RegionRuntime region, CardController cardController, ExplorationResult result)
        {
            MoraleBatchRequest moraleBatch = new MoraleBatchRequest($"Exploration:{region.RegionId}");
            foreach (RegionRiskEntry risk in region.Data.Risks)
            {
                if (risk == null || risk.Amount <= 0)
                {
                    continue;
                }

                bool triggered = risk.RiskType == RegionRiskType.AddZombieCardToDiscard
                    ? RollChance(region.EncounterChance)
                    : RollChance(risk.ChancePercent / 100f);
                if (!triggered)
                {
                    continue;
                }

                ApplyRisk(region, risk, cardController, result, moraleBatch);
            }

            if (moraleBatch.Count > 0)
            {
                result.SetMoraleSettlement(MoraleSystem.CommitBatch(moraleBatch));
            }
        }

        private void ApplyRisk(
            RegionRuntime region,
            RegionRiskEntry risk,
            CardController cardController,
            ExplorationResult result,
            MoraleBatchRequest moraleBatch)
        {
            switch (risk.RiskType)
            {
                case RegionRiskType.AddZombieCardToDiscard:
                    ApplyZombieRisk(region, risk, cardController, result);
                    break;
                case RegionRiskType.LoseMorale:
                    moraleBatch.Add($"RegionRisk:{risk.RiskType}", -risk.Amount);
                    result.AddMessage(BuildRiskMessage(risk, $"士氣下降 {risk.Amount}。"));
                    break;
                case RegionRiskType.LoseFood:
                    FoodSystem.SpendFood(risk.Amount);
                    result.AddMessage(BuildRiskMessage(risk, $"失去食物 {risk.Amount}。"));
                    break;
                default:
                    result.AddMessage(BuildRiskMessage(risk, $"觸發風險：{risk.RiskType}。"));
                    break;
            }
        }

        private void ApplyZombieRisk(
            RegionRuntime region,
            RegionRiskEntry risk,
            CardController cardController,
            ExplorationResult result)
        {
            if (risk.ZombieCard == null)
            {
                result.AddMessage("觸發污染風險，但沒有設定殭屍卡。");
                Debug.LogWarning("Zombie risk triggered but no ZombieCardData is assigned.");
                return;
            }

            int amountToGenerate = Mathf.Min(risk.Amount, region.RemainingZombieCount);
            int generatedCount = 0;
            for (int i = 0; i < amountToGenerate; i++)
            {
                if (!TryAddRegionalZombieCardToDiscard(region, cardController, risk.ZombieCard, result))
                {
                    break;
                }

                generatedCount++;
            }

            if (generatedCount > 0)
            {
                result.AddMessage(BuildRiskMessage(risk, $"污染：{generatedCount} 張殭屍卡進入棄牌堆。"));
            }
            else if (region.RemainingZombieCount <= 0)
            {
                result.AddMessage("風險已觸發，但當地已沒有剩餘殭屍。");
            }
        }

        private static bool TryAddRegionalZombieCardToDiscard(
            RegionRuntime region,
            CardController cardController,
            ZombieCardData zombieCard,
            ExplorationResult result)
        {
            if (region == null || region.RemainingZombieCount <= 0)
            {
                return false;
            }

            if (cardController == null)
            {
                result.AddMessage($"未加入污染：{GetCardName(zombieCard)}（場景缺少 CardController）。");
                Debug.LogWarning("Exploration could not add zombie card to discard pile because CardController is missing.");
                return false;
            }

            if (!cardController.AddCardToDiscardPile(zombieCard))
            {
                return false;
            }

            if (!region.TryConsumeZombie())
            {
                Debug.LogError($"Region '{region.RegionId}' failed to consume zombie stock after creating a zombie card.");
                return false;
            }

            result.AddZombieCard(zombieCard);
            return true;
        }

        private static bool RollChance(float chance)
        {
            chance = Mathf.Clamp01(chance);
            if (chance <= 0f)
            {
                return false;
            }

            return chance >= 1f || UnityEngine.Random.value < chance;
        }

        private static string BuildRiskMessage(RegionRiskEntry risk, string fallback)
        {
            return !string.IsNullOrWhiteSpace(risk.Description) ? risk.Description : fallback;
        }

        private static string GetPackName(CardPackData cardPack)
        {
            return !string.IsNullOrWhiteSpace(cardPack.DisplayName) ? cardPack.DisplayName : cardPack.name;
        }

        private static string GetCardName(CardBase card)
        {
            return card != null && !string.IsNullOrWhiteSpace(card.DisplayName) ? card.DisplayName : card != null ? card.name : "-";
        }

        private void ResolveSceneReferences()
        {
            if (inventory == null)
            {
                inventory = FindObjectOfType<CardInventory>();
            }
        }

        private void ResetFlowState(bool clearLastResult)
        {
            State = ExplorationFlowState.Hidden;
            CurrentExplorationCard = null;
            currentCardController = null;
            SelectedRegion = null;
            currentOffers.Clear();
            if (clearLastResult)
            {
                LastResult = null;
            }
        }

        private void HandleRegionRuntimeChanged(RegionRuntime runtime)
        {
            RegionStateChanged?.Invoke(runtime);
        }

        private static string GetRegionLabel(ExplorationRegionData region)
        {
            if (region == null)
            {
                return "-";
            }

            if (!string.IsNullOrWhiteSpace(region.DisplayName))
            {
                return region.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(region.RegionId))
            {
                return region.RegionId;
            }

            return !string.IsNullOrWhiteSpace(region.name) ? region.name : "Unnamed Region";
        }

        private void DetachRegionRuntimeEvents()
        {
            foreach (RegionRuntime runtime in regionRuntimes.Values)
            {
                runtime.Changed -= HandleRegionRuntimeChanged;
            }
        }

        private void NotifyChanged()
        {
            ExplorationChanged?.Invoke();
        }

        private struct WeightedRegion
        {
            public WeightedRegion(ExplorationRegionData region, float weight)
            {
                Region = region;
                Weight = weight;
            }

            public ExplorationRegionData Region { get; }
            public float Weight { get; }
        }
    }
}
