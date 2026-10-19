using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Exploration.Runtime;

namespace ZombieCardSurvive.UI
{
    /// <summary>
    /// Temporarily hides the active event/exploration presentation without ending its flow.
    /// While hidden, a transparent blocker prevents interaction with gameplay UI; only
    /// controls placed above that blocker (return and the future card warehouse) remain usable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FlowInspectionController : MonoBehaviour
    {
        [Header("Flow presentation")]
        [SerializeField] private CanvasGroup flowLayerGroup;

        [Header("Inspectable flow state")]
        [SerializeField] private EventFlowController eventFlowController;
        [SerializeField] private ExplorationSystem explorationSystem;

        [Header("Inspection mode")]
        [SerializeField] private GameObject inputBlocker;
        [SerializeField] private GameObject inspectButtonRoot;
        [SerializeField] private GameObject returnButtonRoot;
        [SerializeField] private Button inspectButton;
        [SerializeField] private Button returnButton;

        private static FlowInspectionController instance;
        private bool isInspecting;
        private bool buttonsHooked;

        public static bool IsInspectingGameplay => instance != null && instance.isInspecting;

        public bool IsInspecting => isInspecting;

        private void Awake()
        {
            instance = this;
            HookButtons();
            ApplyInspectionState(false);
            RefreshControls();
        }

        private void OnEnable()
        {
            instance = this;
            HookButtons();
            RefreshControls();
        }

        private void OnDisable()
        {
            UnhookButtons();
            ApplyInspectionState(false);

            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            bool hasInspectableFlow = HasInspectableFlow();
            if (isInspecting && !hasInspectableFlow)
            {
                ApplyInspectionState(false);
            }

            RefreshControls(hasInspectableFlow);
        }

        public void InspectGameplay()
        {
            if (!HasInspectableFlow())
            {
                return;
            }

            ApplyInspectionState(true);
            RefreshControls(true);
        }

        public void ReturnToFlow()
        {
            ApplyInspectionState(false);
            RefreshControls();
        }

        private void ApplyInspectionState(bool inspecting)
        {
            isInspecting = inspecting;

            if (flowLayerGroup != null)
            {
                flowLayerGroup.alpha = inspecting ? 0f : 1f;
                flowLayerGroup.interactable = !inspecting;
                flowLayerGroup.blocksRaycasts = !inspecting;
            }

            if (inputBlocker != null)
            {
                inputBlocker.SetActive(inspecting);
            }
        }

        private bool HasInspectableFlow()
        {
            bool hasEvent = eventFlowController != null
                && eventFlowController.State != EventFlowState.Hidden;
            bool hasExploration = explorationSystem != null
                && explorationSystem.State != ExplorationFlowState.Hidden;

            return hasEvent || hasExploration;
        }

        private void RefreshControls()
        {
            RefreshControls(HasInspectableFlow());
        }

        private void RefreshControls(bool hasInspectableFlow)
        {
            if (inspectButtonRoot != null)
            {
                inspectButtonRoot.SetActive(hasInspectableFlow && !isInspecting);
            }

            if (returnButtonRoot != null)
            {
                returnButtonRoot.SetActive(hasInspectableFlow && isInspecting);
            }

            if (inputBlocker != null && inputBlocker.activeSelf != (hasInspectableFlow && isInspecting))
            {
                inputBlocker.SetActive(hasInspectableFlow && isInspecting);
            }
        }

        private void HookButtons()
        {
            if (buttonsHooked)
            {
                return;
            }

            if (inspectButton != null)
            {
                inspectButton.onClick.AddListener(InspectGameplay);
            }

            if (returnButton != null)
            {
                returnButton.onClick.AddListener(ReturnToFlow);
            }

            buttonsHooked = true;
        }

        private void UnhookButtons()
        {
            if (!buttonsHooked)
            {
                return;
            }

            if (inspectButton != null)
            {
                inspectButton.onClick.RemoveListener(InspectGameplay);
            }

            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(ReturnToFlow);
            }

            buttonsHooked = false;
        }
    }
}
