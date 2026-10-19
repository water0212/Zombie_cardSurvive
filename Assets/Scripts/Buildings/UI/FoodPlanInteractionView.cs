using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Buildings.UI
{
    public class FoodPlanInteractionView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private Button tightPlanButton;
        [SerializeField] private Button normalPlanButton;
        [SerializeField] private Button generousPlanButton;

        private BuildingSystem system;
        private BuildingRuntime building;
        private FoodPlanInteractionData data;
        private RunPhaseController runPhaseController;

        public void Bind(BuildingSystem owner, BuildingRuntime building, FoodPlanInteractionData interaction)
        {
            system = owner;
            this.building = building;
            data = interaction;
            runPhaseController = FindObjectOfType<RunPhaseController>();
            HookButtons();
            Refresh();
        }

        private void OnDestroy()
        {
            UnhookButtons();
        }

        public void Refresh()
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            SetText(titleText, data.DisplayName);
            SetText(descriptionText, data.Description);
            SetText(stateText, BuildStateText());
            HideLegacyTightPlanButton();
            SetButtonLabel(normalPlanButton, "標準供應");
            SetButtonLabel(generousPlanButton, "鼓舞餐點");

            bool canChange = CanChangePlan();
            SetButtonInteractable(normalPlanButton, canChange);
            SetButtonInteractable(generousPlanButton, canChange);
        }

        private void SetNormalPlan()
        {
            TrySetPlan(FoodPlanLevel.Normal);
        }

        private void SetGenerousPlan()
        {
            TrySetPlan(FoodPlanLevel.Generous);
        }

        private void TrySetPlan(FoodPlanLevel plan)
        {
            if (!CanChangePlan()) return;
            if (runPhaseController != null)
            {
                runPhaseController.TrySetFoodPlan(plan);
            }
            else
            {
                FoodPlanSystem.TrySetPendingPlan(plan);
            }

            Refresh();
        }

        private bool CanChangePlan()
        {
            if (system == null || !system.CanSubmitActions || building == null
                || !building.IsInteractionUnlocked(data)
                || (building.State != BuildingState.Active && building.State != BuildingState.Upgrading)) return false;
            if (FoodPlanSystem.HasChangedThisTurn)
            {
                return false;
            }

            if (runPhaseController != null)
            {
                return runPhaseController.CurrentPhase == RunPhase.Turn;
            }

            return true;
        }

        private string BuildStateText()
        {
            string current = FoodPlanSystem.GetDisplayName(FoodPlanSystem.CurrentPlan);
            string pending = FoodPlanSystem.HasPendingPlan
                ? $" -> {FoodPlanSystem.GetDisplayName(FoodPlanSystem.PendingPlan)}"
                : string.Empty;
            string useState = CanChangePlan() ? "可調整" : "本回合不可調整";
            return $"目前: {current}{pending} / 固定需求: {FoodPlanSystem.GetPendingRoundFoodConsumption()} / {useState}";
        }

        private void HookButtons()
        {
            UnhookButtons();
            normalPlanButton?.onClick.AddListener(SetNormalPlan);
            generousPlanButton?.onClick.AddListener(SetGenerousPlan);
        }

        private void UnhookButtons()
        {
            normalPlanButton?.onClick.RemoveListener(SetNormalPlan);
            generousPlanButton?.onClick.RemoveListener(SetGenerousPlan);
        }

        private void HideLegacyTightPlanButton()
        {
            if (tightPlanButton != null)
            {
                tightPlanButton.gameObject.SetActive(false);
            }
        }

        private static void SetButtonLabel(Button target, string value)
        {
            if (target != null)
            {
                SetText(target.GetComponentInChildren<TMP_Text>(true), value);
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetButtonInteractable(Button target, bool interactable)
        {
            if (target != null)
            {
                target.interactable = interactable;
            }
        }
    }
}
