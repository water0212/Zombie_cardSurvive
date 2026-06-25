using TMPro;
using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.UI
{
    public class ResourceDebugView : MonoBehaviour
    {
        [SerializeField] private CardController cardController;
        [SerializeField] private RunPhaseController runPhaseController;
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
        }

        private void OnEnable()
        {
            if (runPhaseController == null)
            {
                runPhaseController = FindObjectOfType<RunPhaseController>();
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
            SetText(foodText, $"{FoodSystem.GetFood()}");
            SetText(resourceText, $"{ResourceSystem.GetResource()}");
            SetText(moraleText, $"{MoraleSystem.GetMorale()}/{MoraleSystem.MaxMorale}");
            SetText(foodPlanText, BuildFoodPlanText());
            SetText(foodStorageText, BuildFoodStorageText());
            SetText(roundEndDebugText, BuildRoundEndText());
            SetText(combatLimitText, BuildCombatLimitText());
            SetText(zombieSummaryText, BuildZombieSummaryText());

            if (cardController == null)
            {
                SetText(energyText, $"{EnergySystem.CurrentEnergy}/{EnergySystem.MaxEnergy}");
                SetText(pendingDamageText, $"{CombatSystem.PendingDamage}/{CombatSystem.MaxCombatValue}");
                SetText(zombieThreatText, $"{CombatSystem.ZombieThreatCount}");
                SetText(deckDebugText, string.Empty);
                return;
            }

            SetText(energyText, $"{EnergySystem.CurrentEnergy}/{EnergySystem.MaxEnergy}");
            SetText(pendingDamageText, $"{CombatSystem.PendingDamage}/{CombatSystem.MaxCombatValue}");
            SetText(zombieThreatText, $"{CombatSystem.ZombieThreatCount}");
            SetText(deckDebugText, BuildDeckDebugText());
        }

        private string BuildDeckDebugText()
        {
            if (cardController == null)
            {
                return string.Empty;
            }

            return $"正規抽牌:{cardController.RegularDrawPileCount}  附加:{cardController.AdditiveDrawPileCount}\n"
                + $"手牌:{cardController.HandCards.Count}  棄牌:{cardController.DiscardPile.Count}  已打出:{cardController.PlayedCards.Count}\n"
                + $"耗盡:{cardController.ExhaustedCards.Count}  補位需求:{cardController.ExhaustionRecords.Count}";
        }

        private static string BuildFoodPlanText()
        {
            string pending = FoodPlanSystem.HasPendingPlan ? $" -> {FoodPlanSystem.PendingPlan}" : string.Empty;
            return $"{FoodPlanSystem.CurrentPlan}{pending}";
        }

        private static string BuildFoodStorageText()
        {
            int consumption = FoodPlanSystem.GetRoundFoodConsumption();
            int afterConsumption = FoodSystem.PreviewFoodAfterConsumption(consumption);
            return $"{FoodSystem.GetFood()} - {consumption} / {FoodSystem.StorageCapacity} ({afterConsumption})";
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
    }
}
