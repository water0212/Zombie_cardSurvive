#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Threats.Data;
using ZombieCardSurvive.Threats.Editor;

namespace ZombieCardSurvive.Events.Editor
{
    public static class EventContentV1SetupUtility
    {
        public const string EventAssetFolder = "Assets/ScriptObj/EventScrpitObj/ContentV1";
        public const string RunFlagAssetFolder = "Assets/ScriptObj/EventScrpitObj/RunFlags";
        public const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Zombie Card Survive/Setup Event Content V1")]
        public static void Setup()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            SceneContext sceneContext = null;

            try
            {
                ValidateSpecifications(errors, warnings);
                Dictionary<string, EventData> existingById = BuildExistingEventIndex(errors);
                ValidateAssetTargets(existingById, errors);
                if (errors.Count > 0)
                {
                    LogReport("Event Content V1 setup aborted", errors, warnings);
                    return;
                }

                EnsureFolder("Assets/ScriptObj/EventScrpitObj", "ContentV1");
                EnsureFolder("Assets/ScriptObj/EventScrpitObj", "RunFlags");
                HordeContentV1SetupUtility.CreateOrUpdateContent(
                    errors,
                    out Dictionary<string, ZombieCardSurvive.Cards.Data.ZombieCardData> importedZombies,
                    out Dictionary<string, HordeProfileData> importedProfiles);
                Dictionary<string, RunFlagData> importedFlags = CreateOrUpdateRunFlags(errors);
                Dictionary<string, EventData> imported = CreateOrUpdateEvents(
                    existingById,
                    importedFlags,
                    importedProfiles,
                    errors);
                if (errors.Count > 0)
                {
                    LogReport("Event Content V1 asset update failed", errors, warnings);
                    return;
                }

                if (!TryOpenEventFlow(errors, out sceneContext))
                {
                    LogReport("Event Content V1 scene wiring failed", errors, warnings);
                    return;
                }

                WirePlayablePool(sceneContext.EventFlowController, imported);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(sceneContext.Scene);
                EditorSceneManager.SaveScene(sceneContext.Scene);
                AssetDatabase.Refresh();

                ValidateImportedFlags(importedFlags, errors);
                HordeContentV1SetupUtility.ValidateContent(
                    importedZombies,
                    importedProfiles,
                    errors);
                ValidateImportedAssets(imported, errors);
                ValidatePlayablePool(sceneContext.EventFlowController, imported, errors);
                if (errors.Count > 0)
                {
                    LogReport("Event Content V1 post-import validation failed", errors, warnings);
                    return;
                }

                LogWarnings(warnings);
                int readyCount = EventContentV1Specification.Events.Count(spec => spec.IncludeInPlayablePool);
                Debug.Log(
                    $"Event Content V1 setup complete: {imported.Count} event assets, " +
                    $"{importedFlags.Count} run flag assets, " +
                    $"{importedProfiles.Count} horde profile asset, " +
                    $"{readyCount} playable event, and {imported.Count - readyCount} blocked event assets.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Event Content V1 setup failed. Inspect the exception before running setup again.");
            }
            finally
            {
                sceneContext?.Dispose();
            }
        }

        [MenuItem("Tools/Zombie Card Survive/Validate Event Content V1")]
        public static void Validate()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            SceneContext sceneContext = null;

            try
            {
                ValidateSpecifications(errors, warnings);
                Dictionary<string, EventData> imported = BuildExistingEventIndex(errors, canonicalOnly: true);
                Dictionary<string, RunFlagData> flags = BuildExistingRunFlagIndex();
                Dictionary<string, ZombieCardSurvive.Cards.Data.ZombieCardData> zombies =
                    HordeContentV1SetupUtility.LoadZombieAssets();
                Dictionary<string, HordeProfileData> profiles =
                    HordeContentV1SetupUtility.LoadProfileAssets();
                ValidateImportedFlags(flags, errors);
                HordeContentV1SetupUtility.ValidateContent(zombies, profiles, errors);
                ValidateImportedAssets(imported, errors);
                if (TryOpenEventFlow(errors, out sceneContext))
                {
                    ValidatePlayablePool(sceneContext.EventFlowController, imported, errors);
                }

                if (errors.Count > 0)
                {
                    LogReport("Event Content V1 validation failed", errors, warnings);
                    return;
                }

                LogWarnings(warnings);
                Debug.Log(
                    $"Event Content V1 validation passed: {imported.Count} canonical assets and " +
                    $"{EventContentV1Specification.Events.Count(spec => spec.IncludeInPlayablePool)} playable event.");
            }
            finally
            {
                sceneContext?.Dispose();
            }
        }

        public static void ValidateSpecifications(List<string> errors, List<string> warnings)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            warnings = warnings ?? new List<string>();
            IReadOnlyList<EventContentSpec> specs = EventContentV1Specification.Events;
            if (specs.Count != 14)
            {
                errors.Add($"Expected 14 Event Schedule rows, but found {specs.Count} specifications.");
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> flagIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> profileIds = new HashSet<string>(
                HordeContentV1Specification.Profiles.Select(profile => profile.ProfileId),
                StringComparer.OrdinalIgnoreCase);
            foreach (EventContentRunFlagSpec flagSpec in EventContentV1Specification.RunFlags)
            {
                if (flagSpec == null || string.IsNullOrWhiteSpace(flagSpec.FlagId))
                {
                    errors.Add("A RunFlag specification is missing its ID.");
                    continue;
                }

                if (!flagIds.Add(flagSpec.FlagId))
                {
                    errors.Add($"Duplicate RunFlagId '{flagSpec.FlagId}' in Event Content V1 specifications.");
                }

                if (flagSpec.MaximumValue > 0 && flagSpec.MinimumValue > flagSpec.MaximumValue)
                {
                    errors.Add($"RunFlag '{flagSpec.FlagId}' has an invalid min/max range.");
                }
            }

            for (int index = 0; index < specs.Count; index++)
            {
                EventContentSpec spec = specs[index];
                string expectedId = $"EVT-{index + 1:000}";
                if (spec == null)
                {
                    errors.Add($"Event specification index {index} is null.");
                    continue;
                }

                if (!string.Equals(spec.EventId, expectedId, StringComparison.Ordinal))
                {
                    errors.Add($"Source row {spec.SourceRow} must use sequential ID '{expectedId}', but found '{spec.EventId}'.");
                }

                if (!ids.Add(spec.EventId ?? string.Empty))
                {
                    errors.Add($"Duplicate EventId '{spec.EventId}' in Event Content V1 specifications.");
                }

                foreach (EventContentConditionSpec condition in spec.Conditions)
                {
                    if (IsRunFlagCondition(condition.ConditionType)
                        && !flagIds.Contains(condition.TargetId ?? string.Empty))
                    {
                        errors.Add(
                            $"Event '{spec.EventId}' condition '{condition.DisplayName}' references unknown RunFlag '{condition.TargetId}'.");
                    }
                }

                foreach (EventContentWeightModifierSpec modifier in spec.WeightModifiers)
                {
                    if (modifier.Source == EventWeightModifierSource.RunFlagDaysSinceSet
                        && !flagIds.Contains(modifier.TargetId ?? string.Empty))
                    {
                        errors.Add(
                            $"Event '{spec.EventId}' weight modifier '{modifier.DisplayName}' references unknown RunFlag '{modifier.TargetId}'.");
                    }
                }

                if (string.IsNullOrWhiteSpace(spec.Title))
                {
                    errors.Add($"Event '{spec.EventId}' is missing its title.");
                }

                if (string.IsNullOrWhiteSpace(spec.Description))
                {
                    errors.Add($"Event '{spec.EventId}' is missing its description.");
                }

                if (spec.EarliestDay < 1 || spec.LatestDay < 0
                    || (spec.LatestDay > 0 && spec.LatestDay < spec.EarliestDay))
                {
                    errors.Add($"Event '{spec.EventId}' has an invalid day range {spec.EarliestDay}-{spec.LatestDay}.");
                }

                if (spec.CanRepeat && spec.LatestDay != 0)
                {
                    errors.Add($"Repeatable event '{spec.EventId}' must use LatestDay 0 for the 30-day schedule.");
                }

                if (spec.Weight <= 0f)
                {
                    errors.Add($"Event '{spec.EventId}' must have a positive base weight.");
                }

                if (spec.CooldownDays < 0 || spec.MaximumAppearances < 0)
                {
                    errors.Add($"Event '{spec.EventId}' contains a negative cooldown or appearance limit.");
                }

                if (spec.Choices == null || spec.Choices.Count == 0)
                {
                    errors.Add($"Event '{spec.EventId}' must have at least one choice.");
                    continue;
                }

                for (int choiceIndex = 0; choiceIndex < spec.Choices.Count; choiceIndex++)
                {
                    EventContentChoiceSpec choice = spec.Choices[choiceIndex];
                    if (choice == null || string.IsNullOrWhiteSpace(choice.ChoiceText))
                    {
                        errors.Add($"Event '{spec.EventId}' choice {choiceIndex + 1} is missing its label.");
                    }

                    if (choice == null || string.IsNullOrWhiteSpace(choice.ResultDescription))
                    {
                        errors.Add($"Event '{spec.EventId}' choice {choiceIndex + 1} is missing its intended result text.");
                    }

                    if (choice == null)
                    {
                        continue;
                    }

                    foreach (EventContentValueEffectSpec effect in choice.Effects)
                    {
                        if (effect == null)
                        {
                            errors.Add($"Event '{spec.EventId}' choice {choiceIndex + 1} contains a null effect.");
                        }
                        else if (effect.Category == GameEffectCategory.EventState
                            && !flagIds.Contains(effect.TargetId))
                        {
                            errors.Add(
                                $"Event '{spec.EventId}' choice {choiceIndex + 1} references unknown RunFlag '{effect.TargetId}'.");
                        }
                        else if (effect.Category == GameEffectCategory.Threat
                            && !profileIds.Contains(effect.HordeProfileId))
                        {
                            errors.Add(
                                $"Event '{spec.EventId}' choice {choiceIndex + 1} references unknown HordeProfile '{effect.HordeProfileId}'.");
                        }
                    }
                }

                if (spec.ImplementationStatus == EventImplementationStatus.Ready)
                {
                    if (!string.IsNullOrWhiteSpace(spec.ImplementationNotes))
                    {
                        errors.Add($"Ready event '{spec.EventId}' cannot contain missing-system notes.");
                    }

                    if (spec.Choices.Any(choice => choice.Effects == null || choice.Effects.Count == 0))
                    {
                        errors.Add($"Ready event '{spec.EventId}' contains a choice without an executable effect.");
                    }
                }
                else if (string.IsNullOrWhiteSpace(spec.ImplementationNotes))
                {
                    errors.Add($"Blocked event '{spec.EventId}' must explain its missing support system.");
                }
            }

            int readyCount = specs.Count(spec => spec.IncludeInPlayablePool);
            if (readyCount < 1)
            {
                errors.Add("Event Content V1 must contain at least one implementation-ready event.");
            }
            else if (readyCount < 8)
            {
                warnings.Add(
                    $"Only {readyCount} event is currently implementation-ready. " +
                    "The formal pool is not yet suitable for economy balance testing.");
            }
        }

        private static Dictionary<string, EventData> CreateOrUpdateEvents(
            IReadOnlyDictionary<string, EventData> existingById,
            IReadOnlyDictionary<string, RunFlagData> flagsById,
            IReadOnlyDictionary<string, HordeProfileData> profilesById,
            List<string> errors)
        {
            Dictionary<string, EventData> imported =
                new Dictionary<string, EventData>(StringComparer.OrdinalIgnoreCase);

            foreach (EventContentSpec spec in EventContentV1Specification.Events)
            {
                string targetPath = GetEventAssetPath(spec);
                EventData eventData = null;
                if (existingById != null)
                {
                    existingById.TryGetValue(spec.EventId, out eventData);
                }

                if (eventData != null)
                {
                    string existingPath = AssetDatabase.GetAssetPath(eventData);
                    if (!string.Equals(existingPath, targetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        string moveError = AssetDatabase.MoveAsset(existingPath, targetPath);
                        if (!string.IsNullOrWhiteSpace(moveError))
                        {
                            errors.Add($"Could not move '{spec.EventId}' to '{targetPath}': {moveError}");
                            continue;
                        }
                    }
                }
                else
                {
                    eventData = AssetDatabase.LoadAssetAtPath<EventData>(targetPath);
                    if (eventData == null)
                    {
                        eventData = ScriptableObject.CreateInstance<EventData>();
                        AssetDatabase.CreateAsset(eventData, targetPath);
                    }
                }

                eventData.name = $"{spec.EventId}_{spec.Title}";
                ApplySpecification(eventData, spec, flagsById, profilesById);
                imported.Add(spec.EventId, eventData);
            }

            return imported;
        }

        private static Dictionary<string, RunFlagData> CreateOrUpdateRunFlags(List<string> errors)
        {
            Dictionary<string, RunFlagData> imported =
                new Dictionary<string, RunFlagData>(StringComparer.OrdinalIgnoreCase);

            foreach (EventContentRunFlagSpec spec in EventContentV1Specification.RunFlags)
            {
                string targetPath = GetRunFlagAssetPath(spec);
                RunFlagData flagData = AssetDatabase.LoadAssetAtPath<RunFlagData>(targetPath);
                if (flagData == null)
                {
                    flagData = ScriptableObject.CreateInstance<RunFlagData>();
                    AssetDatabase.CreateAsset(flagData, targetPath);
                }

                flagData.name = $"{spec.FlagId}_{SanitizeFileName(spec.DisplayName)}";
                SerializedObject serialized = new SerializedObject(flagData);
                serialized.FindProperty("flagId").stringValue = spec.FlagId;
                serialized.FindProperty("displayName").stringValue = spec.DisplayName ?? string.Empty;
                serialized.FindProperty("description").stringValue = spec.Description ?? string.Empty;
                serialized.FindProperty("defaultValue").intValue = spec.DefaultValue;
                serialized.FindProperty("minimumValue").intValue = spec.MinimumValue;
                serialized.FindProperty("maximumValue").intValue = spec.MaximumValue;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(flagData);
                imported.Add(spec.FlagId, flagData);
            }

            return imported;
        }

        private static void ApplySpecification(
            EventData eventData,
            EventContentSpec spec,
            IReadOnlyDictionary<string, RunFlagData> flagsById,
            IReadOnlyDictionary<string, HordeProfileData> profilesById)
        {
            SerializedObject serialized = new SerializedObject(eventData);
            serialized.FindProperty("eventId").stringValue = spec.EventId;
            serialized.FindProperty("displayTitle").stringValue = spec.Title;
            serialized.FindProperty("description").stringValue = spec.Description;
            serialized.FindProperty("moodCategory").enumValueIndex = (int)spec.Mood;
            serialized.FindProperty("contentCategory").enumValueIndex = (int)spec.Content;
            serialized.FindProperty("implementationStatus").enumValueIndex = (int)spec.ImplementationStatus;
            serialized.FindProperty("impactLevel").stringValue = spec.ImpactLevel ?? string.Empty;
            serialized.FindProperty("specialRules").stringValue = spec.SpecialRules ?? string.Empty;
            serialized.FindProperty("implementationNotes").stringValue = spec.ImplementationNotes ?? string.Empty;
            serialized.FindProperty("earliestDay").intValue = spec.EarliestDay;
            serialized.FindProperty("latestDay").intValue = spec.LatestDay;
            serialized.FindProperty("baseWeight").floatValue = spec.Weight;
            serialized.FindProperty("canRepeat").boolValue = spec.CanRepeat;
            serialized.FindProperty("maximumAppearancesPerRun").intValue = spec.MaximumAppearances;
            serialized.FindProperty("minimumDaysBetweenAppearances").intValue = spec.CooldownDays;
            SetConditions(serialized.FindProperty("eligibilityConditions"), spec.Conditions, flagsById);
            SetWeightModifiers(serialized.FindProperty("weightModifiers"), spec.WeightModifiers, flagsById);
            SetChoices(serialized.FindProperty("choices"), spec, flagsById, profilesById);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(eventData);
        }

        private static void SetConditions(
            SerializedProperty group,
            IReadOnlyList<EventContentConditionSpec> conditions,
            IReadOnlyDictionary<string, RunFlagData> flagsById)
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
                entry.FindPropertyRelative("runFlag").objectReferenceValue =
                    TryGetRunFlag(flagsById, spec.TargetId);
                entry.FindPropertyRelative("requiredValue").intValue = spec.RequiredValue;
            }
        }

        private static void SetWeightModifiers(
            SerializedProperty entries,
            IReadOnlyList<EventContentWeightModifierSpec> modifiers,
            IReadOnlyDictionary<string, RunFlagData> flagsById)
        {
            entries.arraySize = modifiers?.Count ?? 0;
            for (int index = 0; index < entries.arraySize; index++)
            {
                EventContentWeightModifierSpec spec = modifiers[index];
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("displayName").stringValue = spec.DisplayName ?? string.Empty;
                entry.FindPropertyRelative("source").enumValueIndex = (int)spec.Source;
                entry.FindPropertyRelative("runFlag").objectReferenceValue =
                    TryGetRunFlag(flagsById, spec.TargetId);
                entry.FindPropertyRelative("targetId").stringValue = spec.TargetId ?? string.Empty;
                entry.FindPropertyRelative("ignoredUnitsBeforeBonus").intValue =
                    Mathf.Max(0, spec.IgnoredUnitsBeforeBonus);
                entry.FindPropertyRelative("weightPerUnit").floatValue = spec.WeightPerUnit;
                entry.FindPropertyRelative("minimumBonus").floatValue = spec.MinimumBonus;
                entry.FindPropertyRelative("maximumBonus").floatValue = Mathf.Max(0f, spec.MaximumBonus);
            }
        }

        private static void SetChoices(
            SerializedProperty choices,
            EventContentSpec eventSpec,
            IReadOnlyDictionary<string, RunFlagData> flagsById,
            IReadOnlyDictionary<string, HordeProfileData> profilesById)
        {
            choices.arraySize = eventSpec.Choices.Count;
            for (int index = 0; index < eventSpec.Choices.Count; index++)
            {
                EventContentChoiceSpec spec = eventSpec.Choices[index];
                SerializedProperty choice = choices.GetArrayElementAtIndex(index);
                choice.FindPropertyRelative("choiceText").stringValue = spec.ChoiceText;
                choice.FindPropertyRelative("resultTitle").stringValue = spec.ResultTitle;
                choice.FindPropertyRelative("resultDescription").stringValue = spec.ResultDescription;

                SerializedProperty effects = choice.FindPropertyRelative("effects");
                int effectCount = eventSpec.IncludeInPlayablePool ? spec.Effects.Count : 0;
                effects.arraySize = effectCount;
                for (int effectIndex = 0; effectIndex < effectCount; effectIndex++)
                {
                    SetEffect(
                        effects.GetArrayElementAtIndex(effectIndex),
                        spec.Effects[effectIndex],
                        flagsById,
                        profilesById);
                }

                choice.FindPropertyRelative("forcedLossConsequences").arraySize = 0;
            }
        }

        private static Dictionary<string, RunFlagData> BuildExistingRunFlagIndex()
        {
            Dictionary<string, RunFlagData> index =
                new Dictionary<string, RunFlagData>(StringComparer.OrdinalIgnoreCase);
            foreach (string guid in AssetDatabase.FindAssets("t:RunFlagData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RunFlagData flagData = AssetDatabase.LoadAssetAtPath<RunFlagData>(path);
                if (flagData == null || string.IsNullOrWhiteSpace(flagData.FlagId))
                {
                    continue;
                }

                if (!index.ContainsKey(flagData.FlagId))
                {
                    index.Add(flagData.FlagId, flagData);
                }
            }

            return index;
        }

        private static void SetEffect(
            SerializedProperty effect,
            EventContentValueEffectSpec spec,
            IReadOnlyDictionary<string, RunFlagData> flagsById,
            IReadOnlyDictionary<string, HordeProfileData> profilesById)
        {
            effect.FindPropertyRelative("carrier").enumValueIndex = (int)GameEffectCarrier.Event;
            effect.FindPropertyRelative("category").enumValueIndex = (int)spec.Category;
            effect.FindPropertyRelative("valueEffectType").enumValueIndex = (int)spec.Type;
            effect.FindPropertyRelative("cardFlowEffectType").enumValueIndex = 0;
            effect.FindPropertyRelative("buildingEffectType").enumValueIndex = 0;
            effect.FindPropertyRelative("survivorEffectType").enumValueIndex = 0;
            effect.FindPropertyRelative("eventStateEffectType").enumValueIndex = (int)spec.EventStateType;
            effect.FindPropertyRelative("threatEffectType").enumValueIndex = (int)spec.ThreatType;
            effect.FindPropertyRelative("cardData").objectReferenceValue = null;
            effect.FindPropertyRelative("runFlagData").objectReferenceValue =
                spec.Category == GameEffectCategory.EventState
                    ? TryGetRunFlag(flagsById, spec.TargetId)
                    : null;
            effect.FindPropertyRelative("runFlagId").stringValue =
                spec.Category == GameEffectCategory.EventState ? spec.TargetId : string.Empty;
            effect.FindPropertyRelative("clampRunFlagToDefinition").boolValue = true;
            effect.FindPropertyRelative("amount").intValue = spec.Amount;
            effect.FindPropertyRelative("selectionCount").intValue = 1;
            effect.FindPropertyRelative("effectId").stringValue = string.Empty;
            effect.FindPropertyRelative("isPermanent").boolValue = true;
            effect.FindPropertyRelative("remainingTurns").intValue = 0;
            effect.FindPropertyRelative("eventPaymentMode").enumValueIndex = (int)spec.PaymentMode;
            effect.FindPropertyRelative("hordeProfile").objectReferenceValue =
                spec.Category == GameEffectCategory.Threat
                    && profilesById != null
                    && profilesById.TryGetValue(spec.HordeProfileId, out HordeProfileData profile)
                        ? profile
                        : null;
            effect.FindPropertyRelative("eventBonusBudget").intValue = spec.EventBonusBudget;
            effect.FindPropertyRelative("requiredBuildingId").stringValue = string.Empty;
            effect.FindPropertyRelative("requiredBuildingLevel").intValue = 0;
            effect.FindPropertyRelative("invertBuildingRequirement").boolValue = false;

            SerializedProperty scaling = effect.FindPropertyRelative("eventScaling");
            scaling.FindPropertyRelative("scaleSource").enumValueIndex = (int)spec.ScaleSource;
            scaling.FindPropertyRelative("scalingStrength").floatValue = spec.ScalingStrength;
            scaling.FindPropertyRelative("minimumFinalMagnitude").intValue = 0;
            scaling.FindPropertyRelative("maximumFinalMagnitude").intValue = 0;
            scaling.FindPropertyRelative("roundingMode").enumValueIndex = (int)EventScaleRoundingMode.Round;
        }

        private static Dictionary<string, EventData> BuildExistingEventIndex(
            List<string> errors,
            bool canonicalOnly = false)
        {
            HashSet<string> canonicalIds = new HashSet<string>(
                EventContentV1Specification.Events.Select(spec => spec.EventId),
                StringComparer.OrdinalIgnoreCase);
            Dictionary<string, EventData> index =
                new Dictionary<string, EventData>(StringComparer.OrdinalIgnoreCase);

            foreach (string guid in AssetDatabase.FindAssets("t:EventData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);
                string eventId = eventData != null ? eventData.EventId?.Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(eventId)
                    || (canonicalOnly && !canonicalIds.Contains(eventId)))
                {
                    continue;
                }

                if (!canonicalIds.Contains(eventId) && !canonicalOnly)
                {
                    continue;
                }

                if (index.TryGetValue(eventId, out EventData duplicate))
                {
                    errors.Add(
                        $"Duplicate canonical EventId '{eventId}' at " +
                        $"'{AssetDatabase.GetAssetPath(duplicate)}' and '{path}'.");
                    continue;
                }

                index.Add(eventId, eventData);
            }

            return index;
        }

        private static void ValidateAssetTargets(
            IReadOnlyDictionary<string, EventData> existingById,
            List<string> errors)
        {
            foreach (EventContentSpec spec in EventContentV1Specification.Events)
            {
                string targetPath = GetEventAssetPath(spec);
                UnityEngine.Object target = AssetDatabase.LoadMainAssetAtPath(targetPath);
                if (target == null)
                {
                    continue;
                }

                if (!(target is EventData targetEvent))
                {
                    errors.Add($"Event target path '{targetPath}' is occupied by '{target.GetType().Name}'.");
                    continue;
                }

                if (existingById != null
                    && existingById.TryGetValue(spec.EventId, out EventData existing)
                    && !ReferenceEquals(existing, targetEvent))
                {
                    errors.Add($"Event target path '{targetPath}' is occupied by another EventData asset.");
                }
            }
        }

        private static void WirePlayablePool(
            EventFlowController eventFlowController,
            IReadOnlyDictionary<string, EventData> imported)
        {
            EventContentSpec[] playableSpecs = EventContentV1Specification.Events
                .Where(spec => spec.IncludeInPlayablePool)
                .ToArray();
            SerializedObject serialized = new SerializedObject(eventFlowController);
            SerializedProperty pool = serialized.FindProperty("eventPool");
            pool.arraySize = playableSpecs.Length;
            for (int index = 0; index < playableSpecs.Length; index++)
            {
                pool.GetArrayElementAtIndex(index).objectReferenceValue = imported[playableSpecs[index].EventId];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(eventFlowController);
        }

        private static void ValidateImportedAssets(
            IReadOnlyDictionary<string, EventData> imported,
            List<string> errors)
        {
            foreach (EventContentSpec spec in EventContentV1Specification.Events)
            {
                if (imported == null
                    || !imported.TryGetValue(spec.EventId, out EventData eventData)
                    || eventData == null)
                {
                    errors.Add($"Canonical event asset '{spec.EventId}' is missing.");
                    continue;
                }

                if (!string.Equals(eventData.DisplayTitle, spec.Title, StringComparison.Ordinal)
                    || eventData.MoodCategory != spec.Mood
                    || eventData.ContentCategory != spec.Content
                    || eventData.ImplementationStatus != spec.ImplementationStatus
                    || eventData.EarliestDay != spec.EarliestDay
                    || eventData.LatestDay != spec.LatestDay
                    || Math.Abs(eventData.BaseWeight - spec.Weight) > 0.001f
                    || eventData.CanRepeat != spec.CanRepeat
                    || eventData.MaximumAppearancesPerRun != (spec.CanRepeat ? spec.MaximumAppearances : 1)
                    || eventData.MinimumDaysBetweenAppearances != spec.CooldownDays
                    || eventData.Choices.Count != spec.Choices.Count)
                {
                    errors.Add($"Imported event '{spec.EventId}' does not match its source specification.");
                }

                int expectedEffects = spec.IncludeInPlayablePool
                    ? spec.Choices.Sum(choice => choice.Effects.Count)
                    : 0;
                int actualEffects = eventData.Choices.Sum(choice => choice?.Effects?.Count ?? 0);
                if (actualEffects != expectedEffects)
                {
                    errors.Add(
                        $"Imported event '{spec.EventId}' has {actualEffects} executable effects; " +
                        $"expected {expectedEffects}.");
                }
            }
        }

        private static void ValidateImportedFlags(
            IReadOnlyDictionary<string, RunFlagData> imported,
            List<string> errors)
        {
            foreach (EventContentRunFlagSpec spec in EventContentV1Specification.RunFlags)
            {
                if (imported == null
                    || !imported.TryGetValue(spec.FlagId, out RunFlagData flagData)
                    || flagData == null)
                {
                    errors.Add($"RunFlag asset '{spec.FlagId}' is missing.");
                    continue;
                }

                if (!string.Equals(flagData.DisplayName, spec.DisplayName, StringComparison.Ordinal)
                    || flagData.DefaultValue != spec.DefaultValue
                    || flagData.MinimumValue != spec.MinimumValue
                    || flagData.MaximumValue != spec.MaximumValue)
                {
                    errors.Add($"Imported RunFlag '{spec.FlagId}' does not match its source specification.");
                }
            }
        }

        private static void ValidatePlayablePool(
            EventFlowController eventFlowController,
            IReadOnlyDictionary<string, EventData> imported,
            List<string> errors)
        {
            EventContentSpec[] expected = EventContentV1Specification.Events
                .Where(spec => spec.IncludeInPlayablePool)
                .ToArray();
            SerializedObject serialized = new SerializedObject(eventFlowController);
            SerializedProperty pool = serialized.FindProperty("eventPool");
            if (pool.arraySize != expected.Length)
            {
                errors.Add($"SampleScene event pool has {pool.arraySize} entries; expected {expected.Length}.");
                return;
            }

            for (int index = 0; index < expected.Length; index++)
            {
                EventData actual = pool.GetArrayElementAtIndex(index).objectReferenceValue as EventData;
                EventData expectedAsset = imported != null && imported.TryGetValue(expected[index].EventId, out EventData value)
                    ? value
                    : null;
                if (!ReferenceEquals(actual, expectedAsset))
                {
                    errors.Add($"SampleScene event pool index {index} is not '{expected[index].EventId}'.");
                }

                if (actual != null && !actual.IsImplementationReady)
                {
                    errors.Add($"Blocked event '{actual.EventId}' was added to the playable event pool.");
                }
            }
        }

        private static bool TryOpenEventFlow(List<string> errors, out SceneContext context)
        {
            context = null;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool closeWhenDone = !scene.IsValid() || !scene.isLoaded;
            if (closeWhenDone)
            {
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                if (sceneAsset == null)
                {
                    errors.Add($"Sample scene was not found at '{ScenePath}'.");
                    return false;
                }

                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            List<EventFlowController> controllers = new List<EventFlowController>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                controllers.AddRange(root.GetComponentsInChildren<EventFlowController>(true));
            }

            if (controllers.Count != 1)
            {
                errors.Add($"SampleScene must contain exactly one EventFlowController, but found {controllers.Count}.");
                if (closeWhenDone && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                return false;
            }

            context = new SceneContext(scene, controllers[0], closeWhenDone);
            return true;
        }

        private static string GetEventAssetPath(EventContentSpec spec)
        {
            return $"{EventAssetFolder}/{spec.EventId}_{SanitizeFileName(spec.Title)}.asset";
        }

        private static string GetRunFlagAssetPath(EventContentRunFlagSpec spec)
        {
            return $"{RunFlagAssetFolder}/{spec.FlagId}_{SanitizeFileName(spec.DisplayName)}.asset";
        }

        private static RunFlagData TryGetRunFlag(
            IReadOnlyDictionary<string, RunFlagData> flagsById,
            string flagId)
        {
            return flagsById != null
                && !string.IsNullOrWhiteSpace(flagId)
                && flagsById.TryGetValue(flagId.Trim(), out RunFlagData flagData)
                    ? flagData
                    : null;
        }

        private static bool IsRunFlagCondition(EventConditionType type)
        {
            return type == EventConditionType.RunFlag
                || type == EventConditionType.RunFlagValue
                || type == EventConditionType.RunFlagDaysSinceSet;
        }

        private static string SanitizeFileName(string value)
        {
            string result = value ?? string.Empty;
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(invalid, '_');
            }

            return result;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void LogReport(
            string title,
            IReadOnlyList<string> errors,
            IReadOnlyList<string> warnings)
        {
            LogWarnings(warnings);
            foreach (string error in errors)
            {
                Debug.LogError($"{title}: {error}");
            }

            Debug.LogError($"{title}. Errors: {errors.Count}, warnings: {warnings.Count}.");
        }

        private static void LogWarnings(IEnumerable<string> warnings)
        {
            foreach (string warning in warnings)
            {
                Debug.LogWarning($"Event Content V1: {warning}");
            }
        }

        private sealed class SceneContext : IDisposable
        {
            public SceneContext(Scene scene, EventFlowController eventFlowController, bool closeWhenDone)
            {
                Scene = scene;
                EventFlowController = eventFlowController;
                CloseWhenDone = closeWhenDone;
            }

            public Scene Scene { get; }
            public EventFlowController EventFlowController { get; }
            private bool CloseWhenDone { get; }

            public void Dispose()
            {
                if (CloseWhenDone && Scene.IsValid() && Scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(Scene, true);
                }
            }
        }
    }
}
#endif
