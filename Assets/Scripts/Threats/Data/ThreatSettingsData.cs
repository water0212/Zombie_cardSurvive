using UnityEngine;

namespace ZombieCardSurvive.Threats.Data
{
    [CreateAssetMenu(fileName = "ThreatSettings", menuName = "Zombie Card Survive/Threats/Threat Settings")]
    public sealed class ThreatSettingsData : ScriptableObject
    {
        [Header("World Threat")]
        [Min(1)]
        [Tooltip("Complete days needed to add one hidden world-threat point.")]
        [SerializeField] private int daysPerWorldThreatPoint = 10;
        [Min(1)]
        [Tooltip("Permanent development points needed to add one hidden world-threat point.")]
        [SerializeField] private int developmentPointsPerWorldThreatPoint = 3;
        [Min(1)]
        [Tooltip("Hidden world-threat points needed to advance one public scale tier.")]
        [SerializeField] private int worldThreatPointsPerTier = 2;
        [SerializeField] private int difficultyThreatModifier;
        [SerializeField] private ThreatBandThresholds worldThreatBands = ThreatBandThresholds.DefaultScore;

        [Header("Base Attention")]
        [Min(0f)]
        [SerializeField] private float baseAttention;
        [SerializeField] private ThreatBandThresholds baseAttentionBands = ThreatBandThresholds.DefaultScore;

        [Header("Region Pressure")]
        [Min(0f)]
        [Tooltip("How strongly repeated exploration heat raises local encounter pressure.")]
        [SerializeField] private float regionHeatMultiplier = 1f;
        [Min(0f)]
        [Tooltip("How strongly local clearance lowers local encounter pressure.")]
        [SerializeField] private float regionClearanceMultiplier = 1f;
        [SerializeField] private ThreatBandThresholds regionPressureBands = ThreatBandThresholds.DefaultRegionPressure;

        public ThreatSettingsSnapshot CreateSnapshot()
        {
            return new ThreatSettingsSnapshot(
                daysPerWorldThreatPoint,
                developmentPointsPerWorldThreatPoint,
                worldThreatPointsPerTier,
                difficultyThreatModifier,
                worldThreatBands,
                baseAttention,
                baseAttentionBands,
                regionHeatMultiplier,
                regionClearanceMultiplier,
                regionPressureBands);
        }
    }

    public readonly struct ThreatSettingsSnapshot
    {
        public static ThreatSettingsSnapshot Default => new ThreatSettingsSnapshot(
            10,
            3,
            2,
            0,
            ThreatBandThresholds.DefaultScore,
            0f,
            ThreatBandThresholds.DefaultScore,
            1f,
            1f,
            ThreatBandThresholds.DefaultRegionPressure);

        public ThreatSettingsSnapshot(
            int daysPerWorldThreatPoint,
            int developmentPointsPerWorldThreatPoint,
            int worldThreatPointsPerTier,
            int difficultyThreatModifier,
            ThreatBandThresholds worldThreatBands,
            float baseAttention,
            ThreatBandThresholds baseAttentionBands,
            float regionHeatMultiplier,
            float regionClearanceMultiplier,
            ThreatBandThresholds regionPressureBands)
        {
            DaysPerWorldThreatPoint = Mathf.Max(1, daysPerWorldThreatPoint);
            DevelopmentPointsPerWorldThreatPoint = Mathf.Max(1, developmentPointsPerWorldThreatPoint);
            WorldThreatPointsPerTier = Mathf.Max(1, worldThreatPointsPerTier);
            DifficultyThreatModifier = difficultyThreatModifier;
            WorldThreatBands = worldThreatBands.Sanitized(ThreatBandThresholds.DefaultScore);
            BaseAttention = Mathf.Max(0f, baseAttention);
            BaseAttentionBands = baseAttentionBands.Sanitized(ThreatBandThresholds.DefaultScore);
            RegionHeatMultiplier = Mathf.Max(0f, regionHeatMultiplier);
            RegionClearanceMultiplier = Mathf.Max(0f, regionClearanceMultiplier);
            RegionPressureBands = regionPressureBands.Sanitized(ThreatBandThresholds.DefaultRegionPressure);
        }

        public int DaysPerWorldThreatPoint { get; }
        public int DevelopmentPointsPerWorldThreatPoint { get; }
        public int WorldThreatPointsPerTier { get; }
        public int DifficultyThreatModifier { get; }
        public ThreatBandThresholds WorldThreatBands { get; }
        public float BaseAttention { get; }
        public ThreatBandThresholds BaseAttentionBands { get; }
        public float RegionHeatMultiplier { get; }
        public float RegionClearanceMultiplier { get; }
        public ThreatBandThresholds RegionPressureBands { get; }
    }
}
