using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Exploration.Data;
using ZombieCardSurvive.Exploration.Runtime;
using ZombieCardSurvive.Threats.Data;
using ZombieCardSurvive.Threats.Runtime;

namespace ZombieCardSurvive.Tests.Editor
{
    public class ThreatRuntimeTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void DayOneWithoutDevelopmentStartsAtWorldThreatTierOne()
        {
            ThreatRuntime runtime = CreateRuntime();

            ThreatSnapshot snapshot = runtime.Capture(new ThreatInputSnapshot(day: 1));

            Assert.That(snapshot.TimeThreat, Is.Zero);
            Assert.That(snapshot.DevelopmentThreat, Is.Zero);
            Assert.That(snapshot.WorldThreat, Is.Zero);
            Assert.That(snapshot.WorldThreatTier, Is.EqualTo(1));
            Assert.That(snapshot.WorldThreatBand, Is.EqualTo(ThreatBand.Low));
            Assert.That(snapshot.BaseAttention, Is.Zero);
        }

        [TestCase(1, 0, 1)]
        [TestCase(15, 1, 1)]
        [TestCase(30, 2, 2)]
        [TestCase(60, 5, 3)]
        [TestCase(90, 8, 5)]
        public void DefaultTimeAnchorsCreateExpectedWorldThreatCurve(
            int day,
            int expectedWorldThreat,
            int expectedTier)
        {
            ThreatRuntime runtime = CreateRuntime();

            ThreatSnapshot snapshot = runtime.Capture(new ThreatInputSnapshot(day));

            Assert.That(snapshot.WorldThreat, Is.EqualTo(expectedWorldThreat));
            Assert.That(snapshot.WorldThreatTier, Is.EqualTo(expectedTier));
        }

        [Test]
        public void DevelopmentPointsIncreaseWorldThreatSeparatelyFromTime()
        {
            ThreatRuntime runtime = CreateRuntime();

            ThreatSnapshot snapshot = runtime.Capture(new ThreatInputSnapshot(
                day: 1,
                additionalSurvivorCount: 1,
                activeBuildingLevelSum: 3,
                permanentSlotUpgradeCount: 1,
                otherPermanentDevelopmentModifiers: 1));

            Assert.That(snapshot.Input.DevelopmentPoints, Is.EqualTo(6));
            Assert.That(snapshot.DevelopmentThreat, Is.EqualTo(2));
            Assert.That(snapshot.WorldThreat, Is.EqualTo(2));
            Assert.That(snapshot.WorldThreatTier, Is.EqualTo(2));
        }

        [Test]
        public void BaseAttentionUsesNoiseEventsAndStealthInsteadOfDevelopmentPoints()
        {
            ThreatRuntime runtime = CreateRuntime();

            ThreatSnapshot quietBase = runtime.Capture(new ThreatInputSnapshot(
                day: 1,
                activeBuildingLevelSum: 9));
            ThreatSnapshot noisyBase = runtime.Capture(new ThreatInputSnapshot(
                day: 1,
                buildingNoise: 2f,
                baseEventAttentionModifier: 1.5f,
                stealthModifier: 0.5f));

            Assert.That(quietBase.BaseAttention, Is.Zero);
            Assert.That(noisyBase.BaseAttention, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(noisyBase.BaseAttentionBand, Is.EqualTo(ThreatBand.Medium));
        }

        [Test]
        public void RegionPressureRisesWithHeatAndFallsWithClearance()
        {
            ThreatRuntime runtime = CreateRuntime();
            RegionRuntime region = new RegionRuntime(CreateRegion(
                "REGION-PRESSURE",
                initialZombies: 4,
                baseEncounterChance: 0.15f,
                heatPerExploration: 0.25f));

            RegionPressureSnapshot initial = runtime.CaptureRegionPressure(region);
            region.RecordExploration();
            RegionPressureSnapshot heated = runtime.CaptureRegionPressure(region);
            region.TryConsumeZombie(2);
            RegionPressureSnapshot halfCleared = runtime.CaptureRegionPressure(region);
            region.TryConsumeZombie(2);
            RegionPressureSnapshot cleared = runtime.CaptureRegionPressure(region);

            Assert.That(initial.Pressure, Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(initial.PressureBand, Is.EqualTo(ThreatBand.Low));
            Assert.That(heated.Pressure, Is.GreaterThan(initial.Pressure));
            Assert.That(heated.PressureBand, Is.EqualTo(ThreatBand.High));
            Assert.That(halfCleared.Pressure, Is.LessThan(heated.Pressure));
            Assert.That(cleared.Pressure, Is.Zero);
            Assert.That(cleared.PressureBand, Is.EqualTo(ThreatBand.Low));
        }

        [Test]
        public void RepeatedExplorationChangesLocalPressureButNotWorldThreatTier()
        {
            ThreatRuntime runtime = CreateRuntime();
            ThreatInputSnapshot input = new ThreatInputSnapshot(day: 30);
            ThreatSnapshot worldBefore = runtime.Capture(input);
            RegionRuntime region = new RegionRuntime(CreateRegion(
                "REGION-LOCAL",
                initialZombies: 3,
                baseEncounterChance: 0.1f,
                heatPerExploration: 0.2f));

            RegionPressureSnapshot localBefore = runtime.CaptureRegionPressure(region);
            region.RecordExploration();
            region.RecordExploration();
            RegionPressureSnapshot localAfter = runtime.CaptureRegionPressure(region);
            ThreatSnapshot worldAfter = runtime.Capture(input);

            Assert.That(localAfter.Pressure, Is.GreaterThan(localBefore.Pressure));
            Assert.That(worldAfter.WorldThreatTier, Is.EqualTo(worldBefore.WorldThreatTier));
        }

        [Test]
        public void ZeroSerializedThresholdsFallBackToDefaults()
        {
            ThreatSettingsSnapshot settings = new ThreatSettingsSnapshot(
                daysPerWorldThreatPoint: 10,
                developmentPointsPerWorldThreatPoint: 3,
                worldThreatPointsPerTier: 2,
                difficultyThreatModifier: 0,
                worldThreatBands: new ThreatBandThresholds(0f, 0f, 0f),
                baseAttention: 0f,
                baseAttentionBands: new ThreatBandThresholds(0f, 0f, 0f),
                regionHeatMultiplier: 1f,
                regionClearanceMultiplier: 1f,
                regionPressureBands: new ThreatBandThresholds(0f, 0f, 0f));

            Assert.That(settings.WorldThreatBands.Classify(0f), Is.EqualTo(ThreatBand.Low));
            Assert.That(settings.BaseAttentionBands.Classify(3f), Is.EqualTo(ThreatBand.Medium));
            Assert.That(settings.RegionPressureBands.Classify(0.4f), Is.EqualTo(ThreatBand.High));
        }

        private ThreatRuntime CreateRuntime()
        {
            ThreatRuntime runtime = new ThreatRuntime();
            runtime.InitializeRun();
            return runtime;
        }

        private ExplorationRegionData CreateRegion(
            string regionId,
            int initialZombies,
            float baseEncounterChance,
            float heatPerExploration)
        {
            ExplorationRegionData region = ScriptableObject.CreateInstance<ExplorationRegionData>();
            createdObjects.Add(region);

            SerializedObject serialized = new SerializedObject(region);
            serialized.FindProperty("regionId").stringValue = regionId;
            serialized.FindProperty("displayName").stringValue = regionId;
            serialized.FindProperty("initialZombieCount").intValue = initialZombies;
            serialized.FindProperty("baseEncounterChance").floatValue = baseEncounterChance;
            serialized.FindProperty("heatPerExploration").floatValue = heatPerExploration;
            serialized.FindProperty("dailyHeatDecay").floatValue = 0.1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return region;
        }
    }
}
