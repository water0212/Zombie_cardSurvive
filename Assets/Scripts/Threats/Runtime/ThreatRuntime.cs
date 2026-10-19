using System;
using UnityEngine;
using ZombieCardSurvive.Exploration.Runtime;
using ZombieCardSurvive.Threats.Data;

namespace ZombieCardSurvive.Threats.Runtime
{
    public sealed class ThreatRuntime
    {
        private ThreatSettingsSnapshot settings = ThreatSettingsSnapshot.Default;

        public ThreatSnapshot LastSnapshot { get; private set; }

        public void InitializeRun(ThreatSettingsData settingsData = null)
        {
            settings = settingsData != null
                ? settingsData.CreateSnapshot()
                : ThreatSettingsSnapshot.Default;
            LastSnapshot = null;
        }

        public ThreatSnapshot Capture(ThreatInputSnapshot input)
        {
            int timeThreat = (input.Day - 1) / settings.DaysPerWorldThreatPoint;
            int developmentThreat = input.DevelopmentPoints / settings.DevelopmentPointsPerWorldThreatPoint;
            int worldThreat = Mathf.Max(
                0,
                timeThreat
                + developmentThreat
                + settings.DifficultyThreatModifier
                + input.StoryThreatModifier);
            int worldThreatTier = 1 + worldThreat / settings.WorldThreatPointsPerTier;

            float baseAttention = Mathf.Max(
                0f,
                settings.BaseAttention
                + input.BuildingNoise
                + input.BaseEventAttentionModifier
                - input.StealthModifier);

            LastSnapshot = new ThreatSnapshot(
                input,
                timeThreat,
                developmentThreat,
                worldThreat,
                worldThreatTier,
                settings.WorldThreatBands.Classify(worldThreat),
                baseAttention,
                settings.BaseAttentionBands.Classify(baseAttention));
            return LastSnapshot;
        }

        public RegionPressureSnapshot CaptureRegionPressure(
            RegionRuntime region,
            float localPressureModifier = 0f)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            if (region.RemainingZombieCount <= 0)
            {
                return new RegionPressureSnapshot(
                    region.RegionId,
                    region.Data.BaseEncounterChance,
                    0f,
                    region.Data.BaseEncounterChance,
                    0f,
                    settings.RegionPressureBands.Classify(0f));
            }

            float basePressure = region.Data.BaseEncounterChance;
            float heatContribution = region.Heat * settings.RegionHeatMultiplier;
            float clearanceReduction = basePressure
                * region.Clearance
                * settings.RegionClearanceMultiplier;
            float pressure = Mathf.Clamp01(
                basePressure
                + heatContribution
                + localPressureModifier
                - clearanceReduction);

            return new RegionPressureSnapshot(
                region.RegionId,
                basePressure,
                heatContribution,
                clearanceReduction,
                pressure,
                settings.RegionPressureBands.Classify(pressure));
        }
    }
}
