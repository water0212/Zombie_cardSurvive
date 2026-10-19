#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieCardSurvive.Run;

namespace ZombieCardSurvive.Systems.Editor
{
    /// <summary>建立 PB-P0-014D 固定 Demo Profile，並指定給 SampleScene。</summary>
    public static class MoralePyramidDemoSetupUtility
    {
        public const string ProfileId = "MORALE-PYRAMID-DEMO-01";
        public const string ProfileDisplayName = "標準生存";
        public const string ProfileFolder = "Assets/ScriptObj/Morale";
        public const string ProfileAssetPath =
            ProfileFolder + "/MORALE-PYRAMID-DEMO-01_StandardSurvival.asset";
        public const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Zombie Card Survive/Setup Morale Pyramid Demo")]
        public static void Setup()
        {
            EnsureFolder("Assets/ScriptObj", "Morale");
            MoralePyramidProfileData profile = CreateOrUpdateProfile();
            ValidateProfile(profile);
            AssignProfileToSampleScene(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Morale Pyramid Demo setup complete: '{ProfileId}' assigned to SampleScene. "
                + "Stage 0 none; Stage 1 resource -1/round; Stage 2 maximum energy -1.");
        }

        [MenuItem("Tools/Zombie Card Survive/Validate Morale Pyramid Demo")]
        public static void Validate()
        {
            MoralePyramidProfileData profile =
                AssetDatabase.LoadAssetAtPath<MoralePyramidProfileData>(ProfileAssetPath);
            ValidateProfile(profile);

            RunPhaseController controller = FindSampleSceneController(openSceneWhenNeeded: true);
            if (controller.DefaultMoraleProfile != profile)
            {
                throw new InvalidOperationException(
                    "SampleScene RunPhaseController does not reference the Demo morale profile.");
            }

            Debug.Log("Morale Pyramid Demo validation passed.");
        }

        public static MoralePyramidProfileData CreateOrUpdateProfile()
        {
            MoralePyramidProfileData profile =
                AssetDatabase.LoadAssetAtPath<MoralePyramidProfileData>(ProfileAssetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<MoralePyramidProfileData>();
                AssetDatabase.CreateAsset(profile, ProfileAssetPath);
            }

            profile.name = "MORALE-PYRAMID-DEMO-01_StandardSurvival";
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = ProfileId;
            serialized.FindProperty("displayName").stringValue = ProfileDisplayName;
            serialized.FindProperty("description").stringValue =
                "PB-P0-014D Demo 固定士氣金字塔：高士氣無效果，動搖時每輪損失 1 資源，瀕危時最大能量 -1。";
            ConfigureStage(
                serialized.FindProperty("stage0"),
                "Stage 0－穩定",
                "無效果。",
                null,
                0);
            ConfigureStage(
                serialized.FindProperty("stage1"),
                "Stage 1－動搖",
                "每輪結算時損失 1 資源，最低為 0。",
                MoraleStageEffectType.LoseResourceEachRound,
                1);
            ConfigureStage(
                serialized.FindProperty("stage2"),
                "Stage 2－瀕危",
                "取代 Stage 1；下一個 Turn 起最大能量 -1，最低為 0。",
                MoraleStageEffectType.ModifyMaximumEnergy,
                -1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void AssignProfileToSampleScene(MoralePyramidProfileData profile)
        {
            RunPhaseController controller = FindSampleSceneController(openSceneWhenNeeded: true);
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("defaultMoraleProfile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            Scene scene = controller.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("Failed to save SampleScene after assigning profile.");
            }
        }

        private static RunPhaseController FindSampleSceneController(bool openSceneWhenNeeded)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != SampleScenePath)
            {
                if (!openSceneWhenNeeded)
                {
                    return null;
                }

                scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            }

            RunPhaseController[] controllers =
                UnityEngine.Object.FindObjectsOfType<RunPhaseController>(true);
            RunPhaseController match = null;
            foreach (RunPhaseController controller in controllers)
            {
                if (controller.gameObject.scene != scene)
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        "SampleScene contains more than one RunPhaseController.");
                }

                match = controller;
            }

            if (match == null)
            {
                throw new InvalidOperationException(
                    "SampleScene does not contain a RunPhaseController.");
            }

            return match;
        }

        private static void ValidateProfile(MoralePyramidProfileData profile)
        {
            if (profile == null)
            {
                throw new InvalidOperationException(
                    $"Morale Pyramid Demo profile is missing at '{ProfileAssetPath}'.");
            }

            if (!profile.TryCreateSnapshot(
                    out MoralePyramidProfileSnapshot snapshot,
                    out string error))
            {
                throw new InvalidOperationException($"Invalid Demo morale profile: {error}");
            }

            MoraleStageSnapshot stable = snapshot.GetStage(MoraleCollapseStage.Stable);
            MoraleStageSnapshot shaken = snapshot.GetStage(MoraleCollapseStage.Shaken);
            MoraleStageSnapshot brink = snapshot.GetStage(MoraleCollapseStage.Brink);
            if (!string.Equals(snapshot.ProfileId, ProfileId, StringComparison.Ordinal)
                || stable.ResourceLossPerRound != 0
                || stable.MaximumEnergyModifier != 0
                || shaken.ResourceLossPerRound != 1
                || shaken.MaximumEnergyModifier != 0
                || brink.ResourceLossPerRound != 0
                || brink.MaximumEnergyModifier != -1)
            {
                throw new InvalidOperationException(
                    "Morale Pyramid Demo profile does not match the approved PB-P0-014D values.");
            }
        }

        private static void ConfigureStage(
            SerializedProperty stage,
            string displayName,
            string description,
            MoraleStageEffectType? effectType,
            int amount)
        {
            stage.FindPropertyRelative("displayName").stringValue = displayName;
            stage.FindPropertyRelative("description").stringValue = description;
            SerializedProperty effects = stage.FindPropertyRelative("effects");
            effects.arraySize = effectType.HasValue ? 1 : 0;
            if (!effectType.HasValue)
            {
                return;
            }

            SerializedProperty effect = effects.GetArrayElementAtIndex(0);
            effect.FindPropertyRelative("effectType").enumValueIndex = (int)effectType.Value;
            effect.FindPropertyRelative("amount").intValue = amount;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
