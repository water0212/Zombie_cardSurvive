using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.UI.Replacement
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class DeckReplacementCandidateView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private CardPreviewView previewView;
        [SerializeField] private GameObject selectedRoot;
        [SerializeField] private TMP_Text quantityText;

        private DeckReplacementView owner;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 originalAnchoredPosition;
        private Vector3 dragPointerWorldOffset;

        public CardBase CardData { get; private set; }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            rootCanvas = GetComponentInParent<Canvas>();
        }

        public void Bind(CardBase card, DeckReplacementView replacementView, int quantity = 1)
        {
            CardData = card;
            owner = replacementView;

            if (previewView != null)
            {
                previewView.Bind(card);
            }

            SetQuantity(quantity);
            SetSelected(false);
        }

        private void SetQuantity(int quantity)
        {
            if (quantityText == null)
            {
                return;
            }

            bool shouldShow = quantity > 1;
            quantityText.gameObject.SetActive(shouldShow);
            quantityText.text = shouldShow ? $"x{quantity}" : string.Empty;
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedRoot != null)
            {
                selectedRoot.SetActive(isSelected);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.SelectCandidate(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.SelectCandidate(this);

            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalAnchoredPosition = rectTransform.anchoredPosition;
            dragPointerWorldOffset = Vector3.zero;

            if (TryGetPointerWorldPoint(eventData, out Vector3 pointerWorldPoint))
            {
                dragPointerWorldOffset = rectTransform.position - pointerWorldPoint;
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
            MoveToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            ReturnToOriginalPosition();
        }

        private void ReturnToOriginalPosition()
        {
            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
            }

            rectTransform.anchoredPosition = originalAnchoredPosition;
        }

        private void MoveToPointer(PointerEventData eventData)
        {
            if (rectTransform == null || rootCanvas == null)
            {
                return;
            }

            if (TryGetPointerWorldPoint(eventData, out Vector3 pointerWorldPoint))
            {
                rectTransform.position = pointerWorldPoint + dragPointerWorldOffset;
            }
        }

        private bool TryGetPointerWorldPoint(PointerEventData eventData, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;

            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return false;
            }

            Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;
            return RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, eventData.position, eventCamera, out worldPoint);
        }
    }
}
