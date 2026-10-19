using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Threats.Data;
using ZombieCardSurvive.Threats.Runtime;
using Random = System.Random;

namespace ZombieCardSurvive.Tests.Editor
{
    public class HordeBudgetRuntimeTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void BudgetFormulaBreaksDownAllSources()
        {
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.Mixed,
                baseBudget: 2,
                worldThreatBudgetPerTier: 3,
                baseAttentionBudgetPerPoint: 2f,
                regionPressureBudgetMultiplier: 10f);
            HordeBudgetRuntime runtime = new HordeBudgetRuntime();

            HordeBudgetSnapshot snapshot = runtime.Capture(
                profile,
                new HordeBudgetInputSnapshot(
                    worldThreatTier: 3,
                    baseAttention: 1.6f,
                    regionPressure: 0.45f,
                    eventBonusBudget: 2));

            Assert.That(snapshot.BaseBudget, Is.EqualTo(2));
            Assert.That(snapshot.WorldThreatBudget, Is.EqualTo(6));
            Assert.That(snapshot.BaseAttentionBudget, Is.EqualTo(3));
            Assert.That(snapshot.RegionPressureBudget, Is.EqualTo(4));
            Assert.That(snapshot.EventBonusBudget, Is.EqualTo(2));
            Assert.That(snapshot.RawBudget, Is.EqualTo(17));
            Assert.That(snapshot.FinalBudget, Is.EqualTo(17));
        }

        [Test]
        public void BudgetRespectsMinimumAndMaximumBounds()
        {
            HordeBudgetRuntime runtime = new HordeBudgetRuntime();
            HordeProfileData minimumProfile = CreateProfile(
                HordeCompositionStyle.Mixed,
                baseBudget: 1,
                minimumBudget: 3);
            HordeProfileData maximumProfile = CreateProfile(
                HordeCompositionStyle.Mixed,
                baseBudget: 1,
                worldThreatBudgetPerTier: 10,
                maximumBudget: 5);

            HordeBudgetSnapshot minimum = runtime.Capture(
                minimumProfile,
                new HordeBudgetInputSnapshot(worldThreatTier: 1, eventBonusBudget: -5));
            HordeBudgetSnapshot maximum = runtime.Capture(
                maximumProfile,
                new HordeBudgetInputSnapshot(worldThreatTier: 4));

            Assert.That(minimum.RawBudget, Is.EqualTo(-4));
            Assert.That(minimum.FinalBudget, Is.EqualTo(3));
            Assert.That(maximum.RawBudget, Is.EqualTo(31));
            Assert.That(maximum.FinalBudget, Is.EqualTo(5));
        }

        [Test]
        public void InputCanBeCreatedFromThreatAndRegionSnapshots()
        {
            ThreatSnapshot threat = new ThreatSnapshot(
                new ThreatInputSnapshot(day: 30),
                timeThreat: 2,
                developmentThreat: 0,
                worldThreat: 2,
                worldThreatTier: 4,
                worldThreatBand: ThreatBand.High,
                baseAttention: 2.5f,
                baseAttentionBand: ThreatBand.Medium);
            RegionPressureSnapshot region = new RegionPressureSnapshot(
                "REGION-TEST",
                basePressure: 0.1f,
                heatContribution: 0.3f,
                clearanceReduction: 0.05f,
                pressure: 0.35f,
                pressureBand: ThreatBand.High);

            HordeBudgetInputSnapshot input = HordeBudgetInputSnapshot.FromThreat(
                threat,
                region,
                eventBonusBudget: 2);

            Assert.That(input.WorldThreatTier, Is.EqualTo(4));
            Assert.That(input.BaseAttention, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(input.RegionPressure, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(input.EventBonusBudget, Is.EqualTo(2));
        }

        [Test]
        public void FixedOrderCompositionSpendsBudgetWithoutOverspending()
        {
            ZombieCardData weak = CreateZombie("Weak", health: 1, attack: 1);
            ZombieCardData brute = CreateZombie("Brute", health: 3, attack: 2);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.FixedOrder,
                baseBudget: 4,
                entries: new[]
                {
                    new ArchetypeSetup(weak, cost: 1),
                    new ArchetypeSetup(brute, cost: 3)
                });
            HordeBudgetSnapshot budget = CreateBudget(profile);

            HordeCompositionResult result = new HordeCompositionRuntime().Generate(
                profile,
                budget,
                new Random(123));

            Assert.That(result.SpawnCount, Is.EqualTo(2));
            Assert.That(result.Entries[0].ZombieCard, Is.SameAs(weak));
            Assert.That(result.Entries[1].ZombieCard, Is.SameAs(brute));
            Assert.That(result.SpentBudget, Is.EqualTo(4));
            Assert.That(result.RemainingBudget, Is.Zero);
            Assert.That(result.StopReason, Is.EqualTo(HordeCompositionStopReason.BudgetSpent));
        }

        [Test]
        public void WorldThreatTierFiltersHighTierZombies()
        {
            ZombieCardData elite = CreateZombie("Elite", health: 5, attack: 3);
            ZombieCardData weak = CreateZombie("Weak", health: 1, attack: 1);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.FixedOrder,
                baseBudget: 6,
                entries: new[]
                {
                    new ArchetypeSetup(elite, cost: 2, minTier: 3, maxCount: 1),
                    new ArchetypeSetup(weak, cost: 1, maxCount: 1)
                });

            HordeCompositionRuntime runtime = new HordeCompositionRuntime();
            HordeCompositionResult lowTier = runtime.Generate(
                profile,
                CreateBudget(profile, worldThreatTier: 1),
                new Random(1));
            HordeCompositionResult highTier = runtime.Generate(
                profile,
                CreateBudget(profile, worldThreatTier: 3),
                new Random(1));

            Assert.That(lowTier.SpawnCount, Is.EqualTo(1));
            Assert.That(lowTier.Entries[0].ZombieCard, Is.SameAs(weak));
            Assert.That(highTier.SpawnCount, Is.EqualTo(2));
            Assert.That(highTier.Entries[0].ZombieCard, Is.SameAs(elite));
            Assert.That(highTier.Entries[1].ZombieCard, Is.SameAs(weak));
        }

        [Test]
        public void MaxCountPerHordeIsRespected()
        {
            ZombieCardData weak = CreateZombie("Weak", health: 1, attack: 1);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.FixedOrder,
                baseBudget: 10,
                entries: new[]
                {
                    new ArchetypeSetup(weak, cost: 1, maxCount: 2)
                });

            HordeCompositionResult result = new HordeCompositionRuntime().Generate(
                profile,
                CreateBudget(profile),
                new Random(1));

            Assert.That(result.SpawnCount, Is.EqualTo(2));
            Assert.That(result.SpentBudget, Is.EqualTo(2));
            Assert.That(result.RemainingBudget, Is.EqualTo(8));
            Assert.That(result.StopReason, Is.EqualTo(HordeCompositionStopReason.NoEligibleZombie));
        }

        [Test]
        public void BudgetTooLowWhenEligibleZombiesCostTooMuch()
        {
            ZombieCardData brute = CreateZombie("Brute", health: 3, attack: 2);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.Mixed,
                baseBudget: 4,
                entries: new[]
                {
                    new ArchetypeSetup(brute, cost: 5)
                });

            HordeCompositionResult result = new HordeCompositionRuntime().Generate(
                profile,
                CreateBudget(profile),
                new Random(1));

            Assert.That(result.SpawnCount, Is.Zero);
            Assert.That(result.SpentBudget, Is.Zero);
            Assert.That(result.RemainingBudget, Is.EqualTo(4));
            Assert.That(result.StopReason, Is.EqualTo(HordeCompositionStopReason.BudgetTooLow));
        }

        [Test]
        public void SameSeedProducesStableWeightedComposition()
        {
            ZombieCardData weak = CreateZombie("Weak", health: 1, attack: 1);
            ZombieCardData brute = CreateZombie("Brute", health: 3, attack: 2);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.Mixed,
                baseBudget: 8,
                entries: new[]
                {
                    new ArchetypeSetup(weak, cost: 1, weight: 10),
                    new ArchetypeSetup(brute, cost: 3, weight: 10)
                });
            HordeBudgetSnapshot budget = CreateBudget(profile);
            HordeCompositionRuntime runtime = new HordeCompositionRuntime();

            HordeCompositionResult first = runtime.Generate(profile, budget, new Random(12345));
            HordeCompositionResult second = runtime.Generate(profile, budget, new Random(12345));

            Assert.That(second.SpawnCount, Is.EqualTo(first.SpawnCount));
            for (int index = 0; index < first.SpawnCount; index++)
            {
                Assert.That(second.Entries[index].ZombieCard, Is.SameAs(first.Entries[index].ZombieCard));
                Assert.That(second.Entries[index].BudgetCost, Is.EqualTo(first.Entries[index].BudgetCost));
            }
        }

        [Test]
        public void GeneratedEntriesKeepZombieCardStats()
        {
            ZombieCardData brute = CreateZombie("Brute", health: 4, attack: 3);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.FixedOrder,
                baseBudget: 4,
                entries: new[]
                {
                    new ArchetypeSetup(brute, cost: 4)
                });

            HordeCompositionResult result = new HordeCompositionRuntime().Generate(
                profile,
                CreateBudget(profile),
                new Random(1));

            Assert.That(result.SpawnCount, Is.EqualTo(1));
            Assert.That(result.Entries[0].ZombieCard.Health, Is.EqualTo(4));
            Assert.That(result.Entries[0].ZombieCard.Attack, Is.EqualTo(3));
            Assert.That(result.Entries[0].BudgetCost, Is.EqualTo(4));
        }

        private HordeBudgetSnapshot CreateBudget(
            HordeProfileData profile,
            int worldThreatTier = 1)
        {
            return new HordeBudgetRuntime().Capture(
                profile,
                new HordeBudgetInputSnapshot(worldThreatTier));
        }

        private ZombieCardData CreateZombie(string name, int health, int attack)
        {
            ZombieCardData zombie = ScriptableObject.CreateInstance<ZombieCardData>();
            zombie.name = name;
            createdObjects.Add(zombie);

            SerializedObject serialized = new SerializedObject(zombie);
            serialized.FindProperty("health").intValue = health;
            serialized.FindProperty("attack").intValue = attack;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return zombie;
        }

        private HordeProfileData CreateProfile(
            HordeCompositionStyle style,
            int baseBudget,
            int worldThreatBudgetPerTier = 0,
            float baseAttentionBudgetPerPoint = 0f,
            float regionPressureBudgetMultiplier = 0f,
            int minimumBudget = 0,
            int maximumBudget = 0,
            params ArchetypeSetup[] entries)
        {
            HordeProfileData profile = ScriptableObject.CreateInstance<HordeProfileData>();
            profile.name = "TEST-HORDE";
            createdObjects.Add(profile);

            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = Guid.NewGuid().ToString("N");
            serialized.FindProperty("displayName").stringValue = "Test Horde";
            serialized.FindProperty("compositionStyle").enumValueIndex = (int)style;
            serialized.FindProperty("baseBudget").intValue = baseBudget;
            serialized.FindProperty("worldThreatBudgetPerTier").intValue = worldThreatBudgetPerTier;
            serialized.FindProperty("baseAttentionBudgetPerPoint").floatValue = baseAttentionBudgetPerPoint;
            serialized.FindProperty("regionPressureBudgetMultiplier").floatValue = regionPressureBudgetMultiplier;
            serialized.FindProperty("minimumBudget").intValue = minimumBudget;
            serialized.FindProperty("maximumBudget").intValue = maximumBudget;

            SerializedProperty pool = serialized.FindProperty("zombiePool");
            pool.arraySize = entries != null ? entries.Length : 0;
            for (int index = 0; index < pool.arraySize; index++)
            {
                ArchetypeSetup setup = entries[index];
                SerializedProperty element = pool.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("zombieCard").objectReferenceValue = setup.Zombie;
                element.FindPropertyRelative("budgetCost").intValue = setup.Cost;
                element.FindPropertyRelative("baseWeight").intValue = setup.Weight;
                element.FindPropertyRelative("minWorldThreatTier").intValue = setup.MinTier;
                element.FindPropertyRelative("maxWorldThreatTier").intValue = setup.MaxTier;
                element.FindPropertyRelative("maxCountPerHorde").intValue = setup.MaxCount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private readonly struct ArchetypeSetup
        {
            public ArchetypeSetup(
                ZombieCardData zombie,
                int cost,
                int weight = 10,
                int minTier = 1,
                int maxTier = 0,
                int maxCount = 0)
            {
                Zombie = zombie;
                Cost = cost;
                Weight = weight;
                MinTier = minTier;
                MaxTier = maxTier;
                MaxCount = maxCount;
            }

            public ZombieCardData Zombie { get; }
            public int Cost { get; }
            public int Weight { get; }
            public int MinTier { get; }
            public int MaxTier { get; }
            public int MaxCount { get; }
        }
    }
}
