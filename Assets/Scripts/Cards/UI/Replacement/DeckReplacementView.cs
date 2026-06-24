using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Cards.UI;

namespace ZombieCardSurvive.Cards.UI.Replacement
{
    [Serializable]
    public class DeckReplacementCandidateDisplayData
    {
        public DeckReplacementCandidateDisplayData(CardInventoryEntry entry, int quantity)
        {
            Entry = entry;
            Quantity = Mathf.Max(1, quantity);
        }

        public CardInventoryEntry Entry { get; }
        public CardBase Card => Entry != null ? Entry.Data : null;
        public int Quantity { get; }
        public bool IsReusable => Entry != null && Entry.IsReusable;
    }

    [Serializable]
    public class DeckReplacementSlotVisualConfig
    {
        [SerializeField] private DeckSlotType slotType = DeckSlotType.Unrestricted;
        [SerializeField] private string title;
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.55f);

        public DeckSlotType SlotType => slotType;
        public string Title => !string.IsNullOrWhiteSpace(title) ? title : GetDefaultTitle(slotType);
        public Color BackgroundColor => backgroundColor;

        public DeckReplacementSlotVisualConfig()
        {
        }

        public DeckReplacementSlotVisualConfig(DeckSlotType slotType, string title, Color backgroundColor)
        {
            this.slotType = slotType;
            this.title = title;
            this.backgroundColor = backgroundColor;
        }

        private static string GetDefaultTitle(DeckSlotType slotType)
        {
            switch (slotType)
            {
                case DeckSlotType.Food:
                    return "\u98df\u7269";
                case DeckSlotType.Resource:
                    return "\u8cc7\u6e90";
                case DeckSlotType.Explore:
                    return "\u63a2\u7d22";
                case DeckSlotType.Unrestricted:
                    return "\u7121\u9650\u5236";
                default:
                    return slotType.ToString();
            }
        }
    }

    [Serializable]
    public class DeckReplacementSlotGroupBinding
    {
        [SerializeField] private GameObject groupRoot;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private bool autoPreferredHeight = true;
        [SerializeField] private float minPreferredHeight = 120f;
        [SerializeField] private float fallbackHeaderHeight = 28f;
        [SerializeField] private float fallbackCardHeight = 110f;
        [SerializeField] private int fallbackColumnCount = 4;

        private DeckSlotType activeSlotType;

        public DeckSlotType ActiveSlotType => activeSlotType;
        public Transform ContentRoot => contentRoot != null ? contentRoot : (groupRoot != null ? groupRoot.transform : null);
        public bool IsUsable => groupRoot != null && ContentRoot != null;

        public void Bind(DeckSlotDefinition slot, DeckReplacementSlotVisualConfig visualConfig, int visibleCardCount)
        {
            if (slot == null)
            {
                SetVisible(false);
                return;
            }

            activeSlotType = slot.SlotType;
            SetVisible(true);

            if (headerText != null)
            {
                string title = visualConfig != null ? visualConfig.Title : slot.SlotType.ToString();
                headerText.text = $"{title} {visibleCardCount}/{slot.Count}";
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = visualConfig != null
                    ? visualConfig.BackgroundColor
                    : new Color(0.2f, 0.2f, 0.2f, 0.55f);
            }

            ApplyPreferredHeight(visibleCardCount);
        }

        public void SetVisible(bool isVisible)
        {
            if (groupRoot != null)
            {
                groupRoot.SetActive(isVisible);
            }
        }

        private void ApplyPreferredHeight(int visibleCardCount)
        {
            if (!autoPreferredHeight)
            {
                return;
            }

            LayoutElement targetLayoutElement = ResolveLayoutElement();
            if (targetLayoutElement == null)
            {
                return;
            }

            targetLayoutElement.preferredHeight = Mathf.Max(minPreferredHeight, CalculatePreferredHeight(visibleCardCount));
            targetLayoutElement.flexibleHeight = 0f;
        }

        private float CalculatePreferredHeight(int visibleCardCount)
        {
            VerticalLayoutGroup verticalLayout = groupRoot != null ? groupRoot.GetComponent<VerticalLayoutGroup>() : null;
            GridLayoutGroup gridLayout = ContentRoot != null ? ContentRoot.GetComponent<GridLayoutGroup>() : null;

            float paddingTop = verticalLayout != null ? verticalLayout.padding.top : 0f;
            float paddingBottom = verticalLayout != null ? verticalLayout.padding.bottom : 0f;
            float groupSpacing = verticalLayout != null ? verticalLayout.spacing : 0f;
            float headerHeight = ResolveHeaderHeight();
            float contentHeight = CalculateGridContentHeight(gridLayout, Mathf.Max(0, visibleCardCount));

            if (visibleCardCount <= 0)
            {
                return paddingTop + paddingBottom + headerHeight;
            }

            return paddingTop + paddingBottom + headerHeight + groupSpacing + contentHeight;
        }

        private float CalculateGridContentHeight(GridLayoutGroup gridLayout, int visibleCardCount)
        {
            if (visibleCardCount <= 0)
            {
                return 0f;
            }

            float cellHeight = gridLayout != null ? gridLayout.cellSize.y : fallbackCardHeight;
            float spacingY = gridLayout != null ? gridLayout.spacing.y : 0f;
            int paddingTop = gridLayout != null ? gridLayout.padding.top : 0;
            int paddingBottom = gridLayout != null ? gridLayout.padding.bottom : 0;
            int columnCount = ResolveColumnCount(gridLayout);
            int rowCount = Mathf.CeilToInt(visibleCardCount / (float)Mathf.Max(1, columnCount));

            return paddingTop
                + paddingBottom
                + rowCount * cellHeight
                + Mathf.Max(0, rowCount - 1) * spacingY;
        }

        private int ResolveColumnCount(GridLayoutGroup gridLayout)
        {
            if (gridLayout == null)
            {
                return Mathf.Max(1, fallbackColumnCount);
            }

            if (gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                return Mathf.Max(1, gridLayout.constraintCount);
            }

            if (gridLayout.constraint == GridLayoutGroup.Constraint.FixedRowCount)
            {
                int rowCount = Mathf.Max(1, gridLayout.constraintCount);
                return Mathf.Max(1, Mathf.CeilToInt(fallbackColumnCount / (float)rowCount));
            }

            RectTransform contentRect = ContentRoot as RectTransform;
            if (contentRect == null || gridLayout.cellSize.x <= 0f)
            {
                return Mathf.Max(1, fallbackColumnCount);
            }

            float availableWidth = contentRect.rect.width - gridLayout.padding.left - gridLayout.padding.right + gridLayout.spacing.x;
            float cellWidthWithSpacing = gridLayout.cellSize.x + gridLayout.spacing.x;
            return Mathf.Max(1, Mathf.FloorToInt(availableWidth / cellWidthWithSpacing));
        }

        private float ResolveHeaderHeight()
        {
            if (headerText == null)
            {
                return fallbackHeaderHeight;
            }

            LayoutElement headerLayout = headerText.GetComponent<LayoutElement>();
            if (headerLayout != null && headerLayout.preferredHeight > 0f)
            {
                return headerLayout.preferredHeight;
            }

            RectTransform headerRect = headerText.transform as RectTransform;
            if (headerRect != null && headerRect.rect.height > 0f)
            {
                return headerRect.rect.height;
            }

            return fallbackHeaderHeight;
        }

        private LayoutElement ResolveLayoutElement()
        {
            if (layoutElement != null)
            {
                return layoutElement;
            }

            return groupRoot != null ? groupRoot.GetComponent<LayoutElement>() : null;
        }
    }

    public class DeckReplacementView : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CardController cardController;
        [SerializeField] private CardInventory inventory;
        [SerializeField] private DeckReplacementReason reason = DeckReplacementReason.Debug;
        [SerializeField] private int replacementCount = 1;
        [SerializeField] private bool useInventoryCards = true;
        [SerializeField] private List<CardBase> candidateCards = new List<CardBase>();
        [SerializeField] private List<DeckSlotType> allowedSlotTypes = new List<DeckSlotType>();

        [Header("UI")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Transform targetRoot;
        [SerializeField] private Transform candidateRoot;
        [SerializeField] private Transform defaultCandidateRoot;
        [SerializeField] private DeckReplacementTargetView targetPrefab;
        [SerializeField] private DeckReplacementCandidateView candidatePrefab;
        [SerializeField] private Button closeButton;

        [Header("Default Candidates")]
        [SerializeField] private bool includeDeckLayoutDefaultCandidates = true;

        [Header("Target Slot Groups")]
        [SerializeField] private bool useTargetSlotGroups = true;
        [SerializeField] private bool showEmptyTargetSlotGroups = true;
        [SerializeField] private List<DeckReplacementSlotGroupBinding> targetSlotGroups = new List<DeckReplacementSlotGroupBinding>();
        [SerializeField] private List<DeckReplacementSlotVisualConfig> slotVisualConfigs = new List<DeckReplacementSlotVisualConfig>
        {
            new DeckReplacementSlotVisualConfig(DeckSlotType.Food, "\u98df\u7269", new Color(0.08f, 0.32f, 0.16f, 0.55f)),
            new DeckReplacementSlotVisualConfig(DeckSlotType.Resource, "\u8cc7\u6e90", new Color(0.38f, 0.25f, 0.1f, 0.55f)),
            new DeckReplacementSlotVisualConfig(DeckSlotType.Explore, "\u63a2\u7d22", new Color(0.08f, 0.18f, 0.36f, 0.55f)),
            new DeckReplacementSlotVisualConfig(DeckSlotType.Unrestricted, "\u7121\u9650\u5236", new Color(0.18f, 0.18f, 0.18f, 0.6f))
        };

        [Header("Manual Layout")]
        [SerializeField] private bool useManualLayout;
        [SerializeField] private int targetCardsPerRow = 5;
        [SerializeField] private int candidateCardsPerRow = 5;
        [SerializeField] private Vector2 targetSpacing = new Vector2(110f, 155f);
        [SerializeField] private Vector2 candidateSpacing = new Vector2(110f, 155f);

        private readonly List<DeckReplacementTargetView> targetViews = new List<DeckReplacementTargetView>();
        private readonly List<DeckReplacementCandidateView> candidateViews = new List<DeckReplacementCandidateView>();
        private readonly Dictionary<DeckSlotType, DeckReplacementSlotGroupBinding> activeTargetSlotGroups = new Dictionary<DeckSlotType, DeckReplacementSlotGroupBinding>();
        private DeckReplacementSession session;
        private DeckReplacementCandidateView selectedCandidate;
        private bool isOpen;

        public event Action Completed;
        public event Action Cancelled;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Cancel);
            }

            Close();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Cancel);
            }

            UnhookSession();
        }

        [ContextMenu("Open Replacement View")]
        public void Open()
        {
            DeckReplacementRequest request = CreateInspectorRequest();
            if (!OpenWithRequest(request))
            {
                Close();
            }
        }

        public bool OpenExhaustionRefill(IEnumerable<CardRuntime> targets)
        {
            List<CardInventoryEntry> defaultCandidates = ResolveDefaultRefillCandidates(targets);
            List<CardInventoryEntry> allCandidates = ResolveExhaustionRefillCandidates(defaultCandidates);

            DeckReplacementRequest request = new DeckReplacementRequest(
                DeckReplacementReason.ExhaustionRefill,
                CountRuntimeCards(targets),
                targets,
                allCandidates,
                null,
                defaultCandidates);

            return OpenWithRequest(request);
        }

        public bool OpenWithRequest(DeckReplacementRequest request)
        {
            if (request == null)
            {
                return false;
            }

            StartSession(request);
            if (session == null || !SessionHasUsableOption())
            {
                ClearViews();
                Close();
                return false;
            }

            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
            }

            isOpen = true;
            return true;
        }

        public void Close()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            isOpen = false;
        }

        public void Cancel()
        {
            Close();
            Cancelled?.Invoke();
        }

        public void SelectCandidate(DeckReplacementCandidateView candidateView)
        {
            selectedCandidate = candidateView;

            foreach (DeckReplacementCandidateView view in candidateViews)
            {
                if (view != null)
                {
                    view.SetSelected(view == selectedCandidate);
                }
            }

            RefreshTargetAvailability();
        }

        public bool TryReplace(CardRuntime target, CardInventoryEntry candidate)
        {
            if (session == null || target == null || candidate == null)
            {
                return false;
            }

            bool replaced = session.TryApply(target, candidate, out _);
            if (!replaced)
            {
                RefreshTargetAvailability();
                return false;
            }

            selectedCandidate = null;
            RebuildViews();

            if (session.IsComplete)
            {
                CompleteAndClose();
            }

            return true;
        }

        private DeckReplacementRequest CreateInspectorRequest()
        {
            List<CardInventoryEntry> defaultCandidates = ResolveDeckLayoutDefaultCandidates();
            List<CardInventoryEntry> allCandidates = ResolveAllCandidateEntries(defaultCandidates);

            return new DeckReplacementRequest(
                reason,
                replacementCount,
                cardController != null ? cardController.GetReplaceableRegularCards() : null,
                allCandidates,
                allowedSlotTypes,
                defaultCandidates);
        }

        private void StartSession(DeckReplacementRequest request)
        {
            UnhookSession();

            session = new DeckReplacementSession(cardController, ResolveInventoryForReplacement(), ResolveDeckLayout(), request);
            session.Changed += HandleSessionChanged;
            selectedCandidate = null;
            RebuildViews();
        }

        private void HandleSessionChanged()
        {
            RefreshStatus();
        }

        private void RebuildViews()
        {
            ClearViews();

            if (session == null)
            {
                RefreshStatus();
                return;
            }

            BuildTargetViews();
            BuildCandidateViews();
            ApplyManualLayout();
            RefreshStatus();
            RefreshTargetAvailability();
        }

        private void BuildTargetViews()
        {
            if (targetPrefab == null || session == null)
            {
                return;
            }

            List<DeckReplacementOption> sortedOptions = GetSortedOptions();
            PrepareTargetSlotGroups(sortedOptions);

            foreach (DeckReplacementOption option in sortedOptions)
            {
                if (option == null || option.TargetCard == null)
                {
                    continue;
                }

                Transform parent = ResolveTargetParent(option.TargetCard.AssignedSlotType);
                if (parent == null)
                {
                    continue;
                }

                DeckReplacementTargetView view = Instantiate(targetPrefab, parent);
                view.Bind(option.TargetCard, this);
                targetViews.Add(view);
            }
        }

        private void BuildCandidateViews()
        {
            if (candidatePrefab == null || session == null)
            {
                return;
            }

            List<DeckReplacementCandidateDisplayData> candidateDisplays = GetSortedCandidateDisplayData();

            foreach (DeckReplacementCandidateDisplayData candidateDisplay in candidateDisplays)
            {
                Transform parent = ResolveCandidateParent(candidateDisplay);
                if (parent == null)
                {
                    continue;
                }

                DeckReplacementCandidateView view = Instantiate(candidatePrefab, parent);
                view.Bind(candidateDisplay.Entry, this, candidateDisplay.Quantity, candidateDisplay.IsReusable);
                candidateViews.Add(view);
            }
        }

        private Transform ResolveCandidateParent(DeckReplacementCandidateDisplayData candidateDisplay)
        {
            if (candidateDisplay != null && candidateDisplay.IsReusable && defaultCandidateRoot != null)
            {
                return defaultCandidateRoot;
            }

            return candidateRoot != null ? candidateRoot : defaultCandidateRoot;
        }

        private List<DeckReplacementOption> GetSortedOptions()
        {
            List<CardRuntime> sortedTargets = new List<CardRuntime>();
            foreach (DeckReplacementOption option in session.Options)
            {
                if (option != null && option.TargetCard != null)
                {
                    sortedTargets.Add(option.TargetCard);
                }
            }

            CardDisplaySortUtility.SortRuntimeCards(sortedTargets, CardSortMode.SlotThenTypeThenName);

            List<DeckReplacementOption> sortedOptions = new List<DeckReplacementOption>();
            foreach (CardRuntime target in sortedTargets)
            {
                DeckReplacementOption option = FindOptionForTarget(target);
                if (option != null)
                {
                    sortedOptions.Add(option);
                }
            }

            return sortedOptions;
        }

        private List<DeckReplacementCandidateDisplayData> GetSortedCandidateDisplayData()
        {
            List<CardInventoryEntry> uniqueCandidates = new List<CardInventoryEntry>();

            foreach (DeckReplacementOption option in session.Options)
            {
                foreach (CardInventoryEntry candidate in option.CandidateEntries)
                {
                    if (candidate == null || candidate.Data == null)
                    {
                        continue;
                    }

                    int index = IndexOfCandidateStack(uniqueCandidates, candidate);
                    if (index < 0)
                    {
                        uniqueCandidates.Add(candidate);
                    }
                }
            }

            uniqueCandidates.Sort(CompareCandidateEntries);

            List<DeckReplacementCandidateDisplayData> result = new List<DeckReplacementCandidateDisplayData>();
            foreach (CardInventoryEntry candidate in uniqueCandidates)
            {
                result.Add(new DeckReplacementCandidateDisplayData(candidate, CountCandidateQuantity(candidate)));
            }

            return result;
        }

        private DeckReplacementOption FindOptionForTarget(CardRuntime target)
        {
            if (session == null || target == null)
            {
                return null;
            }

            foreach (DeckReplacementOption option in session.Options)
            {
                if (option != null && ReferenceEquals(option.TargetCard, target))
                {
                    return option;
                }
            }

            return null;
        }

        private static bool ContainsCardReference(List<CardBase> cards, CardBase target)
        {
            return IndexOfCardReference(cards, target) >= 0;
        }

        private static int IndexOfCardReference(List<CardBase> cards, CardBase target)
        {
            if (cards == null || target == null)
            {
                return -1;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                if (ReferenceEquals(cards[i], target))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int IndexOfCandidateStack(List<CardInventoryEntry> entries, CardInventoryEntry target)
        {
            if (entries == null || target == null)
            {
                return -1;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (IsSameCandidateStack(entries[i], target))
                {
                    return i;
                }
            }

            return -1;
        }

        private int CountCandidateQuantity(CardInventoryEntry candidate)
        {
            if (candidate != null && candidate.IsReusable)
            {
                return 1;
            }

            int count = 0;
            if (session == null || session.Request == null || candidate == null)
            {
                return 1;
            }

            foreach (CardInventoryEntry requestCandidate in session.Request.CandidateEntries)
            {
                if (IsSameCandidateStack(requestCandidate, candidate))
                {
                    count++;
                }
            }

            return Mathf.Max(1, count);
        }

        private void RefreshTargetAvailability()
        {
            CardInventoryEntry candidate = selectedCandidate != null ? selectedCandidate.Entry : null;

            foreach (DeckReplacementTargetView targetView in targetViews)
            {
                if (targetView == null)
                {
                    continue;
                }

                bool isAvailable = candidate != null && CanCandidateReplaceTarget(candidate, targetView.RuntimeCard);
                targetView.SetAvailability(isAvailable);
            }
        }

        private bool CanCandidateReplaceTarget(CardInventoryEntry candidate, CardRuntime target)
        {
            if (session == null || candidate == null || target == null)
            {
                return false;
            }

            foreach (DeckReplacementOption option in session.Options)
            {
                if (!ReferenceEquals(option.TargetCard, target))
                {
                    continue;
                }

                foreach (CardInventoryEntry optionCandidate in option.CandidateEntries)
                {
                    if (IsSameCandidateStack(optionCandidate, candidate))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void PrepareTargetSlotGroups(List<DeckReplacementOption> sortedOptions)
        {
            activeTargetSlotGroups.Clear();
            HideAllTargetSlotGroups();

            if (!CanUseTargetSlotGroups())
            {
                return;
            }

            DeckLayoutDefinition layout = ResolveDeckLayout();
            if (layout == null || layout.Slots == null)
            {
                return;
            }

            Dictionary<DeckSlotType, int> visibleCounts = CountOptionsBySlot(sortedOptions);
            int groupIndex = 0;

            foreach (DeckSlotDefinition slot in layout.Slots)
            {
                if (slot == null)
                {
                    continue;
                }

                int visibleCount = visibleCounts.TryGetValue(slot.SlotType, out int count) ? count : 0;
                if (!showEmptyTargetSlotGroups && visibleCount <= 0)
                {
                    continue;
                }

                DeckReplacementSlotGroupBinding group = GetNextUsableSlotGroup(ref groupIndex);
                if (group == null)
                {
                    break;
                }

                group.Bind(slot, FindSlotVisualConfig(slot.SlotType), visibleCount);
                activeTargetSlotGroups[slot.SlotType] = group;
            }
        }

        private Transform ResolveTargetParent(DeckSlotType slotType)
        {
            if (activeTargetSlotGroups.TryGetValue(slotType, out DeckReplacementSlotGroupBinding group)
                && group != null
                && group.ContentRoot != null)
            {
                return group.ContentRoot;
            }

            return targetRoot;
        }

        private bool CanUseTargetSlotGroups()
        {
            return useTargetSlotGroups && targetSlotGroups != null && targetSlotGroups.Count > 0;
        }

        private DeckReplacementSlotGroupBinding GetNextUsableSlotGroup(ref int startIndex)
        {
            if (targetSlotGroups == null)
            {
                return null;
            }

            while (startIndex < targetSlotGroups.Count)
            {
                DeckReplacementSlotGroupBinding group = targetSlotGroups[startIndex];
                startIndex++;

                if (group != null && group.IsUsable)
                {
                    return group;
                }
            }

            return null;
        }

        private Dictionary<DeckSlotType, int> CountOptionsBySlot(List<DeckReplacementOption> options)
        {
            Dictionary<DeckSlotType, int> counts = new Dictionary<DeckSlotType, int>();
            if (options == null)
            {
                return counts;
            }

            foreach (DeckReplacementOption option in options)
            {
                if (option == null || option.TargetCard == null)
                {
                    continue;
                }

                DeckSlotType slotType = option.TargetCard.AssignedSlotType;
                counts.TryGetValue(slotType, out int count);
                counts[slotType] = count + 1;
            }

            return counts;
        }

        private DeckReplacementSlotVisualConfig FindSlotVisualConfig(DeckSlotType slotType)
        {
            if (slotVisualConfigs == null)
            {
                return null;
            }

            foreach (DeckReplacementSlotVisualConfig config in slotVisualConfigs)
            {
                if (config != null && config.SlotType == slotType)
                {
                    return config;
                }
            }

            return null;
        }

        private void HideAllTargetSlotGroups()
        {
            if (targetSlotGroups == null)
            {
                return;
            }

            foreach (DeckReplacementSlotGroupBinding group in targetSlotGroups)
            {
                if (group != null)
                {
                    group.SetVisible(false);
                }
            }
        }

        private IEnumerable<CardInventoryEntry> ResolveCandidateEntries()
        {
            if (useInventoryCards && inventory != null)
            {
                return inventory.Entries;
            }

            return CreateEntriesFromCardData(candidateCards);
        }

        private List<CardInventoryEntry> ResolveAllCandidateEntries(IEnumerable<CardInventoryEntry> defaultCandidates)
        {
            List<CardInventoryEntry> candidates = new List<CardInventoryEntry>();
            AddUniqueCandidateEntries(candidates, ResolveCandidateEntries());
            AddUniqueCandidateEntries(candidates, defaultCandidates);
            return candidates;
        }

        private List<CardInventoryEntry> ResolveDeckLayoutDefaultCandidates()
        {
            List<CardInventoryEntry> candidates = new List<CardInventoryEntry>();
            if (!includeDeckLayoutDefaultCandidates)
            {
                return candidates;
            }

            DeckLayoutDefinition layout = ResolveDeckLayout();
            if (layout == null || layout.Slots == null)
            {
                return candidates;
            }

            foreach (DeckSlotDefinition slot in layout.Slots)
            {
                if (slot == null || slot.DefaultCard == null || ContainsReusableCardDataEntry(candidates, slot.DefaultCard))
                {
                    continue;
                }

                candidates.Add(CreateReusableDefaultEntry(slot.DefaultCard));
            }

            return candidates;
        }

        private List<CardInventoryEntry> ResolveExhaustionRefillCandidates(IEnumerable<CardInventoryEntry> defaultCandidates)
        {
            List<CardInventoryEntry> candidates = new List<CardInventoryEntry>();
            AddUniqueCandidateEntries(candidates, ResolveCandidateEntries());
            AddUniqueCandidateEntries(candidates, defaultCandidates);
            return candidates;
        }

        private List<CardInventoryEntry> ResolveDefaultRefillCandidates(IEnumerable<CardRuntime> targets)
        {
            List<CardInventoryEntry> candidates = new List<CardInventoryEntry>();
            DeckLayoutDefinition layout = ResolveDeckLayout();
            if (layout == null || targets == null)
            {
                return candidates;
            }

            foreach (CardRuntime target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                CardBase defaultCard = layout.GetDefaultCardForSlot(target.AssignedSlotType);
                if (defaultCard != null && !ContainsCardDataEntry(candidates, defaultCard))
                {
                    candidates.Add(CreateReusableDefaultEntry(defaultCard));
                }
            }

            return candidates;
        }

        private static CardInventoryEntry CreateReusableDefaultEntry(CardBase card)
        {
            return new CardInventoryEntry(card, card != null && card.HasLimitedUses ? card.MaxUsesPerRun : -1, null, true);
        }

        private DeckLayoutDefinition ResolveDeckLayout()
        {
            return cardController != null ? cardController.DeckLayout : null;
        }

        private CardInventory ResolveInventoryForReplacement()
        {
            return useInventoryCards ? inventory : null;
        }

        private void RefreshStatus()
        {
            if (titleText != null)
            {
                titleText.text = "卡組替換";
            }

            if (statusText != null)
            {
                if (titleText != null)
                {
                    titleText.text = "卡組替換";
                }

                if (session == null)
                {
                    statusText.text = string.Empty;
                }
                else
                {
                    statusText.text = $"{session.CompletedReplacementCount}/{session.Request.ReplacementCount}";
                }
            }
        }

        private void ClearViews()
        {
            DestroyViews(targetViews);
            DestroyViews(candidateViews);
            HideAllTargetSlotGroups();
            activeTargetSlotGroups.Clear();
        }

        private void ApplyManualLayout()
        {
            if (!useManualLayout)
            {
                return;
            }

            if (activeTargetSlotGroups.Count == 0 && !HasGridLayout(targetRoot))
            {
                LayoutViews(targetViews, Mathf.Max(1, targetCardsPerRow), targetSpacing);
            }

            if (!HasGridLayout(candidateRoot))
            {
                LayoutViews(candidateViews, Mathf.Max(1, candidateCardsPerRow), candidateSpacing);
            }
        }

        private static void LayoutViews<T>(List<T> views, int cardsPerRow, Vector2 spacing) where T : MonoBehaviour
        {
            int count = views.Count;
            if (count == 0)
            {
                return;
            }

            int rowCount = Mathf.CeilToInt(count / (float)cardsPerRow);

            for (int i = 0; i < count; i++)
            {
                RectTransform rectTransform = views[i] != null ? views[i].transform as RectTransform : null;
                if (rectTransform == null)
                {
                    continue;
                }

                int row = i / cardsPerRow;
                int column = i % cardsPerRow;
                int itemsInRow = Mathf.Min(cardsPerRow, count - row * cardsPerRow);

                float x = (column - (itemsInRow - 1) * 0.5f) * spacing.x;
                float y = ((rowCount - 1) * 0.5f - row) * spacing.y;

                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(x, y);
            }
        }

        private static void DestroyViews<T>(List<T> views) where T : MonoBehaviour
        {
            for (int i = views.Count - 1; i >= 0; i--)
            {
                if (views[i] != null)
                {
                    Destroy(views[i].gameObject);
                }
            }

            views.Clear();
        }

        private static bool HasGridLayout(Transform root)
        {
            return root != null && root.GetComponent<GridLayoutGroup>() != null;
        }

        private void UnhookSession()
        {
            if (session != null)
            {
                session.Changed -= HandleSessionChanged;
            }
        }

        private bool SessionHasUsableOption()
        {
            if (session == null)
            {
                return false;
            }

            foreach (DeckReplacementOption option in session.Options)
            {
                if (option != null && option.HasCandidates)
                {
                    return true;
                }
            }

            return false;
        }

        private void CompleteAndClose()
        {
            Close();
            Completed?.Invoke();
        }

        private static int CountRuntimeCards(IEnumerable<CardRuntime> cards)
        {
            int count = 0;
            if (cards == null)
            {
                return count;
            }

            foreach (CardRuntime card in cards)
            {
                if (card != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static void AddUniqueCandidates(List<CardBase> target, IEnumerable<CardBase> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (CardBase card in source)
            {
                if (card != null && !target.Contains(card))
                {
                    target.Add(card);
                }
            }
        }

        private static List<CardInventoryEntry> CreateEntriesFromCardData(IEnumerable<CardBase> source)
        {
            List<CardInventoryEntry> entries = new List<CardInventoryEntry>();
            if (source == null)
            {
                return entries;
            }

            foreach (CardBase card in source)
            {
                if (card != null)
                {
                    entries.Add(new CardInventoryEntry(card));
                }
            }

            return entries;
        }

        private static void AddUniqueCandidateEntries(List<CardInventoryEntry> target, IEnumerable<CardInventoryEntry> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (CardInventoryEntry entry in source)
            {
                if (entry != null && entry.Data != null && !ContainsEntryReference(target, entry))
                {
                    target.Add(entry);
                }
            }
        }

        private static bool ContainsEntryReference(List<CardInventoryEntry> entries, CardInventoryEntry target)
        {
            if (entries == null || target == null)
            {
                return false;
            }

            foreach (CardInventoryEntry entry in entries)
            {
                if (ReferenceEquals(entry, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCardDataEntry(List<CardInventoryEntry> entries, CardBase target)
        {
            if (entries == null || target == null)
            {
                return false;
            }

            foreach (CardInventoryEntry entry in entries)
            {
                if (entry != null && ReferenceEquals(entry.Data, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsReusableCardDataEntry(List<CardInventoryEntry> entries, CardBase target)
        {
            if (entries == null || target == null)
            {
                return false;
            }

            foreach (CardInventoryEntry entry in entries)
            {
                if (entry != null && entry.IsReusable && ReferenceEquals(entry.Data, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameCandidateStack(CardInventoryEntry left, CardInventoryEntry right)
        {
            return left != null
                && right != null
                && ReferenceEquals(left.Data, right.Data)
                && left.RemainingUses == right.RemainingUses
                && left.IsReusable == right.IsReusable;
        }

        private static int CompareCandidateEntries(CardInventoryEntry left, CardInventoryEntry right)
        {
            CardBase leftCard = left != null ? left.Data : null;
            CardBase rightCard = right != null ? right.Data : null;

            List<CardBase> cards = new List<CardBase> { leftCard, rightCard };
            CardDisplaySortUtility.SortCardData(cards, CardSortMode.TypeThenName);

            if (!ReferenceEquals(leftCard, rightCard))
            {
                return ReferenceEquals(cards[0], leftCard) ? -1 : 1;
            }

            int reusableComparison = GetReusableSortValue(left).CompareTo(GetReusableSortValue(right));
            if (reusableComparison != 0)
            {
                return reusableComparison;
            }

            int remainingComparison = GetRemainingUsesSortValue(right).CompareTo(GetRemainingUsesSortValue(left));
            if (remainingComparison != 0)
            {
                return remainingComparison;
            }

            return string.Compare(
                left != null ? left.InstanceId : string.Empty,
                right != null ? right.InstanceId : string.Empty,
                StringComparison.Ordinal);
        }

        private static int GetRemainingUsesSortValue(CardInventoryEntry entry)
        {
            if (entry == null || entry.Data == null || !entry.Data.HasLimitedUses)
            {
                return int.MaxValue;
            }

            return entry.RemainingUses;
        }

        private static int GetReusableSortValue(CardInventoryEntry entry)
        {
            return entry != null && entry.IsReusable ? 1 : 0;
        }
    }
}
