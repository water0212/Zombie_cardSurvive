#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieCardSurvive.Events.UI;
using ZombieCardSurvive.Exploration.UI;
using ZombieCardSurvive.Cards.UI.Zombies;
using ZombieCardSurvive.Buildings.UI;

namespace ZombieCardSurvive.UI.World.Editor
{
    /// <summary>只更新既有面板的表面與文字色，不修改流程、位置或事件引用。</summary>
    public static class PaperUiFlowSkinUtility
    {
        private const string Root = "Assets/Art/GameplayHUD/PaperUI/";
        private static readonly Color Ink = new Color32(49, 48, 44, 255);
        private static readonly Color Chalk = new Color32(232, 229, 218, 255);

        [MenuItem("Tools/Zombie Card Survive/Gameplay HUD/Apply Paper UI To Loaded Flows")]
        public static void ApplyLoadedFlows()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Apply Paper UI in Edit Mode.");
            // 預先驗證所有素材，避免缺圖時留下半套修改。
            Load("ui_background_archival_paper_01.png");
            Load("ui_panel_paper_light_9slice_01.png");
            Load("ui_panel_paper_dark_9slice_01.png");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || scene.path != "Assets/Scenes/SampleScene.unity") continue;
                foreach (GameObject root in scene.GetRootGameObjects()) ApplyHierarchy(root);
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        public static void ApplyHierarchy(GameObject hierarchy)
        {
            foreach (var view in hierarchy.GetComponentsInChildren<EventFlowView>(true))
            {
                Surface(Reference<GameObject>(view, "root"), "ui_background_archival_paper_01.png", false);
                EnsureEventJournalText(view);
                EnsureEventResultSummaryText(view);
                Text(view, "titleText", Ink);
                Text(view, "descriptionText", Ink);
                Text(view, "journalText", Ink);
                Text(view, "resultSummaryText", new Color32(117, 47, 38, 255));
                foreach (var choice in view.GetComponentsInChildren<EventChoiceButtonView>(true))
                {
                    EnsureChoiceSupplementText(choice, "ChoiceCostText", "costText");
                    EnsureChoiceSupplementText(choice, "UnavailableReasonText", "unavailableReasonText");
                    Button button = Reference<Button>(choice, "button") ?? choice.GetComponent<Button>();
                    ButtonSurface(button, false);
                    Text(choice, "labelText", Ink);
                    Text(choice, "costText", new Color32(77, 66, 50, 255));
                    Text(choice, "unavailableReasonText", new Color32(137, 42, 35, 255));
                }
                ButtonSurface(Reference<Button>(view, "endEventButton"), true);
                LayoutEvent(view);
                ApprovedResourceIconSetupUtility.ApplyHierarchy(view.gameObject);
            }
            foreach (var inspection in hierarchy.GetComponentsInChildren<ZombieCardSurvive.UI.FlowInspectionController>(true))
            {
                LayoutInspectionControls(inspection);
            }
            foreach (var view in hierarchy.GetComponentsInChildren<ExplorationFlowView>(true))
            {
                Surface(Reference<GameObject>(view, "root"), "ui_background_archival_paper_01.png", false);
                Text(view, "titleText", Ink);
                Text(view, "descriptionText", Ink);
                ButtonSurface(Reference<Button>(view, "closeButton"), true);
                foreach (var card in view.GetComponentsInChildren<ExplorationRegionCardView>(true))
                {
                    Button button = Reference<Button>(card, "button") ?? card.GetComponent<Button>();
                    ButtonSurface(button, false);
                    // Region1 是現行探索卡的可見底板；不碰 artworkImage。
                    Transform panel = card.transform.Find("Region1");
                    if (panel != null) Surface(panel.gameObject, "ui_panel_paper_light_9slice_01.png", true);
                    Text(card, "nameText", Ink);
                    Text(card, "descriptionText", Ink);
                    Text(card, "dangerText", new Color32(158, 43, 43, 255));
                    Text(card, "rewardsText", new Color32(65, 53, 111, 255));
                }
                ExplorationPresentationSetupUtility.Apply(view);
            }
            foreach (var view in hierarchy.GetComponentsInChildren<ZombieThreatDetailView>(true))
            {
                Surface(view.gameObject, "ui_panel_paper_dark_9slice_01.png", true);
                Transform inner = view.transform.Find("Panel");
                if (inner != null) Surface(inner.gameObject, "ui_panel_paper_dark_9slice_01.png", true);
                Text(view, "nameText", Chalk);
                Text(view, "descriptionText", Chalk);
                Text(view, "negativeEffectText", Chalk);
                Text(view, "healthText", new Color32(240, 143, 132, 255));
                Text(view, "attackText", new Color32(210, 168, 235, 255));
                // 無文字的取消圖示保留原圖。
            }
            foreach (var view in hierarchy.GetComponentsInChildren<BuildingDetailView>(true))
            {
                Surface(Reference<GameObject>(view, "root") ?? view.gameObject, "ui_panel_paper_dark_9slice_01.png", true);
                foreach (string field in new[] { "titleText", "statusText", "requirementText", "rewardText" })
                    Text(view, field, Chalk);
                foreach (string field in new[] { "constructButton", "cancelConstructionButton", "closeButton" })
                    ButtonSurface(Reference<Button>(view, field), false);
            }
        }

        private static T Reference<T>(Component component, string field) where T : Object
        {
            var property = new SerializedObject(component).FindProperty(field);
            return property == null ? null : property.objectReferenceValue as T;
        }

        private static void Text(Component component, string field, Color colour)
        {
            TMP_Text text = Reference<TMP_Text>(component, field);
            if (text == null) return;
            Undo.RecordObject(text, "Paper UI text contrast");
            text.color = colour;
            EditorUtility.SetDirty(text);
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }

        private static void ButtonSurface(Button button, bool dark)
        {
            if (button == null) return;
            Image image = button.targetGraphic as Image;
            if (image == null) return;
            Surface(image.gameObject, dark ? "ui_panel_paper_dark_9slice_01.png" : "ui_panel_paper_light_9slice_01.png", true);
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                Undo.RecordObject(text, "Paper UI button text");
                text.color = dark ? Chalk : Ink;
                EditorUtility.SetDirty(text);
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
        }

        /// <summary>
        /// 事件頁採用「左圖、右文、右下決策」；結果階段沿用同一閱讀動線，
        /// 只由 EventFlowView 將選項換成繼續按鈕。
        /// </summary>
        private static void LayoutEvent(EventFlowView view)
        {
            GameObject root = Reference<GameObject>(view, "root") ?? view.gameObject;
            Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TMP_Text title = Reference<TMP_Text>(view, "titleText");
            Place(title != null ? title.rectTransform : null,
                new Vector2(0.49f, 0.84f), new Vector2(0.835f, 0.965f));
            ConfigureText(title, 38f, 58f, TextAlignmentOptions.Center);

            TMP_Text description = Reference<TMP_Text>(view, "descriptionText");
            Place(description != null ? description.rectTransform : null,
                new Vector2(0.49f, 0.55f), new Vector2(0.975f, 0.81f),
                new Vector2(12f, 8f), new Vector2(-12f, -8f));
            ConfigureText(description, 22f, 32f, TextAlignmentOptions.TopLeft);

            TMP_Text journal = Reference<TMP_Text>(view, "journalText");
            Place(journal != null ? journal.rectTransform : null,
                new Vector2(0.035f, 0.055f), new Vector2(0.455f, 0.30f),
                new Vector2(12f, 8f), new Vector2(-12f, -8f));
            ConfigureText(journal, 19f, 28f, TextAlignmentOptions.TopLeft);

            Image artwork = Reference<Image>(view, "artworkImage");
            if (artwork != null)
            {
                Place(artwork.rectTransform,
                    new Vector2(0.025f, 0.35f), new Vector2(0.46f, 0.965f));
                Undo.RecordObject(artwork, "Layout event artwork");
                artwork.preserveAspect = true;
                EditorUtility.SetDirty(artwork);
                PrefabUtility.RecordPrefabInstancePropertyModifications(artwork);
            }

            var choices = view.GetComponentsInChildren<EventChoiceButtonView>(true);
            System.Array.Sort(choices, (left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
            float[] centres = { 0.465f, 0.35f, 0.235f, 0.12f };
            for (int i = 0; i < choices.Length && i < centres.Length; i++)
            {
                EventChoiceButtonView choice = choices[i];
                RectTransform choiceRect = choice.GetComponent<RectTransform>();
                PlaceFixedHeight(choiceRect, new Vector2(0.49f, centres[i]), new Vector2(0.975f, centres[i]), 82f);

                TMP_Text label = Reference<TMP_Text>(choice, "labelText");
                if (label != null)
                {
                    Stretch(label.rectTransform, new Vector2(0f, 0.26f), new Vector2(0.68f, 1f),
                        new Vector2(30f, 4f), new Vector2(-8f, -4f));
                    ConfigureText(label, 19f, 27f, TextAlignmentOptions.MidlineLeft);
                }

                TMP_Text cost = Reference<TMP_Text>(choice, "costText");
                if (cost != null)
                {
                    Stretch(cost.rectTransform, new Vector2(0.68f, 0.26f), Vector2.one,
                        new Vector2(8f, 4f), new Vector2(-24f, -4f));
                    ConfigureText(cost, 16f, 22f, TextAlignmentOptions.MidlineRight);
                }

                TMP_Text unavailable = Reference<TMP_Text>(choice, "unavailableReasonText");
                if (unavailable != null)
                {
                    Stretch(unavailable.rectTransform, Vector2.zero, new Vector2(1f, 0.28f),
                        new Vector2(30f, 2f), new Vector2(-24f, -2f));
                    ConfigureText(unavailable, 14f, 18f, TextAlignmentOptions.MidlineRight);
                }
            }

            TMP_Text resultSummary = Reference<TMP_Text>(view, "resultSummaryText");
            Place(resultSummary != null ? resultSummary.rectTransform : null,
                new Vector2(0.49f, 0.39f), new Vector2(0.975f, 0.50f),
                new Vector2(12f, 6f), new Vector2(-12f, -6f));
            ConfigureText(resultSummary, 18f, 26f, TextAlignmentOptions.TopLeft);

            Button end = Reference<Button>(view, "endEventButton");
            if (end != null)
            {
                Place(end.GetComponent<RectTransform>(),
                    new Vector2(0.78f, 0.075f), new Vector2(0.975f, 0.16f));
                foreach (TMP_Text text in end.GetComponentsInChildren<TMP_Text>(true))
                {
                    Stretch(text.rectTransform, Vector2.zero, Vector2.one,
                        new Vector2(20f, 8f), new Vector2(-20f, -8f));
                    ConfigureText(text, 20f, 28f, TextAlignmentOptions.Center);
                }
            }
        }

        private static TMP_Text EnsureEventJournalText(EventFlowView view)
        {
            TMP_Text existing = Reference<TMP_Text>(view, "journalText");
            if (existing != null) return existing;

            GameObject root = Reference<GameObject>(view, "root") ?? view.gameObject;
            Transform found = root.transform.Find("JournalText");
            TMP_Text journal = found != null ? found.GetComponent<TMP_Text>() : null;
            if (journal == null)
            {
                TMP_Text description = Reference<TMP_Text>(view, "descriptionText");
                GameObject journalObject;
                if (description != null)
                {
                    journalObject = Object.Instantiate(description.gameObject, root.transform);
                    journalObject.name = "JournalText";
                    journal = journalObject.GetComponent<TMP_Text>();
                }
                else
                {
                    journalObject = new GameObject("JournalText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    journalObject.transform.SetParent(root.transform, false);
                    journal = journalObject.GetComponent<TextMeshProUGUI>();
                }

                Undo.RegisterCreatedObjectUndo(journalObject, "Create event journal text");
                journal.text = string.Empty;
                journal.raycastTarget = false;
            }

            Undo.RecordObject(view, "Bind event journal text");
            SerializedObject serialized = new SerializedObject(view);
            SerializedProperty property = serialized.FindProperty("journalText");
            if (property != null)
            {
                property.objectReferenceValue = journal;
                serialized.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(view);
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            return journal;
        }

        private static TMP_Text EnsureEventResultSummaryText(EventFlowView view)
        {
            TMP_Text existing = Reference<TMP_Text>(view, "resultSummaryText");
            if (existing != null) return existing;

            GameObject root = Reference<GameObject>(view, "root") ?? view.gameObject;
            TMP_Text summary = EnsureTextObject(root.transform, "ResultSummaryText", Reference<TMP_Text>(view, "descriptionText"));
            summary.text = string.Empty;
            summary.gameObject.SetActive(false);
            BindReference(view, "resultSummaryText", summary, "Bind event result summary");
            return summary;
        }

        private static TMP_Text EnsureChoiceSupplementText(EventChoiceButtonView view, string objectName, string field)
        {
            TMP_Text existing = Reference<TMP_Text>(view, field);
            if (existing != null) return existing;

            TMP_Text text = EnsureTextObject(view.transform, objectName, Reference<TMP_Text>(view, "labelText"));
            text.text = string.Empty;
            text.raycastTarget = false;
            BindReference(view, field, text, "Bind event choice supplement text");
            return text;
        }

        private static TMP_Text EnsureTextObject(Transform parent, string objectName, TMP_Text template)
        {
            Transform found = parent.Find(objectName);
            TMP_Text text = found != null ? found.GetComponent<TMP_Text>() : null;
            if (text != null) return text;

            GameObject textObject;
            if (template != null)
            {
                textObject = Object.Instantiate(template.gameObject, parent);
                textObject.name = objectName;
                text = textObject.GetComponent<TMP_Text>();
            }
            else
            {
                textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(parent, false);
                text = textObject.GetComponent<TextMeshProUGUI>();
            }

            Undo.RegisterCreatedObjectUndo(textObject, "Create event presentation text");
            return text;
        }

        private static void BindReference(Component owner, string field, Object value, string undoName)
        {
            Undo.RecordObject(owner, undoName);
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(field);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(owner);
            PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
        }

        private static void LayoutInspectionControls(ZombieCardSurvive.UI.FlowInspectionController inspection)
        {
            foreach (string field in new[] { "inspectButton", "returnButton" })
            {
                Button button = Reference<Button>(inspection, field);
                if (button == null) continue;
                Place(button.GetComponent<RectTransform>(),
                    new Vector2(0.85f, 0.845f), new Vector2(0.975f, 0.955f));
                ButtonSurface(button, true);
                foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    Stretch(label.rectTransform, Vector2.zero, Vector2.one,
                        new Vector2(16f, 8f), new Vector2(-16f, -8f));
                    ConfigureText(label, 18f, 25f, TextAlignmentOptions.Center);
                }
            }
        }

        private static void ConfigureText(TMP_Text text, float minSize, float maxSize, TextAlignmentOptions alignment)
        {
            if (text == null) return;
            Undo.RecordObject(text, "Layout event text");
            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.fontSize = maxSize;
            text.alignment = alignment;
            text.margin = Vector4.zero;
            text.enableWordWrapping = true;
            EditorUtility.SetDirty(text);
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            Stretch(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            Stretch(rect, anchorMin, anchorMax, offsetMin, offsetMax);
        }

        private static void PlaceFixedHeight(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, float height)
        {
            if (rect == null) return;
            Undo.RecordObject(rect, "Layout event control");
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
            EditorUtility.SetDirty(rect);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null) return;
            Undo.RecordObject(rect, "Layout event control");
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            EditorUtility.SetDirty(rect);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        }

        private static Sprite Load(string file)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + file);
            if (sprite == null) throw new System.InvalidOperationException("Missing Paper UI sprite: " + file);
            return sprite;
        }

        private static void Surface(GameObject target, string file, bool sliced)
        {
            if (target == null) return;
            Image image = target.GetComponent<Image>();
            if (image == null) return;
            Undo.RecordObject(image, "Apply Paper UI surface");
            image.sprite = Load(file);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = 4f;
            image.color = Color.white;
            EditorUtility.SetDirty(image);
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        }
    }
}
#endif
