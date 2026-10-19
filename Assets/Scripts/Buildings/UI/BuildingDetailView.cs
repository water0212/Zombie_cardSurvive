using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Buildings.UI
{
    public class BuildingDetailView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText, statusText, requirementText, rewardText;
        [SerializeField] private Button constructButton, cancelConstructionButton, closeButton;
        [SerializeField] private Transform interactionRoot;
        [SerializeField] private CardCraftView cardCraftPrefab;
        [SerializeField] private FoodPlanInteractionView foodPlanPrefab;
        [SerializeField] private CardUpgradeView cardUpgradePrefab;
        [SerializeField] private Transform bookmarkRoot;
        [SerializeField] private Button bookmarkButtonPrefab;
        [SerializeField] private TMP_Text pageStatusText;

        private sealed class Page
        {
            public string Id, Title;
            public BuildingInteractionData Interaction;
            public BuildingInformationPageView InformationPrefab;
            public GameObject Root;
            public Component View;
            public Button Bookmark;
        }
        private readonly List<Page> pages = new List<Page>();
        private readonly HashSet<string> reportedErrors = new HashSet<string>();
        private BuildingSystem system;
        private BuildingRuntime runtime;
        private RunPhaseController phaseController;
        private string selectedPageId;
        public string SelectedPageId => selectedPageId;

        private void Awake()
        {
            constructButton?.onClick.AddListener(StartConstruction);
            cancelConstructionButton?.onClick.AddListener(CancelConstruction);
            closeButton?.onClick.AddListener(Close);
            SetVisible(false);
        }
        private void OnDestroy()
        {
            constructButton?.onClick.RemoveListener(StartConstruction);
            cancelConstructionButton?.onClick.RemoveListener(CancelConstruction);
            closeButton?.onClick.RemoveListener(Close);
            UnhookSystem(); ClearPages();
        }
        public void Bind(BuildingSystem owner, BuildingRuntime buildingRuntime)
        {
            UnhookSystem(); ClearPages(); system = owner; runtime = buildingRuntime;
            if (system != null) system.BuildingsChanged += Refresh;
            FoodSystem.StateChanged += Refresh; ResourceSystem.StateChanged += Refresh;
            ResourceReservationService.StateChanged += Refresh;
            phaseController = FindObjectOfType<RunPhaseController>();
            if (phaseController != null) phaseController.PhaseChanged += OnPhaseChanged;
            SetVisible(runtime != null); Refresh();
        }
        public void RefreshIfBound(BuildingRuntime target) { if (ReferenceEquals(runtime, target)) Refresh(); }
        private void OnPhaseChanged(RunPhase phase) { Refresh(); }
        public void Refresh()
        {
            if (runtime?.Data == null) { Close(); return; }
            SetText(titleText, runtime.Data.DisplayName);
            string progress = runtime.ConstructionJob?.TargetLevel != null
                ? $"\n施工進度 {runtime.ConstructionJob.CurrentProgress} / {runtime.ConstructionJob.TargetLevel.RequiredConstructionProgress}" : "";
            SetText(statusText, $"狀態：{GetStateLabel(runtime.State)}\n等級：{runtime.CurrentLevel}{progress}");
            var next = runtime.GetNextLevel();
            bool startState = runtime.State == BuildingState.Vacant || runtime.State == BuildingState.Active;
            string constructionReason = startState && next != null ? GetConstructionBlockReason(next) : "";
            SetText(requirementText, next == null ? "已達目前最高等級"
                : $"下一級：{next.Level}\n收益：{next.RewardDescription}\n施工需求 {next.RequiredConstructionProgress}\n食物 {next.ConstructionCost.Food} / 資源 {next.ConstructionCost.Resource}"
                    + (string.IsNullOrEmpty(constructionReason) ? "" : "\n" + constructionReason));
            var current = runtime.Data.GetLevel(runtime.CurrentLevel);
            SetText(rewardText, "目前效果\n" + (current != null ? current.RewardDescription : "尚未啟用"));
            if (constructButton != null)
            {
                constructButton.gameObject.SetActive(next != null && startState);
                constructButton.interactable = next != null && startState && string.IsNullOrEmpty(constructionReason);
                SetText(constructButton.GetComponentInChildren<TMP_Text>(true), runtime.CurrentLevel == 0 ? "開始施工" : "升級建築");
            }
            if (cancelConstructionButton != null)
            {
                bool pending = runtime.State == BuildingState.CancelPending;
                bool building = runtime.State == BuildingState.Constructing || runtime.State == BuildingState.Upgrading;
                cancelConstructionButton.gameObject.SetActive(building || pending);
                cancelConstructionButton.interactable = building && system != null && system.CanSubmitActions;
                SetText(cancelConstructionButton.GetComponentInChildren<TMP_Text>(true), pending ? "待取消（結算時處理）" : "取消施工");
            }
            SynchronizePages();
            foreach (var page in pages)
            {
                if (page.View is CardCraftView craft) craft.Refresh();
                else if (page.View is FoodPlanInteractionView food) food.Refresh();
                else if (page.View is CardUpgradeView upgrade) upgrade.Refresh();
                else if (page.View is BuildingInformationPageView information) information.Refresh();
            }
            ApplySelection();
        }
        // Stable IDs retain each page instance (and its reading state) during value refreshes.
        private void SynchronizePages()
        {
            var desired = new List<Page>(); var ids = new HashSet<string>();
            var config = runtime.Data.Presentation;
            if (config != null)
            {
                var definitions = new List<BuildingPageDefinition>();
                if (config.Pages != null) foreach (var item in config.Pages) if (item != null) definitions.Add(item);
                for (int i = 1; i < definitions.Count; i++)
                {
                    var item = definitions[i]; int j = i - 1;
                    while (j >= 0 && definitions[j].Order > item.Order) { definitions[j + 1] = definitions[j]; j--; }
                    definitions[j + 1] = item;
                }
                foreach (var item in definitions)
                {
                    if (string.IsNullOrWhiteSpace(item.PageId) || !ids.Add(item.PageId) || !item.HasSingleSource)
                    {
                        if (reportedErrors.Add(item.PageId ?? "")) Debug.LogError("建築頁面配置無效：重複／空 ID 或來源不唯一。", config);
                        continue;
                    }
                    if (!item.IsPublic) continue;
                    desired.Add(new Page { Id = item.PageId, Title = string.IsNullOrWhiteSpace(item.Title)
                        ? (item.Interaction != null ? item.Interaction.DisplayName : "資訊") : item.Title,
                        Interaction = item.Interaction, InformationPrefab = item.InformationPagePrefab });
                }
            }
            else if (runtime.Data.Interactions != null)
                foreach (var interaction in runtime.Data.Interactions)
                {
                    if (interaction == null) continue;
                    if (string.IsNullOrWhiteSpace(interaction.InteractionId) || !ids.Add(interaction.InteractionId))
                    {
                        if (reportedErrors.Add(interaction.name)) Debug.LogError("建築互動需要唯一且非空 ID。", interaction);
                        continue;
                    }
                    desired.Add(new Page { Id = interaction.InteractionId, Title = interaction.DisplayName, Interaction = interaction });
                }
            var kept = new List<Page>();
            foreach (var item in desired)
            {
                var page = pages.Find(p => p.Id == item.Id && p.Interaction == item.Interaction && p.InformationPrefab == item.InformationPrefab) ?? CreatePage(item);
                page.Title = item.Title; kept.Add(page);
                if (page.Bookmark != null) page.Bookmark.transform.SetSiblingIndex(kept.Count - 1);
            }
            foreach (var page in pages) if (!kept.Contains(page)) ReleasePage(page);
            pages.Clear(); pages.AddRange(kept);
            if (!pages.Exists(p => p.Id == selectedPageId)) selectedPageId = (pages.Find(IsUnlocked) ?? (pages.Count > 0 ? pages[0] : null))?.Id;
        }
        private Page CreatePage(Page page)
        {
            if (interactionRoot == null) return page;
            if (page.InformationPrefab != null)
            {
                var view = Instantiate(page.InformationPrefab, interactionRoot); view.Bind(system, runtime); page.View = view;
            }
            else if (page.Interaction is CardCraftInteractionData craft && cardCraftPrefab != null)
            {
                var view = Instantiate(cardCraftPrefab, interactionRoot); view.Bind(system, runtime, craft); page.View = view;
            }
            else if (page.Interaction is FoodPlanInteractionData food && foodPlanPrefab != null)
            {
                var view = Instantiate(foodPlanPrefab, interactionRoot); view.Bind(system, runtime, food); page.View = view;
            }
            else if (page.Interaction is CardUpgradeInteractionData upgrade && cardUpgradePrefab != null)
            {
                var view = Instantiate(cardUpgradePrefab, interactionRoot); view.Bind(system, runtime, upgrade); page.View = view;
            }
            if (page.View != null) page.Root = page.View.gameObject;
            else
            {
                page.Root = new GameObject("ReadOnlyInteraction", typeof(RectTransform), typeof(TextMeshProUGUI));
                page.Root.transform.SetParent(interactionRoot, false);
                var label = page.Root.GetComponent<TMP_Text>();
                label.font = titleText != null ? titleText.font : TMP_Settings.defaultFontAsset;
                label.fontSize = 24; label.color = new Color32(232, 229, 218, 255);
                label.text = page.Interaction is FoodStorageInteractionData storage
                    ? $"{page.Title}\n\n食物儲存上限 +{storage.FoodCapacityBonus}\n（解鎖後自動生效）" : $"{page.Title}\n\n此功能尚未配置顯示頁。";
            }
            var rect = page.Root.transform as RectTransform;
            if (rect != null) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
            if (bookmarkRoot != null && bookmarkButtonPrefab != null)
            {
                page.Bookmark = Instantiate(bookmarkButtonPrefab, bookmarkRoot); page.Bookmark.gameObject.SetActive(true);
                string id = page.Id; page.Bookmark.onClick.AddListener(() => SelectPage(id));
            }
            return page;
        }
        public void SelectPage(string pageId)
        {
            if (!pages.Exists(p => p.Id == pageId)) return;
            selectedPageId = pageId; ApplySelection();
        }
        private bool IsUnlocked(Page page) { return page.Interaction == null || runtime.IsInteractionUnlocked(page.Interaction); }
        private void ApplySelection()
        {
            string status = pages.Count == 0 ? "目前沒有公開功能" : "";
            foreach (var page in pages)
            {
                bool selected = page.Id == selectedPageId, unlocked = IsUnlocked(page);
                if (page.Root != null) page.Root.SetActive(selected);
                if (page.Bookmark != null) SetText(page.Bookmark.GetComponentInChildren<TMP_Text>(true), (selected ? "▶ " : "") + page.Title + (unlocked ? "" : "\n未解鎖"));
                if (selected) status = !unlocked ? $"功能預覽：需要建築等級 {page.Interaction.RequiredBuildingLevel}（目前 {runtime.CurrentLevel}）"
                    : system != null && !system.CanSubmitActions ? "目前流程僅供查看，無法操作建築" : page.Title;
            }
            SetText(pageStatusText, status);
        }
        public void Close() { UnhookSystem(); ClearPages(); runtime = null; system = null; SetVisible(false); }
        private string GetConstructionBlockReason(BuildingLevelData next)
        {
            if (system == null || !system.CanSubmitActions) return "目前流程不能操作建築";
            if (next.ConstructionCard == null) return "無法施工：尚未設定施工卡牌";
            if (ResourceReservationService.AvailableFood < next.ConstructionCost.Food
                || ResourceReservationService.AvailableResource < next.ConstructionCost.Resource)
                return $"資源不足：可用食物 {ResourceReservationService.AvailableFood}（保留 {ResourceReservationService.ReservedFood}）／可用資源 {ResourceReservationService.AvailableResource}（保留 {ResourceReservationService.ReservedResource}）";
            return "";
        }
        private void StartConstruction() { if (system != null && system.CanSubmitActions && runtime?.Data != null) system.TryStartConstruction(runtime.Data.BuildingId); }
        private void CancelConstruction() { if (system != null && system.CanSubmitActions && runtime?.Data != null) system.RequestCancelConstruction(runtime.Data.BuildingId); }
        private void ClearPages()
        {
            foreach (var page in pages) ReleasePage(page);
            pages.Clear(); selectedPageId = null; reportedErrors.Clear();
        }
        private void ReleasePage(Page page)
        {
            if (page.View is BuildingInformationPageView info) info.Unbind();
            if (page.View is CardCraftView craft) craft.Unbind();
            if (page.Root != null) ReleaseObject(page.Root);
            if (page.Bookmark != null) ReleaseObject(page.Bookmark.gameObject);
        }
        private static void ReleaseObject(GameObject target)
        {
            target.SetActive(false);
            if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
        }
        private void UnhookSystem()
        {
            if (system != null) system.BuildingsChanged -= Refresh;
            FoodSystem.StateChanged -= Refresh; ResourceSystem.StateChanged -= Refresh;
            ResourceReservationService.StateChanged -= Refresh;
            if (phaseController != null) phaseController.PhaseChanged -= OnPhaseChanged;
            phaseController = null;
        }
        private void SetVisible(bool visible) { (root != null ? root : gameObject).SetActive(visible); }
        private static void SetText(TMP_Text target, string value) { if (target != null) target.text = value; }
        private static string GetStateLabel(BuildingState state)
        {
            switch (state)
            {
                case BuildingState.Vacant: return "空地";
                case BuildingState.Constructing: return "施工中";
                case BuildingState.Active: return "運作中";
                case BuildingState.Upgrading: return "升級中";
                case BuildingState.CancelPending: return "等待取消";
                default: return state.ToString();
            }
        }
    }
}
