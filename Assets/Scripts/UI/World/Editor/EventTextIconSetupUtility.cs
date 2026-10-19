#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;
using ZombieCardSurvive.Events.UI;

namespace ZombieCardSurvive.UI.World.Editor
{
    /// <summary>Presentation-only migration; preserves event content and the user's panel anchors.</summary>
    public static class EventTextIconSetupUtility
    {
        private const string Root = "Assets/Art/GameplayHUD/Icons/Review01/";
        private static readonly string[] Names = { "food", "resource", "combat", "energy" };
        private static readonly string[] Files = {
            "icon_resource_food_02.png", "icon_resource_parts_03.png",
            "icon_resource_combat_01.png", "icon_resource_energy_01.png"
        };

        [MenuItem("Tools/Zombie Card Survive/Gameplay HUD/Apply Event Text Icons and Cost Layout")]
        public static void ApplyLoaded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before applying event text icons.");
            var sprites = new Sprite[4];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(Root + Files[i]);
                if (sprites[i] == null) throw new InvalidOperationException("Missing approved Sprite: " + Files[i]);
            }
            foreach (var flow in UnityEngine.Object.FindObjectsOfType<EventFlowView>(true))
            {
                if (!flow.gameObject.scene.IsValid() || !flow.gameObject.scene.isLoaded) continue;
                BindText(flow, sprites);
                foreach (var choice in flow.GetComponentsInChildren<EventChoiceButtonView>(true))
                    BindCostLayout(choice);
                EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Event text sprite assets and cost layouts applied. Save the scene to keep the references.");
        }

        public static void BindText(EventFlowView flow, Sprite[] sprites)
        {
            TMP_SpriteAsset asset = BuildSpriteAssets(sprites);
            var so = new SerializedObject(flow);
            foreach (string field in new[] { "titleText", "descriptionText", "journalText", "resultSummaryText" })
                ConfigureText(so.FindProperty(field).objectReferenceValue as TMP_Text, asset);
            // Choice labels may also contain authored inline sprite tags; amounts remain data-driven.
            foreach (var choice in flow.GetComponentsInChildren<EventChoiceButtonView>(true))
                ConfigureText(new SerializedObject(choice).FindProperty("labelText").objectReferenceValue as TMP_Text, asset);
        }

        private static void ConfigureText(TMP_Text text, TMP_SpriteAsset asset)
        {
            if (text == null) return;
            Undo.RecordObject(text, "Enable inline event sprites");
            text.richText = true;
            text.spriteAsset = asset;
            EditorUtility.SetDirty(text);
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }

        private static TMP_SpriteAsset BuildSpriteAssets(Sprite[] sprites)
        {
            // One texture per asset plus explicit fallbacks: no repacking or modifying approved artwork.
            Shader shader = Shader.Find("TextMeshPro/Sprite");
            if (shader == null) throw new InvalidOperationException("TextMeshPro/Sprite shader is missing.");
            var assets = new TMP_SpriteAsset[4];
            for (int i = 0; i < assets.Length; i++)
            {
                string path = Root + "EventInline_" + Names[i] + ".asset";
                var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                    asset.name = "EventInline_" + Names[i];
                    AssetDatabase.CreateAsset(asset, path);
                }
                Sprite sprite = sprites[i];
                // TMP's public setter is internal; serialize the current schema before lookup-table access.
                var serializedAsset = new SerializedObject(asset);
                serializedAsset.FindProperty("m_Version").stringValue = "1.1.0";
                serializedAsset.ApplyModifiedPropertiesWithoutUndo();
                asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(asset.name);
                Rect rect = sprite.rect;
                asset.spriteSheet = sprite.texture;
                if (asset.material == null)
                {
                    asset.material = new Material(shader) { name = asset.name + " Material" };
                    AssetDatabase.AddObjectToAsset(asset.material, asset);
                }
                asset.material.mainTexture = sprite.texture;
                var glyph = new TMP_SpriteGlyph(0,
                    new GlyphMetrics(rect.width, rect.height, 0, rect.height * 0.85f, rect.width),
                    new GlyphRect((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height), 1, 0, sprite);
                asset.spriteGlyphTable.Clear();
                asset.spriteGlyphTable.Add(glyph);
                asset.spriteCharacterTable.Clear();
                asset.spriteCharacterTable.Add(new TMP_SpriteCharacter((uint)(0xE000 + i), asset, glyph) { name = Names[i] });
                asset.fallbackSpriteAssets = new List<TMP_SpriteAsset>();
                asset.UpdateLookupTables();
                EditorUtility.SetDirty(asset.material);
                EditorUtility.SetDirty(asset);
                assets[i] = asset;
            }
            assets[0].fallbackSpriteAssets.AddRange(new[] { assets[1], assets[2], assets[3] });
            EditorUtility.SetDirty(assets[0]);
            return assets[0];
        }

        public static void BindCostLayout(EventChoiceButtonView choice)
        {
            var so = new SerializedObject(choice);
            // Validate before migration so older scenes retain their text fallback.
            foreach (string prefix in new[] { "food", "resource" })
                if (so.FindProperty(prefix + "CostIcon").objectReferenceValue == null
                    || so.FindProperty(prefix + "CostValue").objectReferenceValue == null) return;

            RectTransform row = EnsureRect(choice.transform, "CostLayout");
            Undo.RecordObject(row, "Position event cost row");
            row.anchorMin = new Vector2(0.52f, 0.38f);
            row.anchorMax = new Vector2(1f, 1f);
            row.offsetMin = Vector2.zero;
            row.offsetMax = new Vector2(-12f, 0);
            ConfigureLayout(row, 16, TextAnchor.MiddleRight);
            var label = so.FindProperty("labelText").objectReferenceValue as TMP_Text;
            if (label != null)
            {
                Undo.RecordObject(label.rectTransform, "Reserve cost layout area");
                Vector2 anchor = label.rectTransform.anchorMax;
                anchor.x = 0.52f;
                label.rectTransform.anchorMax = anchor;
            }
            foreach (string prefix in new[] { "food", "resource" })
            {
                var icon = (Image)so.FindProperty(prefix + "CostIcon").objectReferenceValue;
                var value = (TMP_Text)so.FindProperty(prefix + "CostValue").objectReferenceValue;
                RectTransform group = EnsureRect(row, prefix + "CostGroup");
                ConfigureLayout(group, 6, TextAnchor.MiddleLeft);
                Undo.SetTransformParent(icon.transform, group, "Group cost icon");
                Undo.SetTransformParent(value.transform, group, "Group cost number");
                icon.transform.SetAsFirstSibling();
                value.transform.SetAsLastSibling();
                Undo.RecordObject(icon.rectTransform, "Normalize icon transform");
                Undo.RecordObject(value.rectTransform, "Normalize cost transform");
                icon.transform.localScale = value.transform.localScale = Vector3.one;
                icon.transform.localRotation = value.transform.localRotation = Quaternion.identity;
                LayoutElement size = icon.GetComponent<LayoutElement>();
                if (size == null) size = Undo.AddComponent<LayoutElement>(icon.gameObject);
                Undo.RecordObject(size, "Size cost icon");
                size.minWidth = size.preferredWidth = 56;
                size.minHeight = size.preferredHeight = 56;
                size.flexibleWidth = size.flexibleHeight = 0;
                Undo.RecordObject(value, "Use preferred text width");
                value.enableAutoSizing = false;
                value.fontSize = 32;
                value.enableWordWrapping = false;
                value.margin = Vector4.zero;
                value.alignment = TextAlignmentOptions.MidlineLeft;
                // Runtime toggles the entire group so a missing cost leaves no layout gap.
                Undo.RecordObject(group.gameObject, "Set initial cost visibility");
                group.gameObject.SetActive(icon.gameObject.activeSelf);
                so.FindProperty(prefix + "CostGroup").objectReferenceValue = group.gameObject;
            }
            so.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(choice);
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            if (found != null) return (RectTransform)found;
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create cost layout");
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return (RectTransform)go.transform;
        }

        private static void ConfigureLayout(RectTransform rect, float spacing, TextAnchor alignment)
        {
            var layout = rect.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = Undo.AddComponent<HorizontalLayoutGroup>(rect.gameObject);
            Undo.RecordObject(layout, "Configure cost layout");
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.padding = new RectOffset();
        }
    }
}
#endif
