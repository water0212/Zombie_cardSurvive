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
        private FoodPlanInteractionData data;
        private RunPhaseController runPhaseController;

        public void Bind(BuildingSystem owner, BuildingRuntime building, FoodPlanInteractionData interaction)
        {
            system = owner;
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

            gameObject.SetActive(true);
            SetText(titleText, data.DisplayName);
            SetText(descriptionText, data.Description);
            SetText(stateText, BuildStateText());

            bool canChange = CanChangePlan();
            SetButtonInteractable(tightPlanButton, canChange);
            SetButtonInteractable(normalPlanButton, canChange);
            SetButtonInteractable(generousPlanButton, canChange);
        }

        private void SetTightPlan()
        {
            TrySetPlan(FoodPlanLevel.Tight);
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
            if (FoodPlanSystem.HasChangedThisTurn)
            {
                return false;
            }

            if (runPhaseController != null)
            {
                return runPhaseController.CurrentPhase == RunPhase.Turn;
            }

            return system == null || system.CanSubmitActions;
        }

        private string BuildStateText()
        {
            string pending = FoodPlanSystem.HasPendingPlan ? $" -> {FoodPlanSystem.PendingPlan}" : string.Empty;
            string useState = CanChangePlan() ? "可調整" : "本回合不可調整";
            return $"目前: {FoodPlanSystem.CurrentPlan}{pending} / 固定消耗: {FoodPlanSystem.GetPendingRoundFoodConsumption()} / {useState}";
        }

        private void HookButtons()
        {
            UnhookButtons();
            tightPlanButton?.onClick.AddListener(SetTightPlan);
            normalPlanButton?.onClick.AddListener(SetNormalPlan);
            generousPlanButton?.onClick.AddListener(SetGenerousPlan);
        }

        private void UnhookButtons()
        {
            tightPlanButton?.onClick.RemoveListener(SetTightPlan);
            normalPlanButton?.onClick.RemoveListener(SetNormalPlan);
            generousPlanButton?.onClick.RemoveListener(SetGenerousPlan);
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
