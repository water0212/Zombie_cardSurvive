using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Cards.UI;

namespace ZombieCardSurvive.Cards.UI.Piles
{
    public class CardPileViewSystem : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CardController cardController;
        [SerializeField] private CardView cardViewPrefab;

        [Header("Pile Buttons")]
        [SerializeField] private Button deckButton;
        [SerializeField] private Button drawButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private TMP_Text deckButtonText;
        [SerializeField] private TMP_Text drawButtonText;
        [SerializeField] private TMP_Text discardButtonText;

        [Header("Popup")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform contentRoot;

        [Header("Runtime Setup")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private bool autoWireByName = true;

        private readonly List<CardView> spawnedCards = new List<CardView>();
        private CardPileSource activeSource;
        private bool hasActiveSource;

        private void Awake()
        {
            if (autoWireByName)
            {
                AutoWireMissingReferences();
            }

            Close();
        }

        private void OnEnable()
        {
            HookButtons();
            if (cardController != null)
            {
                cardController.StateChanged += HandleCardStateChanged;
            }

            RefreshButtonLabels();
        }

        private void OnDisable()
        {
            if (cardController != null)
            {
                cardController.StateChanged -= HandleCardStateChanged;
            }

            UnhookButtons();
        }

        [ContextMenu("Auto Wire From Scene")]
        public void AutoWireMissingReferences()
        {
            if (cardController == null)
            {
                cardController = FindObjectOfType<CardController>();
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
            }

            Transform searchRoot = targetCanvas != null ? targetCanvas.transform : transform.root;

            if (deckButton == null)
            {
                deckButton = FindButton(searchRoot, "Deck");
            }

            if (drawButton == null)
            {
                drawButton = FindButton(searchRoot, "Draw");
            }

            if (discardButton == null)
            {
                discardButton = FindButton(searchRoot, "Discard");
            }

            if (deckButtonText == null && deckButton != null)
            {
                deckButtonText = deckButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (drawButtonText == null && drawButton != null)
            {
                drawButtonText = drawButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (discardButtonText == null && discardButton != null)
            {
                discardButtonText = discardButton.GetComponentInChildren<TMP_Text>(true);
            }
        }

        public void OpenDeck()
        {
            Toggle(CardPileSource.FullLibrary);
        }

        public void OpenDrawPile()
        {
            Toggle(CardPileSource.DrawPile);
        }

        public void OpenDiscardPile()
        {
            Toggle(CardPileSource.DiscardPile);
        }

        public void Toggle(CardPileSource source)
        {
            if (popupRoot != null && popupRoot.activeSelf && hasActiveSource && activeSource == source)
            {
                Close();
                return;
            }

            Open(source);
        }

        public void Open(CardPileSource source)
        {
            activeSource = source;
            hasActiveSource = true;

            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
            }

            SetText(titleText, GetLabel(source));
            Refresh(source);
            RefreshButtonLabels();
        }

        public void Close()
        {
            ClearCards();

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            hasActiveSource = false;
            RefreshButtonLabels();
        }

        public void Refresh(CardPileSource source)
        {
            ClearCards();

            if (cardController == null || cardViewPrefab == null || contentRoot == null)
            {
                return;
            }

            foreach (CardRuntime card in GetSortedCards(source))
            {
                CardView view = Instantiate(cardViewPrefab, contentRoot);
                view.gameObject.SetActive(true);
                view.SetDraggingEnabled(false);
                view.Bind(card, null, null);
                EnsureReadOnlyLayout(view);
                spawnedCards.Add(view);
            }
        }

        private void HookButtons()
        {
            if (deckButton != null)
            {
                deckButton.onClick.RemoveListener(OpenDeck);
                deckButton.onClick.AddListener(OpenDeck);
            }

            if (drawButton != null)
            {
                drawButton.onClick.RemoveListener(OpenDrawPile);
                drawButton.onClick.AddListener(OpenDrawPile);
            }

            if (discardButton != null)
            {
                discardButton.onClick.RemoveListener(OpenDiscardPile);
                discardButton.onClick.AddListener(OpenDiscardPile);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        private void HandleCardStateChanged()
        {
            RefreshButtonLabels();

            if (popupRoot != null && popupRoot.activeSelf && hasActiveSource)
            {
                SetText(titleText, GetLabel(activeSource));
                Refresh(activeSource);
            }
        }

        private void RefreshButtonLabels()
        {
            SetText(deckButtonText, GetLabel(CardPileSource.FullLibrary));
            SetText(drawButtonText, GetLabel(CardPileSource.DrawPile));
            SetText(discardButtonText, GetLabel(CardPileSource.DiscardPile));
        }

        private void UnhookButtons()
        {
            if (deckButton != null)
            {
                deckButton.onClick.RemoveListener(OpenDeck);
            }

            if (drawButton != null)
            {
                drawButton.onClick.RemoveListener(OpenDrawPile);
            }

            if (discardButton != null)
            {
                discardButton.onClick.RemoveListener(OpenDiscardPile);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        private List<CardRuntime> GetSortedCards(CardPileSource source)
        {
            List<CardRuntime> cards = new List<CardRuntime>();

            foreach (CardRuntime card in GetCards(source))
            {
                if (card != null && card.Data != null)
                {
                    cards.Add(card);
                }
            }

            cards.Sort((a, b) => string.Compare(GetDisplayName(a), GetDisplayName(b), System.StringComparison.Ordinal));
            return cards;
        }

        private IEnumerable<CardRuntime> GetCards(CardPileSource source)
        {
            if (cardController == null)
            {
                yield break;
            }

            if (source == CardPileSource.DrawPile)
            {
                foreach (CardRuntime card in cardController.DrawPile)
                {
                    yield return card;
                }

                yield break;
            }

            if (source == CardPileSource.DiscardPile)
            {
                foreach (CardRuntime card in cardController.DiscardPile)
                {
                    yield return card;
                }

                yield break;
            }

            foreach (CardRuntime card in cardController.DrawPile)
            {
                yield return card;
            }

            foreach (CardRuntime card in cardController.HandCards)
            {
                yield return card;
            }

            foreach (CardRuntime card in cardController.DiscardPile)
            {
                yield return card;
            }

            foreach (CardRuntime card in cardController.PlayedCards)
            {
                yield return card;
            }
        }

        private void ClearCards()
        {
            for (int i = spawnedCards.Count - 1; i >= 0; i--)
            {
                if (spawnedCards[i] != null)
                {
                    Destroy(spawnedCards[i].gameObject);
                }
            }

            spawnedCards.Clear();
        }

        private static void EnsureReadOnlyLayout(CardView cardView)
        {
            LayoutElement layoutElement = cardView.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredWidth = 160f;
                layoutElement.preferredHeight = 220f;
                layoutElement.flexibleWidth = 0f;
                layoutElement.flexibleHeight = 0f;
            }

            CanvasGroup canvasGroup = cardView.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private static Button FindButton(Transform root, string objectName)
        {
            Transform target = FindChildRecursive(root, objectName);
            if (target == null)
            {
                return null;
            }

            return target.GetComponent<Button>();
        }

        private static Transform FindChildRecursive(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /*
        private static string GetTitle(CardPileSource source)
        {
            switch (source)
            {
                case CardPileSource.DrawPile:
                    return "抽牌堆";
                case CardPileSource.DiscardPile:
                    return "棄牌堆";
                case CardPileSource.FullLibrary:
                    return "卡組";
                default:
                    return source.ToString();
            }
        }

        */
        private static string GetTitle(CardPileSource source)
        {
            switch (source)
            {
                case CardPileSource.DrawPile:
                    return "\u62bd\u724c\u5806";
                case CardPileSource.DiscardPile:
                    return "\u68c4\u724c\u5806";
                case CardPileSource.FullLibrary:
                    return "\u5361\u7d44";
                default:
                    return source.ToString();
            }
        }

        private string GetLabel(CardPileSource source)
        {
            return $"{GetTitle(source)} {CountCards(source)}";
        }

        private int CountCards(CardPileSource source)
        {
            int count = 0;
            foreach (CardRuntime card in GetCards(source))
            {
                if (card != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetDisplayName(CardRuntime card)
        {
            if (card == null || card.Data == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(card.Data.DisplayName))
            {
                return card.Data.DisplayName;
            }

            return card.Data.name;
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
