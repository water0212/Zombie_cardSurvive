using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class MoraleSettlementTests
    {
        [SetUp]
        public void SetUp()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(0);
            FoodSystem.SetStorageCapacity(50);
            EnergySystem.Reset(3);
            CombatSystem.Reset();
            MoraleSystem.Reset();
            FoodPlanSystem.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(0);
            EnergySystem.Reset(0);
            CombatSystem.Reset();
            MoraleSystem.Reset();
            FoodPlanSystem.Reset();
        }

        [Test]
        public void FoodShortage_UsesThreeMoralePerFood_AndCapsAtFifteen()
        {
            FoodPlanSystem.Reset(shortagePenaltyPerFood: 3, shortageLossCap: 15);

            Assert.That(FoodPlanSystem.GetShortageMoraleLoss(0), Is.Zero);
            Assert.That(FoodPlanSystem.GetShortageMoraleLoss(1), Is.EqualTo(3));
            Assert.That(FoodPlanSystem.GetShortageMoraleLoss(5), Is.EqualTo(15));
            Assert.That(FoodPlanSystem.GetShortageMoraleLoss(100), Is.EqualTo(15));
        }

        [Test]
        public void ExistingRoundEndResolver_MigratesPreviousSettlementValuesToV2()
        {
            GameObject owner = new GameObject("MoraleSettlementMigrationTest");
            RoundEndResolver resolver = owner.AddComponent<RoundEndResolver>();
            SerializedObject serialized = new SerializedObject(resolver);
            serialized.FindProperty("moraleSettlementConfigVersion").intValue = 1;
            serialized.FindProperty("shortageMoralePenaltyPerFood").intValue = 5;
            serialized.FindProperty("encouragingMoraleGain").intValue = 3;
            serialized.FindProperty("highMoraleDecayPerRound").intValue = 5;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                resolver.PrepareRun();

                Assert.That(FoodPlanSystem.ShortageMoralePenaltyPerFood, Is.EqualTo(3));
                Assert.That(FoodPlanSystem.ShortageMoraleLossCap, Is.EqualTo(15));
                Assert.That(FoodPlanSystem.EncouragingMoraleGain, Is.EqualTo(5));
                Assert.That(MoraleSystem.HighMoraleDecayPerRound, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void SystemDefaults_UseMealFiveAndDriftThree()
        {
            FoodPlanSystem.Reset();
            MoraleSystem.Reset();

            Assert.That(FoodPlanSystem.EncouragingMoraleGain, Is.EqualTo(5));
            Assert.That(MoraleSystem.HighMoraleDecayPerRound, Is.EqualTo(3));
        }

        [Test]
        public void LegacyTightPlan_NormalizesToStandardSupply()
        {
            FoodPlanSystem.Reset(FoodPlanLevel.Tight, baseConsumption: 5);

            Assert.That(FoodPlanSystem.CurrentPlan, Is.EqualTo(FoodPlanLevel.Normal));
            Assert.That(FoodPlanSystem.GetRoundFoodConsumption(), Is.EqualTo(5));
            Assert.That(FoodPlanSystem.GetDisplayName(FoodPlanLevel.Tight), Is.EqualTo("標準供應"));

            Assert.That(FoodPlanSystem.TrySetPendingPlan(FoodPlanLevel.Tight), Is.True);
            Assert.That(FoodPlanSystem.PendingPlan, Is.EqualTo(FoodPlanLevel.Normal));
            Assert.That(FoodPlanSystem.HasPendingPlan, Is.False);
        }

        [Test]
        public void EncouragingMealFormula_UsesCeiledDemandMultiplierAndMaximumEnergy()
        {
            FoodPlanSystem.Reset(encouragingMultiplier: 1.2f);

            Assert.That(
                FoodPlanSystem.GetEncouragingTotalFoodRequirement(5, 3),
                Is.EqualTo(9));
            Assert.That(
                FoodPlanSystem.GetEncouragingExtraFoodCost(5, 3),
                Is.EqualTo(4));
            Assert.That(
                FoodPlanSystem.GetEncouragingTotalFoodRequirement(1, 3),
                Is.EqualTo(5));
        }

        [Test]
        public void EncouragingMeal_PaysFullExtraPermanentFood_AndRecoversMoraleOnce()
        {
            RoundEndResolver resolver = CreateResolver(FoodPlanLevel.Generous);
            FoodSystem.Reset(10);
            EnergySystem.Reset(3);
            MoraleSystem.Reset(98, highDecayPerRound: 0);

            try
            {
                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 1);

                Assert.That(report.EncouragingMealAttempted, Is.True);
                Assert.That(report.EncouragingMealSucceeded, Is.True);
                Assert.That(report.EncouragingMealTotalRequirement, Is.EqualTo(9));
                Assert.That(report.EncouragingMealExtraFoodCost, Is.EqualTo(4));
                Assert.That(report.EncouragingMealExtraFoodPaid, Is.EqualTo(4));
                Assert.That(report.EncouragingMealMoraleGain, Is.EqualTo(5));
                Assert.That(report.PermanentFoodConsumed, Is.EqualTo(5));
                Assert.That(FoodSystem.GetFood(), Is.EqualTo(1));
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(100));
                Assert.That(
                    report.MoraleSettlement.ChangeResult.AppliedMoraleDelta,
                    Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void EncouragingMealAndDrift_NetToPlusTwoInSameRound()
        {
            RoundEndResolver resolver = CreateResolver(FoodPlanLevel.Generous);
            FoodSystem.Reset(10);
            EnergySystem.Reset(3);
            MoraleSystem.Reset(90, defaultValue: 70, highDecayPerRound: 3);

            try
            {
                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 1);

                Assert.That(report.EncouragingMealMoraleGain, Is.EqualTo(5));
                Assert.That(report.HighMoraleDrift, Is.EqualTo(-3));
                Assert.That(report.MoraleSettlement.NetMoraleDelta, Is.EqualTo(2));
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(92));
            }
            finally
            {
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void EncouragingMeal_DoesNotUseTemporaryFoodForExtraCost()
        {
            RoundEndResolver resolver = CreateResolver(FoodPlanLevel.Generous);
            FoodSystem.Reset(0);
            FoodSystem.AddTemporaryFood(5);
            EnergySystem.Reset(3);
            MoraleSystem.Reset(60, highDecayPerRound: 0);

            try
            {
                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 1);

                Assert.That(report.FoodShortage, Is.Zero);
                Assert.That(report.TemporaryFoodConsumed, Is.EqualTo(5));
                Assert.That(report.EncouragingMealSucceeded, Is.False);
                Assert.That(report.EncouragingMealExtraFoodPaid, Is.Zero);
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(60));
            }
            finally
            {
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void EncouragingMeal_FixedDemandShortageCancelsRecoveryWithoutPartialPayment()
        {
            RoundEndResolver resolver = CreateResolver(FoodPlanLevel.Generous);
            FoodSystem.Reset(2);
            EnergySystem.Reset(3);
            MoraleSystem.Reset(60, highDecayPerRound: 0);

            try
            {
                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 1);

                Assert.That(report.FoodShortage, Is.EqualTo(3));
                Assert.That(report.ShortageMoraleLoss, Is.EqualTo(9));
                Assert.That(report.EncouragingMealSucceeded, Is.False);
                Assert.That(report.EncouragingMealExtraFoodPaid, Is.Zero);
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(51));
            }
            finally
            {
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void EncouragingMeal_AtMaximumMoraleDoesNotChargeExtraFood()
        {
            RoundEndResolver resolver = CreateResolver(FoodPlanLevel.Generous);
            FoodSystem.Reset(10);
            EnergySystem.Reset(3);
            MoraleSystem.Reset(100, highDecayPerRound: 0);

            try
            {
                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 1);

                Assert.That(report.EncouragingMealAttempted, Is.True);
                Assert.That(report.EncouragingMealSucceeded, Is.False);
                Assert.That(report.EncouragingMealExtraFoodPaid, Is.Zero);
                Assert.That(FoodSystem.GetFood(), Is.EqualTo(5));
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(100));
            }
            finally
            {
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void HighMoraleDrift_DecaysByThreeWithoutGoingBelowSeventy()
        {
            RoundEndResolver resolver = CreateResolver(
                FoodPlanLevel.Normal,
                baseFoodDemand: 0,
                highMoraleDecay: 3);
            MoraleSystem.Reset(72, defaultValue: 70, highDecayPerRound: 3);

            try
            {
                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 1);

                Assert.That(report.HighMoraleDrift, Is.EqualTo(-2));
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(70));
            }
            finally
            {
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void FoodDemandOne_DoesNotOverrideMoraleDriftThree()
        {
            RoundEndResolver resolver = CreateResolver(
                FoodPlanLevel.Normal,
                baseFoodDemand: 1,
                highMoraleDecay: 3);
            FoodSystem.Reset(1);
            MoraleSystem.Reset(80, defaultValue: 70, highDecayPerRound: 3);

            try
            {
                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 1);

                Assert.That(report.FoodConsumption, Is.EqualTo(1));
                Assert.That(report.FoodShortage, Is.Zero);
                Assert.That(report.HighMoraleDrift, Is.EqualTo(-3));
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(77));
            }
            finally
            {
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void Batch_NetsPositiveAndNegativeSourcesBeforeThresholdEvaluation()
        {
            MoraleSystem.Reset(75);
            int stateChangedCount = 0;
            void HandleStateChanged(MoraleChangeResult _) => stateChangedCount++;
            MoraleSystem.StateChanged += HandleStateChanged;

            try
            {
                MoraleBatchRequest request = new MoraleBatchRequest("OrderIndependent");
                request.Add("Loss", -10);
                request.Add("Gain", 10);

                MoraleBatchResult result = MoraleSystem.CommitBatch(request);

                Assert.That(result.NetMoraleDelta, Is.Zero);
                Assert.That(result.Contributions.Count, Is.EqualTo(2));
                Assert.That(result.ChangeResult.StageTransitions, Is.Empty);
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(75));
                Assert.That(MoraleSystem.CollapseStage, Is.EqualTo(MoraleCollapseStage.Stable));
                Assert.That(stateChangedCount, Is.EqualTo(1));
            }
            finally
            {
                MoraleSystem.StateChanged -= HandleStateChanged;
            }
        }

        [Test]
        public void CardMoraleEffects_AreNettedBeforeThresholdEvaluation()
        {
            CardBase card = CreateMoraleCard(
                (ValueEffectType.ReduceMorale, 10),
                (ValueEffectType.AddMorale, 10));
            MoraleSystem.Reset(75);
            int stateChangedCount = 0;
            void HandleStateChanged(MoraleChangeResult _) => stateChangedCount++;
            MoraleSystem.StateChanged += HandleStateChanged;

            try
            {
                card.Resolve(new CardPlayContext(null, null));

                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(75));
                Assert.That(MoraleSystem.CollapseStage, Is.EqualTo(MoraleCollapseStage.Stable));
                Assert.That(stateChangedCount, Is.EqualTo(1));
            }
            finally
            {
                MoraleSystem.StateChanged -= HandleStateChanged;
                Object.DestroyImmediate(card);
            }
        }

        private static RoundEndResolver CreateResolver(
            FoodPlanLevel startingPlan,
            int baseFoodDemand = 5,
            int highMoraleDecay = MoraleSystem.DefaultHighMoraleDecayPerRound)
        {
            GameObject owner = new GameObject("MoraleSettlementRoundEndResolverTest");
            RoundEndResolver resolver = owner.AddComponent<RoundEndResolver>();
            SerializedObject serialized = new SerializedObject(resolver);
            serialized.FindProperty("startingFoodPlan").enumValueIndex = (int)startingPlan;
            serialized.FindProperty("baseRoundFoodConsumption").intValue = baseFoodDemand;
            serialized.FindProperty("shortageMoralePenaltyPerFood").intValue = 3;
            serialized.FindProperty("shortageMoraleLossCap").intValue = 15;
            serialized.FindProperty("encouragingDemandMultiplier").floatValue = 1.2f;
            serialized.FindProperty("encouragingMoraleGain").intValue =
                FoodPlanSystem.DefaultEncouragingMoraleGain;
            serialized.FindProperty("highMoraleDecayPerRound").intValue = highMoraleDecay;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            resolver.PrepareRun();
            return resolver;
        }

        private static CardBase CreateMoraleCard(params (ValueEffectType type, int amount)[] values)
        {
            CardBase card = ScriptableObject.CreateInstance<CardBase>();
            SerializedObject serialized = new SerializedObject(card);
            SerializedProperty effects = serialized.FindProperty("effects");
            effects.arraySize = values.Length;

            for (int index = 0; index < values.Length; index++)
            {
                SerializedProperty effect = effects.GetArrayElementAtIndex(index);
                effect.FindPropertyRelative("carrier").enumValueIndex = (int)GameEffectCarrier.Card;
                effect.FindPropertyRelative("category").enumValueIndex = (int)GameEffectCategory.Value;
                effect.FindPropertyRelative("valueEffectType").enumValueIndex = (int)values[index].type;
                effect.FindPropertyRelative("amount").intValue = values[index].amount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }
    }
}
