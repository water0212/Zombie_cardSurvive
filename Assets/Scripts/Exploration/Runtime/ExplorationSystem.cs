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
        private CardController currentCardController;

        public event Action ExplorationChanged;

        public ExplorationFlowState State { get; private set; } = ExplorationFlowState.Hidden;
        public ExplorationCardData CurrentExplorationCard { get; private set; }
        public IReadOnlyList<ExplorationRegionData> CurrentOffers => currentOffers;
        public ExplorationRegionData SelectedRegion { get; private set; }
        public ExplorationResult LastResult { get; private set; }

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
            State = ExplorationFlowState.Hidden;
            CurrentExplorationCard = null;
            currentCardController = null;
            SelectedRegion = null;
            currentOffers.Clear();
            NotifyChanged();
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

            CurrentExplorationCard = explorationCard;
            currentCardController = cardController;
            SelectedRegion = null;
            LastResult = null;
            currentOffers.Clear();
            currentOffers.AddRange(SelectRegionOffers(explorationCard));

            if (currentOffers.Count == 0)
            {
                LastResult = new ExplorationResult();
                LastResult.AddMessage("沒有可探索地區。請確認 ExplorationSystem 的 Region Pool。");
                State = ExplorationFlowState.Result;
                Debug.LogWarning("Exploration has no valid region offers.");
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
                if (finalWeight <= 0f)
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

            ResolveCardPackDrops(region, cardController, result);
            ResolveRegionRisks(region, cardController, result);
            return result;
        }

        private void ResolveCardPackDrops(ExplorationRegionData region, CardController cardController, ExplorationResult result)
        {
            CardPackDropEntry selectedDrop = SelectWeightedPackDrop(region.CardPackDrops);
            if (selectedDrop == null || selectedDrop.CardPack == null)
            {
                result.AddMessage("沒有取得卡包。");
                Debug.LogWarning($"Region '{region.name}' has no valid card pack drops.");
                return;
            }

            int packCount = UnityEngine.Random.Range(selectedDrop.MinPackCount, selectedDrop.MaxPackCount + 1);
            for (int i = 0; i < packCount; i++)
            {
                OpenCardPack(selectedDrop.CardPack, cardController, result);
            }
        }

        private static CardPackDropEntry SelectWeightedPackDrop(IReadOnlyList<CardPackDropEntry> drops)
        {
            int totalWeight = 0;
            if (drops == null)
            {
                return null;
            }

            foreach (CardPackDropEntry drop in drops)
            {
                if (drop != null && drop.CardPack != null)
                {
                    totalWeight += drop.Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;
            foreach (CardPackDropEntry drop in drops)
            {
                if (drop == null || drop.CardPack == null)
                {
                    continue;
                }

                cumulative += drop.Weight;
                if (roll < cumulative)
                {
                    return drop;
                }
            }

            return null;
        }

        private void OpenCardPack(CardPackData cardPack, CardController cardController, ExplorationResult result)
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

            foreach (CardBase card in generatedCards)
            {
                if (card == null)
                {
                    continue;
                }

                if (cardPack.IsZombiePollution(card) && card is ZombieCardData zombieCard)
                {
                    AddZombieCardToDiscard(cardController, zombieCard, result);
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
        }

        private void ResolveRegionRisks(ExplorationRegionData region, CardController cardController, ExplorationResult result)
        {
            foreach (RegionRiskEntry risk in region.Risks)
            {
                if (risk == null || risk.ChancePercent <= 0 || risk.Amount <= 0)
                {
                    continue;
                }

                int roll = UnityEngine.Random.Range(1, 101);
                if (roll > risk.ChancePercent)
                {
                    continue;
                }

                ApplyRisk(risk, cardController, result);
            }
        }

        private void ApplyRisk(RegionRiskEntry risk, CardController cardController, ExplorationResult result)
        {
            switch (risk.RiskType)
            {
                case RegionRiskType.AddZombieCardToDiscard:
                    ApplyZombieRisk(risk, cardController, result);
                    break;
                case RegionRiskType.LoseMorale:
                    MoraleSystem.ReduceMorale(risk.Amount);
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

        private void ApplyZombieRisk(RegionRiskEntry risk, CardController cardController, ExplorationResult result)
        {
            if (risk.ZombieCard == null)
            {
                result.AddMessage("觸發污染風險，但沒有設定殭屍卡。");
                Debug.LogWarning("Zombie risk triggered but no ZombieCardData is assigned.");
                return;
            }

            for (int i = 0; i < risk.Amount; i++)
            {
                AddZombieCardToDiscard(cardController, risk.ZombieCard, result);
            }

            result.AddMessage(BuildRiskMessage(risk, $"污染：{risk.Amount} 張殭屍卡進入棄牌堆。"));
        }

        private static void AddZombieCardToDiscard(CardController cardController, ZombieCardData zombieCard, ExplorationResult result)
        {
            if (cardController == null)
            {
                result.AddMessage($"未加入污染：{GetCardName(zombieCard)}（場景缺少 CardController）。");
                Debug.LogWarning("Exploration could not add zombie card to discard pile because CardController is missing.");
                return;
            }

            if (cardController.AddCardToDiscardPile(zombieCard))
            {
                result.AddZombieCard(zombieCard);
            }
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
