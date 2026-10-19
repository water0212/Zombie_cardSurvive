using System;
using ZombieCardSurvive.Events.Data;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class EventSelectionContext
    {
        private readonly Func<string, int> buildingLevelResolver;
        private readonly Func<string, bool> runFlagResolver;
        private readonly Func<string, int> customValueResolver;
        private readonly RunFlagRuntimeStore runFlagStore;

        public EventSelectionContext(
            int day,
            int morale,
            int food,
            int resource,
            int maxEnergy,
            int activeZombieCount,
            Func<string, int> buildingLevelResolver = null,
            Func<string, bool> runFlagResolver = null,
            Func<string, int> customValueResolver = null,
            RunFlagRuntimeStore runFlagStore = null)
        {
            Day = Math.Max(1, day);
            Morale = morale;
            Food = food;
            Resource = resource;
            MaxEnergy = maxEnergy;
            ActiveZombieCount = activeZombieCount;
            this.buildingLevelResolver = buildingLevelResolver;
            this.runFlagResolver = runFlagResolver;
            this.customValueResolver = customValueResolver;
            this.runFlagStore = runFlagStore;
        }

        public int Day { get; }
        public int Morale { get; }
        public int Food { get; }
        public int Resource { get; }
        public int MaxEnergy { get; }
        public int ActiveZombieCount { get; }

        public int GetBuildingLevel(string buildingId)
        {
            return buildingLevelResolver != null && !string.IsNullOrWhiteSpace(buildingId)
                ? buildingLevelResolver(buildingId.Trim())
                : 0;
        }

        public bool HasRunFlag(string flagId)
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                return false;
            }

            if (runFlagStore != null)
            {
                return runFlagStore.IsActive(flagId.Trim());
            }

            return runFlagResolver != null && runFlagResolver(flagId.Trim());
        }

        public int GetRunFlagValue(RunFlagData flagData, string fallbackId)
        {
            if (runFlagStore != null && flagData != null)
            {
                return runFlagStore.GetValue(flagData);
            }

            string flagId = flagData != null ? flagData.FlagId : fallbackId;
            return GetRunFlagValue(flagId);
        }

        public int GetRunFlagValue(string flagId)
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                return 0;
            }

            if (runFlagStore != null)
            {
                return runFlagStore.GetValue(flagId.Trim());
            }

            return HasRunFlag(flagId) ? 1 : 0;
        }

        public int GetRunFlagDaysSinceSet(RunFlagData flagData, string fallbackId)
        {
            string flagId = flagData != null ? flagData.FlagId : fallbackId;
            return GetRunFlagDaysSinceSet(flagId);
        }

        public int GetRunFlagDaysSinceSet(string flagId)
        {
            return runFlagStore != null && !string.IsNullOrWhiteSpace(flagId)
                ? runFlagStore.GetDaysSinceFirstSet(flagId.Trim(), Day)
                : 0;
        }

        public int GetCustomValue(string valueId)
        {
            return customValueResolver != null && !string.IsNullOrWhiteSpace(valueId)
                ? customValueResolver(valueId.Trim())
                : 0;
        }
    }
}
