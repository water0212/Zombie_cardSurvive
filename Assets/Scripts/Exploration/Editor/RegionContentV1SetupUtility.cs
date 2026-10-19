#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieCardSurvive.Cards.UI;
using ZombieCardSurvive.Exploration.Data;
using ZombieCardSurvive.Exploration.Runtime;
using ZombieCardSurvive.Exploration.UI;

namespace ZombieCardSurvive.Exploration.Editor
{
    public static class RegionContentV1SetupUtility
    {
        private const string RegionAssetFolder = "Assets/ScriptObj/RegionScriptObj/ContentV1";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        private static readonly RegionSpec[] RegionSpecs =
        {
            new RegionSpec("REGION-001", "廢棄超市", ExplorationTier.Basic, DangerLevel.Low, 2, 0.15f, 0.10f, 0.10f),
            new RegionSpec("REGION-002", "住宅區", ExplorationTier.Basic, DangerLevel.Low, 2, 0.15f, 0.10f, 0.10f),
            new RegionSpec("REGION-003", "消防局", ExplorationTier.Basic, DangerLevel.Medium, 3, 0.30f, 0.10f, 0.10f),
            new RegionSpec("REGION-004", "五金行", ExplorationTier.Advanced, DangerLevel.High, 4, 0.45f, 0.15f, 0.10f),
            new RegionSpec("REGION-005", "倉庫", ExplorationTier.Advanced, DangerLevel.High, 6, 0.45f, 0.15f, 0.15f)
        };

        private static readonly RegionPackSpec[] RegionPackSpecs =
        {
            new RegionPackSpec("REGION-001", "PACK-COMMON-FOOD-01", "簡易食物包", RegionSupplyRarity.Common, 4, 70),
            new RegionPackSpec("REGION-001", "PACK-UNCOMMON-FOOD-01", "罕見食物包", RegionSupplyRarity.Rare, 2, 25),
            new RegionPackSpec("REGION-001", "PACK-RARE-SUPPLY-01", "稀有補給包", RegionSupplyRarity.UltraRare, 1, 5),

            new RegionPackSpec("REGION-002", "PACK-COMMON-RESOURCE-01", "簡易資源包", RegionSupplyRarity.Common, 2, 35),
            new RegionPackSpec("REGION-002", "PACK-COMMON-FOOD-01", "簡易食物包", RegionSupplyRarity.Common, 2, 35),
            new RegionPackSpec("REGION-002", "PACK-UNCOMMON-RESOURCE-01", "罕見資源包", RegionSupplyRarity.Rare, 2, 25),
            new RegionPackSpec("REGION-002", "PACK-REGION-002-01", "住宅包", RegionSupplyRarity.UltraRare, 1, 5),

            new RegionPackSpec("REGION-003", "PACK-COMMON-RESOURCE-01", "簡易資源包", RegionSupplyRarity.Common, 4, 70),
            new RegionPackSpec("REGION-003", "PACK-UNCOMMON-RESOURCE-01", "罕見資源包", RegionSupplyRarity.Rare, 2, 25),
            new RegionPackSpec("REGION-003", "PACK-FireDepartment-01", "消防局包", RegionSupplyRarity.UltraRare, 1, 5),

            new RegionPackSpec("REGION-004", "PACK-COMMON-RESOURCE-01", "簡易資源包", RegionSupplyRarity.Common, 3, 60),
            new RegionPackSpec("REGION-004", "PACK-UNCOMMON-RESOURCE-01", "罕見資源包", RegionSupplyRarity.Rare, 3, 30),
            new RegionPackSpec("REGION-004", "PACK-RARE-RESOURCE-01", "稀有資源包", RegionSupplyRarity.UltraRare, 2, 10),

            new RegionPackSpec("REGION-005", "PACK-UNCOMMON-FOOD-01", "罕見食物包", RegionSupplyRarity.Common, 4, 35),
            new RegionPackSpec("REGION-005", "PACK-UNCOMMON-RESOURCE-01", "罕見資源包", RegionSupplyRarity.Common, 4, 35),
            new RegionPackSpec("REGION-005", "PACK-RARE-SUPPLY-01", "稀有補給包", RegionSupplyRarity.Rare, 3, 25),
            new RegionPackSpec("REGION-005", "PACK-STOREHOUSE-01", "倉庫包", RegionSupplyRarity.UltraRare, 2, 5)
        };

        [MenuItem("Tools/Zombie Card Survive/Setup Region Content V1")]
        public static void Setup()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            ImportContext context = null;

            try
            {
                if (!TryPrepareImport(errors, warnings, out context))
                {
                    LogValidationReport("Region Content V1 setup aborted", errors, warnings);
                    return;
                }

                EnsureFolder("Assets/ScriptObj/RegionScriptObj", "ContentV1");
                int normalizedPackCount = NormalizePackIds(context.PacksById);
                Dictionary<string, ExplorationRegionData> regions = CreateOrUpdateRegions(context.PacksById);
                WireExplorationRegionPool(context.ExplorationSystem, regions);

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(context.Scene);
                EditorSceneManager.SaveScene(context.Scene);
                AssetDatabase.Refresh();

                ValidateImportedState(context.ExplorationSystem, regions, errors);
                ValidatePresentationWiring(context.Scene, context.ExplorationSystem, errors);
                if (errors.Count > 0)
                {
                    LogValidationReport("Region Content V1 post-import validation failed", errors, warnings);
                    return;
                }

                LogWarnings(warnings);
                Debug.Log(
                    $"Region Content V1 setup complete: {regions.Count} regions, " +
                    $"{RegionPackSpecs.Length} region-pack entries, {context.PacksById.Count} referenced packs, " +
                    $"and {normalizedPackCount} normalized PackId values.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Region Content V1 setup failed. No YAML was written manually; inspect the exception before running setup again.");
            }
            finally
            {
                context?.Dispose();
            }
        }

        [MenuItem("Tools/Zombie Card Survive/Validate Region Content V1")]
        public static void Validate()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            ImportContext context = null;

            try
            {
                if (!TryPrepareImport(errors, warnings, out context))
                {
                    LogValidationReport("Region Content V1 validation failed", errors, warnings);
                    return;
                }

                Dictionary<string, ExplorationRegionData> regions = LoadExistingRegions();
                ValidateImportedState(context.ExplorationSystem, regions, errors);
                ValidatePresentationWiring(context.Scene, context.ExplorationSystem, errors);
                if (errors.Count > 0)
                {
                    LogValidationReport("Region Content V1 validation failed", errors, warnings);
                    return;
                }

                LogWarnings(warnings);
                Debug.Log(
                    $"Region Content V1 validation passed: {RegionSpecs.Length} region specs, " +
                    $"{RegionPackSpecs.Length} region-pack entries, and {context.PacksById.Count} referenced packs are valid.");
            }
            finally
            {
                context?.Dispose();
            }
        }

        private static bool TryPrepareImport(
            List<string> errors,
            List<string> warnings,
            out ImportContext context)
        {
            context = null;
            ValidateSourceSpecs(errors, warnings);
            Dictionary<string, CardPackData> packsById = BuildPackIndex(errors, warnings);
            ValidateReferencedPacks(packsById, errors, warnings);
            ValidateRegionAssetTargets(errors);

            if (errors.Count > 0)
            {
                return false;
            }

            if (!TryLoadExplorationSystem(errors, out Scene scene, out ExplorationSystem system, out bool closeSceneWhenDone))
            {
                return false;
            }

            context = new ImportContext(packsById, scene, system, closeSceneWhenDone);
            return true;
        }

        private static void ValidateSourceSpecs(List<string> errors, List<string> warnings)
        {
            HashSet<string> regionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (RegionSpec region in RegionSpecs)
            {
                if (string.IsNullOrWhiteSpace(region.RegionId) || !regionIds.Add(region.RegionId))
                {
                    errors.Add($"Region spec contains a missing or duplicate RegionId: '{region.RegionId}'.");
                }

                if (string.IsNullOrWhiteSpace(region.DisplayName))
                {
                    errors.Add($"Region '{region.RegionId}' is missing DisplayName.");
                }

                if (region.InitialZombieCount < 0)
                {
                    errors.Add($"Region '{region.RegionId}' has a negative zombie count.");
                }

                ValidateNormalizedValue(region.RegionId, "BaseEncounterChance", region.BaseEncounterChance, errors);
                ValidateNonNegativeValue(region.RegionId, "HeatPerExploration", region.HeatPerExploration, errors);
                ValidateNonNegativeValue(region.RegionId, "DailyHeatDecay", region.DailyHeatDecay, errors);
            }

            HashSet<string> regionPackKeys = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, string> packNamesById = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (RegionPackSpec entry in RegionPackSpecs)
            {
                if (!regionIds.Contains(entry.RegionId))
                {
                    errors.Add($"Region-pack entry references unknown RegionId '{entry.RegionId}'.");
                }

                if (string.IsNullOrWhiteSpace(entry.PackId))
                {
                    errors.Add($"Region '{entry.RegionId}' has a region-pack entry without PackId.");
                    continue;
                }

                string key = $"{entry.RegionId}|{entry.PackId}";
                if (!regionPackKeys.Add(key))
                {
                    errors.Add($"Duplicate region-pack entry '{key}'.");
                }

                if (entry.InitialCount <= 0)
                {
                    errors.Add($"Region-pack entry '{key}' must have InitialCount greater than zero.");
                }

                if (entry.BaseWeight <= 0)
                {
                    errors.Add($"Region-pack entry '{key}' must have BaseWeight greater than zero.");
                }

                if (packNamesById.TryGetValue(entry.PackId, out string knownName)
                    && !string.Equals(knownName, entry.PackName, StringComparison.Ordinal))
                {
                    errors.Add($"PackId '{entry.PackId}' uses conflicting names '{knownName}' and '{entry.PackName}'.");
                }
                else
                {
                    packNamesById[entry.PackId] = entry.PackName;
                }
            }

            foreach (RegionSpec region in RegionSpecs)
            {
                RegionPackSpec[] entries = RegionPackSpecs.Where(entry => entry.RegionId == region.RegionId).ToArray();
                if (entries.Length == 0)
                {
                    errors.Add($"Region '{region.RegionId}' has no card packs.");
                    continue;
                }

                int totalWeight = entries.Sum(entry => entry.BaseWeight);
                if (totalWeight != 100)
                {
                    warnings.Add($"Region '{region.RegionId}' has total base weight {totalWeight}, not 100. Runtime normalization still works.");
                }
            }
        }

        private static Dictionary<string, CardPackData> BuildPackIndex(
            List<string> errors,
            List<string> warnings)
        {
            Dictionary<string, CardPackData> packsById = new Dictionary<string, CardPackData>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets("t:CardPackData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardPackData pack = AssetDatabase.LoadAssetAtPath<CardPackData>(path);
                if (pack == null)
                {
                    continue;
                }

                string rawId = pack.PackId ?? string.Empty;
                string normalizedId = rawId.Trim();
                if (string.IsNullOrWhiteSpace(normalizedId))
                {
                    errors.Add($"Card pack at '{path}' is missing PackId.");
                    continue;
                }

                if (packsById.TryGetValue(normalizedId, out CardPackData duplicate))
                {
                    string duplicatePath = AssetDatabase.GetAssetPath(duplicate);
                    errors.Add($"Duplicate PackId '{normalizedId}' at '{duplicatePath}' and '{path}'.");
                    continue;
                }

                if (!string.Equals(rawId, normalizedId, StringComparison.Ordinal))
                {
                    warnings.Add($"PackId '{rawId}' at '{path}' contains surrounding whitespace and will be normalized to '{normalizedId}'.");
                }

                packsById.Add(normalizedId, pack);
            }

            return packsById;
        }

        private static void ValidateReferencedPacks(
            IReadOnlyDictionary<string, CardPackData> packsById,
            List<string> errors,
            List<string> warnings)
        {
            foreach (IGrouping<string, RegionPackSpec> group in RegionPackSpecs.GroupBy(entry => entry.PackId))
            {
                if (!packsById.TryGetValue(group.Key, out CardPackData pack))
                {
                    errors.Add($"Required CardPackData '{group.Key}' was not found.");
                    continue;
                }

                string expectedName = group.First().PackName;
                if (!string.Equals(pack.DisplayName?.Trim(), expectedName, StringComparison.Ordinal))
                {
                    warnings.Add(
                        $"Pack '{group.Key}' display name is '{pack.DisplayName}', while the sheet uses '{expectedName}'. " +
                        "The asset name was preserved.");
                }

                if (pack.CardsToGenerate <= 0)
                {
                    errors.Add($"Pack '{group.Key}' has CardsToGenerate set to zero.");
                }

                int validCardCount = pack.CardPool.Count(entry => entry != null && entry.Card != null && entry.Weight > 0);
                if (validCardCount == 0)
                {
                    errors.Add($"Pack '{group.Key}' has no valid weighted cards in CardPool.");
                }
                else if (validCardCount != pack.CardPool.Count)
                {
                    warnings.Add($"Pack '{group.Key}' contains null cards or zero-weight entries; only valid entries can be drawn.");
                }
            }
        }

        private static void ValidateRegionAssetTargets(List<string> errors)
        {
            foreach (RegionSpec spec in RegionSpecs)
            {
                string path = GetRegionAssetPath(spec.RegionId);
                UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
                if (existing != null && !(existing is ExplorationRegionData))
                {
                    errors.Add($"Region target path '{path}' is occupied by '{existing.GetType().Name}'.");
                }
            }

            Dictionary<string, string> existingRegionPaths = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:ExplorationRegionData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ExplorationRegionData region = AssetDatabase.LoadAssetAtPath<ExplorationRegionData>(path);
                string id = region != null ? region.RegionId?.Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(id) || !RegionSpecs.Any(spec => spec.RegionId == id))
                {
                    continue;
                }

                if (existingRegionPaths.TryGetValue(id, out string duplicatePath))
                {
                    errors.Add($"Duplicate canonical RegionId '{id}' at '{duplicatePath}' and '{path}'.");
                }
                else
                {
                    existingRegionPaths.Add(id, path);
                }
            }
        }

        private static bool TryLoadExplorationSystem(
            List<string> errors,
            out Scene scene,
            out ExplorationSystem explorationSystem,
            out bool closeSceneWhenDone)
        {
            scene = SceneManager.GetSceneByPath(ScenePath);
            closeSceneWhenDone = !scene.IsValid() || !scene.isLoaded;
            if (closeSceneWhenDone)
            {
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                if (sceneAsset == null)
                {
                    explorationSystem = null;
                    errors.Add($"Sample scene was not found at '{ScenePath}'.");
                    return false;
                }

                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            List<ExplorationSystem> systems = new List<ExplorationSystem>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                systems.AddRange(root.GetComponentsInChildren<ExplorationSystem>(true));
            }

            if (systems.Count != 1)
            {
                explorationSystem = null;
                errors.Add($"SampleScene must contain exactly one ExplorationSystem, but found {systems.Count}.");
                if (closeSceneWhenDone && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                return false;
            }

            explorationSystem = systems[0];
            return true;
        }

        private static int NormalizePackIds(IReadOnlyDictionary<string, CardPackData> packsById)
        {
            int changedCount = 0;
            foreach (string packId in RegionPackSpecs.Select(entry => entry.PackId).Distinct())
            {
                CardPackData pack = packsById[packId];
                if (string.Equals(pack.PackId, packId, StringComparison.Ordinal))
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(pack);
                serialized.FindProperty("packId").stringValue = packId;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pack);
                changedCount++;
            }

            return changedCount;
        }

        private static Dictionary<string, ExplorationRegionData> CreateOrUpdateRegions(
            IReadOnlyDictionary<string, CardPackData> packsById)
        {
            Dictionary<string, ExplorationRegionData> regions =
                new Dictionary<string, ExplorationRegionData>(StringComparer.Ordinal);

            foreach (RegionSpec spec in RegionSpecs)
            {
                string path = GetRegionAssetPath(spec.RegionId);
                ExplorationRegionData region = AssetDatabase.LoadAssetAtPath<ExplorationRegionData>(path);
                if (region == null)
                {
                    region = ScriptableObject.CreateInstance<ExplorationRegionData>();
                    AssetDatabase.CreateAsset(region, path);
                }

                SerializedObject serialized = new SerializedObject(region);
                serialized.FindProperty("regionId").stringValue = spec.RegionId;
                serialized.FindProperty("displayName").stringValue = spec.DisplayName;
                serialized.FindProperty("regionTier").enumValueIndex = (int)spec.Tier;
                serialized.FindProperty("dangerLevel").enumValueIndex = (int)spec.Danger;
                serialized.FindProperty("initialZombieCount").intValue = spec.InitialZombieCount;
                serialized.FindProperty("baseEncounterChance").floatValue = spec.BaseEncounterChance;
                serialized.FindProperty("heatPerExploration").floatValue = spec.HeatPerExploration;
                serialized.FindProperty("dailyHeatDecay").floatValue = spec.DailyHeatDecay;

                RegionPackSpec[] packSpecs = RegionPackSpecs.Where(entry => entry.RegionId == spec.RegionId).ToArray();
                SerializedProperty drops = serialized.FindProperty("cardPackDrops");
                drops.arraySize = packSpecs.Length;
                for (int index = 0; index < packSpecs.Length; index++)
                {
                    RegionPackSpec packSpec = packSpecs[index];
                    SerializedProperty drop = drops.GetArrayElementAtIndex(index);
                    drop.FindPropertyRelative("cardPack").objectReferenceValue = packsById[packSpec.PackId];
                    drop.FindPropertyRelative("supplyRarity").enumValueIndex = (int)packSpec.Rarity;
                    drop.FindPropertyRelative("initialCount").intValue = packSpec.InitialCount;
                    drop.FindPropertyRelative("baseWeight").intValue = packSpec.BaseWeight;
                    drop.FindPropertyRelative("minPackCount").intValue = 1;
                    drop.FindPropertyRelative("maxPackCount").intValue = 1;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(region);
                regions.Add(spec.RegionId, region);
            }

            return regions;
        }

        private static Dictionary<string, ExplorationRegionData> LoadExistingRegions()
        {
            Dictionary<string, ExplorationRegionData> regions =
                new Dictionary<string, ExplorationRegionData>(StringComparer.Ordinal);
            foreach (RegionSpec spec in RegionSpecs)
            {
                ExplorationRegionData region =
                    AssetDatabase.LoadAssetAtPath<ExplorationRegionData>(GetRegionAssetPath(spec.RegionId));
                if (region != null)
                {
                    regions.Add(spec.RegionId, region);
                }
            }

            return regions;
        }

        private static void WireExplorationRegionPool(
            ExplorationSystem explorationSystem,
            IReadOnlyDictionary<string, ExplorationRegionData> regions)
        {
            SerializedObject serialized = new SerializedObject(explorationSystem);
            SerializedProperty pool = serialized.FindProperty("regionPool");
            pool.arraySize = RegionSpecs.Length;
            for (int index = 0; index < RegionSpecs.Length; index++)
            {
                pool.GetArrayElementAtIndex(index).objectReferenceValue = regions[RegionSpecs[index].RegionId];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(explorationSystem);
        }

        private static void ValidateImportedState(
            ExplorationSystem explorationSystem,
            IReadOnlyDictionary<string, ExplorationRegionData> regions,
            List<string> errors)
        {
            foreach (RegionSpec spec in RegionSpecs)
            {
                if (!regions.TryGetValue(spec.RegionId, out ExplorationRegionData region) || region == null)
                {
                    errors.Add($"Imported region '{spec.RegionId}' is missing.");
                    continue;
                }

                if (!string.Equals(region.RegionId, spec.RegionId, StringComparison.Ordinal)
                    || !string.Equals(region.DisplayName, spec.DisplayName, StringComparison.Ordinal)
                    || region.RegionTier != spec.Tier
                    || region.DangerLevel != spec.Danger
                    || region.InitialZombieCount != spec.InitialZombieCount
                    || !Approximately(region.BaseEncounterChance, spec.BaseEncounterChance)
                    || !Approximately(region.HeatPerExploration, spec.HeatPerExploration)
                    || !Approximately(region.DailyHeatDecay, spec.DailyHeatDecay))
                {
                    errors.Add($"Imported values for region '{spec.RegionId}' do not match the source spec.");
                }

                RegionPackSpec[] expectedPacks = RegionPackSpecs.Where(entry => entry.RegionId == spec.RegionId).ToArray();
                if (region.CardPackDrops.Count != expectedPacks.Length)
                {
                    errors.Add(
                        $"Region '{spec.RegionId}' has {region.CardPackDrops.Count} card packs, expected {expectedPacks.Length}.");
                    continue;
                }

                for (int index = 0; index < expectedPacks.Length; index++)
                {
                    RegionPackSpec expected = expectedPacks[index];
                    CardPackDropEntry actual = region.CardPackDrops[index];
                    if (actual == null
                        || actual.CardPack == null
                        || !string.Equals(actual.CardPack.PackId, expected.PackId, StringComparison.Ordinal)
                        || actual.SupplyRarity != expected.Rarity
                        || actual.InitialCount != expected.InitialCount
                        || actual.BaseWeight != expected.BaseWeight)
                    {
                        errors.Add($"Region '{spec.RegionId}' card pack entry {index} does not match '{expected.PackId}'.");
                    }
                }
            }

            SerializedObject systemSerialized = new SerializedObject(explorationSystem);
            SerializedProperty pool = systemSerialized.FindProperty("regionPool");
            if (pool.arraySize != RegionSpecs.Length)
            {
                errors.Add($"ExplorationSystem Region Pool has {pool.arraySize} entries, expected {RegionSpecs.Length}.");
                return;
            }

            for (int index = 0; index < RegionSpecs.Length; index++)
            {
                ExplorationRegionData region =
                    pool.GetArrayElementAtIndex(index).objectReferenceValue as ExplorationRegionData;
                if (region == null || !string.Equals(region.RegionId, RegionSpecs[index].RegionId, StringComparison.Ordinal))
                {
                    errors.Add($"ExplorationSystem Region Pool entry {index} is not '{RegionSpecs[index].RegionId}'.");
                }
            }
        }

        private static void ValidatePresentationWiring(
            Scene scene,
            ExplorationSystem explorationSystem,
            ICollection<string> errors)
        {
            List<ExplorationFlowView> flowViews = FindSceneComponents<ExplorationFlowView>(scene);
            if (flowViews.Count != 1)
            {
                errors.Add($"SampleScene must contain exactly one ExplorationFlowView, but found {flowViews.Count}.");
            }
            else
            {
                SerializedObject flow = new SerializedObject(flowViews[0]);
                if (flow.FindProperty("explorationSystem").objectReferenceValue != explorationSystem)
                {
                    errors.Add("ExplorationFlowView is not connected to the canonical ExplorationSystem.");
                }

                ValidateRequiredReference(flow, "titleText", "ExplorationFlowView Title Text", errors);
                ValidateRequiredReference(flow, "descriptionText", "ExplorationFlowView Description Text", errors);
                ValidateRequiredReference(flow, "closeButton", "ExplorationFlowView Close Button", errors);

                SerializedProperty regionCards = flow.FindProperty("regionCards");
                if (regionCards.arraySize < 3)
                {
                    errors.Add($"ExplorationFlowView needs at least 3 region cards, but found {regionCards.arraySize}.");
                }

                for (int index = 0; index < regionCards.arraySize; index++)
                {
                    ExplorationRegionCardView card =
                        regionCards.GetArrayElementAtIndex(index).objectReferenceValue as ExplorationRegionCardView;
                    if (card == null)
                    {
                        errors.Add($"ExplorationFlowView Region Cards entry {index} is missing.");
                        continue;
                    }

                    SerializedObject cardSerialized = new SerializedObject(card);
                    ValidateRequiredReference(cardSerialized, "button", $"Region Card {index} Button", errors);
                    ValidateRequiredReference(cardSerialized, "nameText", $"Region Card {index} Name Text", errors);
                    ValidateRequiredReference(cardSerialized, "descriptionText", $"Region Card {index} Description Text", errors);
                    ValidateRequiredReference(cardSerialized, "dangerText", $"Region Card {index} Status Text", errors);
                    ValidateRequiredReference(cardSerialized, "rewardsText", $"Region Card {index} Rewards Text", errors);
                }
            }

            List<ResourceDebugView> debugViews = FindSceneComponents<ResourceDebugView>(scene);
            if (debugViews.Count != 1)
            {
                errors.Add($"SampleScene must contain exactly one ResourceDebugView, but found {debugViews.Count}.");
                return;
            }

            SerializedObject debug = new SerializedObject(debugViews[0]);
            ValidateRequiredReference(debug, "deckDebugText", "ResourceDebugView Deck/Region Debug Text", errors);
        }

        private static void ValidateRequiredReference(
            SerializedObject owner,
            string propertyName,
            string label,
            ICollection<string> errors)
        {
            SerializedProperty property = owner.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                errors.Add($"{label} is missing.");
            }
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

        private static void ValidateNormalizedValue(
            string id,
            string fieldName,
            float value,
            ICollection<string> errors)
        {
            if (value < 0f || value > 1f)
            {
                errors.Add($"'{id}' {fieldName} must be between 0 and 1, but was {value}.");
            }
        }

        private static void ValidateNonNegativeValue(
            string id,
            string fieldName,
            float value,
            ICollection<string> errors)
        {
            if (value < 0f)
            {
                errors.Add($"'{id}' {fieldName} cannot be negative, but was {value}.");
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static string GetRegionAssetPath(string regionId)
        {
            return $"{RegionAssetFolder}/{regionId}.asset";
        }

        private static bool Approximately(float first, float second)
        {
            return Mathf.Abs(first - second) <= 0.0001f;
        }

        private static void LogValidationReport(
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
                Debug.LogWarning($"Region Content V1: {warning}");
            }
        }

        private sealed class ImportContext : IDisposable
        {
            public ImportContext(
                Dictionary<string, CardPackData> packsById,
                Scene scene,
                ExplorationSystem explorationSystem,
                bool closeSceneWhenDone)
            {
                PacksById = packsById;
                Scene = scene;
                ExplorationSystem = explorationSystem;
                CloseSceneWhenDone = closeSceneWhenDone;
            }

            public Dictionary<string, CardPackData> PacksById { get; }
            public Scene Scene { get; }
            public ExplorationSystem ExplorationSystem { get; }
            private bool CloseSceneWhenDone { get; }

            public void Dispose()
            {
                if (CloseSceneWhenDone && Scene.IsValid() && Scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(Scene, true);
                }
            }
        }

        private sealed class RegionSpec
        {
            public RegionSpec(
                string regionId,
                string displayName,
                ExplorationTier tier,
                DangerLevel danger,
                int initialZombieCount,
                float baseEncounterChance,
                float heatPerExploration,
                float dailyHeatDecay)
            {
                RegionId = regionId;
                DisplayName = displayName;
                Tier = tier;
                Danger = danger;
                InitialZombieCount = initialZombieCount;
                BaseEncounterChance = baseEncounterChance;
                HeatPerExploration = heatPerExploration;
                DailyHeatDecay = dailyHeatDecay;
            }

            public string RegionId { get; }
            public string DisplayName { get; }
            public ExplorationTier Tier { get; }
            public DangerLevel Danger { get; }
            public int InitialZombieCount { get; }
            public float BaseEncounterChance { get; }
            public float HeatPerExploration { get; }
            public float DailyHeatDecay { get; }
        }

        private sealed class RegionPackSpec
        {
            public RegionPackSpec(
                string regionId,
                string packId,
                string packName,
                RegionSupplyRarity rarity,
                int initialCount,
                int baseWeight)
            {
                RegionId = regionId;
                PackId = packId;
                PackName = packName;
                Rarity = rarity;
                InitialCount = initialCount;
                BaseWeight = baseWeight;
            }

            public string RegionId { get; }
            public string PackId { get; }
            public string PackName { get; }
            public RegionSupplyRarity Rarity { get; }
            public int InitialCount { get; }
            public int BaseWeight { get; }
        }
    }
}
#endif
