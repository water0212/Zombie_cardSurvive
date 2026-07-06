using System;
using System.Collections.Generic;
using ZombieCardSurvive.Buildings.Data;

namespace ZombieCardSurvive.Buildings.Runtime
{
    [Serializable]
    public class ConstructionJobRuntime
    {
        private readonly List<string> constructionCardInstanceIds = new List<string>();

        public ConstructionJobRuntime(BuildingLevelData targetLevel, ResourceReservationToken reservation)
        {
            JobId = Guid.NewGuid().ToString("N");
            TargetLevel = targetLevel;
            Reservation = reservation;
            State = BuildingJobState.InProgress;
            InitialFreeIntegrationCount = targetLevel != null ? targetLevel.FreeIntegrationCount : 0;
            PendingFreeIntegrationCount = InitialFreeIntegrationCount;
        }

        public string JobId { get; }
        public BuildingLevelData TargetLevel { get; }
        public ResourceReservationToken Reservation { get; }
        public BuildingJobState State { get; private set; }
        public int CurrentProgress { get; private set; }
        public int InitialFreeIntegrationCount { get; }
        public int PendingFreeIntegrationCount { get; private set; }
        public bool IntegrationOfferConsumed { get; private set; }
        public IReadOnlyList<string> ConstructionCardInstanceIds => constructionCardInstanceIds;
        public bool IsProgressComplete => TargetLevel != null && CurrentProgress >= TargetLevel.RequiredConstructionProgress;

        public void AddCardInstance(string instanceId)
        {
            if (!string.IsNullOrWhiteSpace(instanceId) && !constructionCardInstanceIds.Contains(instanceId))
            {
                constructionCardInstanceIds.Add(instanceId);
            }
        }

        public void AddProgress()
        {
            if (State != BuildingJobState.InProgress || TargetLevel == null)
            {
                return;
            }

            CurrentProgress = Math.Min(
                TargetLevel.RequiredConstructionProgress,
                CurrentProgress + TargetLevel.ProgressPerCardPlay);
        }

        public void RequestCancel()
        {
            if (State == BuildingJobState.InProgress)
            {
                State = BuildingJobState.CancelPending;
            }
        }

        public void Complete()
        {
            State = BuildingJobState.Completed;
            PendingFreeIntegrationCount = 0;
        }

        public void Cancel()
        {
            State = BuildingJobState.Cancelled;
            PendingFreeIntegrationCount = 0;
        }

        public void ConsumeIntegrationOffer()
        {
            IntegrationOfferConsumed = true;
            PendingFreeIntegrationCount = 0;
        }

        public void SyncIntegratedCardCount(int integratedCount)
        {
            if (!IntegrationOfferConsumed)
            {
                PendingFreeIntegrationCount = Math.Max(0, InitialFreeIntegrationCount - Math.Max(0, integratedCount));
            }
        }
    }
}
