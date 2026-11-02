#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Exploration.Runtime;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.UI.Settlement;

namespace ZombieCardSurvive.Run.Editor
{
    public static class RunLifecycleSetup
    {
        public const string TestScene = "Assets/Scenes/Sample_E3_IntegrationTest.unity";

        [MenuItem("Tools/E3/Create and validate test copy")]
        public static void CreateTestCopy()
        {
            RequireCleanEditor();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScene) != null)
                throw new InvalidOperationException("E3 test copy already exists; inspect it rather than overwrite it.");
            if (!AssetDatabase.CopyAsset(RunLifecycleController.GameplayScene, TestScene))
                throw new InvalidOperationException("Could not copy SampleScene");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            var test = EditorSceneManager.OpenScene(TestScene, OpenSceneMode.Single);
            AttachEntry(test);
            Validate(test);
            EditorSceneManager.SaveScene(test);
            EditorSceneManager.RestoreSceneManagerSetup(previous);
        }

        [MenuItem("Tools/E3/Install menu and Sample entry")]
        public static void Install()
        {
            RequireCleanEditor();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScene) == null)
                throw new InvalidOperationException("Create and validate the test copy first");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var sample = EditorSceneManager.OpenScene(RunLifecycleController.GameplayScene, OpenSceneMode.Single);
                Validate(sample);
                var run = UnityEngine.Object.FindObjectOfType<RunPhaseController>();
                var profile = run.DefaultMoraleProfile;
                if (profile == null || !profile.TryCreateSnapshot(out _, out var error))
                    throw new InvalidOperationException("Sample requires a valid default profile");
                AttachEntry(sample);
                EditorSceneManager.SaveScene(sample);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RunLifecycleController.MenuScene) == null)
                {
                    var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    CreateMenu(profile);
                    EditorSceneManager.SaveScene(menu, RunLifecycleController.MenuScene);
                }
                var scenes = EditorBuildSettings.scenes.ToList();
                foreach (var path in new[] { RunLifecycleController.MenuScene, RunLifecycleController.GameplayScene })
                {
                    var existing = scenes.FirstOrDefault(s => s.path == path);
                    if (existing != null) existing.enabled = true;
                    else scenes.Add(new EditorBuildSettingsScene(path, true));
                }
                // Menu is the player entry; preserve the relative order of all existing scenes.
                var menuEntry = scenes.First(s => s.path == RunLifecycleController.MenuScene);
                scenes.Remove(menuEntry); scenes.Insert(0, menuEntry);
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }
        private static void RequireCleanEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before installing E3");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your scene changes before installing E3");
        }
        private static void AttachEntry(Scene scene)
        {
            var entries = UnityEngine.Object.FindObjectsOfType<RunSceneEntry>(true).Where(e => e.gameObject.scene == scene).ToArray();
            if (entries.Length > 1) throw new InvalidOperationException("Duplicate RunSceneEntry");
            if (entries.Length == 0) new GameObject("E3 Scene Entry").AddComponent<RunSceneEntry>();
        }
        public static void Validate(Scene scene)
        {
            RequireOne<RunPhaseController>(scene); RequireOne<CardController>(scene);
            RequireOne<BuildingSystem>(scene); RequireOne<ExplorationSystem>(scene);
            RequireOne<EventFlowController>(scene); RequireOne<RoundEndResolver>(scene);
            RequireOne<CollapseReportView>(scene);
        }
        private static void RequireOne<T>(Scene scene) where T : Component
        {
            if (UnityEngine.Object.FindObjectsOfType<T>(true).Count(c => c.gameObject.scene == scene) != 1)
                throw new InvalidOperationException("Expected exactly one " + typeof(T).Name + " in " + scene.path);
        }
        private static void CreateMenu(MoralePyramidProfileData profile)
        {
            var cameraObject = new GameObject("White Background", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.white;
            camera.cullingMask = 0;
            var canvasObject = new GameObject("Main Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Label(canvasObject.transform, "Title", "New Run", 140, 38);
            Label(canvasObject.transform, "Profile label", "Morale profile", 65, 24);
            var dropdownObject = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
            dropdownObject.name = "Morale Profile";
            dropdownObject.transform.SetParent(canvasObject.transform, false);
            Place(dropdownObject.GetComponent<RectTransform>(), 10, 440, 50);
            var dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            var start = Button(canvasObject.transform, "Start", "Start new run", -70);
            var retry = Button(canvasObject.transform, "Retry", "Retry menu", -200);
            var status = Label(canvasObject.transform, "Status", "Preparing menu", -140, 22);
            var menu = canvasObject.AddComponent<MainMenuPlaceholder>();
            var data = new SerializedObject(menu);
            data.FindProperty("profiles").arraySize = 1;
            data.FindProperty("profiles").GetArrayElementAtIndex(0).objectReferenceValue = profile;
            data.FindProperty("profileDropdown").objectReferenceValue = dropdown;
            data.FindProperty("startButton").objectReferenceValue = start;
            data.FindProperty("retryButton").objectReferenceValue = retry;
            data.FindProperty("statusText").objectReferenceValue = status;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static TMP_Text Label(Transform parent, string name, string text, float y, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false); Place(go.GetComponent<RectTransform>(), y, 900, 64);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center;
            label.color = Color.black; label.raycastTarget = false; return label;
        }
        private static Button Button(Transform parent, string name, string text, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false); Place(go.GetComponent<RectTransform>(), y, 440, 56);
            go.GetComponent<Image>().color = new Color(.85f, .85f, .85f);
            var label = Label(go.transform, "Label", text, 0, 24);
            Place(label.rectTransform, 0, 430, 54);
            return go.GetComponent<Button>();
        }
        private static void Place(RectTransform rect, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(width, height);
        }
    }
}
#endif
