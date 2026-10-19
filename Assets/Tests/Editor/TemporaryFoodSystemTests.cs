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
            ResourceSystem.Reset(0);
            EnergySystem.Reset(0);
            CombatSystem.Reset();
            MoraleSystem.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(0);
            ResourceSystem.Reset(0);
            EnergySystem.Reset(0);
            CombatSystem.Reset();
            MoraleSystem.Reset();
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
            Assert.That(report.ShortageMoraleLoss, Is.EqualTo(9));
            Assert.That(FoodSystem.TemporaryFood, Is.Zero);
            Object.DestroyImmediate(resolverObject);
        }

        [Test]
        public void AutoResolvedZombie_PaysCardCost_WithoutSpendingEnergy()
        {
            ZombieCardData card = CreateAutoZombieCard(
                energyCost: 2,
                costType: ValueEffectType.ReduceMorale,
                costAmount: 20,
                effectType: ValueEffectType.AddResource,
                effectAmount: 3);
            CardController controller = CreateCardController();
            MoraleSystem.Reset(100);
            EnergySystem.Reset(0);

            try
            {
                AddToDrawPile(controller, card);

                controller.DrawCards(1);

                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(80));
                Assert.That(EnergySystem.CurrentEnergy, Is.Zero);
                Assert.That(ResourceSystem.GetResource(), Is.EqualTo(3));
                Assert.That(CombatSystem.ZombieThreatCount, Is.EqualTo(1));
                Assert.That(controller.HandCards, Is.Empty);
                Assert.That(controller.ExhaustedCards.Count, Is.EqualTo(1));
                Assert.That(controller.VacancyRecords, Is.Empty);
                Assert.That(controller.ExhaustedCards[0].Zone, Is.EqualTo(DeckZone.Exhausted));
            }
            finally
            {
                Object.DestroyImmediate(controller.gameObject);
                Object.DestroyImmediate(card);
            }
        }

        [Test]
        public void AutoCostFailure_LeavesCardInHand_AndManualPlayPaysEnergyLater()
        {
            ZombieCardData card = CreateAutoZombieCard(
                energyCost: 2,
                costType: ValueEffectType.SpendFood,
                costAmount: 2,
                effectType: ValueEffectType.AddResource,
                effectAmount: 3);
            CardController controller = CreateCardController();
            FoodSystem.Reset(1);
            EnergySystem.Reset(2);

            try
            {
                AddToDrawPile(controller, card);

                controller.DrawCards(1);

                Assert.That(controller.HandCards.Count, Is.EqualTo(1));
                Assert.That(controller.HandCards[0].Zone, Is.EqualTo(DeckZone.Hand));
                Assert.That(FoodSystem.GetFood(), Is.EqualTo(1));
                Assert.That(EnergySystem.CurrentEnergy, Is.EqualTo(2));
                Assert.That(ResourceSystem.GetResource(), Is.Zero);
                Assert.That(CombatSystem.ZombieThreatCount, Is.Zero);

                FoodSystem.AddFood(1);
                EnergySystem.SpendEnergy(2);

                Assert.That(controller.RequestPlayCard(controller.HandCards[0]), Is.False);
                Assert.That(FoodSystem.GetFood(), Is.EqualTo(2));
                Assert.That(controller.HandCards.Count, Is.EqualTo(1));

                EnergySystem.AddEnergy(2);
                CardRuntime pendingCard = controller.HandCards[0];

                Assert.That(controller.RequestPlayCard(pendingCard), Is.True);
                Assert.That(FoodSystem.GetFood(), Is.Zero);
                Assert.That(EnergySystem.CurrentEnergy, Is.Zero);
                Assert.That(ResourceSystem.GetResource(), Is.EqualTo(3));
                Assert.That(CombatSystem.ZombieThreatCount, Is.EqualTo(1));
                Assert.That(controller.HandCards, Is.Empty);
                Assert.That(controller.ExhaustedCards.Count, Is.EqualTo(1));
                Assert.That(controller.VacancyRecords, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(controller.gameObject);
                Object.DestroyImmediate(card);
            }
        }

        [Test]
        public void AutoZombieWithTwoUses_ReturnsToDiscard_ThenExhaustsOnSecondPlay()
        {
            ZombieCardData card = CreateAutoZombieCard(
                energyCost: 0,
                costType: ValueEffectType.ReduceMorale,
                costAmount: 0,
                effectType: ValueEffectType.AddResource,
                effectAmount: 1,
                maxUsesPerRun: 2);
            CardController controller = CreateCardController();

            try
            {
                AddToDrawPile(controller, card);

                controller.DrawCards(1);

                Assert.That(ResourceSystem.GetResource(), Is.EqualTo(1));
                Assert.That(CombatSystem.ZombieThreatCount, Is.EqualTo(1));
                Assert.That(controller.DiscardPile.Count, Is.EqualTo(1));
                Assert.That(controller.DiscardPile[0].RemainingUses, Is.EqualTo(1));
                Assert.That(controller.ExhaustedCards, Is.Empty);

                controller.StartNewRound();
                controller.DrawCards(1);

                Assert.That(ResourceSystem.GetResource(), Is.EqualTo(2));
                Assert.That(CombatSystem.ZombieThreatCount, Is.EqualTo(2));
                Assert.That(controller.DiscardPile, Is.Empty);
                Assert.That(controller.ExhaustedCards.Count, Is.EqualTo(1));
                Assert.That(controller.VacancyRecords, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(controller.gameObject);
                Object.DestroyImmediate(card);
            }
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

        private static ZombieCardData CreateAutoZombieCard(
            int energyCost,
            ValueEffectType costType,
            int costAmount,
            ValueEffectType effectType,
            int effectAmount,
            int maxUsesPerRun = 1)
        {
            ZombieCardData card = ScriptableObject.CreateInstance<ZombieCardData>();
            SerializedObject serialized = new SerializedObject(card);
            serialized.FindProperty("energyCost").intValue = energyCost;
            serialized.FindProperty("isAdditiveCard").boolValue = true;
            serialized.FindProperty("drawReplacementOnAutoResolve").boolValue = true;
            serialized.FindProperty("hasLimitedUses").boolValue = true;
            serialized.FindProperty("maxUsesPerRun").intValue = maxUsesPerRun;
            ConfigureValueEffect(serialized.FindProperty("costs"), costType, costAmount);
            ConfigureValueEffect(serialized.FindProperty("effects"), effectType, effectAmount);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        private static void ConfigureValueEffect(
            SerializedProperty effects,
            ValueEffectType type,
            int amount)
        {
            effects.arraySize = 1;
            SerializedProperty effect = effects.GetArrayElementAtIndex(0);
            effect.FindPropertyRelative("carrier").enumValueIndex = (int)GameEffectCarrier.Card;
            effect.FindPropertyRelative("category").enumValueIndex = (int)GameEffectCategory.Value;
            effect.FindPropertyRelative("valueEffectType").enumValueIndex = (int)type;
            effect.FindPropertyRelative("amount").intValue = amount;
        }

        private static CardController CreateCardController()
        {
            GameObject owner = new GameObject("AutoCardPlayPipelineTest");
            owner.SetActive(false);
            CardController controller = owner.AddComponent<CardController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("startDemoOnAwake").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            owner.SetActive(true);
            return controller;
        }

        private static void AddToDrawPile(CardController controller, CardBase card)
        {
            Assert.That(controller.AddCardToDiscardPile(card), Is.True);
            controller.StartNewRound();
            Assert.That(controller.DrawPile.Count, Is.EqualTo(1));
        }
    }
}
