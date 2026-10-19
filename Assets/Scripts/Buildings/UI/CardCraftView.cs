using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Run;

namespace ZombieCardSurvive.Buildings.UI
{
    public class CardCraftView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText, productionText;
        [SerializeField] private Transform recipeRoot, queueRoot;
        [SerializeField] private Button recipeButtonPrefab; // Legacy serialized reference.
        [SerializeField] private CraftRecipeRowView recipeRowPrefab;
        [SerializeField] private CraftJobRowView jobRowPrefab;
        [SerializeField] private ScrollRect recipeScrollRect, queueScrollRect;
        [SerializeField] private TMP_Text emptyRecipesText, emptyQueueText, selectedJobText, availabilityText;
        private readonly Dictionary<CardCraftRecipeData, CraftRecipeRowView> recipes = new Dictionary<CardCraftRecipeData, CraftRecipeRowView>();
        private readonly Dictionary<string, CraftJobRowView> jobs = new Dictionary<string, CraftJobRowView>();
        private BuildingSystem system;
        private BuildingRuntime building;
        private CardCraftInteractionData data;
        private CraftLineRuntime line;
        private CardCraftRecipeData expandedRecipe;
        private string selectedJobId;
        private bool subscribed;
        private RunPhaseController phaseController;
        public string SelectedJobId => selectedJobId;
        public CardCraftRecipeData ExpandedRecipe => expandedRecipe;

        public void Bind(BuildingSystem owner, BuildingRuntime target, CraftLineRuntime craftLine)
        { Bind(owner, target, craftLine != null ? craftLine.Data : null); }
        public void Bind(BuildingSystem owner, BuildingRuntime target, CardCraftInteractionData interaction)
        {
            Unbind(); system = owner; building = target; data = interaction;
            if (system != null) system.BuildingsChanged += Refresh;
            ResourceReservationService.StateChanged += Refresh;
            FoodSystem.StateChanged += Refresh; ResourceSystem.StateChanged += Refresh;
            phaseController = FindObjectOfType<RunPhaseController>();
            if (phaseController != null) phaseController.PhaseChanged += OnPhaseChanged;
            subscribed = true; Refresh();
        }
        private void OnEnable() { Refresh(); }
        private void OnPhaseChanged(RunPhase phase) { Refresh(); }
        public void Refresh()
        {
            if (data == null) return;
            line = building != null ? building.FindCraftLine(data.InteractionId) : null;
            if (titleText != null) titleText.text = data.DisplayName;
            if (productionText != null) productionText.text = $"製造力 {data.BaseProduction + (building?.GetManufacturingBonus() ?? 0)}　佇列 {line?.Jobs.Count ?? 0}/{data.QueueCapacity}";
            if (availabilityText != null) availabilityText.text = GetPageLockReason();
            var selected = line != null && selectedJobId != null ? line.FindJob(selectedJobId) : null;
            if (selected == null) selectedJobId = null;
            SyncRecipes(); SyncJobs();
            if (selectedJobText != null) selectedJobText.text = selected == null ? "左鍵查看／右鍵取消" :
                $"{selected.Recipe?.DisplayName}　{CraftJobRowView.StateLabel(selected.State)}\n進度 {selected.CurrentProduction}/{selected.RequiredProduction}\n工作 {selected.JobId}\n已保留 食物 {selected.Recipe?.Cost.Food ?? 0}／資源 {selected.Recipe?.Cost.Resource ?? 0}";
        }
        public string GetPageLockReason()
        {
            if (building == null || data == null) return "無建築資料";
            if (!building.IsInteractionUnlocked(data)) return $"需要建築等級 {data.RequiredBuildingLevel}（目前 {building.CurrentLevel}）";
            if (building.State != BuildingState.Active && building.State != BuildingState.Upgrading) return "建築目前無法製作";
            if (system == null || !system.CanSubmitActions) return "目前流程不能操作建築";
            return line == null ? "製作列尚未啟用" : string.Empty;
        }
        public string GetUnavailableReason(CardCraftRecipeData recipe)
        {
            string reason = GetPageLockReason();
            if (!string.IsNullOrEmpty(reason)) return reason;
            if (recipe == null || !ContainsRecipe(recipe)) return "食譜不屬於此製作列";
            if (recipe.OutputCard == null) return "食譜尚未設定產出卡牌";
            if (!line.HasCapacity) return "製作佇列已滿";
            if (ResourceReservationService.AvailableFood < recipe.Cost.Food || ResourceReservationService.AvailableResource < recipe.Cost.Resource)
                return $"資源不足：食物需 {recipe.Cost.Food}／可用 {ResourceReservationService.AvailableFood}（保留 {ResourceReservationService.ReservedFood}）；資源需 {recipe.Cost.Resource}／可用 {ResourceReservationService.AvailableResource}（保留 {ResourceReservationService.ReservedResource}）";
            return string.Empty;
        }
        private bool ContainsRecipe(CardCraftRecipeData recipe)
        { if (data != null && data.Recipes != null) foreach (var item in data.Recipes) if (item == recipe) return true; return false; }
        private void SyncRecipes()
        {
            var wanted = new HashSet<CardCraftRecipeData>(); int index = 0;
            if (data.Recipes != null) foreach (var recipe in data.Recipes)
            {
                if (recipe == null || !wanted.Add(recipe)) continue;
                CraftRecipeRowView row;
                if (!recipes.TryGetValue(recipe, out row) && recipeRoot != null && recipeRowPrefab != null)
                { row = Instantiate(recipeRowPrefab, recipeRoot); row.gameObject.SetActive(true); row.Bind(recipe, ExpandRecipe, QueueRecipe); recipes.Add(recipe, row); }
                if (row != null) { row.transform.SetSiblingIndex(index++); row.Refresh(recipe == expandedRecipe, GetUnavailableReason(recipe)); }
            }
            var obsolete = new List<CardCraftRecipeData>();
            foreach (var pair in recipes) if (!wanted.Contains(pair.Key)) obsolete.Add(pair.Key);
            foreach (var item in obsolete) { DisposeView(recipes[item].gameObject); recipes.Remove(item); }
            if (expandedRecipe != null && !wanted.Contains(expandedRecipe)) expandedRecipe = null;
            if (emptyRecipesText != null) emptyRecipesText.gameObject.SetActive(wanted.Count == 0);
        }
        private void SyncJobs()
        {
            var wanted = new HashSet<string>(); int index = 0;
            if (line != null) foreach (var job in line.Jobs)
            {
                wanted.Add(job.JobId); CraftJobRowView row;
                if (!jobs.TryGetValue(job.JobId, out row) && queueRoot != null && jobRowPrefab != null)
                { row = Instantiate(jobRowPrefab, queueRoot); row.gameObject.SetActive(true); jobs.Add(job.JobId, row); }
                if (row != null) { row.transform.SetSiblingIndex(index++); row.Bind(system, building, line, job, SelectJob, job.JobId == selectedJobId); }
            }
            var obsolete = new List<string>();
            foreach (var pair in jobs) if (!wanted.Contains(pair.Key)) obsolete.Add(pair.Key);
            foreach (var item in obsolete) { DisposeView(jobs[item].gameObject); jobs.Remove(item); }
            if (emptyQueueText != null) emptyQueueText.gameObject.SetActive(wanted.Count == 0);
        }
        public void ExpandRecipe(CardCraftRecipeData recipe)
        {
            expandedRecipe = ReferenceEquals(expandedRecipe, recipe) ? null : recipe;
            selectedJobId = null;
            Refresh();
        }
        public void SelectJob(string jobId)
        {
            var selected = line != null ? line.FindJob(jobId) : null;
            if (selected == null) return;
            selectedJobId = selected.JobId; expandedRecipe = selected.Recipe; Refresh();
            CraftRecipeRowView row;
            if (recipeScrollRect != null && expandedRecipe != null && recipes.TryGetValue(expandedRecipe, out row))
            {
                Canvas.ForceUpdateCanvases();
                var content = recipeScrollRect.content; var viewport = recipeScrollRect.viewport;
                if (content != null && viewport != null)
                {
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, row.transform);
                    float overflow = content.rect.height - viewport.rect.height;
                    recipeScrollRect.verticalNormalizedPosition = overflow > 0 ? 1f - Mathf.Clamp01((content.rect.yMax - bounds.max.y) / overflow) : 1f;
                }
            }
        }
        private void QueueRecipe(CardCraftRecipeData recipe)
        {
            line = building != null && data != null ? building.FindCraftLine(data.InteractionId) : null;
            if (!string.IsNullOrEmpty(GetUnavailableReason(recipe))) { Refresh(); return; }
            bool accepted = system.TryQueueCraft(building.Data.BuildingId, data.InteractionId, recipe);
            Refresh();
            if (!accepted && availabilityText != null) availabilityText.text = "未能加入製作，請確認建築狀態與可用資源。";
        }
        public void Unbind()
        {
            if (subscribed)
            {
                if (system != null) system.BuildingsChanged -= Refresh;
                ResourceReservationService.StateChanged -= Refresh;
                FoodSystem.StateChanged -= Refresh; ResourceSystem.StateChanged -= Refresh;
                if (phaseController != null) phaseController.PhaseChanged -= OnPhaseChanged;
            }
            subscribed = false;
            phaseController = null;
            foreach (var item in recipes.Values) if (item != null) DisposeView(item.gameObject);
            foreach (var item in jobs.Values) if (item != null) DisposeView(item.gameObject);
            recipes.Clear(); jobs.Clear(); system = null; building = null; data = null; line = null; expandedRecipe = null; selectedJobId = null;
        }
        private static void DisposeView(GameObject item)
        { item.SetActive(false); if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
        private void OnDestroy() { Unbind(); }
    }
}
