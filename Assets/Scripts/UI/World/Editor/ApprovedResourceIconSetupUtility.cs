#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieCardSurvive.Events.UI;

namespace ZombieCardSurvive.UI.World.Editor
{
    /// <summary>套用使用者核准的獨立圖示，不重建場景或修改遊戲數值。</summary>
    public static class ApprovedResourceIconSetupUtility
    {
        private const string Root = "Assets/Art/GameplayHUD/Icons/Review01/";
        private static readonly string[] Files = {
            "icon_resource_food_02.png", "icon_resource_parts_03.png",
            "icon_resource_combat_01.png", "icon_resource_energy_01.png"
        };

        [MenuItem("Tools/Zombie Card Survive/Gameplay HUD/Apply Approved Resource Icons")]
        public static void ApplyLoaded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Apply icons in Edit Mode.");
            Sprite[] sprites = ImportIcons();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || (scene.path != "Assets/Scenes/SampleScene.unity"
                    && scene.path != "Assets/Scenes/GameplayHUD_P0_Integration.unity")) continue;
                foreach (GameObject root in scene.GetRootGameObjects()) ApplyHierarchy(root, sprites);
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        public static void ApplyHierarchy(GameObject root)
        {
            ApplyHierarchy(root, ImportIcons());
        }

        private static Sprite[] ImportIcons()
        {
            foreach (string file in Files)
                if (AssetImporter.GetAtPath(Root + file) == null)
                    throw new InvalidOperationException("Missing approved icon: " + file);
            var result = new Sprite[Files.Length];
            for (int i = 0; i < Files.Length; i++)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Root + Files[i]);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                result[i] = AssetDatabase.LoadAssetAtPath<Sprite>(Root + Files[i]);
                if (result[i] == null) throw new InvalidOperationException("Icon import failed: " + Files[i]);
            }
            return result;
        }

        private static void ApplyHierarchy(GameObject root, Sprite[] sprites)
        {
            foreach (var flow in root.GetComponentsInChildren<EventFlowView>(true))
            {
                BindResult(flow, sprites);
                EventTextIconSetupUtility.BindText(flow, sprites);
            }
            foreach (var choice in root.GetComponentsInChildren<EventChoiceButtonView>(true))
            {
                var so = new SerializedObject(choice);
                TMP_Text cost = so.FindProperty("costText").objectReferenceValue as TMP_Text;
                if (cost == null) continue;
                Undo.RecordObject(choice, "Bind event cost icons");
                BindCost(so, choice.transform, cost, "Food", "food", sprites[0], -244f);
                BindCost(so, choice.transform, cost, "Resource", "resource", sprites[1], -116f);
                SizeChoiceText(so.FindProperty("labelText").objectReferenceValue as TMP_Text, 24f, 32f);
                SizeChoiceText(cost, 24f, 30f);
                TMP_Text reason = so.FindProperty("unavailableReasonText").objectReferenceValue as TMP_Text;
                SizeChoiceText(reason, 20f, 24f);
                if (reason != null)
                {
                    Undo.RecordObject(reason.rectTransform, "Increase reason reading area");
                    reason.rectTransform.anchorMax = new Vector2(1f, 0.36f);
                }
                so.ApplyModifiedProperties();
                EventTextIconSetupUtility.BindCostLayout(choice);
                PrefabUtility.RecordPrefabInstancePropertyModifications(choice);
            }
            foreach (var presenter in root.GetComponentsInChildren<GameplayHudRuntimePresenter>(true))
            {
                var so = new SerializedObject(presenter);
                string[] fields = { "foodText", "resourceText", "combatText", "energyText" };
                string[] labels = { "食物", "資源", "戰鬥", "能量" };
                bool complete = true;
                for (int i = 0; i < fields.Length; i++)
                {
                    TMP_Text value = so.FindProperty(fields[i]).objectReferenceValue as TMP_Text;
                    if (value == null) { complete = false; continue; }
                    Image icon = EnsureImage(value.transform, "ResourceIcon", sprites[i]);
                    SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0), new Vector2(44f, 44f));
                    Undo.RecordObject(value, "Reserve HUD icon space");
                    var margin = value.margin; margin.x = 44f; value.margin = margin;
                    value.enableAutoSizing = true;
                    value.fontSizeMin = 16f;
                    value.fontSizeMax = i == 3 ? 28f : 22f;
                    if (value.text.StartsWith(labels[i] + " ")) value.text = value.text.Substring(3);
                    EditorUtility.SetDirty(value);
                }
                so.FindProperty("resourceIconsApplied").boolValue = complete;
                so.ApplyModifiedProperties();
            }
        }

        private static void BindCost(SerializedObject owner, Transform parent, TMP_Text template,
            string name, string field, Sprite sprite, float x)
        {
            Image icon = owner.FindProperty(field + "CostIcon").objectReferenceValue as Image;
            if (icon == null) icon = EnsureImage(parent, name + "CostIcon", sprite);
            else { Undo.RecordObject(icon, "Apply cost sprite"); icon.sprite = sprite; }
            SetRect(icon.rectTransform, new Vector2(1f, 0.70f), new Vector2(x, 0), new Vector2(56f, 56f));
            TMP_Text existingValue = owner.FindProperty(field + "CostValue").objectReferenceValue as TMP_Text;
            Transform found = existingValue != null ? existingValue.transform : parent.Find(name + "CostValue");
            TMP_Text value;
            if (found == null)
            {
                var go = new GameObject(name + "CostValue", typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(go, "Create cost value");
                go.transform.SetParent(parent, false);
                value = go.GetComponent<TMP_Text>();
                EditorUtility.CopySerialized(template, value);
            }
            else value = found.GetComponent<TMP_Text>();
            Undo.RecordObject(value, "Configure cost value");
            value.raycastTarget = false;
            value.enableWordWrapping = false;
            value.enableAutoSizing = true;
            value.fontSizeMin = 26f;
            value.fontSizeMax = 32f;
            value.alignment = TextAlignmentOptions.MidlineLeft;
            value.text = string.Empty;
            SetRect(value.rectTransform, new Vector2(1f, 0.70f), new Vector2(x + 64f, 0), new Vector2(70f, 46f));
            // Runtime availability supplies values and visibility; never bake a fictional cost into the scene.
            icon.gameObject.SetActive(false);
            value.gameObject.SetActive(false);
            owner.FindProperty(field + "CostIcon").objectReferenceValue = icon;
            owner.FindProperty(field + "CostValue").objectReferenceValue = value;
        }

        private static void BindResult(EventFlowView flow, Sprite[] sprites)
        {
            var owner = new SerializedObject(flow);
            TMP_Text summary = owner.FindProperty("resultSummaryText").objectReferenceValue as TMP_Text;
            if (summary == null) return;
            Transform parent = summary.transform.parent;
            Transform found = parent.Find("ResultValueIcons");
            if (found == null)
            {
                var go = new GameObject("ResultValueIcons", typeof(RectTransform), typeof(EventResultValuesView));
                Undo.RegisterCreatedObjectUndo(go, "Create event result values");
                go.transform.SetParent(parent, false);
                found = go.transform;
            }
            var rect = (RectTransform)found;
            Undo.RecordObject(rect, "Layout event result values");
            rect.anchorMin = new Vector2(0.49f, 0.38f);
            rect.anchorMax = new Vector2(0.975f, 0.52f);
            rect.offsetMin = new Vector2(12f, 0f);
            rect.offsetMax = new Vector2(-12f, 0f);
            var view = found.GetComponent<EventResultValuesView>();
            var so = new SerializedObject(view);
            so.FindProperty("icons").arraySize = so.FindProperty("values").arraySize = 4;
            string[] names = { "Food", "Resource", "Combat", "Energy" };
            for (int i = 0; i < 4; i++)
            {
                Image icon = EnsureImage(found, names[i] + "ResultIcon", sprites[i]);
                SetRect(icon.rectTransform, new Vector2(0, 1), new Vector2(32 + i * 205, -83), new Vector2(64, 64));
                TMP_Text value = EnsureResultText(found, names[i] + "ResultValue", summary);
                SetRect(value.rectTransform, new Vector2(0, 1), new Vector2(126 + i * 205, -83), new Vector2(116, 64));
                SizeChoiceText(value, 26, 34);
                value.alignment = TextAlignmentOptions.MidlineLeft;
                so.FindProperty("icons").GetArrayElementAtIndex(i).objectReferenceValue = icon;
                so.FindProperty("values").GetArrayElementAtIndex(i).objectReferenceValue = value;
            }
            TMP_Text heading = EnsureResultText(found, "ResultValuesHeading", summary);
            SetRect(heading.rectTransform, new Vector2(0, 1), new Vector2(300, -20), new Vector2(600, 38));
            SizeChoiceText(heading, 24, 28);
            heading.alignment = TextAlignmentOptions.MidlineLeft;
            heading.color = new Color32(49, 48, 44, 255);
            heading.text = "本次淨變化（含支付）";
            so.FindProperty("heading").objectReferenceValue = heading;
            so.ApplyModifiedProperties();
            owner.FindProperty("resultValuesView").objectReferenceValue = view;
            owner.ApplyModifiedProperties();
            Undo.RecordObject(summary.rectTransform, "Reserve special effect summary area");
            summary.rectTransform.anchorMin = new Vector2(0.49f, 0.19f);
            summary.rectTransform.anchorMax = new Vector2(0.975f, 0.37f);
            summary.rectTransform.offsetMin = new Vector2(12, 6);
            summary.rectTransform.offsetMax = new Vector2(-12, -6);
            SizeChoiceText(summary, 22, 28);
            summary.color = new Color32(49, 48, 44, 255);
            found.gameObject.SetActive(false);
        }

        private static TMP_Text EnsureResultText(Transform parent, string name, TMP_Text template)
        {
            Transform found = parent.Find(name);
            if (found != null) return found.GetComponent<TMP_Text>();
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "Create event result text");
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TMP_Text>();
            EditorUtility.CopySerialized(template, text);
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }

        private static void SizeChoiceText(TMP_Text text, float min, float max)
        {
            if (text == null) return;
            Undo.RecordObject(text, "Improve choice readability");
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            EditorUtility.SetDirty(text);
        }

        private static Image EnsureImage(Transform parent, string name, Sprite sprite)
        {
            Transform found = parent.Find(name);
            if (found == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "Create resource icon");
                go.transform.SetParent(parent, false);
                go.layer = parent.gameObject.layer;
                found = go.transform;
            }
            Image image = found.GetComponent<Image>();
            Undo.RecordObject(image, "Apply approved icon");
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            EditorUtility.SetDirty(image);
            return image;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Undo.RecordObject(rect, "Place resource icon");
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
#endif
