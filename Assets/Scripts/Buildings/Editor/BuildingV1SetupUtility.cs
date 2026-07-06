#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Buildings.UI;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Cards.UI.Replacement;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Run;

namespace ZombieCardSurvive.Buildings.Editor
{
    public static class BuildingV1SetupUtility
    {
        private const string DataFolder = "Assets/ScriptObj/BuildingScriptObj";
        private const string PrefabFolder = "Assets/Prefab/Buildings";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Zombie Card Survive/Setup Building V1")]
        public static void Setup()
        {
            EnsureFolder("Assets/ScriptObj", "BuildingScriptObj");
            EnsureFolder("Assets/Prefab", "Buildings");

            Sprite icon = LoadSprite("Assets/ArtDemos/Icons/transparent/icon_resource_parts.png");
            ExplorationCardData explorationCard = FindExplorationCard();
            ConstructionCardData constructionCard = CreateConstructionCard(icon);
            CardCraftRecipeData recipe = CreateRecipe(explorationCard, icon);
            CardCraftInteractionData craftLine = CreateCraftLine(recipe);
            BuildBaseData workbench = CreateWorkbench(icon, constructionCard, craftLine);

            Button recipeButton = CreateButtonPrefab();
            CraftJobRowView jobRow = CreateJobRowPrefab();
            CardCraftView craftView = CreateCraftViewPrefab(recipeButton, jobRow);
            BuildingWorldView worldView = CreateWorldViewPrefab();
            BuildingDetailView detailView = CreateDetailViewPrefab(craftView);

            WireScene(workbench, worldView, detailView);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Building V1 setup complete. Workbench assets, prefabs, and SampleScene wiring are ready.");
        }

        private static ConstructionCardData CreateConstructionCard(Sprite icon)
        {
            string path = $"{DataFolder}/WorkbenchConstructionCard.asset";
            ConstructionCardData asset = LoadOrCreate<ConstructionCardData>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "displayName", "建造工作台");
            SetInt(serialized, "energyCost", 1);
            SetObject(serialized, "artwork", icon);
            SetString(serialized, "description", "投入施工，推進工作台建造進度。");
            SetInt(serialized, "cardType", (int)CardType.Build);
            SetInt(serialized, "playMode", (int)CardPlayMode.Playable);
            SerializedProperty slots = serialized.FindProperty("allowedSlots");
            if (slots != null)
            {
                slots.arraySize = 1;
                slots.GetArrayElementAtIndex(0).enumValueIndex = (int)DeckSlotType.Unrestricted;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            asset.EnsureId();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CardCraftRecipeData CreateRecipe(ExplorationCardData explorationCard, Sprite icon)
        {
            string path = $"{DataFolder}/BasicExplorationCardRecipe.asset";
            CardCraftRecipeData asset = LoadOrCreate<CardCraftRecipeData>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "displayName", "製作基礎探索卡");
            SetObject(serialized, "artwork", explorationCard != null ? explorationCard.Artwork : icon);
            SetObject(serialized, "outputCard", explorationCard);
            SetInt(serialized, "outputCount", 1);
            SetInt(serialized, "requiredProduction", 4);
            SerializedProperty cost = serialized.FindProperty("cost");
            cost?.FindPropertyRelative("food")?.SetValue(2);
            cost?.FindPropertyRelative("resource")?.SetValue(0);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            asset.EnsureId();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CardCraftInteractionData CreateCraftLine(CardCraftRecipeData recipe)
        {
            string path = $"{DataFolder}/WorkbenchCardCraftLine.asset";
            CardCraftInteractionData asset = LoadOrCreate<CardCraftInteractionData>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "displayName", "卡牌製作列");
            SetInt(serialized, "requiredBuildingLevel", 1);
            SetInt(serialized, "baseProduction", 2);
            SetInt(serialized, "queueCapacity", 3);
            SetObjectArray(serialized, "recipes", recipe);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            asset.EnsureId();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static BuildBaseData CreateWorkbench(Sprite icon, ConstructionCardData constructionCard, CardCraftInteractionData line)
        {
            string path = $"{DataFolder}/Workbench.asset";
            BuildBaseData asset = LoadOrCreate<BuildBaseData>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "displayName", "工作台");
            SetObject(serialized, "artwork", icon);
            SetColor(serialized, "color", new Color(0.32f, 0.45f, 0.42f, 1f));
            SetVector2(serialized, "anchoredPosition", new Vector2(0f, -40f));
            SetBool(serialized, "startsUnlocked", true);
            SetObjectArray(serialized, "interactions", line);

            SerializedProperty levels = serialized.FindProperty("levels");
            levels.arraySize = 1;
            SerializedProperty level = levels.GetArrayElementAtIndex(0);
            level.FindPropertyRelative("level").intValue = 1;
            level.FindPropertyRelative("artwork").objectReferenceValue = icon;
            level.FindPropertyRelative("color").colorValue = new Color(0.36f, 0.52f, 0.46f, 1f);
            level.FindPropertyRelative("rewardDescription").stringValue = "解鎖一條卡牌製作列，製造力 2。";
            level.FindPropertyRelative("constructionCost").FindPropertyRelative("food").intValue = 0;
            level.FindPropertyRelative("constructionCost").FindPropertyRelative("resource").intValue = 3;
            level.FindPropertyRelative("requiredConstructionProgress").intValue = 2;
            level.FindPropertyRelative("progressPerCardPlay").intValue = 1;
            level.FindPropertyRelative("constructionCard").objectReferenceValue = constructionCard;
            level.FindPropertyRelative("constructionCardCount").intValue = 1;
            level.FindPropertyRelative("freeIntegrationCount").intValue = 1;
            level.FindPropertyRelative("manufacturingBonus").intValue = 0;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            asset.EnsureId();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static BuildingWorldView CreateWorldViewPrefab()
        {
            string path = $"{PrefabFolder}/BuildingWorldView.prefab";
            BuildingWorldView existing = AssetDatabase.LoadAssetAtPath<BuildingWorldView>(path);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = CreateUiObject("BuildingWorldView", new Vector2(190f, 150f));
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.12f, 0.14f, 0.14f, 0.94f);
            Button button = root.AddComponent<Button>();
            Image artwork = CreateImage(root.transform, "Artwork", new Vector2(72f, 72f));
            TMP_Text name = CreateText(root.transform, "Name", "建築", 20f, TextAlignmentOptions.Center);
            TMP_Text status = CreateText(root.transform, "Status", "空地", 15f, TextAlignmentOptions.Center);
            SetAnchored(artwork.rectTransform, new Vector2(0f, 20f), new Vector2(72f, 72f));
            SetAnchored(name.rectTransform, new Vector2(0f, -34f), new Vector2(180f, 26f));
            SetAnchored(status.rectTransform, new Vector2(0f, -60f), new Vector2(180f, 22f));

            BuildingWorldView view = root.AddComponent<BuildingWorldView>();
            SetSerializedReferences(view,
                ("rectTransform", root.GetComponent<RectTransform>()),
                ("button", button),
                ("artworkImage", artwork),
                ("nameText", name),
                ("statusText", status));
            return SavePrefab(root, path).GetComponent<BuildingWorldView>();
        }

        private static Button CreateButtonPrefab()
        {
            string path = $"{PrefabFolder}/CraftRecipeButton.prefab";
            Button existing = AssetDatabase.LoadAssetAtPath<Button>(path);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = CreateUiObject("CraftRecipeButton", new Vector2(540f, 44f));
            Image image = root.AddComponent<Image>();
            image.color = new Color(0.24f, 0.32f, 0.31f, 1f);
            Button button = root.AddComponent<Button>();
            TMP_Text label = CreateText(root.transform, "Label", "配方", 16f, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 8f);
            SavePrefab(root, path);
            return AssetDatabase.LoadAssetAtPath<Button>(path);
        }

        private static CraftJobRowView CreateJobRowPrefab()
        {
            string path = $"{PrefabFolder}/CraftJobRow.prefab";
            CraftJobRowView existing = AssetDatabase.LoadAssetAtPath<CraftJobRowView>(path);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = CreateUiObject("CraftJobRow", new Vector2(540f, 38f));
            Image image = root.AddComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.18f, 0.8f);
            TMP_Text label = CreateText(root.transform, "Description", "製作工作", 15f, TextAlignmentOptions.MidlineLeft);
            SetAnchored(label.rectTransform, new Vector2(-52f, 0f), new Vector2(420f, 34f));
            Button cancel = CreateButton(root.transform, "Cancel", "取消", new Vector2(82f, 30f));
            SetAnchored(cancel.GetComponent<RectTransform>(), new Vector2(222f, 0f), new Vector2(82f, 30f));
            CraftJobRowView row = root.AddComponent<CraftJobRowView>();
            SetSerializedReferences(row, ("descriptionText", label), ("cancelButton", cancel));
            return SavePrefab(root, path).GetComponent<CraftJobRowView>();
        }

        private static CardCraftView CreateCraftViewPrefab(Button recipeButton, CraftJobRowView jobRow)
        {
            string path = $"{PrefabFolder}/CardCraftView.prefab";
            CardCraftView existing = AssetDatabase.LoadAssetAtPath<CardCraftView>(path);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = CreateUiObject("CardCraftView", new Vector2(580f, 280f));
            Image image = root.AddComponent<Image>();
            image.color = new Color(0.11f, 0.13f, 0.13f, 0.92f);
            TMP_Text title = CreateText(root.transform, "Title", "卡牌製作列", 20f, TextAlignmentOptions.MidlineLeft);
            TMP_Text production = CreateText(root.transform, "Production", "製造力", 15f, TextAlignmentOptions.MidlineRight);
            SetAnchored(title.rectTransform, new Vector2(-130f, 116f), new Vector2(280f, 30f));
            SetAnchored(production.rectTransform, new Vector2(140f, 116f), new Vector2(250f, 30f));
            RectTransform recipes = CreateVerticalRoot(root.transform, "Recipes", new Vector2(0f, 55f), new Vector2(550f, 92f));
            RectTransform queue = CreateVerticalRoot(root.transform, "Queue", new Vector2(0f, -66f), new Vector2(550f, 132f));
            CardCraftView view = root.AddComponent<CardCraftView>();
            SetSerializedReferences(view,
                ("titleText", title),
                ("productionText", production),
                ("recipeRoot", recipes),
                ("recipeButtonPrefab", recipeButton),
                ("queueRoot", queue),
                ("jobRowPrefab", jobRow));
            return SavePrefab(root, path).GetComponent<CardCraftView>();
        }

        private static BuildingDetailView CreateDetailViewPrefab(CardCraftView craftView)
        {
            string path = $"{PrefabFolder}/BuildingDetailView.prefab";
            BuildingDetailView existing = AssetDatabase.LoadAssetAtPath<BuildingDetailView>(path);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = CreateUiObject("BuildingDetailView", new Vector2(680f, 720f));
            Image image = root.AddComponent<Image>();
            image.color = new Color(0.07f, 0.08f, 0.08f, 0.98f);
            TMP_Text title = CreateText(root.transform, "Title", "工作台", 30f, TextAlignmentOptions.Center);
            TMP_Text status = CreateText(root.transform, "Status", "狀態", 18f, TextAlignmentOptions.MidlineLeft);
            TMP_Text requirement = CreateText(root.transform, "Requirement", "需求", 16f, TextAlignmentOptions.MidlineLeft);
            TMP_Text reward = CreateText(root.transform, "Reward", "獎勵", 16f, TextAlignmentOptions.MidlineLeft);
            SetAnchored(title.rectTransform, new Vector2(0f, 320f), new Vector2(560f, 42f));
            SetAnchored(status.rectTransform, new Vector2(-80f, 276f), new Vector2(440f, 28f));
            SetAnchored(requirement.rectTransform, new Vector2(-40f, 242f), new Vector2(520f, 28f));
            SetAnchored(reward.rectTransform, new Vector2(-40f, 208f), new Vector2(520f, 28f));
            Button construct = CreateButton(root.transform, "Construct", "開始施工／升級", new Vector2(180f, 40f));
            Button cancel = CreateButton(root.transform, "CancelConstruction", "取消施工", new Vector2(130f, 40f));
            Button close = CreateButton(root.transform, "Close", "關閉", new Vector2(92f, 40f));
            SetAnchored(construct.GetComponent<RectTransform>(), new Vector2(-180f, 160f), new Vector2(180f, 40f));
            SetAnchored(cancel.GetComponent<RectTransform>(), new Vector2(0f, 160f), new Vector2(130f, 40f));
            SetAnchored(close.GetComponent<RectTransform>(), new Vector2(240f, 320f), new Vector2(92f, 40f));
            RectTransform interactions = CreateVerticalRoot(root.transform, "Interactions", new Vector2(0f, -90f), new Vector2(610f, 450f));
            BuildingDetailView view = root.AddComponent<BuildingDetailView>();
            SetSerializedReferences(view,
                ("root", root),
                ("titleText", title),
                ("statusText", status),
                ("requirementText", requirement),
                ("rewardText", reward),
                ("constructButton", construct),
                ("cancelConstructionButton", cancel),
                ("closeButton", close),
                ("interactionRoot", interactions),
                ("cardCraftPrefab", craftView));
            return SavePrefab(root, path).GetComponent<BuildingDetailView>();
        }

        private static void WireScene(BuildBaseData workbench, BuildingWorldView worldPrefab, BuildingDetailView detailPrefab)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Building V1 setup could not find a Canvas in SampleScene.");
                return;
            }

            GameObject root = GameObject.Find("BuildingV1Root");
            if (root == null)
            {
                root = CreateUiObject("BuildingV1Root", Vector2.zero);
                root.transform.SetParent(canvas.transform, false);
                Stretch(root.GetComponent<RectTransform>(), 0f);
            }

            RectTransform worldRoot = FindOrCreateChildRoot(root.transform, "BuildingWorldRoot");
            BuildingSystem system = root.GetComponent<BuildingSystem>() ?? root.AddComponent<BuildingSystem>();
            BuildingDetailView detail = root.GetComponentInChildren<BuildingDetailView>(true);
            if (detail == null)
            {
                GameObject detailObject = PrefabUtility.InstantiatePrefab(detailPrefab.gameObject, root.transform) as GameObject;
                detail = detailObject != null ? detailObject.GetComponent<BuildingDetailView>() : null;
                if (detail != null)
                {
                    SetAnchored(detail.transform as RectTransform, Vector2.zero, new Vector2(680f, 720f));
                }
            }

            CardInventory inventory = Object.FindObjectOfType<CardInventory>();
            CardController cardController = Object.FindObjectOfType<CardController>();
            DeckReplacementView replacement = Object.FindObjectOfType<DeckReplacementView>(true);
            EventFlowController eventFlow = Object.FindObjectOfType<EventFlowController>();
            RunPhaseController runPhase = ResolvePrimaryRunPhase(eventFlow);

            SetSerializedReferences(system,
                ("buildingRoot", worldRoot),
                ("buildingPrefab", worldPrefab),
                ("detailView", detail),
                ("inventory", inventory),
                ("cardController", cardController),
                ("replacementView", replacement),
                ("runPhaseController", runPhase));
            SerializedObject systemSerialized = new SerializedObject(system);
            SetObjectArray(systemSerialized, "buildingCatalog", workbench);
            systemSerialized.ApplyModifiedPropertiesWithoutUndo();

            if (runPhase != null)
            {
                SetSerializedReferences(runPhase, ("buildingSystem", system));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static RunPhaseController ResolvePrimaryRunPhase(EventFlowController eventFlow)
        {
            RunPhaseController[] controllers = Object.FindObjectsOfType<RunPhaseController>(true);
            RunPhaseController primary = null;
            if (eventFlow != null)
            {
                SerializedObject serialized = new SerializedObject(eventFlow);
                primary = serialized.FindProperty("runPhaseController")?.objectReferenceValue as RunPhaseController;
            }

            primary ??= controllers.FirstOrDefault();
            foreach (RunPhaseController controller in controllers)
            {
                if (controller != null && controller != primary)
                {
                    Object.DestroyImmediate(controller);
                }
            }

            return primary;
        }

        private static ExplorationCardData FindExplorationCard()
        {
            string[] guids = AssetDatabase.FindAssets("t:ExplorationCardData");
            foreach (string guid in guids)
            {
                ExplorationCardData card = AssetDatabase.LoadAssetAtPath<ExplorationCardData>(AssetDatabase.GUIDToAssetPath(guid));
                if (card != null && (card.name.Contains("基礎") || card.name.Contains("Basic")))
                {
                    return card;
                }
            }

            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<ExplorationCardData>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static GameObject CreateUiObject(string name, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.GetComponent<RectTransform>().sizeDelta = size;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment)
        {
            GameObject go = CreateUiObject(name, Vector2.zero);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.93f, 0.94f, 0.91f, 1f);
            text.enableWordWrapping = true;
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 size)
        {
            GameObject go = CreateUiObject(name, size);
            go.transform.SetParent(parent, false);
            return go.AddComponent<Image>();
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 size)
        {
            GameObject go = CreateUiObject(name, size);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.27f, 0.38f, 0.35f, 1f);
            Button button = go.AddComponent<Button>();
            TMP_Text text = CreateText(go.transform, "Label", label, 15f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 4f);
            return button;
        }

        private static RectTransform CreateVerticalRoot(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = CreateUiObject(name, size);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        private static RectTransform FindOrCreateChildRoot(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing as RectTransform;
            }

            GameObject go = CreateUiObject(name, Vector2.zero);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            Stretch(rect, 0f);
            return rect;
        }

        private static GameObject SavePrefab(GameObject source, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            return prefab;
        }

        private static void SetSerializedReferences(Object target, params (string name, Object value)[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            foreach ((string name, Object value) in values)
            {
                SerializedProperty property = serialized.FindProperty(name);
                if (property != null)
                {
                    property.objectReferenceValue = value;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(SerializedObject serialized, string name, params Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
            {
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void SetString(SerializedObject serialized, string name, string value) => serialized.FindProperty(name).stringValue = value;
        private static void SetInt(SerializedObject serialized, string name, int value) => serialized.FindProperty(name).intValue = value;
        private static void SetBool(SerializedObject serialized, string name, bool value) => serialized.FindProperty(name).boolValue = value;
        private static void SetObject(SerializedObject serialized, string name, Object value) => serialized.FindProperty(name).objectReferenceValue = value;
        private static void SetColor(SerializedObject serialized, string name, Color value) => serialized.FindProperty(name).colorValue = value;
        private static void SetVector2(SerializedObject serialized, string name, Vector2 value) => serialized.FindProperty(name).vector2Value = value;

        private static void SetAnchored(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void SetValue(this SerializedProperty property, int value)
        {
            if (property != null)
            {
                property.intValue = value;
            }
        }
    }
}
#endif
