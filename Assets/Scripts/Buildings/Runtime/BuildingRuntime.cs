using System;
using System.Collections.Generic;
using ZombieCardSurvive.Buildings.Data;

namespace ZombieCardSurvive.Buildings.Runtime
{
    [Serializable]
    public class BuildingRuntime
    {
        private readonly List<CraftLineRuntime> craftLines = new List<CraftLineRuntime>();

        public BuildingRuntime(BuildBaseData data)
        {
            Data = data;
            State = BuildingState.Vacant;
        }

        public BuildBaseData Data { get; }
        public int CurrentLevel { get; private set; }
        public BuildingState State { get; private set; }
        public BuildingState StateBeforeCancel { get; private set; }
        public ConstructionJobRuntime ConstructionJob { get; private set; }
        public IReadOnlyList<CraftLineRuntime> CraftLines => craftLines;

        public void StartConstruction(ConstructionJobRuntime job)
        {
            ConstructionJob = job;
            State = CurrentLevel <= 0 ? BuildingState.Constructing : BuildingState.Upgrading;
        }

        public void RequestCancelConstruction()
        {
            if (State != BuildingState.Constructing && State != BuildingState.Upgrading)
            {
                return;
            }

            StateBeforeCancel = State;
            State = BuildingState.CancelPending;
            ConstructionJob?.RequestCancel();
        }

        public void CompleteConstruction()
        {
            if (ConstructionJob?.TargetLevel == null)
            {
                return;
            }

            CurrentLevel = ConstructionJob.TargetLevel.Level;
            ConstructionJob.Complete();
            ConstructionJob = null;
            State = BuildingState.Active;
            RefreshCraftLines();
        }

        public ConstructionJobRuntime CancelConstruction()
        {
            ConstructionJobRuntime cancelled = ConstructionJob;
            cancelled?.Cancel();
            ConstructionJob = null;
            State = CurrentLevel <= 0 ? BuildingState.Vacant : BuildingState.Active;
            return cancelled;
        }

        public CraftLineRuntime FindCraftLine(string lineId)
        {
            return craftLines.Find(line => line?.Data != null && line.Data.InteractionId == lineId);
        }

        public BuildingLevelData GetNextLevel()
        {
            return Data != null ? Data.GetLevel(CurrentLevel + 1) : null;
        }

        public int GetManufacturingBonus()
        {
            BuildingLevelData level = Data != null ? Data.GetLevel(CurrentLevel) : null;
            return level != null ? level.ManufacturingBonus : 0;
        }

        public void RefreshCraftLines()
        {
            if (Data?.Interactions == null)
            {
                return;
            }

            foreach (BuildingInteractionData interaction in Data.Interactions)
            {
                if (!(interaction is CardCraftInteractionData cardCraft)
                    || cardCraft.RequiredBuildingLevel > CurrentLevel
                    || FindCraftLine(cardCraft.InteractionId) != null)
                {
                    continue;
                }

                craftLines.Add(new CraftLineRuntime(cardCraft));
            }
        }
    }
}
