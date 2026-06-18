using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.Runtime;

namespace ZombieCardSurvive.Cards.UI
{
    public class CardHandView : MonoBehaviour, ICardPlayReceiver
    {
        [SerializeField] private CardController cardController;
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private Transform handContainer;
        [SerializeField] private RectTransform playZone;
        [SerializeField] private float cardSpacing = 80f;
        [SerializeField] private bool useCenteredManualLayout = true;

        [Header("Draw Animation")]
        [SerializeField] private RectTransform drawAnimationStartPoint;
        [SerializeField] private float drawAnimationDuration = 0.25f;
        [SerializeField] private float drawAnimationStagger = 0.06f;
        [SerializeField] private Vector2 fallbackDrawStartOffset = new Vector2(0f, 260f);

        private readonly List<CardView> spawnedCards = new List<CardView>();
        private readonly Dictionary<CardRuntime, CardView> cardViews = new Dictionary<CardRuntime, CardView>();
        private readonly Dictionary<CardRuntime, Coroutine> activeAnimations = new Dictionary<CardRuntime, Coroutine>();
        private readonly HashSet<CardRuntime> pendingDrawAnimations = new HashSet<CardRuntime>();
        private HorizontalOrVerticalLayoutGroup layoutGroup;

        private void OnEnable()
        {
            if (cardController != null)
            {
                cardController.StateChanged += RefreshHand;
            }

            CacheLayoutGroup();
            ResolvePlayZone();
            RefreshHand();
        }

        private void OnDisable()
        {
            if (cardController != null)
            {
                cardController.StateChanged -= RefreshHand;
            }
        }

        public bool RequestPlayCard(CardView cardView)
        {
            if (cardController == null || cardView == null)
            {
                return false;
            }

            bool played = cardController.RequestPlayCard(cardView.RuntimeCard);
            if (!played)
            {
                cardView.ReturnToHandPosition();
            }

            return played;
        }

        [ContextMenu("Refresh Hand")]
        public void RefreshHand()
        {
            ResolvePlayZone();

            if (cardController == null || cardPrefab == null || handContainer == null)
            {
                ClearSpawnedCards();
                return;
            }

            RemoveCardsNotInHand();
            spawnedCards.Clear();

            int index = 0;
            foreach (CardRuntime card in cardController.HandCards)
            {
                bool isNewCard = !cardViews.TryGetValue(card, out CardView view) || view == null;
                if (isNewCard)
                {
                    view = Instantiate(cardPrefab, handContainer);
                    view.Bind(card, this, playZone);
                    cardViews[card] = view;
                    pendingDrawAnimations.Add(card);
                }
                else
                {
                    view.transform.SetParent(handContainer, false);
                    view.Bind(card, this, playZone);
                }

                view.transform.SetSiblingIndex(index);
                spawnedCards.Add(view);
                index++;
            }

            ApplyCenteredLayout(true);
        }

        private void ApplyCenteredLayout(bool animateNewCards)
        {
            if (!useCenteredManualLayout)
            {
                return;
            }

            CacheLayoutGroup();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
            }

            int count = spawnedCards.Count;
            float startX = -((count - 1) * cardSpacing) * 0.5f;
            int newCardAnimationIndex = 0;

            for (int i = 0; i < count; i++)
            {
                CardView view = spawnedCards[i];
                if (view == null)
                {
                    continue;
                }

                Vector2 targetPosition = new Vector2(startX + i * cardSpacing, 0f);
                CardRuntime card = view.RuntimeCard;
                bool shouldAnimate = animateNewCards && card != null && pendingDrawAnimations.Contains(card) && !IsAnimating(card);

                if (shouldAnimate)
                {
                    pendingDrawAnimations.Remove(card);
                    Vector2 startPosition = GetDrawAnimationStartPosition(targetPosition);
                    view.SetDraggingEnabled(false);
                    view.SetHandPosition(startPosition);
                    Coroutine animation = StartCoroutine(AnimateCardDraw(card, view, startPosition, targetPosition, newCardAnimationIndex * drawAnimationStagger));
                    activeAnimations[card] = animation;
                    newCardAnimationIndex++;
                }
                else if (!IsAnimating(card))
                {
                    view.SetHandPosition(targetPosition);
                }
            }
        }

        private bool IsAnimating(CardRuntime card)
        {
            return card != null && activeAnimations.ContainsKey(card);
        }

        private void ClearSpawnedCards()
        {
            foreach (Coroutine animation in activeAnimations.Values)
            {
                if (animation != null)
                {
                    StopCoroutine(animation);
                }
            }

            activeAnimations.Clear();
            pendingDrawAnimations.Clear();

            for (int i = spawnedCards.Count - 1; i >= 0; i--)
            {
                if (spawnedCards[i] != null)
                {
                    Destroy(spawnedCards[i].gameObject);
                }
            }

            spawnedCards.Clear();
            cardViews.Clear();
        }

        private void RemoveCardsNotInHand()
        {
            List<CardRuntime> cardsToRemove = new List<CardRuntime>();

            foreach (KeyValuePair<CardRuntime, CardView> pair in cardViews)
            {
                bool stillInHand = false;
                foreach (CardRuntime handCard in cardController.HandCards)
                {
                    if (ReferenceEquals(pair.Key, handCard))
                    {
                        stillInHand = true;
                        break;
                    }
                }

                if (!stillInHand)
                {
                    cardsToRemove.Add(pair.Key);
                }
            }

            foreach (CardRuntime card in cardsToRemove)
            {
                if (activeAnimations.TryGetValue(card, out Coroutine animation) && animation != null)
                {
                    StopCoroutine(animation);
                    activeAnimations.Remove(card);
                }

                pendingDrawAnimations.Remove(card);

                if (cardViews.TryGetValue(card, out CardView view) && view != null)
                {
                    Destroy(view.gameObject);
                }

                cardViews.Remove(card);
            }
        }

        private IEnumerator AnimateCardDraw(CardRuntime card, CardView view, Vector2 startPosition, Vector2 targetPosition, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            float duration = Mathf.Max(0.01f, drawAnimationDuration);
            float elapsed = 0f;

            while (elapsed < duration && view != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                view.SetHandPosition(Vector2.LerpUnclamped(startPosition, targetPosition, eased));
                yield return null;
            }

            if (view != null)
            {
                view.SetHandPosition(targetPosition);
                view.SetDraggingEnabled(true);
            }

            activeAnimations.Remove(card);
        }

        private Vector2 GetDrawAnimationStartPosition(Vector2 targetPosition)
        {
            RectTransform handRect = handContainer as RectTransform;
            if (handRect == null || drawAnimationStartPoint == null)
            {
                return targetPosition + fallbackDrawStartOffset;
            }

            Canvas canvas = handContainer.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, drawAnimationStartPoint.position);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(handRect, screenPosition, camera, out Vector2 localPoint))
            {
                return localPoint;
            }

            return targetPosition + fallbackDrawStartOffset;
        }

        private void CacheLayoutGroup()
        {
            if (layoutGroup == null && handContainer != null)
            {
                layoutGroup = handContainer.GetComponent<HorizontalOrVerticalLayoutGroup>();
            }
        }

        private void ResolvePlayZone()
        {
            if (playZone == null || (playZone.rect.width > 0f && playZone.rect.height > 0f))
            {
                return;
            }

            for (int i = 0; i < playZone.childCount; i++)
            {
                RectTransform child = playZone.GetChild(i) as RectTransform;
                if (child != null && child.rect.width > 0f && child.rect.height > 0f)
                {
                    playZone = child;
                    return;
                }
            }
        }
    }
}
