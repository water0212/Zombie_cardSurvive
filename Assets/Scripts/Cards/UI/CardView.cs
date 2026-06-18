using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.Runtime;

namespace ZombieCardSurvive.Cards.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image artworkImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private bool draggingEnabled = true;

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

        public void SetDraggingEnabled(bool isEnabled)
        {
            draggingEnabled = isEnabled;
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
                SetArtwork(null);
                SetBackground(null);
                return;
            }

            SetText(nameText, RuntimeCard.Data.DisplayName);
            SetText(energyText, RuntimeCard.Data.EnergyCost.ToString());
            SetText(descriptionText, RuntimeCard.Data.Description);
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
            }
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

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
