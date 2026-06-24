using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.UI
{
    public class FoodPlanDebugView : MonoBehaviour
    {
        [SerializeField] private RunPhaseController runPhaseController;
        [SerializeField] private TMP_Text currentPlanText;
        [SerializeField] private TMP_Text pendingPlanText;
        [SerializeField] private TMP_Text changeStateText;
        [SerializeField] private Button tightPlanButton;
        [SerializeField] private Button normalPlanButton;
        [SerializeField] private Button generousPlanButton;

        private void Reset()
        {
            runPhaseController = FindObjectOfType<RunPhaseController>();
        }

        private void OnEnable()
        {
            if (runPhaseController == null)
            {
                runPhaseController = FindObjectOfType<RunPhaseController>();
            }

            if (runPhaseController != null)
            {
                runPhaseController.PhaseChanged += HandlePhaseChanged;
            }

            HookButtons();
            Refresh();
        }

        private void OnDisable()
        {
            if (runPhaseController != null)
            {
                runPhaseController.PhaseChanged -= HandlePhaseChanged;
            }

            UnhookButtons();
        }

        private void Update()
        {
            Refresh();
        }

        public void SetTightPlan()
        {
            TrySetPlan(FoodPlanLevel.Tight);
        }

        public void SetNormalPlan()
        {
            TrySetPlan(FoodPlanLevel.Normal);
        }

        public void SetGenerousPlan()
        {
            TrySetPlan(FoodPlanLevel.Generous);
        }

        public void Refresh()
        {
            SetText(currentPlanText, FoodPlanSystem.CurrentPlan.ToString());
            SetText(pendingPlanText, FoodPlanSystem.HasPendingPlan ? FoodPlanSystem.PendingPlan.ToString() : "-");
            SetText(changeStateText, BuildChangeStateText());

            bool canChange = CanChangePlan();
            SetButtonInteractable(tightPlanButton, canChange);
            SetButtonInteractable(normalPlanButton, canChange);
            SetButtonInteractable(generousPlanButton, canChange);
        }

        private bool TrySetPlan(FoodPlanLevel plan)
        {
            bool changed = runPhaseController != null
                ? runPhaseController.TrySetFoodPlan(plan)
                : FoodPlanSystem.TrySetPendingPlan(plan);

            Refresh();
            return changed;
        }

        private bool CanChangePlan()
        {
            if (FoodPlanSystem.HasChangedThisTurn)
            {
                return false;
            }

            return runPhaseController == null || runPhaseController.CurrentPhase == RunPhase.Turn;
        }

        private string BuildChangeStateText()
        {
            if (runPhaseController != null && runPhaseController.CurrentPhase != RunPhase.Turn)
            {
                return "Locked";
            }

            return FoodPlanSystem.HasChangedThisTurn ? "Used" : "Ready";
        }

        private void HandlePhaseChanged(RunPhase phase)
        {
            Refresh();
        }

        private void HookButtons()
        {
            if (tightPlanButton != null)
            {
                tightPlanButton.onClick.RemoveListener(SetTightPlan);
                tightPlanButton.onClick.AddListener(SetTightPlan);
            }

            if (normalPlanButton != null)
            {
                normalPlanButton.onClick.RemoveListener(SetNormalPlan);
                normalPlanButton.onClick.AddListener(SetNormalPlan);
            }

            if (generousPlanButton != null)
            {
                generousPlanButton.onClick.RemoveListener(SetGenerousPlan);
                generousPlanButton.onClick.AddListener(SetGenerousPlan);
            }
        }

        private void UnhookButtons()
        {
            if (tightPlanButton != null)
            {
                tightPlanButton.onClick.RemoveListener(SetTightPlan);
            }

            if (normalPlanButton != null)
            {
                normalPlanButton.onClick.RemoveListener(SetNormalPlan);
            }

            if (generousPlanButton != null)
            {
                generousPlanButton.onClick.RemoveListener(SetGenerousPlan);
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
