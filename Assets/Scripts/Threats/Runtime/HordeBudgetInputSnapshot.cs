using UnityEngine;

namespace ZombieCardSurvive.Threats.Runtime
{
    public readonly struct HordeBudgetInputSnapshot
    {
        public HordeBudgetInputSnapshot(
            int worldThreatTier,
            float baseAttention = 0f,
            float regionPressure = 0f,
            int eventBonusBudget = 0)
        {
            WorldThreatTier = Mathf.Max(1, worldThreatTier);
            BaseAttention = Mathf.Max(0f, baseAttention);
            RegionPressure = Mathf.Clamp01(regionPressure);
            EventBonusBudget = eventBonusBudget;
        }

        public int WorldThreatTier { get; }
        public float BaseAttention { get; }
        public float RegionPressure { get; }
        public int EventBonusBudget { get; }

        public static HordeBudgetInputSnapshot FromThreat(
            ThreatSnapshot threat,
            RegionPressureSnapshot regionPressure = null,
            int eventBonusBudget = 0)
        {
            return new HordeBudgetInputSnapshot(
                threat != null ? threat.WorldThreatTier : 1,
                threat != null ? threat.BaseAttention : 0f,
                regionPressure != null ? regionPressure.Pressure : 0f,
                eventBonusBudget);
        }
    }
}
