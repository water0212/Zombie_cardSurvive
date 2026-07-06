using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Tests.Editor
{
    public class TemporaryFoodSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(10);
            FoodSystem.SetStorageCapacity(50);
            CombatSystem.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(0);
            CombatSystem.Reset();
        }

        [Test]
        public void RoundConsumption_UsesTemporaryFoodBeforePermanentFood()
        {
            FoodSystem.AddTemporaryFood(6);

            RoundFoodConsumptionResult result = FoodSystem.ConsumeRoundFoodAllowShortage(8);

            Assert.That(result.TemporaryConsumed, Is.EqualTo(6));
            Assert.That(result.PermanentConsumed, Is.EqualTo(2));
            Assert.That(result.Shortage, Is.Zero);
            Assert.That(FoodSystem.GetFood(), Is.EqualTo(8));
            Assert.That(FoodSystem.TemporaryFood, Is.Zero);
        }

        [Test]
        public void RoundConsumption_DiscardsUnusedTemporaryFood()
        {
            FoodSystem.AddTemporaryFood(6);

            RoundFoodConsumptionResult result = FoodSystem.ConsumeRoundFoodAllowShortage(2);

            Assert.That(result.TemporaryConsumed, Is.EqualTo(2));
            Assert.That(result.TemporaryDiscarded, Is.EqualTo(4));
            Assert.That(result.PermanentConsumed, Is.Zero);
            Assert.That(FoodSystem.TemporaryFood, Is.Zero);
        }

        [Test]
        public void RoundConsumption_ReportsShortageAfterBothFoodSources()
        {
            FoodSystem.Reset(1);
            FoodSystem.AddTemporaryFood(2);

            RoundFoodConsumptionResult result = FoodSystem.ConsumeRoundFoodAllowShortage(5);

            Assert.That(result.TemporaryConsumed, Is.EqualTo(2));
            Assert.That(result.PermanentConsumed, Is.EqualTo(1));
            Assert.That(result.Shortage, Is.EqualTo(2));
            Assert.That(FoodSystem.GetFood(), Is.Zero);
        }

        [Test]
        public void RoundConsumption_DoesNotSpendReservedPermanentFood()
        {
            Assert.That(
                ResourceReservationService.TryReserve(new ResourceCost(8, 0), out _),
                Is.True);
            FoodSystem.AddTemporaryFood(2);

            RoundFoodConsumptionResult result = FoodSystem.ConsumeRoundFoodAllowShortage(6);

            Assert.That(result.TemporaryConsumed, Is.EqualTo(2));
            Assert.That(result.PermanentConsumed, Is.EqualTo(2));
            Assert.That(result.Shortage, Is.EqualTo(2));
            Assert.That(FoodSystem.GetFood(), Is.EqualTo(8));
            Assert.That(FoodSystem.ReservedFood, Is.EqualTo(8));
        }

        [Test]
        public void PermanentFoodCost_CannotUseTemporaryFood()
        {
            FoodSystem.Reset(0);
            FoodSystem.AddTemporaryFood(6);

            bool spent = FoodSystem.SpendFood(1);

            Assert.That(spent, Is.False);
            Assert.That(FoodSystem.GetFood(), Is.Zero);
            Assert.That(FoodSystem.TemporaryFood, Is.EqualTo(6));
        }

        [Test]
        public void StorageTrimIgnoresTemporaryFood_AndResetClearsIt()
        {
            FoodSystem.Reset(60);
            FoodSystem.AddTemporaryFood(10);

            int trimmed = FoodSystem.TrimToStorageCapacity();

            Assert.That(trimmed, Is.EqualTo(10));
            Assert.That(FoodSystem.GetFood(), Is.EqualTo(50));
            Assert.That(FoodSystem.TemporaryFood, Is.EqualTo(10));

            FoodSystem.Reset(3);
            Assert.That(FoodSystem.TemporaryFood, Is.Zero);
        }

        [Test]
        public void AddTemporaryFoodEffect_DoesNotChangePermanentFood()
        {
            CardBase card = CreateEffectCard(ValueEffectType.AddTemporaryFood, 6);

            card.Resolve(new CardPlayContext(null, null));

            Assert.That(FoodSystem.GetFood(), Is.EqualTo(10));
            Assert.That(FoodSystem.TemporaryFood, Is.EqualTo(6));
            Object.DestroyImmediate(card);
        }

        [Test]
        public void RoundEndResolver_AppliesShortageMoralePenaltyAfterTemporaryFood()
        {
            FoodSystem.Reset(0);
            FoodSystem.AddTemporaryFood(2);
            GameObject resolverObject = new GameObject("TemporaryFoodRoundEndResolverTest");
            RoundEndResolver resolver = resolverObject.AddComponent<RoundEndResolver>();
            resolver.PrepareRun();

            RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 3);

            Assert.That(report.FoodConsumption, Is.EqualTo(5));
            Assert.That(report.TemporaryFoodConsumed, Is.EqualTo(2));
            Assert.That(report.PermanentFoodConsumed, Is.Zero);
            Assert.That(report.FoodShortage, Is.EqualTo(3));
            Assert.That(report.ShortageMoraleLoss, Is.EqualTo(15));
            Assert.That(FoodSystem.TemporaryFood, Is.Zero);
            Object.DestroyImmediate(resolverObject);
        }

        private static CardBase CreateEffectCard(ValueEffectType type, int amount)
        {
            CardBase card = ScriptableObject.CreateInstance<CardBase>();
            SerializedObject serialized = new SerializedObject(card);
            SerializedProperty effects = serialized.FindProperty("effects");
            effects.arraySize = 1;
            SerializedProperty effect = effects.GetArrayElementAtIndex(0);
            effect.FindPropertyRelative("carrier").enumValueIndex = (int)GameEffectCarrier.Card;
            effect.FindPropertyRelative("category").enumValueIndex = (int)GameEffectCategory.Value;
            effect.FindPropertyRelative("valueEffectType").enumValueIndex = (int)type;
            effect.FindPropertyRelative("amount").intValue = amount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }
    }
}
