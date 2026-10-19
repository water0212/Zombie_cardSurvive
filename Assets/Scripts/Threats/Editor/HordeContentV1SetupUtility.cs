#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Threats.Data;

namespace ZombieCardSurvive.Threats.Editor
{
    /// <summary>以冪等方式建立並驗證 PB-P2-009D 的正式屍群內容。</summary>
    public static class HordeContentV1SetupUtility
    {
        public const string ZombieAssetFolder = "Assets/ScriptObj/CardScriptObj/HordeV1";
        public const string ProfileAssetFolder = "Assets/ScriptObj/Threats/HordeProfiles";

        [MenuItem("Tools/Zombie Card Survive/Setup Horde Content V1")]
        public static void Setup()
        {
            List<string> errors = new List<string>();
            CreateOrUpdateContent(errors, out Dictionary<string, ZombieCardData> zombies,
                out Dictionary<string, HordeProfileData> profiles);
            if (errors.Count == 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateContent(zombies, profiles, errors);
            }

            if (errors.Count > 0)
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"Horde Content V1: {error}");
                }

                return;
            }

            Debug.Log(
                $"Horde Content V1 setup complete: {zombies.Count} zombie assets and " +
                $"{profiles.Count} horde profile asset.");
        }

        [MenuItem("Tools/Zombie Card Survive/Validate Horde Content V1")]
        public static void Validate()
        {
            List<string> errors = new List<string>();
            Dictionary<string, ZombieCardData> zombies = LoadZombieAssets();
            Dictionary<string, HordeProfileData> profiles = LoadProfileAssets();
            ValidateContent(zombies, profiles, errors);
            if (errors.Count > 0)
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"Horde Content V1: {error}");
                }

                return;
            }

            Debug.Log(
                $"Horde Content V1 validation passed: {zombies.Count} zombie assets and " +
                $"{profiles.Count} horde profile asset.");
        }

        public static void CreateOrUpdateContent(
            List<string> errors,
            out Dictionary<string, ZombieCardData> zombies,
            out Dictionary<string, HordeProfileData> profiles)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            ValidateSpecifications(errors);
            zombies = new Dictionary<string, ZombieCardData>(StringComparer.OrdinalIgnoreCase);
            profiles = new Dictionary<string, HordeProfileData>(StringComparer.OrdinalIgnoreCase);
            if (errors.Count > 0)
            {
                return;
            }

            EnsureFolder("Assets/ScriptObj/CardScriptObj", "HordeV1");
            EnsureFolder("Assets/ScriptObj", "Threats");
            EnsureFolder("Assets/ScriptObj/Threats", "HordeProfiles");

            ZombieCardData visualSource = AssetDatabase.LoadAssetAtPath<ZombieCardData>(
                HordeContentV1Specification.LegacyVisualSourcePath);
            foreach (HordeZombieContentSpec spec in HordeContentV1Specification.Zombies)
            {
                string path = GetZombieAssetPath(spec.ZombieId);
                ZombieCardData zombie = AssetDatabase.LoadAssetAtPath<ZombieCardData>(path);
                if (zombie == null)
                {
                    zombie = ScriptableObject.CreateInstance<ZombieCardData>();
                    AssetDatabase.CreateAsset(zombie, path);
                }

                zombie.name = $"{spec.ZombieId}_{spec.DisplayName}";
                ApplyZombieSpecification(zombie, spec, visualSource);
                zombies.Add(spec.ZombieId, zombie);
            }

            foreach (HordeProfileContentSpec spec in HordeContentV1Specification.Profiles)
            {
                string path = GetProfileAssetPath(spec.ProfileId);
                HordeProfileData profile = AssetDatabase.LoadAssetAtPath<HordeProfileData>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<HordeProfileData>();
                    AssetDatabase.CreateAsset(profile, path);
                }

                profile.name = $"{spec.ProfileId}_{spec.DisplayName}";
                ApplyProfileSpecification(profile, spec, zombies);
                profiles.Add(spec.ProfileId, profile);
            }
        }

        public static void ValidateContent(
            IReadOnlyDictionary<string, ZombieCardData> zombies,
            IReadOnlyDictionary<string, HordeProfileData> profiles,
            List<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            ValidateSpecifications(errors);
            foreach (HordeZombieContentSpec spec in HordeContentV1Specification.Zombies)
            {
                if (zombies == null
                    || !zombies.TryGetValue(spec.ZombieId, out ZombieCardData zombie)
                    || zombie == null)
                {
                    errors.Add($"Formal zombie asset '{spec.ZombieId}' is missing.");
                    continue;
                }

                if (!string.Equals(zombie.CardId, spec.ZombieId, StringComparison.Ordinal)
                    || !string.Equals(zombie.DisplayName, spec.DisplayName, StringComparison.Ordinal)
                    || zombie.Rarity != spec.Rarity
                    || zombie.Health != spec.Health
                    || zombie.Attack != spec.Attack
                    || zombie.EffectiveCardType != CardType.Zombie
                    || zombie.EffectivePlayMode != CardPlayMode.AutoResolveOnDraw)
                {
                    errors.Add($"Formal zombie asset '{spec.ZombieId}' does not match its specification.");
                }
            }

            foreach (HordeProfileContentSpec spec in HordeContentV1Specification.Profiles)
            {
                if (profiles == null
                    || !profiles.TryGetValue(spec.ProfileId, out HordeProfileData profile)
                    || profile == null)
                {
                    errors.Add($"Formal horde profile '{spec.ProfileId}' is missing.");
                    continue;
                }

                if (!string.Equals(profile.ProfileId, spec.ProfileId, StringComparison.Ordinal)
                    || !string.Equals(profile.DisplayName, spec.DisplayName, StringComparison.Ordinal)
                    || profile.CompositionStyle != spec.CompositionStyle
                    || profile.BaseBudget != spec.BaseBudget
                    || profile.WorldThreatBudgetPerTier != spec.WorldThreatBudgetPerTier
                    || Math.Abs(profile.BaseAttentionBudgetPerPoint - spec.BaseAttentionBudgetPerPoint) > 0.001f
                    || Math.Abs(profile.RegionPressureBudgetMultiplier - spec.RegionPressureBudgetMultiplier) > 0.001f
                    || profile.MinimumBudget != spec.MinimumBudget
                    || profile.MaximumBudget != spec.MaximumBudget
                    || profile.ZombiePool.Count != spec.ZombieIds.Count)
                {
                    errors.Add($"Formal horde profile '{spec.ProfileId}' does not match its specification.");
                    continue;
                }

                for (int index = 0; index < spec.ZombieIds.Count; index++)
                {
                    string zombieId = spec.ZombieIds[index];
                    HordeZombieContentSpec zombieSpec = HordeContentV1Specification.Zombies
                        .Single(value => string.Equals(value.ZombieId, zombieId, StringComparison.Ordinal));
                    ZombieArchetypeEntry entry = profile.ZombiePool[index];
                    ZombieCardData expectedZombie = zombies != null
                        && zombies.TryGetValue(zombieId, out ZombieCardData value)
                            ? value
                            : null;
                    if (entry == null
                        || !ReferenceEquals(entry.ZombieCard, expectedZombie)
                        || entry.BudgetCost != zombieSpec.BudgetCost
                        || entry.BaseWeight != zombieSpec.BaseWeight
                        || entry.MinWorldThreatTier != zombieSpec.MinWorldThreatTier
                        || entry.MaxWorldThreatTier != zombieSpec.MaxWorldThreatTier
                        || entry.MaxCountPerHorde != zombieSpec.MaxCountPerHorde)
                    {
                        errors.Add(
                            $"Horde profile '{spec.ProfileId}' entry {index} does not match zombie '{zombieId}'.");
                    }
                }
            }
        }

        public static string GetZombieAssetPath(string zombieId)
        {
            HordeZombieContentSpec spec = HordeContentV1Specification.Zombies.Single(
                value => string.Equals(value.ZombieId, zombieId, StringComparison.OrdinalIgnoreCase));
            return $"{ZombieAssetFolder}/{spec.ZombieId}_{SanitizeFileName(spec.DisplayName)}.asset";
        }

        public static string GetProfileAssetPath(string profileId)
        {
            HordeProfileContentSpec spec = HordeContentV1Specification.Profiles.Single(
                value => string.Equals(value.ProfileId, profileId, StringComparison.OrdinalIgnoreCase));
            return $"{ProfileAssetFolder}/{spec.ProfileId}_{SanitizeFileName(spec.DisplayName)}.asset";
        }

        private static void ApplyZombieSpecification(
            ZombieCardData zombie,
            HordeZombieContentSpec spec,
            ZombieCardData visualSource)
        {
            SerializedObject serialized = new SerializedObject(zombie);
            serialized.FindProperty("cardId").stringValue = spec.ZombieId;
            serialized.FindProperty("displayName").stringValue = spec.DisplayName;
            serialized.FindProperty("energyCost").intValue = 0;
            serialized.FindProperty("artwork").objectReferenceValue = visualSource != null ? visualSource.Artwork : null;
            serialized.FindProperty("background").objectReferenceValue = visualSource != null ? visualSource.Background : null;
            serialized.FindProperty("description").stringValue = spec.Description;
            serialized.FindProperty("cardType").enumValueIndex = (int)CardType.Zombie;
            serialized.FindProperty("rarity").enumValueIndex = (int)spec.Rarity;
            serialized.FindProperty("playMode").enumValueIndex = (int)CardPlayMode.AutoResolveOnDraw;
            serialized.FindProperty("allowedSlots").arraySize = 0;
            serialized.FindProperty("isAdditiveCard").boolValue = true;
            serialized.FindProperty("drawReplacementOnAutoResolve").boolValue = true;
            serialized.FindProperty("hasLimitedUses").boolValue = true;
            serialized.FindProperty("maxUsesPerRun").intValue = 1;
            serialized.FindProperty("costs").arraySize = 0;
            serialized.FindProperty("effects").arraySize = 0;
            serialized.FindProperty("health").intValue = spec.Health;
            serialized.FindProperty("attack").intValue = spec.Attack;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zombie);
        }

        private static void ApplyProfileSpecification(
            HordeProfileData profile,
            HordeProfileContentSpec spec,
            IReadOnlyDictionary<string, ZombieCardData> zombies)
        {
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = spec.ProfileId;
            serialized.FindProperty("displayName").stringValue = spec.DisplayName;
            serialized.FindProperty("description").stringValue = spec.Description;
            serialized.FindProperty("compositionStyle").enumValueIndex = (int)spec.CompositionStyle;
            serialized.FindProperty("baseBudget").intValue = spec.BaseBudget;
            serialized.FindProperty("worldThreatBudgetPerTier").intValue = spec.WorldThreatBudgetPerTier;
            serialized.FindProperty("baseAttentionBudgetPerPoint").floatValue = spec.BaseAttentionBudgetPerPoint;
            serialized.FindProperty("regionPressureBudgetMultiplier").floatValue = spec.RegionPressureBudgetMultiplier;
            serialized.FindProperty("minimumBudget").intValue = spec.MinimumBudget;
            serialized.FindProperty("maximumBudget").intValue = spec.MaximumBudget;

            SerializedProperty pool = serialized.FindProperty("zombiePool");
            pool.arraySize = spec.ZombieIds.Count;
            for (int index = 0; index < spec.ZombieIds.Count; index++)
            {
                HordeZombieContentSpec zombieSpec = HordeContentV1Specification.Zombies.Single(
                    value => string.Equals(value.ZombieId, spec.ZombieIds[index], StringComparison.Ordinal));
                SerializedProperty entry = pool.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("zombieCard").objectReferenceValue = zombies[zombieSpec.ZombieId];
                entry.FindPropertyRelative("budgetCost").intValue = zombieSpec.BudgetCost;
                entry.FindPropertyRelative("baseWeight").intValue = zombieSpec.BaseWeight;
                entry.FindPropertyRelative("minWorldThreatTier").intValue = zombieSpec.MinWorldThreatTier;
                entry.FindPropertyRelative("maxWorldThreatTier").intValue = zombieSpec.MaxWorldThreatTier;
                entry.FindPropertyRelative("maxCountPerHorde").intValue = zombieSpec.MaxCountPerHorde;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        private static void ValidateSpecifications(List<string> errors)
        {
            HashSet<string> zombieIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (HordeZombieContentSpec spec in HordeContentV1Specification.Zombies)
            {
                if (spec == null || string.IsNullOrWhiteSpace(spec.ZombieId) || !zombieIds.Add(spec.ZombieId))
                {
                    errors.Add("Horde Content V1 contains a missing or duplicate zombie ID.");
                    continue;
                }

                if (spec.Health < 1 || spec.Attack < 0 || spec.BudgetCost < 1 || spec.BaseWeight < 1
                    || spec.MinWorldThreatTier < 1 || spec.MaxCountPerHorde < 1)
                {
                    errors.Add($"Zombie specification '{spec.ZombieId}' has invalid combat or pool values.");
                }
            }

            HashSet<string> profileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (HordeProfileContentSpec spec in HordeContentV1Specification.Profiles)
            {
                if (spec == null || string.IsNullOrWhiteSpace(spec.ProfileId) || !profileIds.Add(spec.ProfileId))
                {
                    errors.Add("Horde Content V1 contains a missing or duplicate profile ID.");
                    continue;
                }

                if (spec.BaseBudget < 0 || spec.MinimumBudget < 0
                    || spec.MaximumBudget < spec.MinimumBudget || spec.ZombieIds.Count == 0)
                {
                    errors.Add($"Horde profile specification '{spec.ProfileId}' has invalid budget values.");
                }

                foreach (string zombieId in spec.ZombieIds)
                {
                    if (!zombieIds.Contains(zombieId))
                    {
                        errors.Add($"Horde profile '{spec.ProfileId}' references unknown zombie '{zombieId}'.");
                    }
                }
            }
        }

        public static Dictionary<string, ZombieCardData> LoadZombieAssets()
        {
            Dictionary<string, ZombieCardData> result =
                new Dictionary<string, ZombieCardData>(StringComparer.OrdinalIgnoreCase);
            foreach (HordeZombieContentSpec spec in HordeContentV1Specification.Zombies)
            {
                ZombieCardData asset = AssetDatabase.LoadAssetAtPath<ZombieCardData>(GetZombieAssetPath(spec.ZombieId));
                if (asset != null)
                {
                    result[spec.ZombieId] = asset;
                }
            }

            return result;
        }

        public static Dictionary<string, HordeProfileData> LoadProfileAssets()
        {
            Dictionary<string, HordeProfileData> result =
                new Dictionary<string, HordeProfileData>(StringComparer.OrdinalIgnoreCase);
            foreach (HordeProfileContentSpec spec in HordeContentV1Specification.Profiles)
            {
                HordeProfileData asset = AssetDatabase.LoadAssetAtPath<HordeProfileData>(
                    GetProfileAssetPath(spec.ProfileId));
                if (asset != null)
                {
                    result[spec.ProfileId] = asset;
                }
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

        private static string SanitizeFileName(string value)
        {
            string result = value ?? string.Empty;
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(invalid, '_');
            }

            return result.Trim();
        }
    }
}
#endif
