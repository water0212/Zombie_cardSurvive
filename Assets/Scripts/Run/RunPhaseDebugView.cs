using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Run
{
    public class RunPhaseDebugView : MonoBehaviour
    {
        [SerializeField] private RunPhaseController runPhaseController;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private Button confirmEventButton;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private EventFlowController eventFlowController;

        private void Reset()
        {
            runPhaseController = FindObjectOfType<RunPhaseController>();
            eventFlowController = FindObjectOfType<EventFlowController>();
        }

        private void OnEnable()
        {
            if (eventFlowController == null)
            {
                eventFlowController = FindObjectOfType<EventFlowController>();
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

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            if (runPhaseController == null)
            {
                SetText(phaseText, "Phase: -");
                SetText(roundText, "Round: -");
                SetText(turnText, "Turn: -");
                SetButtonVisible(confirmEventButton, false);
                SetButtonVisible(endTurnButton, false);
                return;
            }

            RunPhase phase = runPhaseController.CurrentPhase;
            SetText(phaseText, $"Phase: {phase}");
            SetText(roundText, $"Round: {runPhaseController.RoundIndex}");
            SetText(turnText, $"Turn: {runPhaseController.TurnIndex}");
            SetButtonVisible(confirmEventButton, phase == RunPhase.Event && eventFlowController == null);
            SetButtonVisible(endTurnButton, phase == RunPhase.Turn);
        }

        private void HandlePhaseChanged(RunPhase phase)
        {
            Refresh();
        }

        private void HookButtons()
        {
            if (confirmEventButton != null)
            {
                confirmEventButton.onClick.RemoveListener(ConfirmEventChoice);
                confirmEventButton.onClick.AddListener(ConfirmEventChoice);
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(EndTurn);
                endTurnButton.onClick.AddListener(EndTurn);
            }
        }

        private void UnhookButtons()
        {
            if (confirmEventButton != null)
            {
                confirmEventButton.onClick.RemoveListener(ConfirmEventChoice);
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(EndTurn);
            }
        }

        private void ConfirmEventChoice()
        {
            if (runPhaseController != null)
            {
                runPhaseController.ConfirmEventChoice();
            }
        }

        private void EndTurn()
        {
            if (runPhaseController != null)
            {
                runPhaseController.EndTurn();
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetButtonVisible(Button target, bool visible)
        {
            if (target != null)
            {
                target.gameObject.SetActive(visible);
            }
        }
    }
}
