using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.UI.World;

namespace ZombieCardSurvive.UI.Morale
{
    [DisallowMultipleComponent]
    public sealed class MoraleHudPresenter : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerMoveHandler
    {
        [SerializeField] private RunPhaseController runPhaseController;
        [SerializeField] private GameplayHudRuntimePresenter basePresenter;
        [Header("Morale")]
        [SerializeField] private TMP_Text moraleText;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private Image[] pyramidLayers = new Image[3];
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private float hoverDelay = 0.45f;
        [Header("Distributed warnings")]
        [SerializeField] private TMP_Text moraleWarningText;
        [SerializeField] private TMP_Text foodWarningText;
        [SerializeField] private TMP_Text resourceWarningText;
        [SerializeField] private TMP_Text combatWarningText;
        [SerializeField] private CanvasGroup warningCanvasGroup;
        [SerializeField] private bool reducedMotion;
        [Header("Danger confirmation")]
        [SerializeField] private GameObject confirmationRoot;
        [SerializeField] private TMP_Text confirmationBody;
        [SerializeField] private Button returnButton;
        [SerializeField] private Button continueButton;

        private bool pointerInside;
        private bool settlementPresentationOwned;
        public void SetSettlementPresentationOwned(bool owned) { settlementPresentationOwned = owned; if (!owned) Refresh(); }
        public void ShowPresentedMorale(int value, MoraleCollapseStage stage)
        { SetText(moraleText, value.ToString()); SetText(stageText, "Stage " + (int)stage); ApplyPyramid(stage); }
        private bool detailPinned;
        private float pointerEnteredAt;
        private float pulseUntil;
        private Vector2 lastPointerScreenPosition;
        private string warningSignature = string.Empty;

        public bool CanHandleRoundEndConfirmation => runPhaseController != null
            && confirmationRoot != null
            && confirmationBody != null
            && returnButton != null
            && continueButton != null;

        private void Awake()
        {
            ResolveSources();
            SetActive(detailRoot, false);
            SetActive(confirmationRoot, false);
        }

        private void OnEnable()
        {
            ResolveSources();
            if (basePresenter != null) basePresenter.SetMoralePresentationOwnedExternally(true);
            Subscribe();
            HookButtons();
            if (runPhaseController != null)
            {
                runPhaseController.SetRoundEndConfirmationAvailable(CanHandleRoundEndConfirmation);
                if (!CanHandleRoundEndConfirmation)
                    Debug.LogError("Morale danger confirmation is incomplete; round end will use the existing unblocked flow.", this);
            }
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnhookButtons();
            pointerInside = false;
            detailPinned = false;
            SetActive(detailRoot, false);
            SetActive(confirmationRoot, false);
            GameplayInteractionLock.Release(this);
            if (runPhaseController != null) runPhaseController.SetRoundEndConfirmationAvailable(false);
            if (basePresenter != null) basePresenter.SetMoralePresentationOwnedExternally(false);
        }

        private void Update()
        {
            if (pointerInside && !detailPinned && Time.unscaledTime - pointerEnteredAt >= hoverDelay)
                ShowDetail(false);
            if (warningCanvasGroup != null)
                warningCanvasGroup.alpha = !reducedMotion && Time.unscaledTime < pulseUntil
                    ? 0.78f + Mathf.PingPong(Time.unscaledTime * 4f, 0.22f) : 1f;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            pointerEnteredAt = Time.unscaledTime;
            lastPointerScreenPosition = eventData.position;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            lastPointerScreenPosition = eventData.position;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            if (!detailPinned) SetActive(detailRoot, false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right) return;
            lastPointerScreenPosition = eventData.position;
            detailPinned = !detailPinned;
            if (detailPinned) ShowDetail(true);
            else CloseDetail();
            eventData.Use();
        }

        public void CloseDetail()
        {
            detailPinned = false;
            SetActive(detailRoot, false);
            GameplayInteractionLock.Release(this);
        }

        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled;
        }

        [ContextMenu("Refresh Morale HUD")]
        public void Refresh()
        {
            // 輪末多個系統會同步提交；等 Phase 離開 RoundEnd 後一次呈現完整結果。
            if (runPhaseController != null && runPhaseController.CurrentPhase == RunPhase.RoundEnd)
                return;
            if (runPhaseController == null || !MoralePyramidSystem.IsInitialized)
            {
                SetText(moraleText, "士氣 —");
                SetText(stageText, "階段尚未初始化");
                ClearWarnings();
                return;
            }

            RoundEndPreview preview = RoundEndPreviewService.Capture(runPhaseController.RoundIndex);
            if (!settlementPresentationOwned) SetText(moraleText, MoraleSystem.CurrentMorale.ToString());
            if (!settlementPresentationOwned) SetText(stageText, $"{MoraleWarningFormatter.PyramidMark(MoraleSystem.CollapseStage)}｜{MoraleWarningFormatter.StageName(MoraleSystem.CollapseStage)}");
            if (!settlementPresentationOwned) ApplyPyramid(MoraleSystem.CollapseStage);
            SetText(moraleWarningText, MoraleWarningFormatter.BuildMoraleWarning(preview));
            SetText(foodWarningText, MoraleWarningFormatter.BuildFoodWarning(preview));
            SetText(resourceWarningText, MoraleWarningFormatter.BuildResourceWarning(preview));
            SetText(combatWarningText, MoraleWarningFormatter.BuildCombatWarning(preview));
            if (detailRoot != null && detailRoot.activeSelf)
                SetText(detailText, MoraleWarningFormatter.BuildDetails(preview));

            string signature = $"{preview.WillCollapse}:{preview.WillCrossStage}:{preview.HasPermanentDamage}:{preview.FoodShortage}";
            bool danger = preview.RequiresConfirmation || preview.FoodShortage > 0;
            if (danger && signature != warningSignature && !reducedMotion) pulseUntil = Time.unscaledTime + 0.8f;
            warningSignature = danger ? signature : string.Empty;
        }

        private void HandleConfirmationRequested(RoundEndPreview preview)
        {
            if (!CanHandleRoundEndConfirmation)
            {
                runPhaseController?.SetRoundEndConfirmationAvailable(false);
                return;
            }
            SetText(confirmationBody, MoraleWarningFormatter.BuildConfirmation(preview));
            SetActive(confirmationRoot, true);
            if (returnButton != null) returnButton.Select();
        }

        private void ReturnToGameplay()
        {
            SetActive(confirmationRoot, false);
            runPhaseController?.CancelRoundEnd();
            Refresh();
        }

        private void ContinueRoundEnd()
        {
            SetActive(confirmationRoot, false);
            runPhaseController?.ConfirmRoundEnd();
        }

        private void ShowDetail(bool pinned)
        {
            if (settlementPresentationOwned && GameplayInteractionLock.IsLocked) return;
            RoundEndPreview preview = runPhaseController != null
                ? RoundEndPreviewService.Capture(runPhaseController.RoundIndex) : null;
            SetText(detailText, MoraleWarningFormatter.BuildDetails(preview));
            PositionDetailNearPointer();
            SetActive(detailRoot, true);
            if (pinned) GameplayInteractionLock.Acquire(this);
        }

        private void PositionDetailNearPointer()
        {
            RectTransform detailRect = detailRoot != null ? detailRoot.transform as RectTransform : null;
            RectTransform parentRect = detailRect != null ? detailRect.parent as RectTransform : null;
            if (detailRect == null || parentRect == null) return;

            Canvas canvas = detailRect.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, lastPointerScreenPosition, eventCamera, out Vector2 localPoint))
                return;

            Vector2 size = detailRect.rect.size;
            Vector2 pivot = detailRect.pivot;
            // Keep the whole panel beside the pointer. Centering the panel on the
            // pointer made it steal the hover raycast and immediately disappear.
            Vector2 target = localPoint + new Vector2(
                24f + size.x * pivot.x,
                -24f - size.y * (1f - pivot.y));
            float minX = parentRect.rect.xMin + size.x * pivot.x;
            float maxX = parentRect.rect.xMax - size.x * (1f - pivot.x);
            float minY = parentRect.rect.yMin + size.y * pivot.y;
            float maxY = parentRect.rect.yMax - size.y * (1f - pivot.y);
            detailRect.anchoredPosition = new Vector2(
                Mathf.Clamp(target.x, minX, maxX),
                Mathf.Clamp(target.y, minY, maxY));
        }

        private void ApplyPyramid(MoraleCollapseStage stage)
        {
            for (int i = 0; i < pyramidLayers.Length; i++)
            {
                if (pyramidLayers[i] == null) continue;
                bool lost = i < (int)stage;
                pyramidLayers[i].color = lost
                    ? new Color32(92, 88, 82, 255)
                    : i == 0 ? new Color32(132, 190, 138, 255)
                    : i == 1 ? new Color32(231, 174, 82, 255)
                    : new Color32(226, 96, 82, 255);
            }
        }

        private void ResolveSources()
        {
            if (runPhaseController == null) runPhaseController = FindObjectOfType<RunPhaseController>();
            if (basePresenter == null) basePresenter = FindObjectOfType<GameplayHudRuntimePresenter>(true);
        }

        private void Subscribe()
        {
            Unsubscribe();
            MoraleSystem.StateChanged += HandleMoraleChanged;
            MoralePyramidSystem.ActiveStageChanged += HandleStageChanged;
            FoodSystem.StateChanged += Refresh;
            ResourceSystem.StateChanged += Refresh;
            CombatSystem.StateChanged += Refresh;
            if (runPhaseController != null)
            {
                runPhaseController.PhaseChanged += HandlePhaseChanged;
                runPhaseController.RoundEndConfirmationRequested += HandleConfirmationRequested;
            }
        }

        private void Unsubscribe()
        {
            MoraleSystem.StateChanged -= HandleMoraleChanged;
            MoralePyramidSystem.ActiveStageChanged -= HandleStageChanged;
            FoodSystem.StateChanged -= Refresh;
            ResourceSystem.StateChanged -= Refresh;
            CombatSystem.StateChanged -= Refresh;
            if (runPhaseController != null)
            {
                runPhaseController.PhaseChanged -= HandlePhaseChanged;
                runPhaseController.RoundEndConfirmationRequested -= HandleConfirmationRequested;
            }
        }

        private void HookButtons()
        {
            if (returnButton != null) { returnButton.onClick.RemoveListener(ReturnToGameplay); returnButton.onClick.AddListener(ReturnToGameplay); }
            if (continueButton != null) { continueButton.onClick.RemoveListener(ContinueRoundEnd); continueButton.onClick.AddListener(ContinueRoundEnd); }
        }

        private void UnhookButtons()
        {
            if (returnButton != null) returnButton.onClick.RemoveListener(ReturnToGameplay);
            if (continueButton != null) continueButton.onClick.RemoveListener(ContinueRoundEnd);
        }

        private void HandleMoraleChanged(MoraleChangeResult value) => Refresh();
        private void HandleStageChanged(MoraleStageSnapshot value) => Refresh();
        private void HandlePhaseChanged(RunPhase value) => Refresh();
        private void ClearWarnings()
        {
            SetText(moraleWarningText, string.Empty); SetText(foodWarningText, string.Empty);
            SetText(resourceWarningText, string.Empty); SetText(combatWarningText, string.Empty);
        }
        private static void SetText(TMP_Text target, string value) { if (target != null) target.text = value; }
        private static void SetActive(GameObject target, bool value) { if (target != null) target.SetActive(value); }
    }
}
