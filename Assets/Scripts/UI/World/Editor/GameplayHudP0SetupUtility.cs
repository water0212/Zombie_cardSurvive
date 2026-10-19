#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.UI.World.Editor
{
    /// <summary>建立可重複產生的 P0 Gameplay HUD 整合驗證場景。</summary>
    public static class GameplayHudP0SetupUtility
    {
        private const string Root = "Assets/Art/GameplayHUD/P0";
        private const string ScenePath = "Assets/Scenes/GameplayHUD_P0_Integration.unity";
        private const string DemoZombiePath = Root + "/Data/Zombie_Wanderer_P0.asset";

        // HUD 使用實心深灰底確保可讀性；破紙圖只作為低透明度的表面紋理與邊緣。
        private static readonly Color32 Paper = new Color32(58, 56, 52, 246);
        private static readonly Color32 PaperDark = new Color32(38, 37, 34, 250);
        private static readonly Color32 Charcoal = new Color32(232, 229, 218, 255);
        private static readonly Color32 Red = new Color32(226, 96, 82, 255);
        private static readonly Color32 Green = new Color32(132, 190, 138, 255);
        private static readonly Color32 Amber = new Color32(231, 174, 82, 255);
        private static Sprite panelSprite;

        private sealed class HudReferences
        {
            public TMP_Text Day;
            public TMP_Text Phase;
            public TMP_Text Morale;
            public TMP_Text MoraleThreshold;
            public TMP_Text Food;
            public TMP_Text Resource;
            public TMP_Text Combat;
            public TMP_Text Energy;
            public TMP_Text DrawPile;
            public TMP_Text DiscardPile;
            public TMP_Text Detail;
            public TMP_Text LockState;
            public Button LockButton;
            public readonly List<RectTransform> CardRoots = new List<RectTransform>();
            public readonly List<TMP_Text> CardNames = new List<TMP_Text>();
            public readonly List<TMP_Text> CardCosts = new List<TMP_Text>();
            public readonly List<TMP_Text> CardTypes = new List<TMP_Text>();
        }

        [MenuItem("Tools/Zombie Card Survive/Gameplay HUD/Build P0 Integration Slice")]
        public static void Build()
        {
            EnsureFolder("Assets/Art/GameplayHUD/P0/Data");
            EnsureFolder("Assets/Art/GameplayHUD/P0/Materials");
            EnsureFolder("Assets/Scenes");

            Sprite backdrop = ImportSprite(Root + "/world_backdrop_parchment_02_dark_fractured_alpha.png", 100f);
            Sprite ground = ImportSprite(Root + "/world_ground_base_charcoal_01.png", 256f);
            Sprite intel = ImportSprite(Root + "/world_building_intel_lv1_01.png", 256f);
            Sprite workbench = ImportSprite(Root + "/world_building_workbench_lv1_01.png", 256f);
            Sprite zombie = ImportSprite(Root + "/world_zombie_wanderer_idle_00.png", 256f);
            panelSprite = ImportPanelSprite(Root + "/ui_panel_torn_gray_03_readable.png");

            Material lit = EnsureMaterial(Root + "/Materials/mat_world_sprite_lit.mat", "Universal Render Pipeline/2D/Sprite-Lit-Default");
            Material unlit = EnsureMaterial(Root + "/Materials/mat_world_sprite_unlit.mat", "Universal Render Pipeline/2D/Sprite-Unlit-Default");
            Material particleUnlit = EnsureMaterial(Root + "/Materials/mat_world_particle_unlit.mat", "Universal Render Pipeline/Particles/Unlit");
            ZombieCardData demoZombie = EnsureDemoZombie(zombie);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GameplayHUD_P0_Integration";

            Camera camera = CreateCamera();
            CreateLights();
            CreateEventSystem();

            GameObject worldRoot = new GameObject("WorldRoot");
            CreateBackdrop(worldRoot.transform, backdrop, unlit, camera);
            CreateWorldGround(worldRoot.transform, ground, lit);

            GameObject hudRoot = new GameObject("GameplayHUD_P0");
            Canvas hudCanvas = hudRoot.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 100;
            CanvasScaler scaler = hudRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            hudRoot.AddComponent<GraphicRaycaster>();

            RectTransform hud = hudRoot.GetComponent<RectTransform>();
            HudReferences hudReferences = BuildHud(hud);
            ApplyPaperUiToHud(hud);
            TMP_Text detailText = hudReferences.Detail;

            CardController cardController = CreateDemoCardController();
            RunPhaseController runPhaseController = CreateDemoRunPhaseController(cardController);
            GameplayHudRuntimePresenter hudPresenter = hudRoot.AddComponent<GameplayHudRuntimePresenter>();
            hudPresenter.Configure(
                runPhaseController,
                cardController,
                hudReferences.Day,
                hudReferences.Phase,
                hudReferences.Morale,
                hudReferences.MoraleThreshold,
                hudReferences.Food,
                hudReferences.Resource,
                hudReferences.Combat,
                hudReferences.Energy,
                hudReferences.DrawPile,
                hudReferences.DiscardPile,
                hudReferences.CardRoots,
                hudReferences.CardNames,
                hudReferences.CardCosts,
                hudReferences.CardTypes);
            ApprovedResourceIconSetupUtility.ApplyHierarchy(hudRoot);

            GameObject buildingRoot = new GameObject("BuildingWorldRoot");
            buildingRoot.transform.SetParent(worldRoot.transform, false);
            BuildingWorldSpriteView intelView = CreateBuilding(buildingRoot.transform, "IntelligenceCenter", intel, lit, new Vector3(-1.55f, 0.2f, 0f), 0.78f, 20, detailText, "情報中心");
            intelView.gameObject.AddComponent<ShadowCaster2D>();
            CreateBuilding(buildingRoot.transform, "Workbench", workbench, lit, new Vector3(1.35f, 0.75f, 0f), 0.54f, 21, detailText, "工作台");
            CreateCampfire(worldRoot.transform, particleUnlit);

            GameObject slotRoot = new GameObject("ZombieThreatSlots_3x4");
            slotRoot.transform.SetParent(worldRoot.transform, false);
            List<Transform> slots = CreateThreatSlots(slotRoot.transform);
            ZombieThreatSpriteView zombieView = CreateZombie(slots[0], zombie, lit, detailText);

            WorldInteractionRouter router = new GameObject("WorldInteractionRouter").AddComponent<WorldInteractionRouter>();
            router.Configure(camera, ~0);

            RectTransform labelRoot = CreateRect("WorldLabelRoot", hud, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            labelRoot.anchorMin = Vector2.zero;
            labelRoot.anchorMax = Vector2.one;
            labelRoot.offsetMin = Vector2.zero;
            labelRoot.offsetMax = Vector2.zero;
            WorldLabelController labelController = labelRoot.gameObject.AddComponent<WorldLabelController>();
            labelController.Configure(camera, labelRoot);
            RectTransform zombieLabel = CreateZombieLabel(labelRoot, out TMP_Text hp, out TMP_Text attack, out TMP_Text kill);
            zombieView.Configure(zombieView.GetComponent<SpriteRenderer>(), zombieView.transform.Find("HoverOutline").GetComponent<SpriteRenderer>(), hp, attack, kill, detailText);

            P0GameplayHudSliceController demo = new GameObject("P0GameplayHudSliceController").AddComponent<P0GameplayHudSliceController>();
            demo.Configure(
                demoZombie,
                cardController,
                runPhaseController,
                router,
                hudReferences.LockState,
                hudReferences.LockButton,
                zombieView,
                labelController,
                zombieLabel);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = hudRoot;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"P0 Gameplay HUD integration scene created: {ScenePath}");
        }

        /// <summary>將已核准紙張套用到常駐 HUD，保留原有數值與互動引用。</summary>
        public static void ApplyPaperUiToHud(Transform hud)
        {
            Sprite paper = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/GameplayHUD/PaperUI/ui_panel_paper_dark_9slice_01.png");
            if (paper == null) throw new System.InvalidOperationException("Paper UI dark panel is missing.");
            string[] names = { "TimePanel", "MoralePanel", "ResourceBar", "ShortcutRail", "DetailPanel", "Notification" };
            foreach (string name in names)
            {
                Transform panel = hud.Find(name);
                if (panel == null) continue;
                ApplyPaperSurface(panel, paper);
                if (name == "ShortcutRail")
                {
                    foreach (Transform child in panel)
                        if (child.name.StartsWith("Shortcut_")) ApplyPaperSurface(child, paper);
                }
            }
            LayoutPermanentHud(hud);
        }

        private static void LayoutPermanentHud(Transform hud)
        {
            var morale = hud.Find("MoralePanel/Morale").GetComponent<TMP_Text>();
            morale.rectTransform.anchoredPosition = new Vector2(20f, -22f);
            morale.rectTransform.sizeDelta = new Vector2(390f, 70f);
            morale.fontSize = 40f;
            morale.enableWordWrapping = false;
            morale.enableAutoSizing = true;
            morale.fontSizeMin = 28f;
            morale.fontSizeMax = 40f;
            var pyramid = hud.Find("MoralePanel/Pyramid") ?? hud.Find("TimePanel/Pyramid");
            pyramid.SetParent(hud.Find("TimePanel"), false);
            var pyramidRect = (RectTransform)pyramid;
            pyramidRect.anchoredPosition = new Vector2(265f, -8f);
            pyramidRect.sizeDelta = new Vector2(65f, 65f);
            pyramid.GetComponent<TMP_Text>().fontSize = 18f;
            hud.Find("TimePanel/Day").GetComponent<RectTransform>().sizeDelta = new Vector2(235f, 42f);
            var resources = hud.Find("ResourceBar").GetComponent<RectTransform>();
            resources.sizeDelta = new Vector2(660f, 105f);
            for (int i = 0; i < 6; i++)
            {
                var item = resources.Find("Resource_" + i).GetComponent<TMP_Text>();
                item.rectTransform.anchoredPosition = new Vector2(12f + i * 106f, -28f);
                item.rectTransform.sizeDelta = new Vector2(104f, 48f);
                item.fontSize = 20f;
            }
        }

        public static void RepairP0PhaseReferences(Transform hud)
        {
            var cards = Object.FindObjectOfType<CardController>();
            var phase = Object.FindObjectOfType<RunPhaseController>();
            if (cards == null) throw new System.InvalidOperationException("P0 CardController missing.");
            if (phase == null) phase = CreateDemoRunPhaseController(cards);
            var targets = new Component[] { hud.GetComponent<GameplayHudRuntimePresenter>(),
                Object.FindObjectOfType<P0GameplayHudSliceController>() };
            foreach (var target in targets)
            {
                if (target == null) throw new System.InvalidOperationException("P0 HUD controller missing.");
                var serialized = new SerializedObject(target);
                serialized.FindProperty("runPhaseController").objectReferenceValue = phase;
                serialized.ApplyModifiedProperties();
            }
        }

        private static void ApplyPaperSurface(Transform panel, Sprite paper)
        {
            Image image = panel.GetComponent<Image>();
            if (image == null) return;
            Undo.RecordObject(image, "Apply Paper UI surface");
            image.sprite = paper;
            image.type = Image.Type.Sliced;
            // 保持邊緣細小，避免小型 HUD 被九宮格邊界擠滿。
            image.pixelsPerUnitMultiplier = 4f;
            image.color = Color.white;
            Transform oldTexture = panel.Find("TornPaperTexture");
            if (oldTexture != null)
            {
                Undo.RecordObject(oldTexture.gameObject, "Disable previous paper texture");
                oldTexture.gameObject.SetActive(false);
            }
        }

        private static Camera CreateCamera()
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(18, 18, 17, 255);
            go.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static void CreateLights()
        {
            Light2D global = new GameObject("Global Light 2D").AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            global.intensity = 0.72f;
            global.color = new Color32(226, 222, 207, 255);

            Light2D camp = new GameObject("Campfire Light 2D").AddComponent<Light2D>();
            camp.lightType = Light2D.LightType.Point;
            camp.intensity = 0.9f;
            camp.pointLightInnerRadius = 0.4f;
            camp.pointLightOuterRadius = 2.3f;
            camp.color = new Color32(255, 176, 80, 255);
            camp.transform.position = new Vector3(0f, -1.35f, -0.5f);
        }

        private static void CreateEventSystem()
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static void CreateBackdrop(Transform parent, Sprite sprite, Material material, Camera camera)
        {
            GameObject go = new GameObject("Backdrop_Parchment");
            go.transform.SetParent(parent, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = -100;
            if (sprite != null)
            {
                float targetHeight = camera.orthographicSize * 2f;
                float targetWidth = targetHeight * (16f / 9f);
                Vector2 size = sprite.bounds.size;
                go.transform.localScale = new Vector3(targetWidth / size.x, targetHeight / size.y, 1f);
            }
        }

        private static void CreateWorldGround(Transform parent, Sprite sprite, Material material)
        {
            GameObject go = new GameObject("Ground_Charcoal");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = -60;
            go.transform.localScale = Vector3.one * 1.35f;
        }

        private static BuildingWorldSpriteView CreateBuilding(Transform parent, string name, Sprite sprite, Material material, Vector3 position, float scale, int order, TMP_Text detail, string displayName)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * scale;
            SpriteRenderer body = go.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.sharedMaterial = material;
            body.sortingOrder = order;
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size *= 0.68f;

            GameObject outlineGo = new GameObject("HoverOutline");
            outlineGo.transform.SetParent(go.transform, false);
            outlineGo.transform.localScale = Vector3.one * 1.05f;
            SpriteRenderer outline = outlineGo.AddComponent<SpriteRenderer>();
            outline.sprite = sprite;
            outline.sharedMaterial = material;
            outline.color = new Color32(221, 168, 57, 190);
            outline.sortingOrder = order - 1;

            BuildingWorldSpriteView view = go.AddComponent<BuildingWorldSpriteView>();
            view.Configure(body, outline, detail, displayName);
            return view;
        }

        private static List<Transform> CreateThreatSlots(Transform parent)
        {
            List<Transform> slots = new List<Transform>(12);
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    GameObject slot = new GameObject($"ThreatSlot_{row * 3 + column + 1:00}");
                    slot.transform.SetParent(parent, false);
                    slot.transform.localPosition = new Vector3(5.8f + column * 1.15f, 2.15f - row * 1.45f, 0f);
                    slots.Add(slot.transform);
                }
            }
            return slots;
        }

        private static ZombieThreatSpriteView CreateZombie(Transform parent, Sprite sprite, Material material, TMP_Text detail)
        {
            GameObject go = new GameObject("WanderingZombie_P0");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.48f;
            SpriteRenderer body = go.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.sharedMaterial = material;
            body.sortingOrder = 40;
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size *= 0.7f;

            GameObject outlineGo = new GameObject("HoverOutline");
            outlineGo.transform.SetParent(go.transform, false);
            outlineGo.transform.localScale = Vector3.one * 1.06f;
            SpriteRenderer outline = outlineGo.AddComponent<SpriteRenderer>();
            outline.sprite = sprite;
            outline.sharedMaterial = material;
            outline.color = new Color32(193, 62, 51, 200);
            outline.sortingOrder = 39;

            return go.AddComponent<ZombieThreatSpriteView>();
        }

        private static void CreateCampfire(Transform parent, Material material)
        {
            GameObject fire = new GameObject("Campfire_FX_P0");
            fire.transform.SetParent(parent, false);
            fire.transform.localPosition = new Vector3(0f, -1.35f, 0f);
            ParticleSystem particles = fire.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 0.45f;
            main.startSize = 0.09f;
            main.startColor = new ParticleSystem.MinMaxGradient(Amber, Red);
            main.maxParticles = 28;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 9f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;
            ParticleSystemRenderer renderer = fire.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 35;
            renderer.sharedMaterial = material;
        }

        private static CardController CreateDemoCardController()
        {
            GameObject go = new GameObject("CardController");
            CardController controller = go.AddComponent<CardController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("deckLayout").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DeckLayoutDefinition>("Assets/ScriptObj/DefaultDeckLayout.asset");
            serialized.FindProperty("drawCountPerTurn").intValue = 5;
            serialized.FindProperty("maxEnergy").intValue = 5;
            serialized.FindProperty("startingFood").intValue = 23;
            serialized.FindProperty("startingResource").intValue = 17;
            serialized.FindProperty("startDemoOnAwake").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static RunPhaseController CreateDemoRunPhaseController(CardController cardController)
        {
            GameObject go = new GameObject("RunPhaseController");
            RunPhaseController controller = go.AddComponent<RunPhaseController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("cardController").objectReferenceValue = cardController;
            serialized.FindProperty("defaultMoraleProfile").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<MoralePyramidProfileData>(
                    "Assets/ScriptObj/Morale/MORALE-PYRAMID-DEMO-01_StandardSurvival.asset");
            serialized.FindProperty("openExhaustionReplacementBeforeRound").boolValue = false;
            serialized.FindProperty("startRunOnAwake").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static HudReferences BuildHud(RectTransform root)
        {
            HudReferences references = new HudReferences();
            RectTransform time = CreatePanel("TimePanel", root, new Vector2(24f, -22f), new Vector2(355f, 136f), new Vector2(0f, 1f), new Vector2(0f, 1f), Paper);
            references.Day = CreateText("Day", time, "第 1 天", 34, Charcoal, new Vector2(20f, -16f), new Vector2(300f, 42f), TextAlignmentOptions.Left);
            references.Phase = CreateText("Phase", time, "【白天】 → 下午 → 黑夜", 25, Charcoal, new Vector2(20f, -70f), new Vector2(310f, 40f), TextAlignmentOptions.Left);

            RectTransform morale = CreatePanel("MoralePanel", root, new Vector2(0f, -18f), new Vector2(430f, 172f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Paper);
            references.Morale = CreateText("Morale", morale, "士氣  62/100", 50, Green, new Vector2(0f, -22f), new Vector2(270f, 60f), TextAlignmentOptions.Center);
            CreateText("Pyramid", morale, "△\n▱\n▰", 30, Charcoal, new Vector2(145f, -10f), new Vector2(110f, 145f), TextAlignmentOptions.Center);
            references.MoraleThreshold = CreateText("Threshold", morale, "下一階段 < 35", 23, Red, new Vector2(38f, -112f), new Vector2(205f, 35f), TextAlignmentOptions.Center);

            RectTransform resources = CreatePanel("ResourceBar", root, new Vector2(-24f, -22f), new Vector2(850f, 105f), new Vector2(1f, 1f), new Vector2(1f, 1f), Paper);
            string[] values = { "食物 23/30", "資源 17", "戰鬥", "醫療 8", "彈藥 6", "人口 7/12" };
            for (int i = 0; i < values.Length; i++)
            {
                TMP_Text item = CreateText($"Resource_{i}", resources, values[i], 22, i == 2 ? Amber : Charcoal, new Vector2(18f + i * 136f, -28f), new Vector2(130f, 48f), TextAlignmentOptions.Center);
                if (i == 0) references.Food = item;
                if (i == 1) references.Resource = item;
                if (i == 2) references.Combat = item;
            }

            RectTransform rail = CreatePanel("ShortcutRail", root, new Vector2(22f, 0f), new Vector2(82f, 530f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Color32(31, 30, 28, 245));
            string[] shortcuts = { "基", "工", "食", "戰", "冊", "設" };
            for (int i = 0; i < shortcuts.Length; i++)
            {
                RectTransform button = CreatePanel($"Shortcut_{i}", rail, new Vector2(8f, -10f - i * 84f), new Vector2(66f, 66f), new Vector2(0f, 1f), new Vector2(0f, 1f), PaperDark);
                CreateText("Icon", button, shortcuts[i], 30, Charcoal, Vector2.zero, new Vector2(60f, 60f), TextAlignmentOptions.Center);
            }

            RectTransform hand = CreateRect("HandRoot", root, Vector2.zero, new Vector2(980f, 295f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            string[] cardNames = { "搜索物資", "種植箱", "加固柵欄", "短暫休息", "鐵鏟" };
            for (int i = 0; i < 5; i++)
            {
                RectTransform card = CreatePanel($"Card_{i + 1}", hand, new Vector2(-390f + i * 195f, 5f + Mathf.Abs(2 - i) * -13f), new Vector2(180f, 245f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Paper);
                card.localRotation = Quaternion.Euler(0f, 0f, (2 - i) * 2.5f);
                TMP_Text cost = CreateText("Cost", card, (i % 3 + 1).ToString(), 28, Red, new Vector2(12f, -10f), new Vector2(42f, 42f), TextAlignmentOptions.Center);
                TMP_Text type = CreateText("Art", card, i % 2 == 0 ? "▓▓▓" : "▒▒▒", 34, Charcoal, new Vector2(20f, -55f), new Vector2(140f, 90f), TextAlignmentOptions.Center);
                TMP_Text cardName = CreateText("Name", card, cardNames[i], 22, Charcoal, new Vector2(10f, -158f), new Vector2(160f, 55f), TextAlignmentOptions.Center);
                references.CardRoots.Add(card);
                references.CardCosts.Add(cost);
                references.CardTypes.Add(type);
                references.CardNames.Add(cardName);
            }

            RectTransform energy = CreatePanel("EnergyAndDraw", root, new Vector2(120f, 20f), new Vector2(280f, 140f), new Vector2(0f, 0f), new Vector2(0f, 0f), Paper);
            references.Energy = CreateText("Energy", energy, "能量 3/5", 28, Amber, new Vector2(15f, -16f), new Vector2(190f, 42f), TextAlignmentOptions.Left);
            references.DrawPile = CreateText("Draw", energy, "抽牌堆 10", 22, Charcoal, new Vector2(15f, -72f), new Vector2(190f, 36f), TextAlignmentOptions.Left);

            RectTransform discard = CreatePanel("DiscardAndEnd", root, new Vector2(-22f, 20f), new Vector2(255f, 145f), new Vector2(1f, 0f), new Vector2(1f, 0f), Paper);
            references.DiscardPile = CreateText("Discard", discard, "棄牌堆 0", 23, Charcoal, new Vector2(10f, -15f), new Vector2(220f, 38f), TextAlignmentOptions.Center);
            CreateText("End", discard, "結束回合", 28, Red, new Vector2(10f, -72f), new Vector2(220f, 48f), TextAlignmentOptions.Center);

            RectTransform detail = CreatePanel("DetailPanel", root, new Vector2(-28f, 20f), new Vector2(390f, 245f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Paper);
            references.Detail = CreateText("DetailText", detail, "詳細資訊\n右鍵世界物件可查看\n左鍵殭屍血量可配置擊殺", 22, Charcoal, new Vector2(22f, -20f), new Vector2(346f, 205f), TextAlignmentOptions.TopLeft);

            RectTransform notice = CreatePanel("Notification", root, new Vector2(-32f, -300f), new Vector2(360f, 82f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Color32(76, 57, 48, 248));
            CreateText("Text", notice, "下午：食物預計短缺", 23, Red, new Vector2(14f, -17f), new Vector2(330f, 45f), TextAlignmentOptions.Center);

            RectTransform lockPanel = CreatePanel("InputLockTester", root, new Vector2(120f, 170f), new Vector2(430f, 92f), new Vector2(0f, 0f), new Vector2(0f, 0f), Paper);
            references.LockState = CreateText("State", lockPanel, "世界操作：可用", 20, Green, new Vector2(12f, -10f), new Vector2(250f, 32f), TextAlignmentOptions.Left);
            references.LockButton = CreateButton("Toggle", lockPanel, "切換事件鎖定", new Vector2(258f, -18f), new Vector2(160f, 55f));
            return references;
        }

        private static RectTransform CreateZombieLabel(RectTransform parent, out TMP_Text hp, out TMP_Text attack, out TMP_Text kill)
        {
            RectTransform label = CreatePanel("ZombieWorldLabel", parent, Vector2.zero, new Vector2(210f, 72f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Paper);
            Image image = label.GetComponent<Image>();
            image.raycastTarget = false;
            hp = CreateText("Health", label, "HP 4", 24, Red, new Vector2(8f, -5f), new Vector2(92f, 34f), TextAlignmentOptions.Center);
            attack = CreateText("Attack", label, "攻 2", 24, Charcoal, new Vector2(108f, -5f), new Vector2(92f, 34f), TextAlignmentOptions.Center);
            kill = CreateText("KillMark", label, string.Empty, 20, Green, new Vector2(15f, -39f), new Vector2(180f, 28f), TextAlignmentOptions.Center);
            hp.raycastTarget = false;
            attack.raycastTarget = false;
            kill.raycastTarget = false;
            return label;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchor, Vector2 pivot, Color color)
        {
            RectTransform rect = CreateRect(name, parent, anchoredPosition, size, anchor, pivot);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = color;

            if (panelSprite != null)
            {
                RectTransform texture = CreateRect("TornPaperTexture", rect, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                texture.anchorMin = Vector2.zero;
                texture.anchorMax = Vector2.one;
                texture.offsetMin = new Vector2(-5f, -5f);
                texture.offsetMax = new Vector2(5f, 5f);

                Image textureImage = texture.gameObject.AddComponent<Image>();
                textureImage.sprite = panelSprite;
                textureImage.type = Image.Type.Sliced;
                textureImage.color = new Color32(205, 198, 184, 78);
                textureImage.raycastTarget = false;
            }
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchor, Vector2 pivot)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, Vector2 anchoredPosition, Vector2 dimensions, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent, anchoredPosition, dimensions, new Vector2(0f, 1f), new Vector2(0f, 1f));
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform rect = CreatePanel(name, parent, anchoredPosition, size, new Vector2(0f, 1f), new Vector2(0f, 1f), PaperDark);
            Button button = rect.gameObject.AddComponent<Button>();
            CreateText("Label", rect, label, 19, Charcoal, Vector2.zero, size, TextAlignmentOptions.Center).rectTransform.pivot = new Vector2(0f, 1f);
            return button;
        }

        private static Sprite ImportSprite(string path, float pixelsPerUnit)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite ImportPanelSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = new Vector4(190f, 150f, 190f, 150f);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Material EnsureMaterial(string path, string shaderName)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName);
            if (material == null)
            {
                material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        private static ZombieCardData EnsureDemoZombie(Sprite artwork)
        {
            ZombieCardData data = AssetDatabase.LoadAssetAtPath<ZombieCardData>(DemoZombiePath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ZombieCardData>();
                AssetDatabase.CreateAsset(data, DemoZombiePath);
            }

            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("cardId").stringValue = "ZOMBIE-WANDERER-P0";
            serialized.FindProperty("displayName").stringValue = "流浪殭屍";
            serialized.FindProperty("description").stringValue = "若輪末仍存活，造成士氣損失。";
            serialized.FindProperty("artwork").objectReferenceValue = artwork;
            serialized.FindProperty("health").intValue = 4;
            serialized.FindProperty("attack").intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }
    }
}
#endif
