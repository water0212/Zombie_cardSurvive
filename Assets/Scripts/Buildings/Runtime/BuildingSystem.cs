using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.UI;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Cards.UI.Replacement;
using ZombieCardSurvive.Run;

namespace ZombieCardSurvive.Buildings.Runtime
{
    public class BuildingSystem : MonoBehaviour
    {
        private static BuildingSystem activeInstance;

        [Header("Data")]
        [SerializeField] private List<BuildBaseData> buildingCatalog = new List<BuildBaseData>();

        [Header("Scene")]
        [SerializeField] private RectTransform buildingRoot;
        [SerializeField] private BuildingWorldView buildingPrefab;
        [SerializeField] private BuildingDetailView detailView;
        [SerializeField] private CardInventory inventory;
        [SerializeField] private CardController cardController;
        [SerializeField] private DeckReplacementView replacementView;
        [SerializeField] private RunPhaseController runPhaseController;

        private readonly Dictionary<string, BuildingRuntime> runtimes = new Dictionary<string, BuildingRuntime>();
        private readonly Dictionary<string, BuildingWorldView> worldViews = new Dictionary<string, BuildingWorldView>();

        public event Action BuildingsChanged;
        public event Action<BuildingRuntime> BuildingUnlocked;
        public event Action<BuildingResolutionReport> ResolutionCompleted;

        public IReadOnlyDictionary<string, BuildingRuntime> Runtimes => runtimes;
        public BuildingResolutionReport LastResolutionReport { get; private set; }
        public bool CanSubmitActions => runPhaseController == null || runPhaseController.CurrentPhase == RunPhase.Turn;

        private void Awake()
        {
            activeInstance = this;
            ResolveReferences();
            InitializeRun();
        }

        private void OnEnable()
        {
            activeInstance = this;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(activeInstance, this))
            {
                activeInstance = null;
            }
        }

        public void InitializeRun()
        {
            ResourceReservationService.Reset();
            ClearWorldViews();
            runtimes.Clear();

            foreach (BuildBaseData building in buildingCatalog)
            {
                if (building != null && building.StartsUnlocked)
                {
                    UnlockBuilding(building.BuildingId);
                }
            }

            NotifyChanged();
        }

        public bool UnlockBuilding(string buildingId)
        {
            if (string.IsNullOrWhiteSpace(buildingId) || runtimes.ContainsKey(buildingId))
            {
                return false;
            }

            BuildBaseData data = buildingCatalog.Find(candidate => candidate != null && candidate.BuildingId == buildingId);
            if (data == null)
            {
                Debug.LogWarning($"Building unlock failed: no BuildBaseData found for id '{buildingId}'.");
                return false;
            }

            BuildingRuntime runtime = new BuildingRuntime(data);
            runtimes.Add(buildingId, runtime);
            CreateWorldView(runtime);
            BuildingUnlocked?.Invoke(runtime);
            NotifyChanged();
            return true;
        }

        public BuildingRuntime GetBuilding(string buildingId)
        {
            return !string.IsNullOrWhiteSpace(buildingId) && runtimes.TryGetValue(buildingId, out BuildingRuntime runtime)
                ? runtime
                : null;
        }

        public bool TryStartConstruction(string buildingId)
        {
            if (!CanSubmitActions || inventory == null)
            {
                return false;
            }

            BuildingRuntime runtime = GetBuilding(buildingId);
            if (runtime == null || (runtime.State != BuildingState.Vacant && runtime.State != BuildingState.Active))
            {
                return false;
            }

            BuildingLevelData targetLevel = runtime.GetNextLevel();
            if (targetLevel == null || targetLevel.ConstructionCard == null)
            {
                return false;
            }

            if (!ResourceReservationService.TryReserve(targetLevel.ConstructionCost, out ResourceReservationToken reservation))
            {
                return false;
            }

            ConstructionJobRuntime job = new ConstructionJobRuntime(targetLevel, reservation);
            for (int i = 0; i < targetLevel.ConstructionCardCount; i++)
            {
                CardInventoryEntry entry = inventory.AddCard(targetLevel.ConstructionCard);
                if (entry != null)
                {
                    job.AddCardInstance(entry.InstanceId);
                }
            }

            runtime.StartConstruction(job);
            RefreshWorldView(runtime);
            NotifyChanged();
            return true;
        }

        public bool RequestCancelConstruction(string buildingId)
        {
            if (!CanSubmitActions)
            {
                return false;
            }

            BuildingRuntime runtime = GetBuilding(buildingId);
            if (runtime == null || (runtime.State != BuildingState.Constructing && runtime.State != BuildingState.Upgrading))
            {
                return false;
            }

            runtime.RequestCancelConstruction();
            RefreshWorldView(runtime);
            NotifyChanged();
            return true;
        }

        public bool TryQueueCraft(string buildingId, string lineId, CardCraftRecipeData recipe)
        {
            if (!CanSubmitActions || recipe == null)
            {
                return false;
            }

            BuildingRuntime runtime = GetBuilding(buildingId);
            CraftLineRuntime line = runtime?.FindCraftLine(lineId);
            if (runtime == null
                || (runtime.State != BuildingState.Active && runtime.State != BuildingState.Upgrading)
                || line == null
                || !line.HasCapacity
                || !ContainsRecipe(line.Data, recipe))
            {
                return false;
            }

            if (!ResourceReservationService.TryReserve(recipe.Cost, out ResourceReservationToken reservation))
            {
                return false;
            }

            CraftJobRuntime job = new CraftJobRuntime(recipe, reservation);
            if (!line.AddJob(job))
            {
                ResourceReservationService.Release(reservation);
                return false;
            }

            NotifyChanged();
            return true;
        }

        public bool RequestCancelCraft(string buildingId, string lineId, string jobId)
        {
            if (!CanSubmitActions)
            {
                return false;
            }

            CraftJobRuntime job = GetBuilding(buildingId)?.FindCraftLine(lineId)?.FindJob(jobId);
            if (job == null || job.State == BuildingJobState.CancelPending)
            {
                return false;
            }

            job.RequestCancel();
            NotifyChanged();
            return true;
        }

        public BuildingResolutionReport ResolveAfterEvent()
        {
            BuildingResolutionReport report = new BuildingResolutionReport();
            Dictionary<BuildingRuntime, HashSet<string>> activeLinesBeforeResolution = SnapshotActiveCraftLines();

            foreach (BuildingRuntime runtime in runtimes.Values)
            {
                ResolvePendingConstructionCancellation(runtime, report);
                ResolvePendingCraftCancellations(runtime, report);
            }

            foreach (BuildingRuntime runtime in runtimes.Values)
            {
                ResolveCompletedConstruction(runtime, report);
            }

            foreach (KeyValuePair<BuildingRuntime, HashSet<string>> entry in activeLinesBeforeResolution)
            {
                ResolveCraftLines(entry.Key, entry.Value, report);
            }

            LastResolutionReport = report;
            ResolutionCompleted?.Invoke(report);
            NotifyChanged();
            return report;
        }

        public bool TryCreateConstructionIntegrationRequest(out DeckReplacementRequest request)
        {
            request = null;
            if (inventory == null || cardController == null)
            {
                return false;
            }

            List<CardInventoryEntry> candidates = new List<CardInventoryEntry>();
            int replacementCount = 0;
            foreach (BuildingRuntime runtime in runtimes.Values)
            {
                ConstructionJobRuntime job = runtime.ConstructionJob;
                if (job == null || job.State != BuildingJobState.InProgress)
                {
                    continue;
                }

                int alreadyIntegrated = 0;
                List<CardInventoryEntry> jobEntries = new List<CardInventoryEntry>();
                foreach (string instanceId in job.ConstructionCardInstanceIds)
                {
                    CardInventoryEntry entry = inventory.FindEntry(instanceId);
                    if (entry == null)
                    {
                        alreadyIntegrated++;
                    }
                    else
                    {
                        jobEntries.Add(entry);
                    }
                }

                job.SyncIntegratedCardCount(alreadyIntegrated);
                int availableRight = Mathf.Min(job.PendingFreeIntegrationCount, jobEntries.Count);
                for (int i = 0; i < availableRight; i++)
                {
                    candidates.Add(jobEntries[i]);
                }

                replacementCount += availableRight;
            }

            List<CardRuntime> targets = cardController.GetReplaceableRegularCards();
            targets.RemoveAll(card => card == null || card.AssignedSlotType != DeckSlotType.Unrestricted);
            replacementCount = Mathf.Min(replacementCount, targets.Count, candidates.Count);
            if (replacementCount <= 0)
            {
                return false;
            }

            request = new DeckReplacementRequest(
                DeckReplacementReason.ConstructionIntegration,
                replacementCount,
                targets,
                candidates,
                new[] { DeckSlotType.Unrestricted });
            return true;
        }

        public void ConsumeConstructionIntegrationOffers()
        {
            foreach (BuildingRuntime runtime in runtimes.Values)
            {
                runtime.ConstructionJob?.ConsumeIntegrationOffer();
            }

            NotifyChanged();
        }

        public static bool ApplyConstructionProgress(string cardRuntimeId)
        {
            BuildingSystem system = activeInstance != null ? activeInstance : FindObjectOfType<BuildingSystem>();
            if (system == null || string.IsNullOrWhiteSpace(cardRuntimeId))
            {
                return false;
            }

            foreach (BuildingRuntime runtime in system.runtimes.Values)
            {
                ConstructionJobRuntime job = runtime.ConstructionJob;
                if (job == null || !ContainsId(job.ConstructionCardInstanceIds, cardRuntimeId))
                {
                    continue;
                }

                job.AddProgress();
                system.RefreshWorldView(runtime);
                system.NotifyChanged();
                return true;
            }

            return false;
        }

        public void OpenDetails(BuildingRuntime runtime)
        {
            detailView?.Bind(this, runtime);
        }

        private void ResolvePendingConstructionCancellation(BuildingRuntime runtime, BuildingResolutionReport report)
        {
            ConstructionJobRuntime job = runtime?.ConstructionJob;
            if (runtime == null || runtime.State != BuildingState.CancelPending || job == null)
            {
                return;
            }

            ResourceReservationService.Release(job.Reservation);
            RemoveConstructionCards(job, DeckVacancyReason.BuildingCancelled, report);
            runtime.CancelConstruction();
            report.CancelledConstructions++;
            RefreshWorldView(runtime);
        }

        private void ResolveCompletedConstruction(BuildingRuntime runtime, BuildingResolutionReport report)
        {
            ConstructionJobRuntime job = runtime?.ConstructionJob;
            if (job == null || job.State != BuildingJobState.InProgress || !job.IsProgressComplete)
            {
                return;
            }

            if (!ResourceReservationService.Commit(job.Reservation))
            {
                Debug.LogWarning($"Building '{runtime.Data?.name}' completed progress but its reserved cost could not be committed.");
                return;
            }

            RemoveConstructionCards(job, DeckVacancyReason.BuildingCompleted, report);
            runtime.CompleteConstruction();
            report.CompletedConstructions++;
            RefreshWorldView(runtime);
        }

        private void RemoveConstructionCards(ConstructionJobRuntime job, DeckVacancyReason reason, BuildingResolutionReport report)
        {
            if (job == null)
            {
                return;
            }

            int removed = 0;
            if (inventory != null)
            {
                removed += inventory.RemoveByInstanceIds(job.ConstructionCardInstanceIds);
            }

            if (cardController != null)
            {
                removed += cardController.RemoveRuntimeCardsByInstanceIds(job.ConstructionCardInstanceIds, reason);
            }

            report.RemovedConstructionCards += removed;
        }

        private void ResolvePendingCraftCancellations(BuildingRuntime runtime, BuildingResolutionReport report)
        {
            if (runtime == null)
            {
                return;
            }

            foreach (CraftLineRuntime line in runtime.CraftLines)
            {
                List<CraftJobRuntime> cancelled = new List<CraftJobRuntime>();
                foreach (CraftJobRuntime job in line.Jobs)
                {
                    if (job != null && job.State == BuildingJobState.CancelPending)
                    {
                        ResourceReservationService.Release(job.Reservation);
                        job.Cancel();
                        cancelled.Add(job);
                        report.CancelledCraftJobs++;
                    }
                }

                foreach (CraftJobRuntime job in cancelled)
                {
                    line.RemoveJob(job);
                }
            }
        }

        private void ResolveCraftLines(BuildingRuntime runtime, HashSet<string> activeLineIds, BuildingResolutionReport report)
        {
            if (runtime == null || activeLineIds == null)
            {
                return;
            }

            foreach (CraftLineRuntime line in runtime.CraftLines)
            {
                if (line?.Data == null || !activeLineIds.Contains(line.Data.InteractionId))
                {
                    continue;
                }

                int production = line.Data.BaseProduction + runtime.GetManufacturingBonus();
                while (production > 0 && line.Jobs.Count > 0)
                {
                    CraftJobRuntime job = line.Jobs[0];
                    if (job == null)
                    {
                        line.RemoveJob(job);
                        continue;
                    }

                    production = job.ApplyProduction(production);
                    if (job.RemainingProduction > 0)
                    {
                        break;
                    }

                    if (!ResourceReservationService.Commit(job.Reservation))
                    {
                        Debug.LogWarning($"Craft job '{job.JobId}' completed progress but its reserved cost could not be committed.");
                        break;
                    }

                    ProduceCards(job.Recipe, report);
                    job.Complete();
                    line.RemoveJob(job);
                    report.CompletedCraftJobs++;
                }
            }
        }

        private void ProduceCards(CardCraftRecipeData recipe, BuildingResolutionReport report)
        {
            if (inventory == null || recipe?.OutputCard == null)
            {
                return;
            }

            for (int i = 0; i < recipe.OutputCount; i++)
            {
                inventory.AddCard(recipe.OutputCard);
                report.ProducedCards++;
            }
        }

        private Dictionary<BuildingRuntime, HashSet<string>> SnapshotActiveCraftLines()
        {
            Dictionary<BuildingRuntime, HashSet<string>> snapshot = new Dictionary<BuildingRuntime, HashSet<string>>();
            foreach (BuildingRuntime runtime in runtimes.Values)
            {
                HashSet<string> ids = new HashSet<string>();
                if (runtime.CurrentLevel > 0)
                {
                    foreach (CraftLineRuntime line in runtime.CraftLines)
                    {
                        if (line?.Data != null)
                        {
                            ids.Add(line.Data.InteractionId);
                        }
                    }
                }

                snapshot[runtime] = ids;
            }

            return snapshot;
        }

        private void CreateWorldView(BuildingRuntime runtime)
        {
            if (runtime?.Data == null || buildingPrefab == null || buildingRoot == null)
            {
                return;
            }

            BuildingWorldView view = Instantiate(buildingPrefab, buildingRoot);
            view.Bind(this, runtime);
            view.RectTransform.anchoredPosition = runtime.Data.AnchoredPosition;
            worldViews[runtime.Data.BuildingId] = view;
        }

        private void RefreshWorldView(BuildingRuntime runtime)
        {
            if (runtime?.Data != null && worldViews.TryGetValue(runtime.Data.BuildingId, out BuildingWorldView view))
            {
                view.Refresh();
            }

            detailView?.RefreshIfBound(runtime);
        }

        private void ClearWorldViews()
        {
            foreach (BuildingWorldView view in worldViews.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            worldViews.Clear();
        }

        private void ResolveReferences()
        {
            inventory = inventory != null ? inventory : FindObjectOfType<CardInventory>();
            cardController = cardController != null ? cardController : FindObjectOfType<CardController>();
            replacementView = replacementView != null ? replacementView : FindObjectOfType<DeckReplacementView>(true);
            runPhaseController = runPhaseController != null ? runPhaseController : FindObjectOfType<RunPhaseController>();
        }

        private void NotifyChanged()
        {
            BuildingsChanged?.Invoke();
        }

        private static bool ContainsRecipe(CardCraftInteractionData line, CardCraftRecipeData recipe)
        {
            if (line?.Recipes == null)
            {
                return false;
            }

            foreach (CardCraftRecipeData candidate in line.Recipes)
            {
                if (ReferenceEquals(candidate, recipe))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsId(IReadOnlyList<string> ids, string target)
        {
            foreach (string id in ids)
            {
                if (id == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
