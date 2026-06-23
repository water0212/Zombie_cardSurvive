using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Cards.UI;

namespace ZombieCardSurvive.Cards.UI
{
    public enum CardSortMode
    {
        SlotThenTypeThenName,
        TypeThenName,
        EnergyThenTypeThenName,
        Name
    }

    public static class CardDisplaySortUtility
    {
        public static void SortRuntimeCards(List<CardRuntime> cards, CardSortMode sortMode)
        {
            if (cards == null)
            {
                return;
            }

            cards.Sort((a, b) => CompareRuntimeCards(a, b, sortMode));
        }

        public static void SortCardData(List<CardBase> cards, CardSortMode sortMode)
        {
            if (cards == null)
            {
                return;
            }

            cards.Sort((a, b) => CompareCardData(a, b, sortMode));
        }

        private static int CompareRuntimeCards(CardRuntime left, CardRuntime right, CardSortMode sortMode)
        {
            int result = CompareNullable(left, right);
            if (result != 0 || left == null || right == null)
            {
                return result;
            }

            result = CompareByMode(left, right, sortMode);
            if (result != 0)
            {
                return result;
            }

            return string.Compare(left.RuntimeId, right.RuntimeId, System.StringComparison.Ordinal);
        }

        private static int CompareCardData(CardBase left, CardBase right, CardSortMode sortMode)
        {
            int result = CompareNullable(left, right);
            if (result != 0 || left == null || right == null)
            {
                return result;
            }

            result = CompareByMode(left, right, sortMode);
            if (result != 0)
            {
                return result;
            }

            return string.Compare(left.CardId, right.CardId, System.StringComparison.Ordinal);
        }

        private static int CompareByMode(CardRuntime left, CardRuntime right, CardSortMode sortMode)
        {
            switch (sortMode)
            {
                case CardSortMode.SlotThenTypeThenName:
                    return CompareChain(
                        CompareSlot(left.AssignedSlotType, right.AssignedSlotType),
                        CompareCardDataCore(left.Data, right.Data, includeEnergy: true));
                case CardSortMode.TypeThenName:
                    return CompareCardDataCore(left.Data, right.Data, includeEnergy: true);
                case CardSortMode.EnergyThenTypeThenName:
                    return CompareChain(
                        CompareEnergy(left.Data, right.Data),
                        CompareCardDataCore(left.Data, right.Data, includeEnergy: false));
                case CardSortMode.Name:
                    return CompareNameThenId(left.Data, right.Data);
                default:
                    return CompareCardDataCore(left.Data, right.Data, includeEnergy: true);
            }
        }

        private static int CompareByMode(CardBase left, CardBase right, CardSortMode sortMode)
        {
            switch (sortMode)
            {
                case CardSortMode.SlotThenTypeThenName:
                case CardSortMode.TypeThenName:
                    return CompareCardDataCore(left, right, includeEnergy: true);
                case CardSortMode.EnergyThenTypeThenName:
                    return CompareChain(
                        CompareEnergy(left, right),
                        CompareCardDataCore(left, right, includeEnergy: false));
                case CardSortMode.Name:
                    return CompareNameThenId(left, right);
                default:
                    return CompareCardDataCore(left, right, includeEnergy: true);
            }
        }

        private static int CompareCardDataCore(CardBase left, CardBase right, bool includeEnergy)
        {
            int result = CompareNullable(left, right);
            if (result != 0 || left == null || right == null)
            {
                return result;
            }

            result = CompareType(left.EffectiveCardType, right.EffectiveCardType);
            if (result != 0)
            {
                return result;
            }

            if (includeEnergy)
            {
                result = CompareEnergy(left, right);
                if (result != 0)
                {
                    return result;
                }
            }

            return CompareNameThenId(left, right);
        }

        private static int CompareNameThenId(CardBase left, CardBase right)
        {
            int result = string.Compare(GetDisplayName(left), GetDisplayName(right), System.StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            return string.Compare(GetCardId(left), GetCardId(right), System.StringComparison.Ordinal);
        }

        private static int CompareSlot(DeckSlotType left, DeckSlotType right)
        {
            return GetSlotSortOrder(left).CompareTo(GetSlotSortOrder(right));
        }

        private static int CompareType(CardType left, CardType right)
        {
            return GetCardTypeSortOrder(left).CompareTo(GetCardTypeSortOrder(right));
        }

        private static int CompareEnergy(CardBase left, CardBase right)
        {
            int leftEnergy = left != null ? left.EnergyCost : int.MaxValue;
            int rightEnergy = right != null ? right.EnergyCost : int.MaxValue;
            return leftEnergy.CompareTo(rightEnergy);
        }

        private static int CompareNullable<T>(T left, T right) where T : class
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            return right == null ? -1 : 0;
        }

        private static int CompareChain(params int[] comparisons)
        {
            foreach (int comparison in comparisons)
            {
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        private static int GetSlotSortOrder(DeckSlotType slotType)
        {
            switch (slotType)
            {
                case DeckSlotType.Food:
                    return 0;
                case DeckSlotType.Resource:
                    return 1;
                case DeckSlotType.Explore:
                    return 2;
                case DeckSlotType.Unrestricted:
                    return 3;
                default:
                    return 99;
            }
        }

        private static int GetCardTypeSortOrder(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Food:
                    return 0;
                case CardType.Resource:
                    return 1;
                case CardType.Combat:
                    return 2;
                case CardType.Explore:
                    return 3;
                case CardType.Build:
                    return 4;
                case CardType.Special:
                    return 5;
                case CardType.Zombie:
                    return 6;
                case CardType.Wound:
                    return 7;
                default:
                    return 99;
            }
        }

        private static string GetDisplayName(CardBase card)
        {
            if (card == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(card.DisplayName))
            {
                return card.DisplayName;
            }

            return card.name;
        }

        private static string GetCardId(CardBase card)
        {
            return card != null ? card.CardId : string.Empty;
        }
    }
}

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
        [SerializeField] private TMP_Dropdown sortDropdown;
        [SerializeField] private Transform contentRoot;

        [Header("Sorting")]
        [SerializeField] private CardSortMode currentSortMode = CardSortMode.SlotThenTypeThenName;

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
            HookSortDropdown();
            if (cardController != null)
            {
                cardController.StateChanged += HandleCardStateChanged;
            }

            SyncSortDropdownValue();
            RefreshButtonLabels();
        }

        private void OnDisable()
        {
            if (cardController != null)
            {
                cardController.StateChanged -= HandleCardStateChanged;
            }

            UnhookButtons();
            UnhookSortDropdown();
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

        private void HookSortDropdown()
        {
            if (sortDropdown == null)
            {
                return;
            }

            sortDropdown.onValueChanged.RemoveListener(HandleSortDropdownChanged);
            sortDropdown.onValueChanged.AddListener(HandleSortDropdownChanged);
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

        private void UnhookSortDropdown()
        {
            if (sortDropdown == null)
            {
                return;
            }

            sortDropdown.onValueChanged.RemoveListener(HandleSortDropdownChanged);
        }

        private void HandleSortDropdownChanged(int value)
        {
            currentSortMode = ResolveSortMode(value);

            if (popupRoot != null && popupRoot.activeSelf && hasActiveSource)
            {
                Refresh(activeSource);
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

            CardDisplaySortUtility.SortRuntimeCards(cards, currentSortMode);
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

        private void SyncSortDropdownValue()
        {
            if (sortDropdown == null)
            {
                return;
            }

            int value = (int)currentSortMode;
            if (sortDropdown.value != value)
            {
                sortDropdown.SetValueWithoutNotify(value);
            }
        }

        private static CardSortMode ResolveSortMode(int value)
        {
            if (System.Enum.IsDefined(typeof(CardSortMode), value))
            {
                return (CardSortMode)value;
            }

            return CardSortMode.SlotThenTypeThenName;
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
