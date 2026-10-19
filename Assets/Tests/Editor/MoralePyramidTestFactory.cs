using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Tests.Editor
{
    internal static class MoralePyramidTestFactory
    {
        public static MoralePyramidProfileData CreateDemoProfile(
            int shakenResourceLoss = 1,
            int brinkMaximumEnergyModifier = -1)
        {
            MoralePyramidProfileData profile =
                ScriptableObject.CreateInstance<MoralePyramidProfileData>();
            profile.name = "Morale Pyramid Test Profile";

            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = "MORALE-PYRAMID-TEST";
            serialized.FindProperty("displayName").stringValue = "Test Profile";
            serialized.FindProperty("description").stringValue = "Editor test profile.";
            ConfigureStage(serialized.FindProperty("stage0"), "Stable", 0, 0);
            ConfigureStage(
                serialized.FindProperty("stage1"),
                "Shaken",
                shakenResourceLoss,
                0);
            ConfigureStage(
                serialized.FindProperty("stage2"),
                "Brink",
                0,
                brinkMaximumEnergyModifier);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        public static void Assign(
            RunPhaseController controller,
            MoralePyramidProfileData profile)
        {
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("defaultMoraleProfile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureStage(
            SerializedProperty stage,
            string displayName,
            int resourceLoss,
            int maximumEnergyModifier)
        {
            stage.FindPropertyRelative("displayName").stringValue = displayName;
            stage.FindPropertyRelative("description").stringValue = displayName;
            SerializedProperty effects = stage.FindPropertyRelative("effects");
            int effectCount = (resourceLoss != 0 ? 1 : 0)
                + (maximumEnergyModifier != 0 ? 1 : 0);
            effects.arraySize = effectCount;

            int index = 0;
            if (resourceLoss != 0)
            {
                ConfigureEffect(
                    effects.GetArrayElementAtIndex(index++),
                    MoraleStageEffectType.LoseResourceEachRound,
                    resourceLoss);
            }

            if (maximumEnergyModifier != 0)
            {
                ConfigureEffect(
                    effects.GetArrayElementAtIndex(index),
                    MoraleStageEffectType.ModifyMaximumEnergy,
                    maximumEnergyModifier);
            }
        }

        private static void ConfigureEffect(
            SerializedProperty effect,
            MoraleStageEffectType effectType,
            int amount)
        {
            effect.FindPropertyRelative("effectType").enumValueIndex = (int)effectType;
            effect.FindPropertyRelative("amount").intValue = amount;
        }
    }
}
