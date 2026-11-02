#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Cards.UI;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Exploration.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.UI.Morale;
using ZombieCardSurvive.UI.Morale.Editor;
using ZombieCardSurvive.UI.World;

namespace ZombieCardSurvive.UI.Settlement.Editor
{
    /// <summary>Installs presentation on the existing playable Sample controls; never copies P0 demo controllers.</summary>
    public static class SampleE1E2IntegrationSetupUtility
    {
        public const string MainScenePath = "Assets/Scenes/SampleScene.unity";
        public const string TestScenePath = "Assets/Scenes/Sample_E1E2_IntegrationTest.unity";
        public const string OriginalMainHashKey = "E1E2.SampleOriginalHash";
        public const string PassedSmokeHashKey = "E1E2.PassedSmokeHash";
        public const string ProbeResultKey = "E1E2.PlayProbe";
        private const string HudRootName = "GameplayE1E2HUD";

        [MenuItem("Tools/QA/E2/Sample integration/1 Create test scene from Sample")]
        public static void CreateTestScene()
        {
            RequireEditAndCleanScene();
            if (File.Exists(TestScenePath)) throw new InvalidOperationException("Test scene already exists. Open it instead of replacing it.");
            SessionState.SetString(OriginalMainHashKey, FileHash(MainScenePath));
            SessionState.SetString(PassedSmokeHashKey, string.Empty);
            if (!AssetDatabase.CopyAsset(MainScenePath, TestScenePath)) throw new InvalidOperationException("Could not copy SampleScene.");
            var scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            ApplyToCurrentScene();
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save integration test scene.");
        }

        [MenuItem("Tools/QA/E2/Sample integration/2 Apply E1 E2 to current Sample copy")]
        public static void ApplyToCurrentScene()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Install only in Edit mode.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != TestScenePath && scene.path != MainScenePath)
                throw new InvalidOperationException("This installer targets SampleScene or its integration test copy.");
            var run = Unique<RunPhaseController>(scene);
            var cards = Unique<CardController>(scene);
            var hand = Unique<CardHandView>(scene);
            var debug = Unique<ResourceDebugView>(scene);
            if (InScene<P0GameplayHudSliceController>(scene).Length != 0) throw new InvalidOperationException("P0 demo initialization is not allowed in the playable scene.");
            if (InScene<GameplayHudRuntimePresenter>(scene).Length > 0)
            {
                ConfigurePresentationLayout(scene);
                ThrowIfInvalid(scene);
                return;
            }
            var debugSerialized = new SerializedObject(debug);
            var food = Read<TMP_Text>(debugSerialized, "foodText");
            var resource = Read<TMP_Text>(debugSerialized, "resourceText");
            var combat = Read<TMP_Text>(debugSerialized, "pendingDamageText");
            var energy = Read<TMP_Text>(debugSerialized, "energyText");
            if (food == null || resource == null || combat == null || energy == null)
                throw new InvalidOperationException("Sample resource display references are incomplete.");
            var canvas = food.GetComponentInParent<Canvas>();
            if (canvas == null || hand == null) throw new InvalidOperationException("Missing playable Canvas or hand.");

            var root = Rect(HudRootName, canvas.transform);
            Stretch(root);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Integrate Sample E1 E2");
            // Keep real CardHandView, prefab, play zone, effects and event/building/exploration UI untouched.
            var moralePanel = Rect("MoralePanel", root);
            moralePanel.anchorMin = moralePanel.anchorMax = new Vector2(.37f, 1f);
            moralePanel.pivot = new Vector2(.5f, 1f);
            moralePanel.anchoredPosition = new Vector2(0, -12);
            moralePanel.sizeDelta = new Vector2(430, 210);
            var morale = Text("LegacyMoralePlaceholder", moralePanel, food.font);
            var threshold = Text("LegacyThresholdPlaceholder", moralePanel, food.font);
            var hud = root.gameObject.AddComponent<GameplayHudRuntimePresenter>();
            hud.Configure(run, cards, null, null, morale, threshold, food, resource, combat, energy, null, null,
                Array.Empty<RectTransform>(), Array.Empty<TMP_Text>(), Array.Empty<TMP_Text>(), Array.Empty<TMP_Text>());
            debugSerialized.FindProperty("primaryValuesOwnedExternally").boolValue = true;
            debugSerialized.ApplyModifiedProperties();

            MoraleHudE1SetupUtility.ApplyToCurrentScene();
            SettlementE2Setup.Install();
            var e1 = Unique<MoraleHudPresenter>(scene);
            var e2 = Unique<SettlementPresenter>(scene);
            var e1Serialized = new SerializedObject(e1);
            // The old main HUD has its own numeric layout. Place warnings immediately below each number.
            foreach (var field in new[] { "foodWarningText", "resourceWarningText", "combatWarningText" })
            {
                var text = Read<TMP_Text>(e1Serialized, field);
                if (text == null) continue;
                var rect = text.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
                rect.pivot = new Vector2(.5f, 1f);
                rect.anchoredPosition = new Vector2(0, -4);
                rect.sizeDelta = new Vector2(230, 58);
                text.fontSize = 15;
            }
            var e2Serialized = new SerializedObject(e2);
            var summary = Read<TMP_Text>(e2Serialized, "summaryText");
            summary.rectTransform.anchorMin = new Vector2(.73f, .73f);
            summary.rectTransform.anchorMax = new Vector2(.98f, .89f);
            summary.rectTransform.offsetMin = summary.rectTransform.offsetMax = Vector2.zero;
            foreach (var view in InScene<ResourceValuePresenter>(scene))
            {
                var delta = Read<TMP_Text>(new SerializedObject(view), "deltaText");
                if (delta == null) continue;
                delta.rectTransform.anchorMin = new Vector2(.08f, .02f);
                delta.rectTransform.anchorMax = new Vector2(.92f, .30f);
                delta.rectTransform.offsetMin = delta.rectTransform.offsetMax = Vector2.zero;
                delta.alignment = TextAlignmentOptions.Center;
                delta.enableWordWrapping = false;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            ConfigurePresentationLayout(scene);
            ThrowIfInvalid(scene);
        }

        private static void ConfigurePresentationLayout(Scene scene)
        {
            var hud = Unique<GameplayHudRuntimePresenter>(scene);
            var canvas = hud.GetComponentInParent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            // Main flow overlays must cover the passive HUD while event/exploration choices are open.
            var gameplayLayer = canvas.transform.Find("00_TurnPlay/20_TurnHUDLayer");
            if (gameplayLayer != null && hud.transform.parent != gameplayLayer) hud.transform.SetParent(gameplayLayer, false);
            Stretch((RectTransform)hud.transform);
            var moralePanel = (RectTransform)hud.transform.Find("MoralePanel");
            moralePanel.anchorMin = moralePanel.anchorMax = new Vector2(.37f, 1f);
            moralePanel.pivot = new Vector2(.5f, 1f);
            moralePanel.anchoredPosition = new Vector2(0, -12);
            moralePanel.sizeDelta = new Vector2(430, 210);
            var presenter = Unique<SettlementPresenter>(scene);
            var summary = Read<TMP_Text>(new SerializedObject(presenter), "summaryText");
            if (summary.transform.parent != hud.transform) summary.transform.SetParent(hud.transform, false);
            summary.rectTransform.anchorMin = new Vector2(.73f, .52f);
            summary.rectTransform.anchorMax = new Vector2(.98f, .70f);
            summary.rectTransform.offsetMin = summary.rectTransform.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(scaler);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Tools/QA/E2/Sample integration/4 Promote verified test scene to Sample")]
        public static void PromoteVerifiedTestScene()
        {
            RequireEditAndCleanScene();
            var scene = SceneManager.GetActiveScene();
            if (scene.path != TestScenePath) throw new InvalidOperationException("Open the verified test scene first.");
            ThrowIfInvalid(scene);
            if (SessionState.GetString(PassedSmokeHashKey, "") != FileHash(TestScenePath))
                throw new InvalidOperationException("The saved test scene has not passed the current Play probe.");
            if (SessionState.GetString(OriginalMainHashKey, "") != FileHash(MainScenePath))
                throw new InvalidOperationException("SampleScene changed since the copy was made; do not overwrite those changes.");
            // Save as a copy keeps SampleScene.meta and its existing scene GUID.
            if (!EditorSceneManager.SaveScene(scene, MainScenePath, true)) throw new InvalidOperationException("Could not save verified scene to Sample.");
            AssetDatabase.ImportAsset(MainScenePath);
            var main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            ThrowIfInvalid(main);
        }

        public static string[] ValidateScene(Scene scene)
        {
            var errors = new List<string>();
            CheckCount<RunPhaseController>(scene, 1, errors); CheckCount<CardController>(scene, 1, errors);
            CheckCount<CardHandView>(scene, 1, errors); CheckCount<RoundEndResolver>(scene, 1, errors);
            CheckCount<EventFlowController>(scene, 1, errors); CheckCount<BuildingSystem>(scene, 1, errors);
            CheckCount<ExplorationSystem>(scene, 1, errors); CheckCount<EventSystem>(scene, 1, errors);
            CheckCount<GameplayHudRuntimePresenter>(scene, 1, errors); CheckCount<MoraleHudPresenter>(scene, 1, errors);
            CheckCount<SettlementPresenter>(scene, 1, errors); CheckCount<ResourceValuePresenter>(scene, 4, errors);
            CheckCount<P0GameplayHudSliceController>(scene, 0, errors);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0) errors.Add("Missing script: " + transform.name);
            var cards = InScene<CardController>(scene).FirstOrDefault();
            var run = InScene<RunPhaseController>(scene).FirstOrDefault();
            foreach (var hand in InScene<CardHandView>(scene))
            {
                var so = new SerializedObject(hand);
                RequireReference(so, "cardController", cards, errors);
                RequireAssigned(so, new[] { "cardPrefab", "handContainer", "playZone", "playEffectController" }, errors);
            }
            foreach (var hud in InScene<GameplayHudRuntimePresenter>(scene))
            {
                var so = new SerializedObject(hud);
                RequireReference(so, "cardController", cards, errors); RequireReference(so, "runPhaseController", run, errors);
                RequireAssigned(so, new[] { "foodText", "resourceText", "combatText", "energyText" }, errors);
                if (so.FindProperty("handCardRoots").arraySize != 0) errors.Add("Presentation must not create a second fake hand.");
                var scaler = hud.GetComponentInParent<Canvas>().GetComponent<CanvasScaler>();
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                    || scaler.referenceResolution != new Vector2(1920,1080) || scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand)
                    errors.Add("Main Canvas would crop hand/end-turn controls at smaller Game view sizes.");
            }
            foreach (var debug in InScene<ResourceDebugView>(scene))
                if (!new SerializedObject(debug).FindProperty("primaryValuesOwnedExternally").boolValue) errors.Add("Legacy polling would overwrite numeric animation.");
            foreach (var morale in InScene<MoraleHudPresenter>(scene))
            {
                var so = new SerializedObject(morale);
                RequireReference(so, "runPhaseController", run, errors);
                RequireAssigned(so, new[] { "basePresenter", "moraleText", "stageText", "detailRoot", "detailText", "confirmationRoot", "confirmationBody", "returnButton", "continueButton" }, errors);
            }
            foreach (var presenter in InScene<SettlementPresenter>(scene))
            {
                var so = new SerializedObject(presenter);
                RequireReference(so, "run", run, errors);
                RequireAssigned(so, new[] { "hud", "moraleHud", "inputBlocker", "stagePanel", "stageText", "summaryText", "collapseView" }, errors);
                foreach (var field in new[] { "resourceViews", "centralLayers" })
                {
                    var array = so.FindProperty(field);
                    for (int i = 0; i < array.arraySize; i++) if (array.GetArrayElementAtIndex(i).objectReferenceValue == null) errors.Add("Missing " + field + "[" + i + "]");
                }
                foreach (var field in new[] { "run", "hud", "moraleHud" })
                    if (so.FindProperty(field).objectReferenceValue is Component component && component.gameObject.scene != scene) errors.Add("Cross-scene reference: " + field);
            }
            return errors.ToArray();
        }
        public static void ThrowIfInvalid(Scene scene)
        {
            var errors = ValidateScene(scene);
            if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }
        public static string FileHash(string path)
        { using (var algorithm = SHA256.Create()) return BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""); }
        public static T[] InScene<T>(Scene scene) where T : Component
            => UnityEngine.Object.FindObjectsOfType<T>(true).Where(component => component.gameObject.scene == scene).ToArray();
        private static T Unique<T>(Scene scene) where T : Component
        { var all = InScene<T>(scene); if (all.Length != 1) throw new InvalidOperationException(typeof(T).Name + " count=" + all.Length); return all[0]; }
        private static void CheckCount<T>(Scene scene, int expected, List<string> errors) where T : Component
        { int count = InScene<T>(scene).Length; if (count != expected) errors.Add(typeof(T).Name + ": " + count + " (expected " + expected + ")"); }
        private static void RequireReference(SerializedObject owner, string field, UnityEngine.Object target, List<string> errors)
        { if (owner.FindProperty(field)?.objectReferenceValue != target) errors.Add(owner.targetObject.GetType().Name + "." + field + " has the wrong owner."); }
        private static void RequireAssigned(SerializedObject owner, string[] fields, List<string> errors)
        { foreach (var field in fields) if (owner.FindProperty(field)?.objectReferenceValue == null) errors.Add(owner.targetObject.GetType().Name + "." + field + " is missing."); }
        private static T Read<T>(SerializedObject owner, string field) where T : UnityEngine.Object => owner.FindProperty(field)?.objectReferenceValue as T;
        private static void RequireEditAndCleanScene()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save existing scene changes before this operation.");
        }
        private static RectTransform Rect(string name, Transform parent)
        { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect; }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font)
        { var rect = Rect(name, parent); Stretch(rect); var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.raycastTarget = false; return text; }
    }
}
#endif
