#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZombieCardSurvive.EditorTools
{
    public static class TmpFontMigrationUtility
    {
        private const string TargetFontPath = "Assets/Font/TW-Sung-98_1 SDF.asset";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("Tools/Zombie Card Survive/Apply TW-Sung TMP Font")]
        public static void Apply()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontPath);
            if (font == null)
            {
                Debug.LogError($"TW-Sung TMP font asset was not found at '{TargetFontPath}'.");
                return;
            }

            int changed = 0;
            changed += ApplyToTmpSettings(font);
            changed += ApplyToPrefabs(font);
            changed += ApplyToSampleScene(font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"TW-Sung TMP font migration complete. Updated {changed} TMP text/settings references.");
        }

        private static int ApplyToTmpSettings(TMP_FontAsset font)
        {
            Object settings = AssetDatabase.LoadAssetAtPath<Object>(TmpSettingsPath);
            if (settings == null)
            {
                Debug.LogWarning($"TMP Settings asset was not found at '{TmpSettingsPath}'.");
                return 0;
            }

            SerializedObject serialized = new SerializedObject(settings);
            SerializedProperty defaultFont = serialized.FindProperty("m_defaultFontAsset");
            if (defaultFont == null || defaultFont.objectReferenceValue == font)
            {
                return 0;
            }

            defaultFont.objectReferenceValue = font;
            SerializedProperty defaultPath = serialized.FindProperty("m_defaultFontAssetPath");
            if (defaultPath != null)
            {
                defaultPath.stringValue = "Assets/Font/";
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            return 1;
        }

        private static int ApplyToPrefabs(TMP_FontAsset font)
        {
            int changed = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool dirty = false;
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    dirty |= ApplyToText(text, font);
                }

                if (dirty)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            return changed;
        }

        private static int ApplyToSampleScene(TMP_FontAsset font)
        {
            if (!File.Exists(SampleScenePath))
            {
                return 0;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            string activeScenePath = activeScene.path;
            Scene scene = activeScenePath == SampleScenePath
                ? activeScene
                : EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

            int changed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (ApplyToText(text, font))
                    {
                        changed++;
                    }
                }
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            return changed;
        }

        private static bool ApplyToText(TMP_Text text, TMP_FontAsset font)
        {
            if (text == null)
            {
                return false;
            }

            bool needsFont = text.font != font;
            bool needsMaterial = text.fontSharedMaterial != font.material;
            if (!needsFont && !needsMaterial)
            {
                return false;
            }

            Undo.RecordObject(text, "Apply TW-Sung TMP Font");
            text.font = font;
            text.fontSharedMaterial = font.material;
            EditorUtility.SetDirty(text);
            return true;
        }
    }
}
#endif
