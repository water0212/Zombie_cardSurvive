using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class EventPaymentBoundaryTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(0);
            ResourceSystem.Reset(0);
            MoraleSystem.Reset(70);
            CombatSystem.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(0);
            ResourceSystem.Reset(0);
            MoraleSystem.Reset(70);
            CombatSystem.Reset();

            foreach (Object createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void ChoiceCost_PaysPermanentFoodAndAppliesReward()
        {
            FoodSystem.Reset(4);
            ResolvedEventChoiceRuntime choice = CreateChoice(
                new[]
                {
                    CreatePayment(ValueEffectType.SpendFood, 4, EventPaymentMode.ChoiceCost),
                    CreateEffect(ValueEffectType.AddResource, 2)
                });

            bool succeeded = choice.TryExecute(
                GameEffectContext.ForEvent(),
                out EventChoiceExecutionResult result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(FoodSystem.Food, Is.Zero);
            Assert.That(ResourceSystem.Resource, Is.EqualTo(2));
            Assert.That(result.ValueChanges.Food, Is.EqualTo(-4));
            Assert.That(result.ValueChanges.Resource, Is.EqualTo(2));
        }

        [Test]
        public void ChoiceCost_CannotUseTemporaryFood()
        {
            FoodSystem.AddTemporaryFood(6);
            ResolvedEventChoiceRuntime choice = CreateChoice(
                new[] { CreatePayment(ValueEffectType.SpendFood, 1, EventPaymentMode.ChoiceCost) });

            EventChoiceAvailability availability = choice.EvaluateAvailability(GameEffectContext.ForEvent());
            bool succeeded = choice.TryExecute(GameEffectContext.ForEvent(), out _);

            Assert.That(availability.IsAvailable, Is.False);
            Assert.That(availability.BlockReason, Is.EqualTo(EventChoiceBlockReason.InsufficientPermanentFood));
            Assert.That(succeeded, Is.False);
            Assert.That(FoodSystem.TemporaryFood, Is.EqualTo(6));
        }

        [Test]
        public void ChoiceCost_CannotUseReservedResources()
        {
            FoodSystem.Reset(10);
            ResourceSystem.Reset(5);
            Assert.That(
                ResourceReservationService.TryReserve(
                    new ResourceCost(8, 4),
                    out ResourceReservationToken reservation),
                Is.True);
            ResolvedEventChoiceRuntime choice = CreateChoice(
                new[]
                {
                    CreatePayment(ValueEffectType.SpendFood, 3, EventPaymentMode.ChoiceCost),
                    CreatePayment(ValueEffectType.SpendResource, 2, EventPaymentMode.ChoiceCost)
                });

            EventChoiceAvailability availability = choice.EvaluateAvailability(GameEffectContext.ForEvent());

            Assert.That(availability.IsAvailable, Is.False);
            Assert.That(FoodSystem.Food, Is.EqualTo(10));
            Assert.That(ResourceSystem.Resource, Is.EqualTo(5));
            Assert.That(ResourceReservationService.Contains(reservation), Is.True);
        }

        [Test]
        public void MultipleChoiceCosts_AreAggregatedBeforeAnythingIsApplied()
        {
            FoodSystem.Reset(6);
            ResolvedEventChoiceRuntime choice = CreateChoice(
                new[]
                {
                    CreatePayment(ValueEffectType.SpendFood, 4, EventPaymentMode.ChoiceCost),
                    CreatePayment(ValueEffectType.SpendFood, 4, EventPaymentMode.ChoiceCost),
                    CreateEffect(ValueEffectType.AddResource, 5)
                });

            bool succeeded = choice.TryExecute(GameEffectContext.ForEvent(), out _);

            Assert.That(succeeded, Is.False);
            Assert.That(FoodSystem.Food, Is.EqualTo(6));
            Assert.That(ResourceSystem.Resource, Is.Zero);
        }

        [Test]
        public void MixedChoiceCosts_DoNotSpendFoodWhenResourceIsInsufficient()
        {
            FoodSystem.Reset(4);
            ResourceSystem.Reset(1);
            ResolvedEventChoiceRuntime choice = CreateChoice(
                new[]
                {
                    CreatePayment(ValueEffectType.SpendFood, 4, EventPaymentMode.ChoiceCost),
                    CreatePayment(ValueEffectType.SpendResource, 2, EventPaymentMode.ChoiceCost),
                    CreateEffect(ValueEffectType.AddResource, 10)
                });

            bool succeeded = choice.TryExecute(GameEffectContext.ForEvent(), out _);

            Assert.That(succeeded, Is.False);
            Assert.That(FoodSystem.Food, Is.EqualTo(4));
            Assert.That(ResourceSystem.Resource, Is.EqualTo(1));
        }

        [Test]
        public void ForcedLoss_ConsumesOnlyAvailablePermanentFoodAndReportsShortage()
        {
            FoodSystem.Reset(5);
            FoodSystem.AddTemporaryFood(6);
            Assert.That(
                ResourceReservationService.TryReserve(new ResourceCost(3, 0), out _),
                Is.True);
            ResolvedEventChoiceRuntime choice = CreateChoice(
                new[] { CreatePayment(ValueEffectType.SpendFood, 5, EventPaymentMode.ForcedLoss) },
                resultDescription: "Paid {paid:0}; shortage {shortage:0}.");

            bool succeeded = choice.TryExecute(
                GameEffectContext.ForEvent(),
                out EventChoiceExecutionResult result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.ForcedLosses[0].Consumed, Is.EqualTo(2));
            Assert.That(result.ForcedLosses[0].Shortage, Is.EqualTo(3));
            Assert.That(result.ResultDescription, Is.EqualTo("Paid 2; shortage 3."));
            Assert.That(FoodSystem.Food, Is.EqualTo(3));
            Assert.That(FoodSystem.TemporaryFood, Is.EqualTo(6));
            Assert.That(FoodSystem.ReservedFood, Is.EqualTo(3));
        }

        [Test]
        public void ForcedLoss_ConsumesResourceToZeroWithoutBlockingChoice()
        {
            ResourceSystem.Reset(2);
            ResolvedEventChoiceRuntime choice = CreateChoice(
                new[] { CreatePayment(ValueEffectType.SpendResource, 5, EventPaymentMode.ForcedLoss) });

            EventChoiceAvailability availability = choice.EvaluateAvailability(GameEffectContext.ForEvent());
            bool succeeded = choice.TryExecute(
                GameEffectContext.ForEvent(),
                out EventChoiceExecutionResult result);

            Assert.That(availability.IsAvailable, Is.True);
            Assert.That(succeeded, Is.True);
            Assert.That(ResourceSystem.Resource, Is.Zero);
            Assert.That(result.ForcedLosses[0].Shortage, Is.EqualTo(3));
        }

        [Test]
        public void PerMissingUnitConsequence_UsesForcedLossShortage()
        {
            FoodSystem.Reset(2);
            GameEffectData forcedLoss = CreatePayment(
                ValueEffectType.SpendFood,
                5,
                EventPaymentMode.ForcedLoss);
            GameEffectData moraleLoss = CreateEffect(ValueEffectType.ReduceMorale, 2);
            EventForcedLossConsequenceData consequence = CreateConsequence(
                0,
                EventShortageConsequenceAmountMode.PerMissingUnit,
                moraleLoss);
            ResolvedEventChoiceRuntime choice = CreateChoice(
                new[] { forcedLoss },
                new[] { consequence });

            bool succeeded = choice.TryExecute(GameEffectContext.ForEvent(), out _);

            Assert.That(succeeded, Is.True);
            Assert.That(MoraleSystem.Morale, Is.EqualTo(64));
        }

        [Test]
        public void ForcedLossConsequence_DoesNotRunWithoutShortage()
        {
            FoodSystem.Reset(5);
            EventForcedLossConsequenceData consequence = CreateConsequence(
                0,
                EventShortageConsequenceAmountMode.Fixed,
                CreateEffect(ValueEffectType.ReduceMorale, 10));
            ResolvedEventChoiceRuntime choice = CreateChoice(
                new[] { CreatePayment(ValueEffectType.SpendFood, 5, EventPaymentMode.ForcedLoss) },
                new[] { consequence });

            bool succeeded = choice.TryExecute(GameEffectContext.ForEvent(), out _);

            Assert.That(succeeded, Is.True);
            Assert.That(MoraleSystem.Morale, Is.EqualTo(70));
        }

        [Test]
        public void FixedConsequence_RunsOnceWhenForcedLossHasShortage()
        {
            EventForcedLossConsequenceData consequence = CreateConsequence(
                0,
                EventShortageConsequenceAmountMode.Fixed,
                CreateEffect(ValueEffectType.ReduceMorale, 7));
            ResolvedEventChoiceRuntime choice = CreateChoice(
                new[] { CreatePayment(ValueEffectType.SpendFood, 5, EventPaymentMode.ForcedLoss) },
                new[] { consequence });

            bool succeeded = choice.TryExecute(GameEffectContext.ForEvent(), out _);

            Assert.That(succeeded, Is.True);
            Assert.That(MoraleSystem.Morale, Is.EqualTo(63));
        }

        [Test]
        public void EventFlow_RemainsChoosingWhenChoiceCostIsUnavailable()
        {
            EventData eventData = CreateEvent(
                CreateChoiceData(
                    new[] { CreatePayment(ValueEffectType.SpendFood, 1, EventPaymentMode.ChoiceCost) }));
            GameObject owner = new GameObject("EventPaymentFlowTest");
            createdObjects.Add(owner);
            EventFlowController flow = owner.AddComponent<EventFlowController>();
            SetField(flow, "eventPool", new List<EventData> { eventData });
            flow.InitializeRun(1234);
            flow.BeginEventPhase(1);

            bool selected = flow.ChooseOption(0);

            Assert.That(selected, Is.False);
            Assert.That(flow.State, Is.EqualTo(EventFlowState.Choosing));
            Assert.That(flow.LastChoiceExecutionResult.Succeeded, Is.False);
            Assert.That(flow.SelectedResolvedChoice, Is.Null);
        }

        [Test]
        public void CardSpendEffect_IgnoresEventForcedLossMode()
        {
            GameEffectData cardSpend = CreatePayment(
                ValueEffectType.SpendFood,
                3,
                EventPaymentMode.ForcedLoss,
                GameEffectCarrier.Card);

            bool canApply = cardSpend.CanApply(GameEffectContext.ForOther());
            bool applied = cardSpend.TryApply(GameEffectContext.ForOther());

            Assert.That(canApply, Is.False);
            Assert.That(applied, Is.False);
            Assert.That(FoodSystem.Food, Is.Zero);
        }

        private ResolvedEventChoiceRuntime CreateChoice(
            IReadOnlyList<GameEffectData> effects,
            IReadOnlyList<EventForcedLossConsequenceData> consequences = null,
            string resultDescription = "")
        {
            EventChoiceData choice = CreateChoiceData(effects, consequences, resultDescription);
            EventMagnitudeRuntime magnitudeRuntime = new EventMagnitudeRuntime();
            magnitudeRuntime.InitializeRun();
            EventMagnitudeSnapshot snapshot = magnitudeRuntime.Capture(
                new EventMagnitudeInputSnapshot(
                    day: 1,
                    survivorCount: 1,
                    additionalSurvivorCount: 0,
                    activeBuildingLevelSum: 0,
                    permanentSlotUpgradeCount: 0,
                    otherPermanentDevelopmentModifiers: 0,
                    maxEnergy: 1,
                    foodStorageCapacity: 50,
                    foodStorageCapacityBonus: 0));
            return ResolvedEventChoiceRuntime.Create(choice, snapshot);
        }

        private static EventChoiceData CreateChoiceData(
            IReadOnlyList<GameEffectData> effects,
            IReadOnlyList<EventForcedLossConsequenceData> consequences = null,
            string resultDescription = "")
        {
            EventChoiceData choice = new EventChoiceData();
            SetField(choice, "choiceText", "Test choice");
            SetField(choice, "resultDescription", resultDescription);
            SetField(choice, "effects", new List<GameEffectData>(effects));
            SetField(
                choice,
                "forcedLossConsequences",
                consequences != null
                    ? new List<EventForcedLossConsequenceData>(consequences)
                    : new List<EventForcedLossConsequenceData>());
            return choice;
        }

        private EventData CreateEvent(EventChoiceData choice)
        {
            EventData eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.name = "EVENT-PAYMENT-TEST";
            SetField(eventData, "eventId", "EVENT-PAYMENT-TEST");
            SetField(eventData, "choices", new List<EventChoiceData> { choice });
            createdObjects.Add(eventData);
            return eventData;
        }

        private static GameEffectData CreatePayment(
            ValueEffectType type,
            int amount,
            EventPaymentMode paymentMode,
            GameEffectCarrier carrier = GameEffectCarrier.Event)
        {
            GameEffectData effect = CreateEffect(type, amount, carrier);
            SetField(effect, "eventPaymentMode", paymentMode);
            return effect;
        }

        private static GameEffectData CreateEffect(
            ValueEffectType type,
            int amount,
            GameEffectCarrier carrier = GameEffectCarrier.Event)
        {
            GameEffectData effect = new GameEffectData();
            SetField(effect, "carrier", carrier);
            SetField(effect, "category", GameEffectCategory.Value);
            SetField(effect, "valueEffectType", type);
            SetField(effect, "amount", amount);
            return effect;
        }

        private static EventForcedLossConsequenceData CreateConsequence(
            int sourceEffectIndex,
            EventShortageConsequenceAmountMode amountMode,
            GameEffectData effect)
        {
            EventForcedLossConsequenceData consequence = new EventForcedLossConsequenceData();
            SetField(consequence, "sourceEffectIndex", sourceEffectIndex);
            SetField(consequence, "amountMode", amountMode);
            SetField(consequence, "consequenceEffect", effect);
            return consequence;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            field.SetValue(target, value);
        }
    }
}
