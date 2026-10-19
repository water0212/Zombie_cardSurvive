using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Tests.Editor
{
    public class EventSchedulerTests
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
        public void SchedulerExcludesEventsThatFailDateOrConditions()
        {
            EventData tooEarly = CreateEvent("EVENT-EARLY", earliestDay: 3);
            EventData moraleLocked = CreateEvent("EVENT-MORALE");
            AddCondition(moraleLocked, EventConditionType.Morale, EventValueComparison.AtLeast, 60);
            EventData valid = CreateEvent("EVENT-VALID");
            EventScheduler scheduler = CreateScheduler(10, tooEarly, moraleLocked, valid);

            bool selected = scheduler.TrySelectAndRecord(CreateContext(day: 1, morale: 50), out EventSelectionResult result);

            Assert.That(selected, Is.True);
            Assert.That(result.SelectedEvent, Is.SameAs(valid));
            Assert.That(GetEvaluation(result, tooEarly).RejectionReason, Is.EqualTo(EventRejectionReason.BeforeEarliestDay));
            Assert.That(GetEvaluation(result, moraleLocked).RejectionReason, Is.EqualTo(EventRejectionReason.ConditionFailed));
        }

        [Test]
        public void OneTimeEventCannotAppearTwiceInOneRun()
        {
            EventData oneTime = CreateEvent("EVENT-ONCE", canRepeat: false, minimumDayGap: 0);
            EventScheduler scheduler = CreateScheduler(20, oneTime);

            Assert.That(scheduler.TrySelectAndRecord(CreateContext(1), out _), Is.True);
            Assert.That(scheduler.TrySelectAndRecord(CreateContext(2), out EventSelectionResult second), Is.False);

            Assert.That(scheduler.History.GetAppearanceCount(oneTime.EventId), Is.EqualTo(1));
            Assert.That(GetEvaluation(second, oneTime).RejectionReason, Is.EqualTo(EventRejectionReason.RepeatLimitReached));
        }

        [Test]
        public void RepeatableEventHonorsCompleteDayGap()
        {
            EventData repeatable = CreateEvent("EVENT-REPEAT", minimumDayGap: 1);
            EventScheduler scheduler = CreateScheduler(30, repeatable);

            Assert.That(scheduler.TrySelectAndRecord(CreateContext(1), out _), Is.True);
            Assert.That(scheduler.TrySelectAndRecord(CreateContext(2), out EventSelectionResult dayTwo), Is.False);
            Assert.That(GetEvaluation(dayTwo, repeatable).RejectionReason, Is.EqualTo(EventRejectionReason.CooldownActive));
            Assert.That(scheduler.TrySelectAndRecord(CreateContext(3), out _), Is.True);
        }

        [Test]
        public void SameSeedProducesSameEventSequence()
        {
            EventData first = CreateEvent("EVENT-A", minimumDayGap: 0);
            EventData second = CreateEvent("EVENT-B", minimumDayGap: 0);
            EventData third = CreateEvent("EVENT-C", minimumDayGap: 0);
            EventScheduler left = CreateScheduler(91234, first, second, third);
            EventScheduler right = CreateScheduler(91234, first, second, third);
            List<string> leftSequence = new List<string>();
            List<string> rightSequence = new List<string>();

            for (int day = 1; day <= 20; day++)
            {
                Assert.That(left.TrySelectAndRecord(CreateContext(day), out EventSelectionResult leftResult), Is.True);
                Assert.That(right.TrySelectAndRecord(CreateContext(day), out EventSelectionResult rightResult), Is.True);
                leftSequence.Add(leftResult.SelectedEvent.EventId);
                rightSequence.Add(rightResult.SelectedEvent.EventId);
            }

            CollectionAssert.AreEqual(leftSequence, rightSequence);
        }

        [Test]
        public void LowMoraleChangesPositiveAndNegativeWeightsWithoutHardExclusion()
        {
            EventData positive = CreateEvent("EVENT-POSITIVE", mood: EventMoodCategory.Positive);
            EventData negative = CreateEvent("EVENT-NEGATIVE", mood: EventMoodCategory.Negative);
            EventScheduler scheduler = CreateScheduler(40, positive, negative);

            Assert.That(scheduler.TrySelectAndRecord(CreateContext(day: 1, morale: 20), out EventSelectionResult result), Is.True);

            Assert.That(GetEvaluation(result, positive).FinalWeight, Is.EqualTo(125f).Within(0.001f));
            Assert.That(GetEvaluation(result, negative).FinalWeight, Is.EqualTo(75f).Within(0.001f));
        }

        [Test]
        public void ConditionGroupCanReadBuildingLevelAndRunFlag()
        {
            EventData eventData = CreateEvent("EVENT-COMPOUND");
            AddConditions(
                eventData,
                new ConditionSetup(EventConditionType.BuildingLevel, EventValueComparison.AtLeast, 2, "BUILD-WORKBENCH"),
                new ConditionSetup(EventConditionType.RunFlag, EventValueComparison.Equal, 1, "FOUND-SIGNAL"));
            EventScheduler scheduler = CreateScheduler(50, eventData);
            EventSelectionContext context = new EventSelectionContext(
                5, 50, 0, 0, 1, 0,
                id => id == "BUILD-WORKBENCH" ? 2 : 0,
                id => id == "FOUND-SIGNAL");

            Assert.That(scheduler.TrySelectAndRecord(context, out EventSelectionResult result), Is.True);
            Assert.That(result.SelectedEvent, Is.SameAs(eventData));
        }

        [Test]
        public void EventFlowUsesNeutralFallbackWhenNoEventIsLegal()
        {
            GameObject owner = new GameObject("EventFlowTest");
            createdObjects.Add(owner);
            EventFlowController flow = owner.AddComponent<EventFlowController>();
            flow.InitializeRun(60);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("No legal daily event was found for day 1.*neutral fallback"));

            flow.BeginEventPhase(1);

            Assert.That(flow.State, Is.EqualTo(EventFlowState.Result));
            Assert.That(flow.CurrentEvent, Is.Null);
            Assert.That(flow.IsNeutralFallback, Is.True);
            Assert.That(flow.NeutralFallbackTitle, Is.Not.Empty);
        }

        [Test]
        public void ActiveSequenceSourceCompletelyOverridesGeneralPool()
        {
            EventData general = CreateEvent("EVENT-GENERAL");
            EventData ending = CreateEvent("EVENT-ENDING");
            GameObject owner = new GameObject("EventFlowOverrideTest");
            createdObjects.Add(owner);
            EventFlowController flow = owner.AddComponent<EventFlowController>();
            SetEventPool(flow, general);
            flow.SetEventSequenceSource(new FixedSequenceSource(ending));
            flow.InitializeRun(70);

            flow.BeginEventPhase(4);

            Assert.That(flow.CurrentEvent, Is.SameAs(ending));
            Assert.That(flow.EventHistory.GetAppearanceCount(ending.EventId), Is.EqualTo(1));
            Assert.That(flow.EventHistory.GetAppearanceCount(general.EventId), Is.Zero);
        }

        private EventScheduler CreateScheduler(int seed, params EventData[] events)
        {
            EventScheduler scheduler = new EventScheduler();
            scheduler.InitializeRun(events, null, seed);
            return scheduler;
        }

        private EventData CreateEvent(
            string id,
            EventMoodCategory mood = EventMoodCategory.Mixed,
            int earliestDay = 1,
            int latestDay = 0,
            bool canRepeat = true,
            int maximumAppearances = 0,
            int minimumDayGap = 0)
        {
            EventData eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.name = id;
            createdObjects.Add(eventData);
            SerializedObject serialized = new SerializedObject(eventData);
            serialized.FindProperty("eventId").stringValue = id;
            serialized.FindProperty("displayTitle").stringValue = id;
            serialized.FindProperty("moodCategory").enumValueIndex = (int)mood;
            serialized.FindProperty("earliestDay").intValue = earliestDay;
            serialized.FindProperty("latestDay").intValue = latestDay;
            serialized.FindProperty("baseWeight").floatValue = 100f;
            serialized.FindProperty("canRepeat").boolValue = canRepeat;
            serialized.FindProperty("maximumAppearancesPerRun").intValue = maximumAppearances;
            serialized.FindProperty("minimumDaysBetweenAppearances").intValue = minimumDayGap;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return eventData;
        }

        private static EventSelectionContext CreateContext(int day, int morale = 50)
        {
            return new EventSelectionContext(day, morale, 0, 0, 1, 0);
        }

        private static void AddCondition(
            EventData eventData,
            EventConditionType type,
            EventValueComparison comparison,
            int requiredValue,
            string targetId = "")
        {
            AddConditions(eventData, new ConditionSetup(type, comparison, requiredValue, targetId));
        }

        private static void AddConditions(EventData eventData, params ConditionSetup[] setups)
        {
            SerializedObject serialized = new SerializedObject(eventData);
            SerializedProperty group = serialized.FindProperty("eligibilityConditions");
            SerializedProperty conditions = group.FindPropertyRelative("conditions");
            conditions.arraySize = setups.Length;

            for (int index = 0; index < setups.Length; index++)
            {
                ConditionSetup setup = setups[index];
                SerializedProperty entry = conditions.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("conditionType").enumValueIndex = (int)setup.Type;
                entry.FindPropertyRelative("comparison").enumValueIndex = (int)setup.Comparison;
                entry.FindPropertyRelative("requiredValue").intValue = setup.RequiredValue;
                entry.FindPropertyRelative("targetId").stringValue = setup.TargetId;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static void SetEventPool(EventFlowController flow, params EventData[] events)
        {
            SerializedObject serialized = new SerializedObject(flow);
            SerializedProperty pool = serialized.FindProperty("eventPool");
            pool.arraySize = events.Length;
            for (int index = 0; index < events.Length; index++)
            {
                pool.GetArrayElementAtIndex(index).objectReferenceValue = events[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct ConditionSetup
        {
            public ConditionSetup(
                EventConditionType type,
                EventValueComparison comparison,
                int requiredValue,
                string targetId)
            {
                Type = type;
                Comparison = comparison;
                RequiredValue = requiredValue;
                TargetId = targetId;
            }

            public EventConditionType Type { get; }
            public EventValueComparison Comparison { get; }
            public int RequiredValue { get; }
            public string TargetId { get; }
        }

        private sealed class FixedSequenceSource : IEventSequenceSource
        {
            private readonly EventData eventData;

            public FixedSequenceSource(EventData eventData)
            {
                this.eventData = eventData;
            }

            public bool IsActive => true;

            public bool TryGetNextEvent(int day, out EventData selectedEvent)
            {
                selectedEvent = eventData;
                return selectedEvent != null;
            }
        }
    }
}
