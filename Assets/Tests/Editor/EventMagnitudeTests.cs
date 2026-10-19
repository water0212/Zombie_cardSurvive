using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Tests.Editor
{
    public class EventMagnitudeTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(0);
            ResourceSystem.Reset(0);
        }

        [TearDown]
        public void TearDown()
        {
            ResourceReservationService.Reset();
            foreach (UnityEngine.Object createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void DayOneWithoutDevelopmentStartsAtMagnitudeOne()
        {
            EventMagnitudeRuntime runtime = CreateRuntime();

            EventMagnitudeSnapshot snapshot = runtime.Capture(CreateInput(day: 1));

            Assert.That(snapshot.TimeTier, Is.EqualTo(1));
            Assert.That(snapshot.DevelopmentPoints, Is.Zero);
            Assert.That(snapshot.CurrentSettlementTier, Is.EqualTo(1));
            Assert.That(snapshot.PeakSettlementTier, Is.EqualTo(1));
            Assert.That(snapshot.EventMagnitude, Is.EqualTo(1));
        }

        [Test]
        public void TimeAndSettlementGrowthUseMaximumInsteadOfSum()
        {
            EventMagnitudeRuntime runtime = CreateRuntime();

            EventMagnitudeSnapshot snapshot = runtime.Capture(
                CreateInput(day: 21, activeBuildingLevels: 6));

            Assert.That(snapshot.TimeTier, Is.EqualTo(3));
            Assert.That(snapshot.CurrentSettlementTier, Is.EqualTo(3));
            Assert.That(snapshot.EventMagnitude, Is.EqualTo(3));
        }

        [Test]
        public void PeakSettlementTierNeverFallsDuringRun()
        {
            EventMagnitudeRuntime runtime = CreateRuntime();
            runtime.Capture(CreateInput(day: 1, activeBuildingLevels: 6));

            EventMagnitudeSnapshot afterLoss = runtime.Capture(CreateInput(day: 2));

            Assert.That(afterLoss.CurrentSettlementTier, Is.EqualTo(1));
            Assert.That(afterLoss.PeakSettlementTier, Is.EqualTo(3));
            Assert.That(afterLoss.EventMagnitude, Is.EqualTo(3));
        }

        [Test]
        public void FullScalingTurnsBaseTwoAtTierThreeIntoSix()
        {
            EventMagnitudeSnapshot snapshot = CreateRuntime().Capture(
                CreateInput(day: 21));
            EventEffectScalingData scaling = CreateScaling(EventScaleSource.EventMagnitude, 1f);

            int result = EventEffectScalingResolver.ResolveAmount(2, scaling, snapshot);

            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public void ScalingSupportsRoundingBoundsAndNegativeModifiers()
        {
            EventMagnitudeSnapshot snapshot = CreateRuntime().Capture(
                CreateInput(day: 11));
            EventEffectScalingData floor = CreateScaling(
                EventScaleSource.TimeTier,
                0.25f,
                minimum: 4,
                maximum: 5,
                rounding: EventScaleRoundingMode.Floor);
            EventEffectScalingData ceil = CreateScaling(
                EventScaleSource.TimeTier,
                0.25f,
                rounding: EventScaleRoundingMode.Ceil);

            Assert.That(EventEffectScalingResolver.ResolveAmount(3, floor, snapshot), Is.EqualTo(4));
            Assert.That(EventEffectScalingResolver.ResolveAmount(10, floor, snapshot), Is.EqualTo(5));
            Assert.That(EventEffectScalingResolver.ResolveAmount(3, ceil, snapshot), Is.EqualTo(4));
            Assert.That(EventEffectScalingResolver.ResolveAmount(-3, ceil, snapshot), Is.EqualTo(-4));
        }

        [Test]
        public void FoodStorageUsesBonusTiersInsteadOfRawCapacity()
        {
            EventMagnitudeSnapshot snapshot = CreateRuntime().Capture(
                CreateInput(day: 1, foodStorageCapacity: 70, foodStorageBonus: 20));

            Assert.That(snapshot.FoodStorageCapacityTier, Is.EqualTo(3));
            Assert.That(snapshot.GetScaleTier(EventScaleSource.FoodStorageCapacity), Is.EqualTo(3));
        }

        [Test]
        public void ResolvedEventLocksAmountsAndFormatsVisibleText()
        {
            EventMagnitudeRuntime magnitudeRuntime = CreateRuntime();
            EventMagnitudeSnapshot dayTwentyOne = magnitudeRuntime.Capture(CreateInput(day: 21));
            GameEffectData spend = CreateEffect(
                ValueEffectType.SpendResource,
                2,
                CreateScaling(EventScaleSource.EventMagnitude, 1f));
            GameEffectData reward = CreateEffect(
                ValueEffectType.AddFood,
                4,
                CreateScaling(EventScaleSource.EventMagnitude, 1f));
            EventData eventData = CreateEvent(
                "EVENT-SNAPSHOT",
                "交出 {magnitude:0} 資源，取得 {value:1} 食物",
                "實際支付 {value:0}，實際取得 {value:1}",
                spend,
                reward);

            ResolvedEventRuntime resolved = ResolvedEventRuntime.Create(eventData, dayTwentyOne);
            magnitudeRuntime.Capture(CreateInput(day: 41, activeBuildingLevels: 12));

            Assert.That(resolved.Choices[0].Effects[0].FinalAmount, Is.EqualTo(6));
            Assert.That(resolved.Choices[0].Effects[1].FinalAmount, Is.EqualTo(12));
            Assert.That(resolved.Choices[0].ChoiceText, Is.EqualTo("交出 6 資源，取得 12 食物"));
            Assert.That(resolved.Choices[0].ResultDescription, Is.EqualTo("實際支付 6，實際取得 12"));
        }

        [Test]
        public void ResolvedAmountIsAppliedWithoutChangingAssetBaseAmount()
        {
            EventMagnitudeSnapshot snapshot = CreateRuntime().Capture(CreateInput(day: 21));
            GameEffectData effect = CreateEffect(
                ValueEffectType.AddFood,
                2,
                CreateScaling(EventScaleSource.EventMagnitude, 1f));
            ResolvedGameEffectRuntime resolved = ResolvedGameEffectRuntime.Create(effect, snapshot);

            Assert.That(resolved.TryApply(GameEffectContext.ForEvent()), Is.True);

            Assert.That(FoodSystem.Food, Is.EqualTo(6));
            Assert.That(effect.Amount, Is.EqualTo(2));
        }

        [Test]
        public void MatchingScaleSettingsPreserveExchangeRatio()
        {
            EventMagnitudeSnapshot snapshot = CreateRuntime().Capture(CreateInput(day: 21));
            EventEffectScalingData scaling = CreateScaling(EventScaleSource.EventMagnitude, 1f);

            int cost = EventEffectScalingResolver.ResolveAmount(2, scaling, snapshot);
            int reward = EventEffectScalingResolver.ResolveAmount(4, scaling, snapshot);

            Assert.That(cost, Is.EqualTo(6));
            Assert.That(reward, Is.EqualTo(12));
            Assert.That((float)reward / cost, Is.EqualTo(2f));
        }

        [Test]
        public void FixedScalingLeavesSharedCardEffectsUnchanged()
        {
            EventMagnitudeSnapshot snapshot = CreateRuntime().Capture(
                CreateInput(day: 91, activeBuildingLevels: 30));
            GameEffectData effect = CreateEffect(
                ValueEffectType.AddFood,
                2,
                CreateScaling(EventScaleSource.Fixed, 1f));

            ResolvedGameEffectRuntime resolved = ResolvedGameEffectRuntime.Create(effect, snapshot);
            effect.TryApply(GameEffectContext.ForOther());

            Assert.That(resolved.FinalAmount, Is.EqualTo(2));
            Assert.That(FoodSystem.Food, Is.EqualTo(2));
        }

        [Test]
        public void EventFlowAppliesTheAmountLockedWhenEventOpened()
        {
            GameEffectData reward = CreateEffect(
                ValueEffectType.AddFood,
                2,
                CreateScaling(EventScaleSource.EventMagnitude, 1f));
            EventData eventData = CreateEvent(
                "EVENT-FLOW-SNAPSHOT",
                "取得 {value:0} 食物",
                "取得 {value:0} 食物",
                reward);
            GameObject owner = new GameObject("EventMagnitudeFlowTest");
            createdObjects.Add(owner);
            EventFlowController flow = owner.AddComponent<EventFlowController>();
            SetField(flow, "eventPool", new List<EventData> { eventData });
            flow.InitializeRun(1234);

            flow.BeginEventPhase(21);
            int lockedAmount = flow.CurrentResolvedEvent.Choices[0].Effects[0].FinalAmount;
            flow.SetOtherPermanentDevelopmentModifiers(30);
            bool selected = flow.ChooseOption(0);

            Assert.That(lockedAmount, Is.EqualTo(6));
            Assert.That(selected, Is.True);
            Assert.That(FoodSystem.Food, Is.EqualTo(6));
        }

        [Test]
        public void EventFlowUsesThreatRuntimeForWorldThreatScaling()
        {
            GameEffectData reward = CreateEffect(
                ValueEffectType.AddFood,
                2,
                CreateScaling(EventScaleSource.WorldThreat, 1f));
            EventData eventData = CreateEvent(
                "EVENT-WORLD-THREAT-SNAPSHOT",
                "?? {value:0} 憌",
                "?? {value:0} 憌",
                reward);
            GameObject owner = new GameObject("EventWorldThreatFlowTest");
            createdObjects.Add(owner);
            EventFlowController flow = owner.AddComponent<EventFlowController>();
            SetField(flow, "eventPool", new List<EventData> { eventData });
            flow.InitializeRun(1234);

            flow.BeginEventPhase(30);
            int lockedAmount = flow.CurrentResolvedEvent.Choices[0].Effects[0].FinalAmount;
            bool selected = flow.ChooseOption(0);

            Assert.That(flow.LastThreatSnapshot.WorldThreatTier, Is.EqualTo(2));
            Assert.That(lockedAmount, Is.EqualTo(4));
            Assert.That(selected, Is.True);
            Assert.That(FoodSystem.Food, Is.EqualTo(4));
        }

        private static EventMagnitudeRuntime CreateRuntime()
        {
            EventMagnitudeRuntime runtime = new EventMagnitudeRuntime();
            runtime.InitializeRun();
            return runtime;
        }

        private static EventMagnitudeInputSnapshot CreateInput(
            int day,
            int activeBuildingLevels = 0,
            int foodStorageCapacity = 50,
            int foodStorageBonus = 0)
        {
            return new EventMagnitudeInputSnapshot(
                day,
                survivorCount: 1,
                additionalSurvivorCount: 0,
                activeBuildingLevelSum: activeBuildingLevels,
                permanentSlotUpgradeCount: 0,
                otherPermanentDevelopmentModifiers: 0,
                maxEnergy: 1,
                foodStorageCapacity: foodStorageCapacity,
                foodStorageCapacityBonus: foodStorageBonus);
        }

        private static EventEffectScalingData CreateScaling(
            EventScaleSource source,
            float strength,
            int minimum = 0,
            int maximum = 0,
            EventScaleRoundingMode rounding = EventScaleRoundingMode.Round)
        {
            EventEffectScalingData scaling = new EventEffectScalingData();
            SetField(scaling, "scaleSource", source);
            SetField(scaling, "scalingStrength", strength);
            SetField(scaling, "minimumFinalMagnitude", minimum);
            SetField(scaling, "maximumFinalMagnitude", maximum);
            SetField(scaling, "roundingMode", rounding);
            return scaling;
        }

        private static GameEffectData CreateEffect(
            ValueEffectType type,
            int amount,
            EventEffectScalingData scaling)
        {
            GameEffectData effect = new GameEffectData();
            SetField(effect, "carrier", GameEffectCarrier.Event);
            SetField(effect, "category", GameEffectCategory.Value);
            SetField(effect, "valueEffectType", type);
            SetField(effect, "amount", amount);
            SetField(effect, "eventScaling", scaling);
            return effect;
        }

        private EventData CreateEvent(
            string eventId,
            string choiceText,
            string resultDescription,
            params GameEffectData[] effects)
        {
            EventChoiceData choice = new EventChoiceData();
            SetField(choice, "choiceText", choiceText);
            SetField(choice, "resultDescription", resultDescription);
            SetField(choice, "effects", new List<GameEffectData>(effects));

            EventData eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.name = eventId;
            SetField(eventData, "eventId", eventId);
            SetField(eventData, "choices", new List<EventChoiceData> { choice });
            createdObjects.Add(eventData);
            return eventData;
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
