using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Editor;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class EventRunFlagTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
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
        public void RuntimeStore_UsesAssetDefinitionWithoutMutatingTheAsset()
        {
            RunFlagData flag = CreateFlag("FLAG-HORDE", defaultValue: 0, min: 0, max: 1);
            RunFlagRuntimeStore store = new RunFlagRuntimeStore();

            int value = store.AddValue(flag, 3, day: 5);

            Assert.That(value, Is.EqualTo(1));
            Assert.That(store.GetValue(flag), Is.EqualTo(1));
            Assert.That(store.GetDaysSinceFirstSet(flag, 8), Is.EqualTo(3));
            Assert.That(flag.DefaultValue, Is.Zero);
            Assert.That(flag.MaximumValue, Is.EqualTo(1));
        }

        [Test]
        public void Scheduler_RejectsFlagLockedEventUntilRuntimeFlagIsSet()
        {
            RunFlagData flag = CreateFlag("FLAG-HORDE", defaultValue: 0, min: 0, max: 1);
            EventData horde = CreateEvent("EVT-HORDE");
            AddCondition(
                horde,
                EventConditionType.RunFlagValue,
                EventValueComparison.AtLeast,
                1,
                flag);
            RunFlagRuntimeStore store = new RunFlagRuntimeStore();
            EventScheduler scheduler = CreateScheduler(10, horde);

            Assert.That(
                scheduler.TrySelectAndRecord(CreateContext(9, store), out EventSelectionResult blocked),
                Is.False);
            Assert.That(GetEvaluation(blocked, horde).RejectionReason, Is.EqualTo(EventRejectionReason.ConditionFailed));

            store.AddValue(flag, 1, day: 9);

            Assert.That(
                scheduler.TrySelectAndRecord(CreateContext(10, store), out EventSelectionResult selected),
                Is.True);
            Assert.That(selected.SelectedEvent, Is.SameAs(horde));
        }

        [Test]
        public void Scheduler_AddsWaitingWeightAfterConfiguredIgnoredDays()
        {
            RunFlagData flag = CreateFlag("FLAG-HORDE", defaultValue: 0, min: 0, max: 1);
            EventData horde = CreateEvent("EVT-HORDE", baseWeight: 50f);
            AddCondition(
                horde,
                EventConditionType.RunFlagValue,
                EventValueComparison.AtLeast,
                1,
                flag);
            AddWeightModifier(
                horde,
                EventWeightModifierSource.RunFlagDaysSinceSet,
                flag,
                weightPerUnit: 50f,
                ignoredUnitsBeforeBonus: 1);

            RunFlagRuntimeStore store = new RunFlagRuntimeStore();
            store.AddValue(flag, 1, day: 8);

            EventScheduler scheduler = CreateScheduler(20, horde);
            Assert.That(scheduler.TrySelectAndRecord(CreateContext(9, store), out EventSelectionResult dayNine), Is.True);
            Assert.That(GetEvaluation(dayNine, horde).FinalWeight, Is.EqualTo(50f).Within(0.001f));

            scheduler = CreateScheduler(21, horde);
            Assert.That(scheduler.TrySelectAndRecord(CreateContext(10, store), out EventSelectionResult dayTen), Is.True);
            Assert.That(GetEvaluation(dayTen, horde).FinalWeight, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void EventStateEffects_CanAddAndClearRunFlags()
        {
            RunFlagData flag = CreateFlag("FLAG-HORDE", defaultValue: 0, min: 0, max: 1);
            RunFlagRuntimeStore store = new RunFlagRuntimeStore();
            ResolvedEventChoiceRuntime addChoice = CreateChoice(CreateRunFlagEffect(
                EventStateEffectType.AddRunFlagValue,
                flag,
                1));
            ResolvedEventChoiceRuntime clearChoice = CreateChoice(CreateRunFlagEffect(
                EventStateEffectType.ClearRunFlag,
                flag,
                0));

            Assert.That(
                addChoice.TryExecute(GameEffectContext.ForEvent(store, 4), out EventChoiceExecutionResult addResult),
                Is.True);
            Assert.That(addResult.Succeeded, Is.True);
            Assert.That(store.GetValue(flag), Is.EqualTo(1));
            Assert.That(store.GetDaysSinceFirstSet(flag, 6), Is.EqualTo(2));

            Assert.That(
                clearChoice.TryExecute(GameEffectContext.ForEvent(store, 7), out EventChoiceExecutionResult clearResult),
                Is.True);
            Assert.That(clearResult.Succeeded, Is.True);
            Assert.That(store.GetValue(flag), Is.Zero);
            Assert.That(store.GetDaysSinceFirstSet(flag, 8), Is.Zero);
        }

        [Test]
        public void EventContentSpecification_GatesHordeArrivalWithPendingFlag()
        {
            EventContentSpec horde = EventContentV1Specification.Events.Single(spec => spec.EventId == "EVT-014");

            Assert.That(EventContentV1Specification.RunFlags.Select(flag => flag.FlagId), Does.Contain("FLAG_HORDE_PENDING"));
            Assert.That(
                horde.Conditions.Any(condition =>
                    condition.ConditionType == EventConditionType.RunFlagValue
                    && condition.TargetId == "FLAG_HORDE_PENDING"
                    && condition.RequiredValue == 1),
                Is.True);
            Assert.That(
                horde.WeightModifiers.Any(modifier =>
                    modifier.Source == EventWeightModifierSource.RunFlagDaysSinceSet
                    && modifier.TargetId == "FLAG_HORDE_PENDING"
                    && modifier.WeightPerUnit == 50f
                    && modifier.IgnoredUnitsBeforeBonus == 1),
                Is.True);
        }

        private EventScheduler CreateScheduler(int seed, params EventData[] events)
        {
            EventScheduler scheduler = new EventScheduler();
            scheduler.InitializeRun(events, null, seed);
            return scheduler;
        }

        private static EventSelectionContext CreateContext(int day, RunFlagRuntimeStore store)
        {
            return new EventSelectionContext(
                day,
                morale: 50,
                food: 0,
                resource: 0,
                maxEnergy: 1,
                activeZombieCount: 0,
                runFlagStore: store);
        }

        private RunFlagData CreateFlag(string id, int defaultValue, int min, int max)
        {
            RunFlagData flag = ScriptableObject.CreateInstance<RunFlagData>();
            flag.name = id;
            createdObjects.Add(flag);
            SetField(flag, "flagId", id);
            SetField(flag, "displayName", id);
            SetField(flag, "defaultValue", defaultValue);
            SetField(flag, "minimumValue", min);
            SetField(flag, "maximumValue", max);
            return flag;
        }

        private EventData CreateEvent(string id, float baseWeight = 100f)
        {
            EventData eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.name = id;
            createdObjects.Add(eventData);
            SetField(eventData, "eventId", id);
            SetField(eventData, "displayTitle", id);
            SetField(eventData, "baseWeight", baseWeight);
            SetField(eventData, "minimumDaysBetweenAppearances", 0);
            return eventData;
        }

        private static void AddCondition(
            EventData eventData,
            EventConditionType type,
            EventValueComparison comparison,
            int requiredValue,
            RunFlagData flag)
        {
            SerializedObject serialized = new SerializedObject(eventData);
            SerializedProperty group = serialized.FindProperty("eligibilityConditions");
            SerializedProperty conditions = group.FindPropertyRelative("conditions");
            conditions.arraySize = 1;
            SerializedProperty entry = conditions.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("conditionType").enumValueIndex = (int)type;
            entry.FindPropertyRelative("comparison").enumValueIndex = (int)comparison;
            entry.FindPropertyRelative("targetId").stringValue = flag.FlagId;
            entry.FindPropertyRelative("runFlag").objectReferenceValue = flag;
            entry.FindPropertyRelative("requiredValue").intValue = requiredValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddWeightModifier(
            EventData eventData,
            EventWeightModifierSource source,
            RunFlagData flag,
            float weightPerUnit,
            int ignoredUnitsBeforeBonus)
        {
            SerializedObject serialized = new SerializedObject(eventData);
            SerializedProperty modifiers = serialized.FindProperty("weightModifiers");
            modifiers.arraySize = 1;
            SerializedProperty entry = modifiers.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("source").enumValueIndex = (int)source;
            entry.FindPropertyRelative("runFlag").objectReferenceValue = flag;
            entry.FindPropertyRelative("targetId").stringValue = flag.FlagId;
            entry.FindPropertyRelative("ignoredUnitsBeforeBonus").intValue = ignoredUnitsBeforeBonus;
            entry.FindPropertyRelative("weightPerUnit").floatValue = weightPerUnit;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameEffectData CreateRunFlagEffect(
            EventStateEffectType effectType,
            RunFlagData flag,
            int amount)
        {
            GameEffectData effect = new GameEffectData();
            SetField(effect, "carrier", GameEffectCarrier.Event);
            SetField(effect, "category", GameEffectCategory.EventState);
            SetField(effect, "eventStateEffectType", effectType);
            SetField(effect, "runFlagData", flag);
            SetField(effect, "amount", amount);
            return effect;
        }

        private static ResolvedEventChoiceRuntime CreateChoice(GameEffectData effect)
        {
            EventChoiceData choice = new EventChoiceData();
            SetField(choice, "choiceText", "Test choice");
            SetField(choice, "effects", new List<GameEffectData> { effect });
            return ResolvedEventChoiceRuntime.Create(choice, null);
        }

        private static EventCandidateEvaluation GetEvaluation(EventSelectionResult result, EventData eventData)
        {
            foreach (EventCandidateEvaluation evaluation in result.Evaluations)
            {
                if (ReferenceEquals(evaluation.EventData, eventData))
                {
                    return evaluation;
                }
            }

            Assert.Fail($"No evaluation was found for {eventData.name}.");
            return default;
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
