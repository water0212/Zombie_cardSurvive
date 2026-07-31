using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using ZombieCardSurvive.Exploration.Data;
using ZombieCardSurvive.Exploration.Runtime;
using ZombieCardSurvive.Run;

namespace ZombieCardSurvive.Tests.Editor
{
    public class RegionRuntimeTests
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
        public void RuntimeCopiesInitialDataWithoutMutatingAssets()
        {
            CardPackData foodPack = CreatePack("PACK-FOOD", "Food Pack");
            CardPackData rarePack = CreatePack("PACK-RARE", "Rare Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-001",
                "Market",
                3,
                new PackSetup(foodPack, 4, 70),
                new PackSetup(rarePack, 2, 30));

            RegionRuntime runtime = new RegionRuntime(region);

            Assert.That(runtime.RegionId, Is.EqualTo("REGION-001"));
            Assert.That(runtime.InitialPackCount, Is.EqualTo(6));
            Assert.That(runtime.RemainingPackCount, Is.EqualTo(6));
            Assert.That(runtime.InitialZombieCount, Is.EqualTo(3));
            Assert.That(runtime.RemainingZombieCount, Is.EqualTo(3));
            Assert.That(runtime.Heat, Is.Zero);

            Assert.That(runtime.TryConsumePack("PACK-FOOD"), Is.True);
            Assert.That(runtime.TryConsumeZombie(), Is.True);
            Assert.That(runtime.AddHeat(0.25f), Is.True);

            Assert.That(runtime.RemainingPackCount, Is.EqualTo(5));
            Assert.That(runtime.RemainingZombieCount, Is.EqualTo(2));
            Assert.That(runtime.Heat, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(region.InitialZombieCount, Is.EqualTo(3));
            Assert.That(region.CardPackDrops[0].InitialCount, Is.EqualTo(4));
            Assert.That(region.CardPackDrops[1].InitialCount, Is.EqualTo(2));
        }

        [Test]
        public void TwoRegionRuntimesKeepIndependentState()
        {
            CardPackData sharedPack = CreatePack("PACK-SHARED", "Shared Pack");
            ExplorationRegionData firstRegion = CreateRegion(
                "REGION-A",
                "First",
                2,
                new PackSetup(sharedPack, 3, 10));
            ExplorationRegionData secondRegion = CreateRegion(
                "REGION-B",
                "Second",
                4,
                new PackSetup(sharedPack, 3, 10));
            ExplorationSystem system = CreateSystem(firstRegion, secondRegion);

            system.InitializeRun();
            RegionRuntime first = system.GetRegionRuntime("REGION-A");
            RegionRuntime second = system.GetRegionRuntime("REGION-B");

            Assert.That(first.TryConsumePack("PACK-SHARED"), Is.True);
            Assert.That(first.TryConsumeZombie(), Is.True);
            Assert.That(first.AddHeat(0.4f), Is.True);

            Assert.That(first.RemainingPackCount, Is.EqualTo(2));
            Assert.That(first.RemainingZombieCount, Is.EqualTo(1));
            Assert.That(second.RemainingPackCount, Is.EqualTo(3));
            Assert.That(second.RemainingZombieCount, Is.EqualTo(4));
            Assert.That(second.Heat, Is.Zero);
        }

        [Test]
        public void InitializeRunCreatesFreshRegionState()
        {
            CardPackData pack = CreatePack("PACK-RESET", "Reset Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-RESET",
                "Reset Region",
                2,
                new PackSetup(pack, 2, 10));
            ExplorationSystem system = CreateSystem(region);
            system.InitializeRun();
            RegionRuntime original = system.GetRegionRuntime("REGION-RESET");
            original.TryConsumePack("PACK-RESET");
            original.TryConsumeZombie();
            original.AddHeat(0.5f);

            system.InitializeRun();
            RegionRuntime rebuilt = system.GetRegionRuntime("REGION-RESET");

            Assert.That(rebuilt, Is.Not.SameAs(original));
            Assert.That(rebuilt.RemainingPackCount, Is.EqualTo(2));
            Assert.That(rebuilt.RemainingZombieCount, Is.EqualTo(2));
            Assert.That(rebuilt.Heat, Is.Zero);
        }

        [Test]
        public void HideDoesNotResetRegionState()
        {
            CardPackData pack = CreatePack("PACK-HIDE", "Hide Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-HIDE",
                "Hide Region",
                1,
                new PackSetup(pack, 2, 10));
            ExplorationSystem system = CreateSystem(region);
            system.InitializeRun();
            RegionRuntime runtime = system.GetRegionRuntime("REGION-HIDE");
            runtime.TryConsumePack("PACK-HIDE");

            system.Hide();

            Assert.That(system.GetRegionRuntime("REGION-HIDE"), Is.SameAs(runtime));
            Assert.That(runtime.RemainingPackCount, Is.EqualTo(1));
        }

        [Test]
        public void ZeroInitialCountsAreAlreadyCompleteAndDoNotDivideByZero()
        {
            CardPackData pack = CreatePack("PACK-EMPTY", "Empty Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-EMPTY",
                "Empty Region",
                0,
                new PackSetup(pack, 0, 10));

            RegionRuntime runtime = new RegionRuntime(region);

            Assert.That(runtime.HasRemainingPacks, Is.False);
            Assert.That(runtime.SearchProgress, Is.EqualTo(1f));
            Assert.That(runtime.Clearance, Is.EqualTo(1f));
            Assert.That(float.IsNaN(runtime.SearchProgress), Is.False);
            Assert.That(float.IsNaN(runtime.Clearance), Is.False);
        }

        [Test]
        public void MissingAndDuplicateRegionIdsAreSkippedWithWarnings()
        {
            CardPackData pack = CreatePack("PACK-VALID", "Valid Pack");
            ExplorationRegionData missing = CreateRegion(
                string.Empty,
                "Missing",
                0,
                new PackSetup(pack, 1, 10));
            ExplorationRegionData first = CreateRegion(
                "REGION-DUPLICATE",
                "First",
                0,
                new PackSetup(pack, 1, 10));
            ExplorationRegionData duplicate = CreateRegion(
                "REGION-DUPLICATE",
                "Duplicate",
                0,
                new PackSetup(pack, 1, 10));
            ExplorationSystem system = CreateSystem(missing, first, duplicate);

            LogAssert.Expect(LogType.Warning, "Exploration region 'Missing' was skipped because RegionId is missing.");
            LogAssert.Expect(
                LogType.Warning,
                "Exploration region 'Duplicate' was skipped because RegionId 'REGION-DUPLICATE' is duplicated. The first entry was kept.");

            system.InitializeRun();

            Assert.That(system.RegionRuntimes.Count, Is.EqualTo(1));
            Assert.That(system.GetRegionRuntime("REGION-DUPLICATE").Data, Is.SameAs(first));
        }

        [Test]
        public void DuplicatePackIdsKeepFirstEntryAndWarn()
        {
            CardPackData firstPack = CreatePack("PACK-DUPLICATE", "First Pack");
            CardPackData duplicatePack = CreatePack("PACK-DUPLICATE", "Duplicate Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-PACK-DUPLICATE",
                "Duplicate Pack Region",
                0,
                new PackSetup(firstPack, 2, 10),
                new PackSetup(duplicatePack, 5, 20));

            LogAssert.Expect(
                LogType.Warning,
                "Region 'Duplicate Pack Region' skipped duplicate PackId 'PACK-DUPLICATE'. The first entry was kept.");

            RegionRuntime runtime = new RegionRuntime(region);

            Assert.That(runtime.PackStocks.Count, Is.EqualTo(1));
            Assert.That(runtime.InitialPackCount, Is.EqualTo(2));
            Assert.That(runtime.PackStocks["PACK-DUPLICATE"].Data, Is.SameAs(firstPack));
        }

        [Test]
        public void RuntimeMutationRaisesRegionStateChanged()
        {
            CardPackData pack = CreatePack("PACK-EVENT", "Event Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-EVENT",
                "Event Region",
                1,
                new PackSetup(pack, 1, 10));
            ExplorationSystem system = CreateSystem(region);
            system.InitializeRun();
            RegionRuntime received = null;
            system.RegionStateChanged += runtime => received = runtime;

            RegionRuntime target = system.GetRegionRuntime("REGION-EVENT");
            target.AddHeat(0.1f);

            Assert.That(received, Is.SameAs(target));
        }

        [Test]
        public void RunPhaseStartRunRebuildsExplorationRuntime()
        {
            CardPackData pack = CreatePack("PACK-RUN", "Run Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-RUN",
                "Run Region",
                1,
                new PackSetup(pack, 2, 10));
            ExplorationSystem system = CreateSystem(region);
            system.InitializeRun();
            RegionRuntime original = system.GetRegionRuntime("REGION-RUN");
            original.TryConsumePack("PACK-RUN");

            GameObject phaseObject = Track(new GameObject("RunPhaseControllerTest"));
            phaseObject.SetActive(false);
            RunPhaseController controller = phaseObject.AddComponent<RunPhaseController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("startRunOnAwake").boolValue = false;
            serialized.FindProperty("explorationSystem").objectReferenceValue = system;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            phaseObject.SetActive(true);

            controller.StartRun();
            RegionRuntime rebuilt = system.GetRegionRuntime("REGION-RUN");

            Assert.That(rebuilt, Is.Not.SameAs(original));
            Assert.That(rebuilt.RemainingPackCount, Is.EqualTo(2));
            Assert.That(rebuilt.RemainingZombieCount, Is.EqualTo(1));
            Assert.That(rebuilt.Heat, Is.Zero);
        }

        private ExplorationSystem CreateSystem(params ExplorationRegionData[] regions)
        {
            GameObject gameObject = Track(new GameObject("ExplorationSystemTest"));
            ExplorationSystem system = gameObject.AddComponent<ExplorationSystem>();
            SerializedObject serialized = new SerializedObject(system);
            SerializedProperty pool = serialized.FindProperty("regionPool");
            pool.arraySize = regions.Length;
            for (int index = 0; index < regions.Length; index++)
            {
                pool.GetArrayElementAtIndex(index).objectReferenceValue = regions[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return system;
        }

        private ExplorationRegionData CreateRegion(
            string regionId,
            string displayName,
            int initialZombieCount,
            params PackSetup[] packs)
        {
            ExplorationRegionData region = Track(ScriptableObject.CreateInstance<ExplorationRegionData>());
            SetField(region, "regionId", regionId);
            SetField(region, "displayName", displayName);
            SetField(region, "initialZombieCount", initialZombieCount);

            SerializedObject serialized = new SerializedObject(region);
            SerializedProperty drops = serialized.FindProperty("cardPackDrops");
            drops.arraySize = packs.Length;
            for (int index = 0; index < packs.Length; index++)
            {
                SerializedProperty entry = drops.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("cardPack").objectReferenceValue = packs[index].Pack;
                entry.FindPropertyRelative("initialCount").intValue = packs[index].InitialCount;
                entry.FindPropertyRelative("baseWeight").intValue = packs[index].BaseWeight;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetField(region, "regionId", regionId);
            return region;
        }

        private CardPackData CreatePack(string packId, string displayName)
        {
            CardPackData pack = Track(ScriptableObject.CreateInstance<CardPackData>());
            SetField(pack, "packId", packId);
            SetField(pack, "displayName", displayName);
            return pack;
        }

        private T Track<T>(T target) where T : UnityEngine.Object
        {
            createdObjects.Add(target);
            return target;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().Name, fieldName);
            }

            field.SetValue(target, value);
        }

        private readonly struct PackSetup
        {
            public PackSetup(CardPackData pack, int initialCount, int baseWeight)
            {
                Pack = pack;
                InitialCount = initialCount;
                BaseWeight = baseWeight;
            }

            public CardPackData Pack { get; }
            public int InitialCount { get; }
            public int BaseWeight { get; }
        }
    }
}
