#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ZombieCardSurvive.UI.World.Editor
{
    /// <summary>設定 Paper UI 素材匯入規格，並建立不影響正式場景的材質預覽。</summary>
    public static class PaperUiMaterialPreviewSetupUtility
    {
        private const string Root = "Assets/Art/GameplayHUD/PaperUI";
        private const string ScenePath = "Assets/Scenes/PaperUI_MaterialPreview.unity";
        private const string RegularFontPath = "Assets/Font/UI_Noto_Sans_TC/static/UI_Regular_Font.asset";
        private const string BoldFontPath = "Assets/Font/UI_Noto_Sans_TC/static/UI_Bold_Font.asset";

        private static readonly Color32 Ink = new Color32(49, 48, 44, 255);
        private static readonly Color32 InkSoft = new Color32(77, 74, 67, 255);
        private static readonly Color32 Chalk = new Color32(232, 229, 218, 255);
        private static readonly Color32 Red = new Color32(174, 67, 58, 255);
        private static readonly Color32 Amber = new Color32(197, 141, 55, 255);
        private static readonly Color32 Green = new Color32(71, 132, 88, 255);

        [MenuItem("Tools/Zombie Card Survive/Gameplay HUD/Build Paper UI Material Preview")]
        public static void Build()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty)
            {
                Debug.LogWarning("Paper UI preview was not created because the current scene has unsaved changes.");
                return;
            }

            EnsureFolder("Assets/Scenes");
            ConfigureImporters();

            Sprite background = LoadSprite("ui_background_archival_paper_01.png");
            Sprite lightPanel = LoadSprite("ui_panel_paper_light_9slice_01.png");
            Sprite darkPanel = LoadSprite("ui_panel_paper_dark_9slice_01.png");
            Sprite overlay = LoadSprite("ui_overlay_graphite_wear_01.png");
            Texture2D fibreTile = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/ui_texture_paper_fiber_tile_01.png");
            TMP_FontAsset regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularFontPath);
            TMP_FontAsset boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PaperUI_MaterialPreview";
            CreateCamera();
            CreatePreviewLight();
            CreateEventSystem();

            RectTransform canvasRoot = CreateCanvas();
            CreateImage("ArchivalPaperBackground", canvasRoot, background, Color.white, Image.Type.Simple,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RawImage tile = CreateRawImage("FibreTile_Repeat_12Percent", canvasRoot, fibreTile,
                new Color(1f, 1f, 1f, 0.12f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            tile.uvRect = new Rect(0f, 0f, 3f, 2f);

            CreateImage("GraphiteWearOverlay_35Percent", canvasRoot, overlay,
                new Color(1f, 1f, 1f, 0.35f), Image.Type.Simple,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            BuildTopHud(canvasRoot, darkPanel, regularFont, boldFont);
            BuildQuickBar(canvasRoot, darkPanel, boldFont);
            BuildNotebookPanel(canvasRoot, lightPanel, regularFont, boldFont);
            BuildDetailPanel(canvasRoot, darkPanel, regularFont, boldFont);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = canvasRoot.gameObject;
            Debug.Log("Paper UI material preview created: " + ScenePath);
        }

        private static void BuildTopHud(RectTransform parent, Sprite darkPanel, TMP_FontAsset regularFont, TMP_FontAsset boldFont)
        {
            RectTransform panel = CreateImage("TopResourceHud_Dark9Slice", parent, darkPanel, Color.white,
                Image.Type.Sliced, new Vector2(0.24f, 1f), new Vector2(0.985f, 1f),
                new Vector2(0f, -142f), new Vector2(0f, -20f)).rectTransform;

            CreateText("ResourceValues", panel,
                "食物  23     資源  17     戰鬥  6     能量  3 / 5     卡牌倉庫  12",
                regularFont, 31f, Chalk, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(46f, 20f), new Vector2(-46f, -20f));

            CreateText("HudCaption", panel, "橫向永久資訊列 · Dark 9-slice",
                boldFont, 17f, new Color32(176, 171, 157, 255), TextAlignmentOptions.BottomRight,
                Vector2.zero, Vector2.one, new Vector2(30f, 8f), new Vector2(-36f, -9f));
        }

        private static void BuildQuickBar(RectTransform parent, Sprite darkPanel, TMP_FontAsset boldFont)
        {
            RectTransform bar = CreateImage("LeftQuickBar", parent, darkPanel, Color.white, Image.Type.Sliced,
                new Vector2(0f, 0.12f), new Vector2(0f, 0.82f),
                new Vector2(24f, 0f), new Vector2(138f, 0f)).rectTransform;

            string[] labels = { "基", "工", "糧", "戰", "圖", "設" };
            Color32[] colors = { Chalk, Amber, Green, Red, new Color32(104, 146, 176, 255), Chalk };
            for (int i = 0; i < labels.Length; i++)
            {
                float top = -34f - (i * 104f);
                RectTransform slot = CreateImage("QuickSlot_" + labels[i], bar, null,
                    new Color(0.09f, 0.09f, 0.085f, 0.62f), Image.Type.Simple,
                    new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(15f, top - 82f), new Vector2(-15f, top)).rectTransform;
                CreateText("Label", slot, labels[i], boldFont, 34f, colors[i], TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }

        private static void BuildNotebookPanel(RectTransform parent, Sprite lightPanel, TMP_FontAsset regularFont, TMP_FontAsset boldFont)
        {
            RectTransform panel = CreateImage("NotebookPanel_Light9Slice", parent, lightPanel, Color.white,
                Image.Type.Sliced, new Vector2(0.18f, 0.12f), new Vector2(0.68f, 0.79f),
                Vector2.zero, Vector2.zero).rectTransform;

            CreateText("Title", panel, "絕望的筆記", boldFont, 52f, Ink, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(94f, 78f), new Vector2(-88f, -80f));

            CreateText("Body", panel,
                "……食物正在逐漸失去味道。\n\n今天的圍牆又多了一道裂痕。沒有人開口，但每個人都看見了。\n\n一切意義都在失去。",
                regularFont, 30f, InkSoft, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(96f, 96f), new Vector2(-88f, -175f));

            CreateText("MaterialCaption", panel,
                "Light 9-slice · 中央低對比 · 文字與圖示獨立疊加",
                regularFont, 19f, new Color32(105, 99, 87, 255), TextAlignmentOptions.BottomLeft,
                Vector2.zero, Vector2.one, new Vector2(96f, 55f), new Vector2(-88f, -54f));
        }

        private static void BuildDetailPanel(RectTransform parent, Sprite darkPanel, TMP_FontAsset regularFont, TMP_FontAsset boldFont)
        {
            RectTransform panel = CreateImage("DetailPanel_Dark9Slice", parent, darkPanel, Color.white,
                Image.Type.Sliced, new Vector2(0.705f, 0.17f), new Vector2(0.975f, 0.74f),
                Vector2.zero, Vector2.zero).rectTransform;

            CreateText("Title", panel, "輪末預測", boldFont, 34f, Chalk, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(54f, 50f), new Vector2(-48f, -45f));
            CreateText("Details", panel,
                "食物          4  →  0\n預計短缺      3\n士氣變化     −9\n\n資源          17  →  16\nStage 1 損失  −1\n\n右鍵或長按查看詳細資訊",
                regularFont, 25f, Chalk, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(54f, 55f), new Vector2(-48f, -125f));
            CreateText("Warning", panel, "食物短缺", boldFont, 25f, Red, TextAlignmentOptions.BottomRight,
                Vector2.zero, Vector2.one, new Vector2(48f, 36f), new Vector2(-48f, -38f));
        }

        private static RectTransform CreateCanvas()
        {
            GameObject go = new GameObject("PaperUI_MaterialPreview", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return go.GetComponent<RectTransform>();
        }

        private static void CreateCamera()
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(28, 27, 25, 255);
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreatePreviewLight()
        {
            GameObject go = new GameObject("Preview Directional Light");
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.8f;
            light.color = new Color32(232, 226, 209, 255);
            go.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        private static void CreateEventSystem()
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Image.Type type,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage CreateRawImage(string name, Transform parent, Texture texture, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, TMP_FontAsset font, float size,
            Color color, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/" + fileName);
        }

        private static void ConfigureImporters()
        {
            ConfigureImporter("ui_background_archival_paper_01.png", false, TextureWrapMode.Clamp, 2048, Vector4.zero);
            ConfigureImporter("ui_panel_paper_light_9slice_01.png", true, TextureWrapMode.Clamp, 2048, new Vector4(160f, 120f, 160f, 120f));
            ConfigureImporter("ui_panel_paper_dark_9slice_01.png", true, TextureWrapMode.Clamp, 2048, new Vector4(96f, 72f, 96f, 72f));
            ConfigureImporter("ui_texture_paper_fiber_tile_01.png", false, TextureWrapMode.Repeat, 1024, Vector4.zero);
            ConfigureImporter("ui_overlay_graphite_wear_01.png", true, TextureWrapMode.Clamp, 2048, Vector4.zero);
        }

        private static void ConfigureImporter(string fileName, bool alpha, TextureWrapMode wrapMode, int maxSize, Vector4 border)
        {
            string path = Root + "/" + fileName;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("Paper UI texture importer missing: " + path);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = alpha;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = wrapMode;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            string child = path.Substring(split + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
