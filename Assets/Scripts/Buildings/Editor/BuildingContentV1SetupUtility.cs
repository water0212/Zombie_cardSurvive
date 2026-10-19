#if UNITY_EDITOR
using System.Collections.Generic;
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
using ZombieCardSurvive.Effects.Data;

namespace ZombieCardSurvive.Buildings.Editor
{
    public static class BuildingContentV1SetupUtility
    {
        private const string BuildingFolder = "Assets/ScriptObj/BuildingScriptObj/ContentV1";
        private const string CardFolder = "Assets/ScriptObj/CardScriptObj/ContentV1";
        private const string PrefabFolder = "Assets/Prefab/Buildings";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Zombie Card Survive/Setup Building Content V1")]
        public static void Setup()
        {
            EnsureFolder("Assets/ScriptObj/BuildingScriptObj", "ContentV1");
            EnsureFolder("Assets/ScriptObj/CardScriptObj", "ContentV1");
            EnsureFolder("Assets/Prefab", "Buildings");

            Sprite fallbackIcon = LoadSprite("Assets/ArtDemos/Cards/card_bases/special_base.png");
            Sprite combatIcon = LoadSprite("Assets/ArtDemos/Cards/card_bases/combat_base.png");

            CardBase dispatch = CreateGenericCard(
                $"{CardFolder}/Dispatch.asset",
                "card_dispatch",
                "調度",
                "獲得 1 次事件後卡牌替換機會。",
                fallbackIcon,
                CardType.Special,
                DeckSlotType.Unrestricted,
                true,
                1,
                EffectSpec.CardFlow(CardFlowEffectType.GrantReplacementChance, 1));

            CardBase improvedDispatch = CreateGenericCard(
                $"{CardFolder}/ImprovedDispatch.asset",
                "card_improved_dispatch",
                "改良調度",
                "獲得 2 次事件後卡牌替換機會。",
                fallbackIcon,
                CardType.Special,
                DeckSlotType.Unrestricted,
                true,
                1,
                EffectSpec.CardFlow(CardFlowEffectType.GrantReplacementChance, 2));

            CombatCardData guardShot = CreateCombatCard(
                $"{CardFolder}/GuardShot.asset",
                "card_guard_shot",
                "守備射擊",
                "對殭屍造成 2 點戰鬥值。",
                combatIcon,
                2);

            CombatCardData improvedGuardShot = CreateCombatCard(
                $"{CardFolder}/ImprovedGuardShot.asset",
                "card_improved_guard_shot",
                "強化守備射擊",
                "對殭屍造成 3 點戰鬥值。",
                combatIcon,
                3);

            BuildBaseData storage = CreateStorageBuilding(fallbackIcon);
            BuildBaseData intel = CreateIntelBuilding(fallbackIcon, dispatch, improvedDispatch);
            BuildBaseData fortification = CreateFortificationBuilding(combatIcon, guardShot, improvedGuardShot);

            FoodPlanInteractionView foodPlanPrefab = CreateFoodPlanPrefab();
            CardUpgradeView cardUpgradePrefab = CreateCardUpgradePrefab();
            WireScene(new[] { storage, intel, fortification }, foodPlanPrefab, cardUpgradePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Building Content V1 setup complete: storage, intel center, fortification, cards, and UI references are ready.");
        }

        private static BuildBaseData CreateStorageBuilding(Sprite icon)
        {
            ConstructionCardData construction = CreateConstructionCard(
                $"{BuildingFolder}/StorageConstructionCard.asset",
                "card_construct_storage",
                "建造倉庫",
                icon);

            FoodStorageInteractionData level1Bonus = CreateStorageBonus(
                $"{BuildingFolder}/StorageFoodCapacityLevel1.asset",
                "interaction_storage_food_capacity_1",
                "食物容量 I",
                1,
                3);

            FoodStorageInteractionData level2Bonus = CreateStorageBonus(
                $"{BuildingFolder}/StorageFoodCapacityLevel2.asset",
                "interaction_storage_food_capacity_2",
                "食物容量 II",
                2,
                3);

            BuildBaseData asset = LoadOrCreate<BuildBaseData>($"{BuildingFolder}/Storage.asset");
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "buildingId", "building_storage");
            SetString(serialized, "displayName", "倉庫");
            SetObject(serialized, "artwork", icon);
            SetColor(serialized, "color", new Color(0.34f, 0.31f, 0.22f, 1f));
            SetVector2(serialized, "anchoredPosition", new Vector2(-260f, -210f));
            SetBool(serialized, "startsUnlocked", true);
            SetObjectArray(serialized, "interactions", level1Bonus, level2Bonus);
            SetLevels(serialized, construction,
                new LevelSpec(1, "食物容量 +3。", 0, 3, 2, 1, 1, 1, 0, icon, new Color(0.39f, 0.34f, 0.22f, 1f)),
                new LevelSpec(2, "食物容量再 +3，總計 +6。", 0, 5, 3, 1, 1, 1, 0, icon, new Color(0.46f, 0.39f, 0.24f, 1f)));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static BuildBaseData CreateIntelBuilding(Sprite icon, CardBase dispatch, CardBase improvedDispatch)
        {
            ConstructionCardData construction = CreateConstructionCard(
                $"{BuildingFolder}/IntelConstructionCard.asset",
                "card_construct_intel_center",
                "建造情報中心",
                icon);

            CardCraftRecipeData recipe = CreateRecipe(
                $"{BuildingFolder}/IntelDispatchRecipe.asset",
                "recipe_dispatch",
                "製作調度",
                dispatch,
                icon,
                1,
                0,
                1,
                3);

            CardCraftInteractionData craftLine = CreateCraftLine(
                $"{BuildingFolder}/IntelCardCraftLine.asset",
                "interaction_intel_dispatch_craft",
                "調度製作列",
                1,
                2,
                2,
                recipe);

            FoodPlanInteractionData foodPlan = CreateFoodPlanInteraction(
                $"{BuildingFolder}/IntelFoodPlanInteraction.asset",
                "interaction_intel_food_plan",
                "食物計畫",
                "設定下一次輪末固定食物消耗使用的食物計畫。",
                1);

            CardUpgradeInteractionData upgrade = CreateCardUpgrade(
                $"{BuildingFolder}/IntelDispatchUpgrade.asset",
                "interaction_upgrade_dispatch",
                "調度強化",
                "將調度升級為改良調度。",
                2,
                dispatch,
                improvedDispatch,
                0,
                2);

            BuildBaseData asset = LoadOrCreate<BuildBaseData>($"{BuildingFolder}/IntelCenter.asset");
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "buildingId", "building_intel_center");
            SetString(serialized, "displayName", "情報中心");
            SetObject(serialized, "artwork", icon);
            SetColor(serialized, "color", new Color(0.18f, 0.34f, 0.43f, 1f));
            SetVector2(serialized, "anchoredPosition", new Vector2(0f, -210f));
            SetBool(serialized, "startsUnlocked", true);
            SetObjectArray(serialized, "interactions", craftLine, foodPlan, upgrade);
            SetLevels(serialized, construction,
                new LevelSpec(1, "解鎖調度製作列與食物計畫。", 0, 3, 2, 1, 1, 1, 0, icon, new Color(0.2f, 0.38f, 0.48f, 1f)),
                new LevelSpec(2, "可將調度強化為改良調度。", 0, 5, 3, 1, 1, 1, 0, icon, new Color(0.23f, 0.45f, 0.56f, 1f)));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static BuildBaseData CreateFortificationBuilding(Sprite icon, CardBase guardShot, CardBase improvedGuardShot)
        {
            ConstructionCardData construction = CreateConstructionCard(
                $"{BuildingFolder}/FortificationConstructionCard.asset",
                "card_construct_fortification",
                "建造防禦工事",
                icon);

            CardCraftRecipeData recipe = CreateRecipe(
                $"{BuildingFolder}/FortificationGuardShotRecipe.asset",
                "recipe_guard_shot",
                "製作守備射擊",
                guardShot,
                icon,
                1,
                0,
                1,
                3);

            CardCraftInteractionData craftLine = CreateCraftLine(
                $"{BuildingFolder}/FortificationCardCraftLine.asset",
                "interaction_fortification_guard_shot_craft",
                "戰鬥卡製作列",
                1,
                2,
                2,
                recipe);

            CardUpgradeInteractionData upgrade = CreateCardUpgrade(
                $"{BuildingFolder}/FortificationGuardShotUpgrade.asset",
                "interaction_upgrade_guard_shot",
                "守備射擊強化",
                "將守備射擊升級為強化守備射擊。",
                2,
                guardShot,
                improvedGuardShot,
                0,
                2);

            BuildBaseData asset = LoadOrCreate<BuildBaseData>($"{BuildingFolder}/Fortification.asset");
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "buildingId", "building_fortification");
            SetString(serialized, "displayName", "防禦工事");
            SetObject(serialized, "artwork", icon);
            SetColor(serialized, "color", new Color(0.36f, 0.22f, 0.2f, 1f));
            SetVector2(serialized, "anchoredPosition", new Vector2(260f, -210f));
            SetBool(serialized, "startsUnlocked", true);
            SetObjectArray(serialized, "interactions", craftLine, upgrade);
            SetLevels(serialized, construction,
                new LevelSpec(1, "解鎖守備射擊製作列。", 0, 4, 3, 1, 1, 1, 0, icon, new Color(0.42f, 0.25f, 0.22f, 1f)),
                new LevelSpec(2, "可將守備射擊強化。", 0, 6, 4, 1, 1, 1, 0, icon, new Color(0.52f, 0.29f, 0.24f, 1f)));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CardBase CreateGenericCard(string path, string id, string name, string description, Sprite artwork, CardType cardType, DeckSlotType slot, bool limited, int uses, params EffectSpec[] effects)
        {
            CardBase asset = LoadOrCreate<CardBase>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "cardId", id);
            SetString(serialized, "displayName", name);
            SetInt(serialized, "energyCost", 1);
            SetObject(serialized, "artwork", artwork);
            SetString(serialized, "description", description);
            SetInt(serialized, "cardType", (int)cardType);
            SetInt(serialized, "playMode", (int)CardPlayMode.Playable);
            SetBool(serialized, "hasLimitedUses", limited);
            SetInt(serialized, "maxUsesPerRun", uses);
            SetAllowedSlots(serialized, slot);
            for (int i = 0; i < effects.Length; i++)
            {
                effects[i].Carrier = GameEffectCarrier.Card;
            }

            SetEffects(serialized.FindProperty("effects"), effects);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CombatCardData CreateCombatCard(string path, string id, string name, string description, Sprite artwork, int damage)
        {
            CombatCardData asset = LoadOrCreate<CombatCardData>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "cardId", id);
            SetString(serialized, "displayName", name);
            SetInt(serialized, "energyCost", 1);
            SetObject(serialized, "artwork", artwork);
            SetString(serialized, "description", description);
            SetInt(serialized, "cardType", (int)CardType.Combat);
            SetInt(serialized, "playMode", (int)CardPlayMode.Playable);
            SetAllowedSlots(serialized, DeckSlotType.Unrestricted);
            SetInt(serialized, "damage", damage);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ConstructionCardData CreateConstructionCard(string path, string id, string name, Sprite artwork)
        {
            ConstructionCardData asset = LoadOrCreate<ConstructionCardData>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "cardId", id);
            SetString(serialized, "displayName", name);
            SetInt(serialized, "energyCost", 1);
            SetObject(serialized, "artwork", artwork);
            SetString(serialized, "description", "投入施工，推進對應建築的建造或升級進度。");
            SetInt(serialized, "cardType", (int)CardType.Build);
            SetInt(serialized, "playMode", (int)CardPlayMode.Playable);
            SetAllowedSlots(serialized, DeckSlotType.Unrestricted);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CardCraftRecipeData CreateRecipe(string path, string id, string name, CardBase output, Sprite artwork, int outputCount, int foodCost, int resourceCost, int requiredProduction)
        {
            CardCraftRecipeData asset = LoadOrCreate<CardCraftRecipeData>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "recipeId", id);
            SetString(serialized, "displayName", name);
            SetObject(serialized, "artwork", artwork);
            SetObject(serialized, "outputCard", output);
            SetInt(serialized, "outputCount", outputCount);
            SetInt(serialized, "requiredProduction", requiredProduction);
            SetCost(serialized.FindProperty("cost"), foodCost, resourceCost);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CardCraftInteractionData CreateCraftLine(string path, string id, string name, int requiredLevel, int production, int capacity, params CardCraftRecipeData[] recipes)
        {
            CardCraftInteractionData asset = LoadOrCreate<CardCraftInteractionData>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "interactionId", id);
            SetString(serialized, "displayName", name);
            SetInt(serialized, "requiredBuildingLevel", requiredLevel);
            SetInt(serialized, "baseProduction", production);
            SetInt(serialized, "queueCapacity", capacity);
            SetObjectArray(serialized, "recipes", recipes);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static FoodStorageInteractionData CreateStorageBonus(string path, string id, string name, int requiredLevel, int bonus)
        {
            FoodStorageInteractionData asset = LoadOrCreate<FoodStorageInteractionData>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "interactionId", id);
            SetString(serialized, "displayName", name);
            SetInt(serialized, "requiredBuildingLevel", requiredLevel);
            SetInt(serialized, "foodCapacityBonus", bonus);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static FoodPlanInteractionData CreateFoodPlanInteraction(string path, string id, string name, string description, int requiredLevel)
        {
            FoodPlanInteractionData asset = LoadOrCreate<FoodPlanInteractionData>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "interactionId", id);
            SetString(serialized, "displayName", name);
            SetInt(serialized, "requiredBuildingLevel", requiredLevel);
            SetString(serialized, "description", description);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CardUpgradeInteractionData CreateCardUpgrade(string path, string id, string name, string description, int requiredLevel, CardBase source, CardBase upgraded, int foodCost, int resourceCost)
        {
            CardUpgradeInteractionData asset = LoadOrCreate<CardUpgradeInteractionData>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "interactionId", id);
            SetString(serialized, "displayName", name);
            SetInt(serialized, "requiredBuildingLevel", requiredLevel);
            SetObject(serialized, "sourceCard", source);
            SetObject(serialized, "upgradedCard", upgraded);
            SetString(serialized, "description", description);
            SetCost(serialized.FindProperty("cost"), foodCost, resourceCost);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static FoodPlanInteractionView CreateFoodPlanPrefab()
        {
            string path = $"{PrefabFolder}/FoodPlanInteractionView.prefab";
            FoodPlanInteractionView existing = AssetDatabase.LoadAssetAtPath<FoodPlanInteractionView>(path);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = CreateUiObject("FoodPlanInteractionView", new Vector2(580f, 160f));
            root.AddComponent<Image>().color = new Color(0.11f, 0.13f, 0.15f, 0.92f);
            TMP_Text title = CreateText(root.transform, "Title", "食物計畫", 20f, TextAlignmentOptions.MidlineLeft);
            TMP_Text desc = CreateText(root.transform, "Description", "設定基地食物計畫。", 15f, TextAlignmentOptions.MidlineLeft);
            TMP_Text state = CreateText(root.transform, "State", "-", 14f, TextAlignmentOptions.MidlineLeft);
            Button tight = CreateButton(root.transform, "Tight", "節制", new Vector2(120f, 34f));
            Button normal = CreateButton(root.transform, "Normal", "一般", new Vector2(120f, 34f));
            Button generous = CreateButton(root.transform, "Generous", "寬裕", new Vector2(120f, 34f));
            SetAnchored(title.rectTransform, new Vector2(-130f, 56f), new Vector2(300f, 28f));
            SetAnchored(desc.rectTransform, new Vector2(0f, 24f), new Vector2(540f, 24f));
            SetAnchored(state.rectTransform, new Vector2(0f, -4f), new Vector2(540f, 24f));
            SetAnchored(tight.GetComponent<RectTransform>(), new Vector2(-140f, -48f), new Vector2(120f, 34f));
            SetAnchored(normal.GetComponent<RectTransform>(), new Vector2(0f, -48f), new Vector2(120f, 34f));
            SetAnchored(generous.GetComponent<RectTransform>(), new Vector2(140f, -48f), new Vector2(120f, 34f));
            FoodPlanInteractionView view = root.AddComponent<FoodPlanInteractionView>();
            SetSerializedReferences(view, ("titleText", title), ("descriptionText", desc), ("stateText", state), ("tightPlanButton", tight), ("normalPlanButton", normal), ("generousPlanButton", generous));
            return SavePrefab(root, path).GetComponent<FoodPlanInteractionView>();
        }

        private static CardUpgradeView CreateCardUpgradePrefab()
        {
            string path = $"{PrefabFolder}/CardUpgradeView.prefab";
            CardUpgradeView existing = AssetDatabase.LoadAssetAtPath<CardUpgradeView>(path);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = CreateUiObject("CardUpgradeView", new Vector2(580f, 132f));
            root.AddComponent<Image>().color = new Color(0.14f, 0.12f, 0.11f, 0.92f);
            TMP_Text title = CreateText(root.transform, "Title", "卡牌強化", 20f, TextAlignmentOptions.MidlineLeft);
            TMP_Text desc = CreateText(root.transform, "Description", "-", 15f, TextAlignmentOptions.MidlineLeft);
            TMP_Text cost = CreateText(root.transform, "Cost", "-", 14f, TextAlignmentOptions.MidlineLeft);
            Button upgrade = CreateButton(root.transform, "Upgrade", "強化", new Vector2(120f, 34f));
            SetAnchored(title.rectTransform, new Vector2(-130f, 46f), new Vector2(300f, 28f));
            SetAnchored(desc.rectTransform, new Vector2(-48f, 10f), new Vector2(430f, 28f));
            SetAnchored(cost.rectTransform, new Vector2(-48f, -22f), new Vector2(430f, 24f));
            SetAnchored(upgrade.GetComponent<RectTransform>(), new Vector2(206f, -22f), new Vector2(120f, 34f));
            CardUpgradeView view = root.AddComponent<CardUpgradeView>();
            SetSerializedReferences(view, ("titleText", title), ("descriptionText", desc), ("costText", cost), ("upgradeButton", upgrade));
            return SavePrefab(root, path).GetComponent<CardUpgradeView>();
        }

        private static void WireScene(IEnumerable<BuildBaseData> buildings, FoodPlanInteractionView foodPlanPrefab, CardUpgradeView cardUpgradePrefab)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            BuildingSystem buildingSystem = Object.FindObjectOfType<BuildingSystem>();
            if (buildingSystem != null)
            {
                SerializedObject serialized = new SerializedObject(buildingSystem);
                AppendObjects(serialized.FindProperty("buildingCatalog"), buildings.Cast<Object>());
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("Building Content V1 setup could not find BuildingSystem in SampleScene. Run Setup Building V1 first.");
            }

            BuildingDetailView[] detailViews = Object.FindObjectsOfType<BuildingDetailView>(true);
            foreach (BuildingDetailView detail in detailViews)
            {
                SetSerializedReferences(detail, ("foodPlanPrefab", foodPlanPrefab), ("cardUpgradePrefab", cardUpgradePrefab));
            }

            BuildingDetailView detailPrefab = AssetDatabase.LoadAssetAtPath<BuildingDetailView>($"{PrefabFolder}/BuildingDetailView.prefab");
            if (detailPrefab != null)
            {
                SetSerializedReferences(detailPrefab, ("foodPlanPrefab", foodPlanPrefab), ("cardUpgradePrefab", cardUpgradePrefab));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetLevels(SerializedObject serialized, ConstructionCardData constructionCard, params LevelSpec[] specs)
        {
            SerializedProperty levels = serialized.FindProperty("levels");
            levels.arraySize = specs.Length;
            for (int i = 0; i < specs.Length; i++)
            {
                LevelSpec spec = specs[i];
                SerializedProperty level = levels.GetArrayElementAtIndex(i);
                level.FindPropertyRelative("level").intValue = spec.Level;
                level.FindPropertyRelative("rewardDescription").stringValue = spec.RewardDescription;
                level.FindPropertyRelative("artwork").objectReferenceValue = spec.Artwork;
                level.FindPropertyRelative("color").colorValue = spec.Color;
                SetCost(level.FindPropertyRelative("constructionCost"), spec.FoodCost, spec.ResourceCost);
                level.FindPropertyRelative("requiredConstructionProgress").intValue = spec.RequiredProgress;
                level.FindPropertyRelative("progressPerCardPlay").intValue = spec.ProgressPerCard;
                level.FindPropertyRelative("constructionCard").objectReferenceValue = constructionCard;
                level.FindPropertyRelative("constructionCardCount").intValue = spec.ConstructionCardCount;
                level.FindPropertyRelative("freeIntegrationCount").intValue = spec.FreeIntegrationCount;
                level.FindPropertyRelative("manufacturingBonus").intValue = spec.ManufacturingBonus;
            }
        }

        private static void SetEffects(SerializedProperty property, EffectSpec[] effects)
        {
            property.arraySize = effects.Length;
            for (int i = 0; i < effects.Length; i++)
            {
                SetEffect(property.GetArrayElementAtIndex(i), effects[i]);
            }
        }

        private static void SetEffect(SerializedProperty property, EffectSpec effect)
        {
            property.FindPropertyRelative("carrier").enumValueIndex = (int)effect.Carrier;
            property.FindPropertyRelative("category").enumValueIndex = (int)effect.Category;
            property.FindPropertyRelative("valueEffectType").enumValueIndex = (int)effect.ValueEffect;
            property.FindPropertyRelative("cardFlowEffectType").enumValueIndex = (int)effect.CardFlowEffect;
            property.FindPropertyRelative("cardData").objectReferenceValue = effect.Card;
            property.FindPropertyRelative("amount").intValue = effect.Amount;
            property.FindPropertyRelative("requiredBuildingId").stringValue = effect.RequiredBuildingId;
            property.FindPropertyRelative("requiredBuildingLevel").intValue = effect.RequiredBuildingLevel;
            property.FindPropertyRelative("invertBuildingRequirement").boolValue = effect.InvertBuildingRequirement;
        }

        private static void SetAllowedSlots(SerializedObject serialized, params DeckSlotType[] slots)
        {
            SerializedProperty property = serialized.FindProperty("allowedSlots");
            property.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                property.GetArrayElementAtIndex(i).enumValueIndex = (int)slots[i];
            }
        }

        private static void SetCost(SerializedProperty cost, int food, int resource)
        {
            cost.FindPropertyRelative("food").intValue = food;
            cost.FindPropertyRelative("resource").intValue = resource;
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

        private static void AppendObjects(SerializedProperty property, IEnumerable<Object> values)
        {
            if (property == null)
            {
                return;
            }

            List<Object> existing = new List<Object>();
            for (int i = 0; i < property.arraySize; i++)
            {
                Object value = property.GetArrayElementAtIndex(i).objectReferenceValue;
                if (value != null && !existing.Contains(value))
                {
                    existing.Add(value);
                }
            }

            foreach (Object value in values)
            {
                if (value != null && !existing.Contains(value))
                {
                    existing.Add(value);
                }
            }

            property.arraySize = existing.Count;
            for (int i = 0; i < existing.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = existing[i];
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

        private static Button CreateButton(Transform parent, string name, string label, Vector2 size)
        {
            GameObject go = CreateUiObject(name, size);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.27f, 0.38f, 0.35f, 1f);
            Button button = go.AddComponent<Button>();
            TMP_Text text = CreateText(go.transform, "Label", label, 15f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 4f);
            return button;
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
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectArray(SerializedObject serialized, string name, params Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(name);
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

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
        }

        private readonly struct LevelSpec
        {
            public LevelSpec(int level, string rewardDescription, int foodCost, int resourceCost, int requiredProgress, int progressPerCard, int constructionCardCount, int freeIntegrationCount, int manufacturingBonus, Sprite artwork, Color color)
            {
                Level = level;
                RewardDescription = rewardDescription;
                FoodCost = foodCost;
                ResourceCost = resourceCost;
                RequiredProgress = requiredProgress;
                ProgressPerCard = progressPerCard;
                ConstructionCardCount = constructionCardCount;
                FreeIntegrationCount = freeIntegrationCount;
                ManufacturingBonus = manufacturingBonus;
                Artwork = artwork;
                Color = color;
            }

            public int Level { get; }
            public string RewardDescription { get; }
            public int FoodCost { get; }
            public int ResourceCost { get; }
            public int RequiredProgress { get; }
            public int ProgressPerCard { get; }
            public int ConstructionCardCount { get; }
            public int FreeIntegrationCount { get; }
            public int ManufacturingBonus { get; }
            public Sprite Artwork { get; }
            public Color Color { get; }
        }

        private struct EffectSpec
        {
            public GameEffectCarrier Carrier;
            public GameEffectCategory Category;
            public ValueEffectType ValueEffect;
            public CardFlowEffectType CardFlowEffect;
            public int Amount;
            public CardBase Card;
            public string RequiredBuildingId;
            public int RequiredBuildingLevel;
            public bool InvertBuildingRequirement;

            public static EffectSpec Value(ValueEffectType effect, int amount)
            {
                return new EffectSpec
                {
                    Carrier = GameEffectCarrier.Event,
                    Category = GameEffectCategory.Value,
                    ValueEffect = effect,
                    Amount = amount
                };
            }

            public static EffectSpec CardFlow(CardFlowEffectType effect, int amount)
            {
                return new EffectSpec
                {
                    Carrier = GameEffectCarrier.Event,
                    Category = GameEffectCategory.CardFlow,
                    CardFlowEffect = effect,
                    Amount = amount
                };
            }

            public static EffectSpec CardToDiscard(CardBase card, int amount)
            {
                EffectSpec spec = CardFlow(CardFlowEffectType.AddCardToDiscardPile, amount);
                spec.Card = card;
                return spec;
            }

            public EffectSpec RequireBuilding(string buildingId, int level, bool invert)
            {
                RequiredBuildingId = buildingId;
                RequiredBuildingLevel = level;
                InvertBuildingRequirement = invert;
                return this;
            }
        }
    }
}
#endif
