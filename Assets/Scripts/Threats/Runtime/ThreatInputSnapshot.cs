using System;

namespace ZombieCardSurvive.Threats.Runtime
{
    public readonly struct ThreatInputSnapshot
    {
        public ThreatInputSnapshot(
            int day,
            int additionalSurvivorCount = 0,
            int activeBuildingLevelSum = 0,
            int permanentSlotUpgradeCount = 0,
            int otherPermanentDevelopmentModifiers = 0,
            int storyThreatModifier = 0,
            float buildingNoise = 0f,
            float baseEventAttentionModifier = 0f,
            float stealthModifier = 0f)
        {
            Day = Math.Max(1, day);
            AdditionalSurvivorCount = Math.Max(0, additionalSurvivorCount);
            ActiveBuildingLevelSum = Math.Max(0, activeBuildingLevelSum);
            PermanentSlotUpgradeCount = Math.Max(0, permanentSlotUpgradeCount);
            OtherPermanentDevelopmentModifiers = Math.Max(0, otherPermanentDevelopmentModifiers);
            StoryThreatModifier = storyThreatModifier;
            BuildingNoise = Math.Max(0f, buildingNoise);
            BaseEventAttentionModifier = baseEventAttentionModifier;
            StealthModifier = Math.Max(0f, stealthModifier);
        }

        public int Day { get; }
        public int AdditionalSurvivorCount { get; }
        public int ActiveBuildingLevelSum { get; }
        public int PermanentSlotUpgradeCount { get; }
        public int OtherPermanentDevelopmentModifiers { get; }
        public int StoryThreatModifier { get; }
        public float BuildingNoise { get; }
        public float BaseEventAttentionModifier { get; }
        public float StealthModifier { get; }

        public int DevelopmentPoints => AdditionalSurvivorCount
            + ActiveBuildingLevelSum
            + PermanentSlotUpgradeCount
            + OtherPermanentDevelopmentModifiers;
    }
}
