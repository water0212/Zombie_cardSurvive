using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Editor;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class EventContentV1Tests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
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
        public void SpecificationsUseSequentialUniqueIdsAndApprovedLatestDays()
        {
            IReadOnlyList<EventContentSpec> specs = EventContentV1Specification.Events;

            Assert.That(specs.Count, Is.EqualTo(14));
            Assert.That(specs.Select(spec => spec.EventId).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(14));
            for (int index = 0; index < specs.Count; index++)
            {
                Assert.That(specs[index].EventId, Is.EqualTo($"EVT-{index + 1:000}"));
                if (specs[index].CanRepeat)
                {
                    Assert.That(specs[index].LatestDay, Is.Zero, specs[index].EventId);
                }
            }
        }

        [Test]
        public void SpecificationsContainThreeReadyEventsAndExplainEveryBlockedEvent()
        {
            EventContentSpec[] ready = EventContentV1Specification.Events
                .Where(spec => spec.IncludeInPlayablePool)
                .ToArray();
            EventContentSpec[] blocked = EventContentV1Specification.Events
                .Where(spec => !spec.IncludeInPlayablePool)
                .ToArray();

            Assert.That(ready.Select(spec => spec.EventId), Is.EqualTo(new[] { "EVT-003", "EVT-008", "EVT-014" }));
            Assert.That(blocked.Length, Is.EqualTo(11));
            Assert.That(blocked.All(spec => !string.IsNullOrWhiteSpace(spec.ImplementationNotes)), Is.True);
            Assert.That(blocked.SelectMany(spec => spec.Choices).All(choice => choice.Effects.Count == 0), Is.True);
        }

        [Test]
        public void NightTemperatureUsesExecutableMaxEnergyScaledEffects()
        {
            EventContentSpec nightTemperature = EventContentV1Specification.Events
                .Single(spec => spec.EventId == "EVT-003");

            Assert.That(nightTemperature.Choices.Count, Is.EqualTo(3));
            Assert.That(nightTemperature.Choices.SelectMany(choice => choice.Effects).Count(), Is.EqualTo(4));
            Assert.That(
                nightTemperature.Choices.SelectMany(choice => choice.Effects)
                    .All(effect => effect.ScaleSource == EventScaleSource.MaxEnergy),
                Is.True);
        }

        [Test]
        public void FixedSeedScheduleIsReproducibleAndHasNoFallbackDays()
        {
            List<EventData> events = EventContentV1Specification.Events.Select(CreateTransientEvent).ToList();

            EventScheduleSimulationResult first = EventScheduleSimulationUtility.Run(events, null, 30, 14014);
            EventScheduleSimulationResult second = EventScheduleSimulationUtility.Run(events, null, 30, 14014);

            CollectionAssert.AreEqual(
                first.Days.Select(day => day.EventData != null ? day.EventData.EventId : "FALLBACK").ToArray(),
                second.Days.Select(day => day.EventData != null ? day.EventData.EventId : "FALLBACK").ToArray());
            Assert.That(first.FallbackCount, Is.Zero);
            Assert.That(
                first.EventCounts.TryGetValue("EVT-010", out int oneTimeCount) ? oneTimeCount : 0,
                Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void ImportedAssetsAndSampleSceneContainAllReadyPlayableEvents()
        {
            Dictionary<string, EventData> assets = LoadCanonicalAssets();
            Assert.That(assets.Count, Is.EqualTo(14), "Run Setup Event Content V1 before this content test.");
            Assert.That(assets.Values.Count(eventData => eventData.IsImplementationReady), Is.EqualTo(3));
            Assert.That(assets["EVT-003"].DisplayTitle, Is.EqualTo("夜間低溫"));
            Assert.That(assets["EVT-003"].Choices.SelectMany(choice => choice.Effects).Count(), Is.EqualTo(4));
            Assert.That(assets["EVT-008"].Choices.SelectMany(choice => choice.Effects).Count(), Is.EqualTo(6));
            Assert.That(assets["EVT-014"].Choices.SelectMany(choice => choice.Effects).Count(), Is.EqualTo(2));
            Assert.That(
                assets.Where(pair => pair.Key != "EVT-003" && pair.Key != "EVT-008" && pair.Key != "EVT-014")
                    .SelectMany(pair => pair.Value.Choices)
                    .All(choice => choice.Effects.Count == 0),
                Is.True);

            Scene scene = SceneManager.GetSceneByPath(EventContentV1SetupUtility.ScenePath);
            bool closeWhenDone = !scene.IsValid() || !scene.isLoaded;
            if (closeWhenDone)
            {
                scene = EditorSceneManager.OpenScene(EventContentV1SetupUtility.ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                EventFlowController controller = FindSceneComponents<EventFlowController>(scene).Single();
                SerializedProperty pool = new SerializedObject(controller).FindProperty("eventPool");
                Assert.That(pool.arraySize, Is.EqualTo(3));
                Assert.That(
                    pool.GetArrayElementAtIndex(0).objectReferenceValue as EventData,
                    Is.SameAs(assets["EVT-003"]));
                Assert.That(
                    pool.GetArrayElementAtIndex(1).objectReferenceValue as EventData,
                    Is.SameAs(assets["EVT-008"]));
                Assert.That(
                    pool.GetArrayElementAtIndex(2).objectReferenceValue as EventData,
                    Is.SameAs(assets["EVT-014"]));
            }
            finally
            {
                if (closeWhenDone && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private EventData CreateTransientEvent(EventContentSpec spec)
        {
            EventData eventData = ScriptableObject.CreateInstance<EventData>();
            createdObjects.Add(eventData);
            SerializedObject serialized = new SerializedObject(eventData);
            serialized.FindProperty("eventId").stringValue = spec.EventId;
            serialized.FindProperty("displayTitle").stringValue = spec.Title;
            serialized.FindProperty("moodCategory").enumValueIndex = (int)spec.Mood;
            serialized.FindProperty("contentCategory").enumValueIndex = (int)spec.Content;
            serialized.FindProperty("earliestDay").intValue = spec.EarliestDay;
            serialized.FindProperty("latestDay").intValue = spec.LatestDay;
            serialized.FindProperty("baseWeight").floatValue = spec.Weight;
            serialized.FindProperty("canRepeat").boolValue = spec.CanRepeat;
            serialized.FindProperty("maximumAppearancesPerRun").intValue = spec.MaximumAppearances;
            serialized.FindProperty("minimumDaysBetweenAppearances").intValue = spec.CooldownDays;
            SetConditions(serialized.FindProperty("eligibilityConditions"), spec.Conditions);
            SetWeightModifiers(serialized.FindProperty("weightModifiers"), spec.WeightModifiers);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return eventData;
        }

        private static void SetConditions(
            SerializedProperty group,
            IReadOnlyList<EventContentConditionSpec> conditions)
        {
            group.FindPropertyRelative("matchMode").enumValueIndex = (int)EventConditionMatchMode.All;
            group.FindPropertyRelative("invertResult").boolValue = false;
            SerializedProperty entries = group.FindPropertyRelative("conditions");
            entries.arraySize = conditions?.Count ?? 0;
            for (int index = 0; index < entries.arraySize; index++)
            {
                EventContentConditionSpec spec = conditions[index];
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("displayName").stringValue = spec.DisplayName ?? string.Empty;
                entry.FindPropertyRelative("conditionType").enumValueIndex = (int)spec.ConditionType;
                entry.FindPropertyRelative("comparison").enumValueIndex = (int)spec.Comparison;
                entry.FindPropertyRelative("targetId").stringValue = spec.TargetId ?? string.Empty;
                entry.FindPropertyRelative("runFlag").objectReferenceValue = null;
                entry.FindPropertyRelative("requiredValue").intValue = spec.RequiredValue;
            }
        }

        private static void SetWeightModifiers(
            SerializedProperty entries,
            IReadOnlyList<EventContentWeightModifierSpec> modifiers)
        {
            entries.arraySize = modifiers?.Count ?? 0;
            for (int index = 0; index < entries.arraySize; index++)
            {
                EventContentWeightModifierSpec spec = modifiers[index];
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("displayName").stringValue = spec.DisplayName ?? string.Empty;
                entry.FindPropertyRelative("source").enumValueIndex = (int)spec.Source;
                entry.FindPropertyRelative("runFlag").objectReferenceValue = null;
                entry.FindPropertyRelative("targetId").stringValue = spec.TargetId ?? string.Empty;
                entry.FindPropertyRelative("ignoredUnitsBeforeBonus").intValue = spec.IgnoredUnitsBeforeBonus;
                entry.FindPropertyRelative("weightPerUnit").floatValue = spec.WeightPerUnit;
                entry.FindPropertyRelative("minimumBonus").floatValue = spec.MinimumBonus;
                entry.FindPropertyRelative("maximumBonus").floatValue = spec.MaximumBonus;
            }
        }

        private static Dictionary<string, EventData> LoadCanonicalAssets()
        {
            HashSet<string> ids = new HashSet<string>(
                EventContentV1Specification.Events.Select(spec => spec.EventId),
                StringComparer.OrdinalIgnoreCase);
            Dictionary<string, EventData> result =
                new Dictionary<string, EventData>(StringComparer.OrdinalIgnoreCase);
            foreach (string guid in AssetDatabase.FindAssets("t:EventData"))
            {
                EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>(AssetDatabase.GUIDToAssetPath(guid));
                if (eventData == null || !ids.Contains(eventData.EventId))
                {
                    continue;
                }

                Assert.That(result.ContainsKey(eventData.EventId), Is.False, $"Duplicate {eventData.EventId}");
                result.Add(eventData.EventId, eventData);
            }

            return result;
        }

        private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> components = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                components.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return components;
        }
    }
}
