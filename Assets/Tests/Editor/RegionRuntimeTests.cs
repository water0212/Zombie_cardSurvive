using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Exploration.Data;
using ZombieCardSurvive.Exploration.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

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
            MoralePyramidProfileData profile = Track(MoralePyramidTestFactory.CreateDemoProfile());
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("startRunOnAwake").boolValue = false;
            serialized.FindProperty("explorationSystem").objectReferenceValue = system;
            serialized.FindProperty("defaultMoraleProfile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            phaseObject.SetActive(true);

            controller.StartRun();
            RegionRuntime rebuilt = system.GetRegionRuntime("REGION-RUN");

            Assert.That(rebuilt, Is.Not.SameAs(original));
            Assert.That(rebuilt.RemainingPackCount, Is.EqualTo(2));
            Assert.That(rebuilt.RemainingZombieCount, Is.EqualTo(1));
            Assert.That(rebuilt.Heat, Is.Zero);
        }

        [Test]
        public void RemainingProportionWeightsMatchGddExample()
        {
            CardPackData commonPack = CreatePack("PACK-COMMON", "Common Pack");
            CardPackData rarePack = CreatePack("PACK-RARE-WEIGHT", "Rare Pack");
            CardPackData ultraRarePack = CreatePack("PACK-ULTRA", "Ultra Rare Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-WEIGHTS",
                "Weight Region",
                0,
                new PackSetup(commonPack, 10, 50),
                new PackSetup(rarePack, 5, 25),
                new PackSetup(ultraRarePack, 1, 10));
            RegionRuntime runtime = new RegionRuntime(region);

            Assert.That(runtime.GetCurrentWeight("PACK-COMMON"), Is.EqualTo(50f).Within(0.0001f));
            Assert.That(runtime.GetCurrentWeight("PACK-RARE-WEIGHT"), Is.EqualTo(25f).Within(0.0001f));
            Assert.That(runtime.GetCurrentWeight("PACK-ULTRA"), Is.EqualTo(10f).Within(0.0001f));

            Assert.That(runtime.TryConsumePack("PACK-COMMON", 8), Is.True);

            Assert.That(runtime.GetCurrentWeight("PACK-COMMON"), Is.EqualTo(10f).Within(0.0001f));
            Assert.That(runtime.GetCurrentWeight("PACK-RARE-WEIGHT"), Is.EqualTo(25f).Within(0.0001f));
            Assert.That(runtime.GetCurrentWeight("PACK-ULTRA"), Is.EqualTo(10f).Within(0.0001f));
            Assert.That(runtime.GetPackProbability("PACK-COMMON"), Is.EqualTo(2f / 9f).Within(0.0001f));
            Assert.That(runtime.GetPackProbability("PACK-RARE-WEIGHT"), Is.EqualTo(5f / 9f).Within(0.0001f));
            Assert.That(runtime.GetPackProbability("PACK-ULTRA"), Is.EqualTo(2f / 9f).Within(0.0001f));
        }

        [Test]
        public void DrawingConsumesOnePackAndStopsAfterAllStockIsExhausted()
        {
            CardPackData commonPack = CreatePack("PACK-DRAW-COMMON", "Draw Common");
            CardPackData rarePack = CreatePack("PACK-DRAW-RARE", "Draw Rare");
            ExplorationRegionData region = CreateRegion(
                "REGION-DRAW",
                "Draw Region",
                0,
                new PackSetup(commonPack, 2, 50),
                new PackSetup(rarePack, 1, 25));
            RegionRuntime runtime = new RegionRuntime(region);
            Dictionary<string, int> drawsByPack = new Dictionary<string, int>();

            while (runtime.TryDrawPack(out RegionPackRuntime selectedPack))
            {
                drawsByPack.TryGetValue(selectedPack.PackId, out int drawCount);
                drawsByPack[selectedPack.PackId] = drawCount + 1;
            }

            Assert.That(drawsByPack["PACK-DRAW-COMMON"], Is.EqualTo(2));
            Assert.That(drawsByPack["PACK-DRAW-RARE"], Is.EqualTo(1));
            Assert.That(runtime.RemainingPackCount, Is.Zero);
            Assert.That(runtime.HasDrawablePacks, Is.False);
            Assert.That(runtime.SearchProgress, Is.EqualTo(1f));
            Assert.That(runtime.ResourceState, Is.EqualTo(RegionResourceState.Depleted));
            Assert.That(runtime.TryDrawPack(out _), Is.False);
        }

        [Test]
        public void RestockingPackReactivatesDepletedRegion()
        {
            CardPackData pack = CreatePack("PACK-RESTOCK", "Restock Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-RESTOCK",
                "Restock Region",
                0,
                new PackSetup(pack, 1, 10));
            ExplorationSystem system = CreateSystem(region);
            system.InitializeRun();
            RegionRuntime runtime = system.GetRegionRuntime("REGION-RESTOCK");
            Assert.That(runtime.TryDrawPack(out _), Is.True);
            Assert.That(runtime.ResourceState, Is.EqualTo(RegionResourceState.Depleted));

            Assert.That(system.TryRestockRegionPack("REGION-RESTOCK", "PACK-RESTOCK", 1), Is.True);

            Assert.That(runtime.RemainingPackCount, Is.EqualTo(1));
            Assert.That(runtime.HasDrawablePacks, Is.True);
            Assert.That(runtime.ResourceState, Is.EqualTo(RegionResourceState.Abundant));
        }

        [Test]
        public void ExplorationOffersExcludeDepletedRegionsAndConsumeSelectedStock()
        {
            CardPackData depletedPack = CreatePack("PACK-OFFER-EMPTY", "Empty Pack");
            CardPackData availablePack = CreatePack("PACK-OFFER-AVAILABLE", "Available Pack");
            ExplorationRegionData depletedRegion = CreateRegion(
                "REGION-OFFER-EMPTY",
                "Empty Region",
                0,
                new PackSetup(depletedPack, 0, 50));
            ExplorationRegionData availableRegion = CreateRegion(
                "REGION-OFFER-AVAILABLE",
                "Available Region",
                0,
                new PackSetup(availablePack, 1, 50));
            ExplorationSystem system = CreateSystem(depletedRegion, availableRegion);
            system.InitializeRun();
            ExplorationCardData explorationCard = CreateExplorationCard();

            LogAssert.Expect(LogType.Warning, "Exploration only found 1 region offers, expected 3.");
            Assert.That(ExplorationSystem.BeginExploration(explorationCard, null), Is.True);

            Assert.That(system.CurrentOffers.Count, Is.EqualTo(1));
            Assert.That(system.CurrentOffers[0], Is.SameAs(availableRegion));
            LogAssert.Expect(LogType.Warning, "Card pack 'Available Pack' generated no cards. Check its card pool.");
            Assert.That(system.ChooseRegion(0), Is.True);
            Assert.That(system.GetRegionRuntime("REGION-OFFER-AVAILABLE").RemainingPackCount, Is.Zero);
            Assert.That(system.LastResult.OpenedPacks, Has.Count.EqualTo(1));
            Assert.That(system.LastResult.OpenedPacks[0], Is.SameAs(availablePack));
        }

        [Test]
        public void ExplorationWithAllRegionsDepletedShowsResultInsteadOfFailing()
        {
            CardPackData pack = CreatePack("PACK-ALL-EMPTY", "All Empty Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-ALL-EMPTY",
                "All Empty Region",
                0,
                new PackSetup(pack, 0, 50));
            ExplorationSystem system = CreateSystem(region);
            system.InitializeRun();
            ExplorationCardData explorationCard = CreateExplorationCard();

            LogAssert.Expect(LogType.Warning, "Exploration only found 0 region offers, expected 3.");
            LogAssert.Expect(LogType.Warning, "Exploration has no valid region offers with remaining supplies.");
            Assert.That(ExplorationSystem.BeginExploration(explorationCard, null), Is.True);

            Assert.That(system.State, Is.EqualTo(ExplorationFlowState.Result));
            Assert.That(system.CurrentOffers, Is.Empty);
            Assert.That(system.LastResult.Messages, Does.Contain("目前沒有尚有物資的可探索地區。"));
        }

        [Test]
        public void EncounterChanceCombinesBaseHeatAndProportionalClearance()
        {
            CardPackData pack = CreatePack("PACK-THREAT-FORMULA", "Threat Formula Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-THREAT-FORMULA",
                "Threat Formula Region",
                4,
                new PackSetup(pack, 1, 10));
            ConfigureThreat(region, 0.4f, 0.2f, 0.1f);
            RegionRuntime runtime = new RegionRuntime(region);

            Assert.That(runtime.EncounterChance, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(runtime.AddHeat(0.2f), Is.True);
            Assert.That(runtime.EncounterChance, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(runtime.TryConsumeZombie(), Is.True);
            Assert.That(runtime.Clearance, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(runtime.ClearanceModifier, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(runtime.EncounterChance, Is.EqualTo(0.5f).Within(0.0001f));

            Assert.That(runtime.TryConsumeZombie(3), Is.True);
            Assert.That(runtime.EncounterChance, Is.Zero);
        }

        [TestCase(0f, DangerLevel.Low)]
        [TestCase(0.20f, DangerLevel.Low)]
        [TestCase(0.2001f, DangerLevel.Medium)]
        [TestCase(0.35f, DangerLevel.Medium)]
        [TestCase(0.3501f, DangerLevel.High)]
        [TestCase(0.60f, DangerLevel.High)]
        [TestCase(0.6001f, DangerLevel.Extreme)]
        [TestCase(1f, DangerLevel.Extreme)]
        public void RelativeDangerLevelUsesStableFuzzyBands(float chance, DangerLevel expected)
        {
            Assert.That(RegionRuntime.ClassifyDanger(chance), Is.EqualTo(expected));
        }

        [Test]
        public void PublicRegionStatusUsesFuzzyLabelsWithoutExactRuntimeValues()
        {
            CardPackData pack = CreatePack("PACK-PUBLIC-INFO", "Public Info Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-PUBLIC-INFO",
                "Public Info Region",
                2,
                new PackSetup(pack, 3, 10));
            ConfigureThreat(region, 0.15f, 0.10f, 0.10f);
            RegionRuntime runtime = new RegionRuntime(region);

            Assert.That(
                ExplorationTextUtility.FormatRegionStatus(runtime.ResourceState, runtime.RelativeDangerLevel),
                Is.EqualTo("物資：充足｜危險：低"));

            Assert.That(runtime.TryConsumePack("PACK-PUBLIC-INFO", 2), Is.True);
            Assert.That(runtime.AddHeat(0.30f), Is.True);
            string status = ExplorationTextUtility.FormatRegionStatus(
                runtime.ResourceState,
                runtime.RelativeDangerLevel);

            Assert.That(status, Is.EqualTo("物資：稀少｜危險：高"));
            Assert.That(status, Does.Not.Contain("1/3"));
            Assert.That(status, Does.Not.Contain("45%"));
        }

        [Test]
        public void DailyHeatDecaySkipsExploredRegionAndAppliesOnFollowingDay()
        {
            CardPackData pack = CreatePack("PACK-HEAT-DECAY", "Heat Decay Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-HEAT-DECAY",
                "Heat Decay Region",
                2,
                new PackSetup(pack, 1, 10));
            ConfigureThreat(region, 0.2f, 0.1f, 0.2f);
            RegionRuntime runtime = new RegionRuntime(region);
            runtime.AddHeat(0.4f);

            runtime.RecordExploration();
            Assert.That(runtime.Heat, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(runtime.ResolveDailyHeatDecay(), Is.False);
            Assert.That(runtime.Heat, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(runtime.WasExploredSinceLastDailyDecay, Is.False);

            Assert.That(runtime.ResolveDailyHeatDecay(), Is.True);
            Assert.That(runtime.Heat, Is.EqualTo(0.3f).Within(0.0001f));
            runtime.ResolveDailyHeatDecay();
            runtime.ResolveDailyHeatDecay();
            Assert.That(runtime.Heat, Is.Zero);
        }

        [Test]
        public void ZombieRiskUsesDynamicChanceAndCannotExceedRemainingStock()
        {
            CardPackData pack = CreatePack("PACK-ZOMBIE-RISK", "Zombie Risk Pack");
            ZombieCardData zombie = CreateZombieCard("Risk Zombie");
            ExplorationRegionData region = CreateRegion(
                "REGION-ZOMBIE-RISK",
                "Zombie Risk Region",
                2,
                new PackSetup(pack, 1, 10));
            ConfigureThreat(region, 1f, 0.1f, 0.1f);
            ConfigureZombieRisk(region, zombie, 3, 0);
            ExplorationSystem system = CreateSystem(region);
            system.InitializeRun();
            CardController controller = CreateCardController();

            LogAssert.Expect(LogType.Warning, "Exploration only found 1 region offers, expected 3.");
            Assert.That(ExplorationSystem.BeginExploration(CreateExplorationCard(), controller), Is.True);
            LogAssert.Expect(LogType.Warning, "Card pack 'Zombie Risk Pack' generated no cards. Check its card pool.");
            Assert.That(system.ChooseRegion(0), Is.True);

            RegionRuntime runtime = system.GetRegionRuntime("REGION-ZOMBIE-RISK");
            Assert.That(controller.DiscardPile, Has.Count.EqualTo(2));
            Assert.That(system.LastResult.ZombieCardsAddedToDiscard, Has.Count.EqualTo(2));
            Assert.That(runtime.RemainingZombieCount, Is.Zero);
            Assert.That(runtime.EncounterChance, Is.Zero);
            Assert.That(runtime.Heat, Is.EqualTo(0.1f).Within(0.0001f));
        }

        [Test]
        public void MissedZombieRiskDoesNotConsumeStockEvenWhenLegacyChanceIsHundredPercent()
        {
            CardPackData pack = CreatePack("PACK-ZOMBIE-MISS", "Zombie Miss Pack");
            ZombieCardData zombie = CreateZombieCard("Miss Zombie");
            ExplorationRegionData region = CreateRegion(
                "REGION-ZOMBIE-MISS",
                "Zombie Miss Region",
                2,
                new PackSetup(pack, 1, 10));
            ConfigureThreat(region, 0f, 0.2f, 0.1f);
            ConfigureZombieRisk(region, zombie, 2, 100);
            ExplorationSystem system = CreateSystem(region);
            system.InitializeRun();
            CardController controller = CreateCardController();

            LogAssert.Expect(LogType.Warning, "Exploration only found 1 region offers, expected 3.");
            ExplorationSystem.BeginExploration(CreateExplorationCard(), controller);
            LogAssert.Expect(LogType.Warning, "Card pack 'Zombie Miss Pack' generated no cards. Check its card pool.");
            system.ChooseRegion(0);

            RegionRuntime runtime = system.GetRegionRuntime("REGION-ZOMBIE-MISS");
            Assert.That(controller.DiscardPile, Is.Empty);
            Assert.That(runtime.RemainingZombieCount, Is.EqualTo(2));
            Assert.That(runtime.Heat, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(runtime.EncounterChance, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void CardPackZombiePollutionConsumesAndRespectsRegionStock()
        {
            CardPackData pack = CreatePack("PACK-ZOMBIE-POLLUTION", "Zombie Pollution Pack");
            ZombieCardData zombie = CreateZombieCard("Pollution Zombie");
            ConfigureZombiePollutionPack(pack, zombie, 3);
            ExplorationRegionData region = CreateRegion(
                "REGION-ZOMBIE-POLLUTION",
                "Zombie Pollution Region",
                2,
                new PackSetup(pack, 1, 10));
            ConfigureThreat(region, 0f, 0f, 0f);
            ExplorationSystem system = CreateSystem(region);
            system.InitializeRun();
            CardController controller = CreateCardController();

            LogAssert.Expect(LogType.Warning, "Exploration only found 1 region offers, expected 3.");
            ExplorationSystem.BeginExploration(CreateExplorationCard(), controller);
            system.ChooseRegion(0);

            Assert.That(controller.DiscardPile, Has.Count.EqualTo(2));
            Assert.That(system.GetRegionRuntime("REGION-ZOMBIE-POLLUTION").RemainingZombieCount, Is.Zero);
            Assert.That(system.LastResult.Messages, Does.Contain("當地殭屍已清空，略過 1 張污染卡。"));
        }

        [Test]
        public void RunPhaseEventResolutionAppliesDailyHeatDecayOnce()
        {
            CardPackData pack = CreatePack("PACK-RUN-HEAT", "Run Heat Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-RUN-HEAT",
                "Run Heat Region",
                2,
                new PackSetup(pack, 1, 10));
            ConfigureThreat(region, 0.2f, 0.1f, 0.2f);
            ExplorationSystem system = CreateSystem(region);
            system.InitializeRun();
            RegionRuntime runtime = system.GetRegionRuntime("REGION-RUN-HEAT");
            runtime.AddHeat(0.5f);

            GameObject phaseObject = Track(new GameObject("RunPhaseHeatTest"));
            phaseObject.SetActive(false);
            RunPhaseController controller = phaseObject.AddComponent<RunPhaseController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("startRunOnAwake").boolValue = false;
            serialized.FindProperty("explorationSystem").objectReferenceValue = system;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            phaseObject.SetActive(true);

            controller.StartRoundFromEvent();
            Assert.That(runtime.Heat, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(controller.RoundIndex, Is.EqualTo(1));
        }

        [Test]
        public void RestockingZombiesReactivatesEncounterChance()
        {
            CardPackData pack = CreatePack("PACK-ZOMBIE-RESTOCK", "Zombie Restock Pack");
            ExplorationRegionData region = CreateRegion(
                "REGION-ZOMBIE-RESTOCK",
                "Zombie Restock Region",
                0,
                new PackSetup(pack, 1, 10));
            ConfigureThreat(region, 0.35f, 0f, 0f);
            ExplorationSystem system = CreateSystem(region);
            system.InitializeRun();
            RegionRuntime runtime = system.GetRegionRuntime("REGION-ZOMBIE-RESTOCK");

            Assert.That(runtime.EncounterChance, Is.Zero);
            Assert.That(system.TryRestockRegionZombies("REGION-ZOMBIE-RESTOCK", 2), Is.True);
            Assert.That(runtime.RemainingZombieCount, Is.EqualTo(2));
            Assert.That(runtime.Clearance, Is.Zero);
            Assert.That(runtime.EncounterChance, Is.EqualTo(0.35f).Within(0.0001f));
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
            pack.name = displayName;
            SetField(pack, "packId", packId);
            SetField(pack, "displayName", displayName);
            return pack;
        }

        private ExplorationCardData CreateExplorationCard()
        {
            ExplorationCardData card = Track(ScriptableObject.CreateInstance<ExplorationCardData>());
            card.name = "Exploration Card";
            return card;
        }

        private ZombieCardData CreateZombieCard(string objectName)
        {
            ZombieCardData card = Track(ScriptableObject.CreateInstance<ZombieCardData>());
            card.name = objectName;
            return card;
        }

        private CardController CreateCardController()
        {
            GameObject gameObject = Track(new GameObject("CardControllerTest"));
            gameObject.SetActive(false);
            CardController controller = gameObject.AddComponent<CardController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("startDemoOnAwake").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            gameObject.SetActive(true);
            return controller;
        }

        private static void ConfigureThreat(
            ExplorationRegionData region,
            float baseEncounterChance,
            float heatPerExploration,
            float dailyHeatDecay)
        {
            SetField(region, "baseEncounterChance", baseEncounterChance);
            SetField(region, "heatPerExploration", heatPerExploration);
            SetField(region, "dailyHeatDecay", dailyHeatDecay);
        }

        private static void ConfigureZombieRisk(
            ExplorationRegionData region,
            ZombieCardData zombie,
            int amount,
            int legacyChancePercent)
        {
            SerializedObject serialized = new SerializedObject(region);
            SerializedProperty risks = serialized.FindProperty("risks");
            risks.arraySize = 1;
            SerializedProperty risk = risks.GetArrayElementAtIndex(0);
            risk.FindPropertyRelative("riskType").enumValueIndex = (int)RegionRiskType.AddZombieCardToDiscard;
            risk.FindPropertyRelative("chancePercent").intValue = legacyChancePercent;
            risk.FindPropertyRelative("amount").intValue = amount;
            risk.FindPropertyRelative("zombieCard").objectReferenceValue = zombie;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureZombiePollutionPack(
            CardPackData pack,
            ZombieCardData zombie,
            int cardsToGenerate)
        {
            SetField(pack, "cardsToGenerate", cardsToGenerate);
            SerializedObject serialized = new SerializedObject(pack);
            SerializedProperty cardPool = serialized.FindProperty("cardPool");
            cardPool.arraySize = 1;
            SerializedProperty entry = cardPool.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("card").objectReferenceValue = zombie;
            entry.FindPropertyRelative("weight").intValue = 1;
            entry.FindPropertyRelative("isZombiePollution").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
