using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Exploration.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.UI
{
    public class ResourceDebugView : MonoBehaviour
    {
        [SerializeField] private CardController cardController;
        [SerializeField] private RunPhaseController runPhaseController;
        [Tooltip("Optional. Automatically found in the scene when left empty.")]
        [SerializeField] private ExplorationSystem explorationSystem;
        [SerializeField] private TMP_Text foodText;
        [SerializeField] private TMP_Text resourceText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text pendingDamageText;
        [SerializeField] private TMP_Text zombieThreatText;
        [SerializeField] private TMP_Text combatLimitText;
        [SerializeField] private TMP_Text zombieSummaryText;
        [SerializeField] private TMP_Text moraleText;
        [SerializeField] private TMP_Text foodPlanText;
        [SerializeField] private TMP_Text foodStorageText;
        [SerializeField] private TMP_Text roundEndDebugText;
        [SerializeField] private TMP_Text deckDebugText;

        private void Reset()
        {
            cardController = FindObjectOfType<CardController>();
            runPhaseController = FindObjectOfType<RunPhaseController>();
            explorationSystem = FindObjectOfType<ExplorationSystem>();
        }

        private void OnEnable()
        {
            if (runPhaseController == null)
            {
                runPhaseController = FindObjectOfType<RunPhaseController>();
            }

            if (cardController == null)
            {
                cardController = FindObjectOfType<CardController>();
            }

            if (explorationSystem == null)
            {
                explorationSystem = FindObjectOfType<ExplorationSystem>();
            }

            if (cardController != null)
            {
                cardController.StateChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (cardController != null)
            {
                cardController.StateChanged -= Refresh;
            }
        }

        private void Update()
        {
            Refresh();
        }

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            SetText(foodText, BuildFoodResourceText());
            SetText(resourceText, BuildResourceText(ResourceSystem.GetResource(), ResourceSystem.AvailableResource, ResourceSystem.ReservedResource));
            SetText(
                moraleText,
                $"{MoraleSystem.CurrentMorale}/{MoraleSystem.MaximumMorale} [{MoraleSystem.CollapseStage}]");
            SetText(foodPlanText, BuildFoodPlanText());
            SetText(foodStorageText, BuildFoodStorageText());
            SetText(roundEndDebugText, BuildRoundEndText());
            SetText(combatLimitText, BuildCombatLimitText());
            SetText(zombieSummaryText, BuildZombieSummaryText());
            SetText(deckDebugText, BuildDeckAndRegionDebugText());

            if (cardController == null)
            {
                SetText(energyText, $"{EnergySystem.CurrentEnergy}/{EnergySystem.MaxEnergy}");
                SetText(pendingDamageText, $"{CombatSystem.PendingDamage}/{CombatSystem.MaxCombatValue}");
                SetText(zombieThreatText, $"{CombatSystem.ZombieThreatCount}");
                return;
            }

            SetText(energyText, $"{EnergySystem.CurrentEnergy}/{EnergySystem.MaxEnergy}");
            SetText(pendingDamageText, $"{CombatSystem.PendingDamage}/{CombatSystem.MaxCombatValue}");
            SetText(zombieThreatText, $"{CombatSystem.ZombieThreatCount}");
        }

        private string BuildDeckAndRegionDebugText()
        {
            List<string> sections = new List<string>();
            if (cardController != null)
            {
                sections.Add(
                    $"正規抽牌:{cardController.RegularDrawPileCount}  附加:{cardController.AdditiveDrawPileCount}\n" +
                    $"手牌:{cardController.HandCards.Count}  棄牌:{cardController.DiscardPile.Count}  已打出:{cardController.PlayedCards.Count}\n" +
                    $"耗盡:{cardController.ExhaustedCards.Count}  補位需求:{cardController.VacancyRecords.Count}");
            }

            string regionDebug = BuildRegionDebugText();
            if (!string.IsNullOrEmpty(regionDebug))
            {
                sections.Add(regionDebug);
            }

            return string.Join("\n", sections);
        }

        private string BuildRegionDebugText()
        {
            if (explorationSystem == null || !explorationSystem.IsRunInitialized)
            {
                return string.Empty;
            }

            List<RegionRuntime> regions = new List<RegionRuntime>(explorationSystem.RegionRuntimes.Values);
            regions.Sort((left, right) => string.CompareOrdinal(left.RegionId, right.RegionId));

            List<string> lines = new List<string> { "地區 Runtime：" };
            foreach (RegionRuntime region in regions)
            {
                string label = !string.IsNullOrWhiteSpace(region.Data.DisplayName)
                    ? region.Data.DisplayName
                    : region.RegionId;
                lines.Add(
                    $"{label} 包{region.RemainingPackCount}/{region.InitialPackCount} " +
                    $"屍{region.RemainingZombieCount}/{region.InitialZombieCount} " +
                    $"熱{region.Heat:P0} 遭遇{region.EncounterChance:P0} " +
                    $"搜索{region.SearchProgress:P0} 清理{region.Clearance:P0}");
            }

            return string.Join("\n", lines);
        }

        private static string BuildFoodPlanText()
        {
            string pending = FoodPlanSystem.HasPendingPlan ? $" -> {FoodPlanSystem.PendingPlan}" : string.Empty;
            return $"{FoodPlanSystem.CurrentPlan}{pending}";
        }

        private static string BuildFoodStorageText()
        {
            int consumption = FoodPlanSystem.GetRoundFoodConsumption();
            RoundFoodConsumptionResult preview = FoodSystem.PreviewRoundFoodConsumption(consumption);
            return $"需求 {preview.Requested}：臨時 {preview.TemporaryConsumed}、永久 {preview.PermanentConsumed}、短缺 {preview.Shortage}"
                + $"，結算後永久 {preview.PermanentAfter}/{FoodSystem.StorageCapacity}"
                + (preview.TemporaryDiscarded > 0 ? $"，浪費臨時 {preview.TemporaryDiscarded}" : string.Empty);
        }

        private string BuildRoundEndText()
        {
            if (runPhaseController == null)
            {
                return string.Empty;
            }

            RoundEndReport report = runPhaseController.LastRoundEndReport;
            return report != null ? report.BuildSummary() : string.Empty;
        }

        private static string BuildCombatLimitText()
        {
            return $"{CombatSystem.PendingDamage}/{CombatSystem.MaxCombatValue}, loss {CombatSystem.OvercapLossPercent}%";
        }

        private static string BuildZombieSummaryText()
        {
            int markedCount = 0;
            foreach (ZombieThreatRuntime threat in CombatSystem.ZombieThreats)
            {
                if (threat != null && threat.IsMarkedForKill)
                {
                    markedCount++;
                }
            }

            return $"{CombatSystem.ZombieThreatCount} zombies, {markedCount} marked";
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static string BuildResourceText(int total, int available, int reserved)
        {
            return reserved > 0 ? $"{total}（可用 {available}／保留 {reserved}）" : total.ToString();
        }

        private static string BuildFoodResourceText()
        {
            string permanent = BuildResourceText(FoodSystem.GetFood(), FoodSystem.AvailableFood, FoodSystem.ReservedFood);
            return FoodSystem.TemporaryFood > 0
                ? $"{permanent}（臨時 {FoodSystem.TemporaryFood}）"
                : permanent;
        }
    }
}
