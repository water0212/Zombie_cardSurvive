using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.Runtime;

namespace ZombieCardSurvive.Cards.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text remainingUsesText;
        [SerializeField] private Image artworkImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private bool draggingEnabled = true;

        [Header("Hand Motion")]
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private bool hoverMotionEnabled = true;
        [SerializeField] private float hoverScale = 1.12f;
        [SerializeField] private float hoverLift = 45f;
        [SerializeField] private float hoverDuration = 0.12f;

        private Coroutine visualMotion;
        private Vector2 visualRestPosition;
        private bool isPointerOver;
        private bool isDragging;
        private bool usingFallbackVisualRoot;

        private Canvas rootCanvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private ICardPlayReceiver playReceiver;
        private RectTransform playZone;
        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 originalAnchoredPosition;
        private LayoutElement layoutElement;

        public CardRuntime RuntimeCard { get; private set; }
        public event Action<CardView> Clicked;

        public void SetDraggingEnabled(bool isEnabled)
        {
            draggingEnabled = isEnabled;
        }

        public void SetHoverMotionEnabled(bool isEnabled)
        {
            hoverMotionEnabled = isEnabled;

            if (!hoverMotionEnabled)
            {
                isPointerOver = false;
                ResetVisualMotion();
            }
        }

        public RectTransform CreatePlayEffectVisual(Transform targetParent)
        {
            RectTransform source = visualRoot != null ? visualRoot : rectTransform;
            if (source == null)
            {
                return null;
            }

            Transform parent = targetParent != null ? targetParent : ResolveFallbackEffectParent();
            if (parent == null)
            {
                return null;
            }

            if (usingFallbackVisualRoot)
            {
                Debug.LogWarning($"{nameof(CardView)} on {name} has no Visual Root assigned. Play effect will clone the whole card root as a fallback.", this);
            }

            GameObject wrapperObject = new GameObject($"{name}_PlayEffectVisual", typeof(RectTransform), typeof(CanvasGroup));
            RectTransform wrapper = wrapperObject.GetComponent<RectTransform>();
            wrapper.SetParent(parent, false);
            MatchRectToSource(wrapper, source);

            RectTransform visualClone = Instantiate(source, wrapper, false);
            visualClone.name = $"{source.name}_VisualClone";
            StretchToParent(visualClone);
            PreparePlayEffectClone(wrapper);

            return wrapper;
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            layoutElement = GetComponent<LayoutElement>();
            rootCanvas = GetComponentInParent<Canvas>();

            if (visualRoot == null)
            {
                usingFallbackVisualRoot = true;
                visualRoot = rectTransform;
            }

            visualRestPosition = visualRoot.anchoredPosition;
        }

        public void Bind(CardRuntime runtimeCard, ICardPlayReceiver receiver, RectTransform targetPlayZone)
        {
            RuntimeCard = runtimeCard;
            playReceiver = receiver;
            playZone = ResolveUsablePlayZone(targetPlayZone);
            Refresh();
        }

        public void Refresh()
        {
            if (RuntimeCard == null || RuntimeCard.Data == null)
            {
                SetText(nameText, string.Empty);
                SetText(energyText, string.Empty);
                SetText(descriptionText, string.Empty);
                SetRemainingUses(false, string.Empty);
                SetArtwork(null);
                SetBackground(null);
                return;
            }

            SetText(nameText, RuntimeCard.Data.DisplayName);
            SetText(energyText, RuntimeCard.EffectiveEnergyCost.ToString());
            SetText(descriptionText, RuntimeCard.Data.Description);
            RefreshRemainingUses();
            SetArtwork(RuntimeCard.Data.Artwork);
            SetBackground(RuntimeCard.Data.Background);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!draggingEnabled)
            {
                return;
            }

            if (rectTransform == null)
            {
                return;
            }

            isDragging = true;
            AnimateVisual(false);

            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalAnchoredPosition = rectTransform.anchoredPosition;

            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = true;
            }

            if (rootCanvas != null)
            {
                transform.SetParent(rootCanvas.transform, true);
                transform.SetAsLastSibling();
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }

            MoveToPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!draggingEnabled)
            {
                return;
            }

            MoveToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!draggingEnabled)
            {
                return;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            bool played = IsPointerInsidePlayZone(eventData) && playReceiver != null && playReceiver.RequestPlayCard(this);
            if (!played)
            {
                ReturnToHandPosition();

                isDragging = false;

                if (isPointerOver)
                {
                    AnimateVisual(true);
                }

                return;
            }

            isDragging = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanPlayHoverMotion())
            {
                return;
            }

            isPointerOver = true;
            transform.SetAsLastSibling();
            AnimateVisual(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isDragging)
            {
                return;
            }

            isPointerOver = false;
            if (hoverMotionEnabled)
            {
                AnimateVisual(false);
            }
            else
            {
                ResetVisualMotion();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (draggingEnabled)
            {
                return;
            }

            Clicked?.Invoke(this);
        }

        public void ReturnToHandPosition()
        {
            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
            }

            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = false;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }
        }

        public void SetHandPosition(Vector2 anchoredPosition)
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private bool IsPointerInsidePlayZone(PointerEventData eventData)
        {
            if (playZone == null)
            {
                return false;
            }

            Camera eventCamera = eventData.pressEventCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(playZone, eventData.position, eventCamera);
        }

        private static RectTransform ResolveUsablePlayZone(RectTransform candidate)
        {
            if (candidate == null)
            {
                return null;
            }

            if (candidate.rect.width > 0f && candidate.rect.height > 0f)
            {
                return candidate;
            }

            for (int i = 0; i < candidate.childCount; i++)
            {
                RectTransform child = candidate.GetChild(i) as RectTransform;
                if (child != null && child.rect.width > 0f && child.rect.height > 0f)
                {
                    return child;
                }
            }

            return candidate;
        }

        private void MoveToPointer(PointerEventData eventData)
        {
            if (rectTransform == null || rootCanvas == null)
            {
                return;
            }

            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
            {
                rectTransform.anchoredPosition = localPoint;
            }
        }

        private Transform ResolveFallbackEffectParent()
        {
            if (rootCanvas != null)
            {
                return rootCanvas.transform;
            }

            return transform.parent;
        }

        private static void MatchRectToSource(RectTransform target, RectTransform source)
        {
            if (target == null || source == null)
            {
                return;
            }

            RectTransform parentRect = target.parent as RectTransform;
            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.localScale = Vector3.one;
            target.localRotation = Quaternion.identity;

            if (parentRect == null)
            {
                target.position = source.position;
                target.sizeDelta = source.rect.size;
                return;
            }

            Canvas canvas = parentRect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector3[] corners = new Vector3[4];
            source.GetWorldCorners(corners);

            Vector2 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 topRightScreen = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, bottomLeftScreen, camera, out Vector2 bottomLeft)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, topRightScreen, camera, out Vector2 topRight))
            {
                target.position = source.position;
                target.sizeDelta = source.rect.size;
                return;
            }

            target.anchoredPosition = (bottomLeft + topRight) * 0.5f;
            target.sizeDelta = new Vector2(Mathf.Abs(topRight.x - bottomLeft.x), Mathf.Abs(topRight.y - bottomLeft.y));
        }

        private static void StretchToParent(RectTransform target)
        {
            if (target == null)
            {
                return;
            }

            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.pivot = new Vector2(0.5f, 0.5f);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
            target.anchoredPosition = Vector2.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }

        private static void PreparePlayEffectClone(RectTransform cloneRoot)
        {
            if (cloneRoot == null)
            {
                return;
            }

            CanvasGroup[] canvasGroups = cloneRoot.GetComponentsInChildren<CanvasGroup>(true);
            foreach (CanvasGroup group in canvasGroups)
            {
                if (group == null)
                {
                    continue;
                }

                group.alpha = 1f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            Graphic[] graphics = cloneRoot.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic != null)
                {
                    graphic.raycastTarget = false;
                }
            }

            CardView[] cardViews = cloneRoot.GetComponentsInChildren<CardView>(true);
            foreach (CardView cardView in cardViews)
            {
                if (cardView != null)
                {
                    cardView.enabled = false;
                }
            }

            LayoutElement[] layoutElements = cloneRoot.GetComponentsInChildren<LayoutElement>(true);
            foreach (LayoutElement layout in layoutElements)
            {
                if (layout != null)
                {
                    layout.ignoreLayout = true;
                }
            }
        }

        private void AnimateVisual(bool hovered)
        {
            if (visualRoot == null)
            {
                return;
            }

            if (visualMotion != null)
            {
                StopCoroutine(visualMotion);
            }

            Vector2 targetPosition = hovered
                ? visualRestPosition + new Vector2(0f, hoverLift)
                : visualRestPosition;

            Vector3 targetScale = hovered
                ? Vector3.one * hoverScale
                : Vector3.one;

            visualMotion = StartCoroutine(AnimateVisualRoutine(targetPosition, targetScale));
        }

        private bool CanPlayHoverMotion()
        {
            return hoverMotionEnabled && draggingEnabled && !isDragging;
        }

        private void ResetVisualMotion()
        {
            if (visualRoot == null)
            {
                return;
            }

            if (visualMotion != null)
            {
                StopCoroutine(visualMotion);
                visualMotion = null;
            }

            visualRoot.anchoredPosition = visualRestPosition;
            visualRoot.localScale = Vector3.one;
        }

        private IEnumerator AnimateVisualRoutine(Vector2 targetPosition, Vector3 targetScale)
        {
            Vector2 startPosition = visualRoot.anchoredPosition;
            Vector3 startScale = visualRoot.localScale;
            float duration = Mathf.Max(0.01f, hoverDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                visualRoot.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
                visualRoot.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
                yield return null;
            }

            visualRoot.anchoredPosition = targetPosition;
            visualRoot.localScale = targetScale;
            visualMotion = null;
        }

        private void SetArtwork(Sprite sprite)
        {
            if (artworkImage == null)
            {
                return;
            }

            artworkImage.sprite = sprite;
            artworkImage.enabled = sprite != null;
        }

        private void SetBackground(Sprite sprite)
        {
            if (backgroundImage == null)
            {
                return;
            }

            backgroundImage.sprite = sprite;
            backgroundImage.enabled = sprite != null;
        }

        private void RefreshRemainingUses()
        {
            if (RuntimeCard == null || RuntimeCard.Data == null || !RuntimeCard.Data.HasLimitedUses)
            {
                SetRemainingUses(false, string.Empty);
                return;
            }

            SetRemainingUses(true, RuntimeCard.RemainingUses.ToString());
        }

        private void SetRemainingUses(bool isVisible, string value)
        {
            if (remainingUsesText == null)
            {
                return;
            }

            remainingUsesText.text = value;
            remainingUsesText.gameObject.SetActive(isVisible);
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
