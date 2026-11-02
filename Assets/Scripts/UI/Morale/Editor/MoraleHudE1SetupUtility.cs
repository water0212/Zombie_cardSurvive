#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.UI.World;

namespace ZombieCardSurvive.UI.Morale.Editor
{
    /// <summary>只在目前場景補上 E1 專用 UI；不儲存場景、不重建既有 HUD。</summary>
    public static class MoraleHudE1SetupUtility
    {
        private const string RootName = "PB_P0_014E_E1_MoraleHUD";

        [MenuItem("Tools/Zombie Card Survive/Gameplay HUD/Apply Morale E1 To Current Scene")]
        public static void ApplyToCurrentScene()
        {
            GameplayHudRuntimePresenter basePresenter = Object.FindObjectOfType<GameplayHudRuntimePresenter>(true);
            RunPhaseController phase = Object.FindObjectOfType<RunPhaseController>(true);
            Canvas canvas = basePresenter != null ? basePresenter.GetComponentInParent<Canvas>() : Object.FindObjectOfType<Canvas>(true);
            if (basePresenter == null || phase == null || canvas == null)
            {
                Debug.LogError("Morale E1 setup needs an existing GameplayHudRuntimePresenter, RunPhaseController, and Canvas.");
                return;
            }

            SerializedObject baseSerialized = new SerializedObject(basePresenter);
            TMP_Text oldMorale = baseSerialized.FindProperty("moraleText")?.objectReferenceValue as TMP_Text;
            TMP_Text oldThreshold = baseSerialized.FindProperty("moraleThresholdText")?.objectReferenceValue as TMP_Text;
            Transform moralePanel = oldMorale != null ? oldMorale.transform.parent : canvas.transform;
            Transform existing = moralePanel.Find(RootName) ?? canvas.transform.Find(RootName);
            bool migratedToMoralePanel = existing != null && existing.parent != moralePanel;
            if (migratedToMoralePanel) existing.SetParent(moralePanel, false);
            GameObject root = existing != null ? existing.gameObject : CreateUiObject(RootName, moralePanel);
            bool activateAfterSetup = existing == null && root.activeSelf;
            if (activateAfterSetup) root.SetActive(false);
            Undo.RegisterFullObjectHierarchyUndo(root, "Apply Morale E1 HUD");
            RectTransform rootRect = root.GetComponent<RectTransform>();
            if (existing == null || migratedToMoralePanel)
            {
                rootRect.anchorMin = new Vector2(0.5f, 1f);
                rootRect.anchorMax = new Vector2(0.5f, 1f);
                rootRect.pivot = new Vector2(0.5f, 1f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = new Vector2(430f, 210f);
            }

            if (oldMorale != null) oldMorale.gameObject.SetActive(false);
            if (oldThreshold != null) oldThreshold.gameObject.SetActive(false);
            Transform oldPyramid = moralePanel.Find("Pyramid") ?? basePresenter.transform.Find("TimePanel/Pyramid");
            if (oldPyramid != null) oldPyramid.gameObject.SetActive(false);

            Image hitArea = GetOrAdd<Image>(root);
            hitArea.color = new Color32(28, 27, 25, 165);
            hitArea.raycastTarget = true;
            CanvasGroup warningGroup = GetOrAdd<CanvasGroup>(root);
            MoraleHudPresenter presenter = GetOrAdd<MoraleHudPresenter>(root);

            TMP_FontAsset font = basePresenter.GetComponentInChildren<TMP_Text>(true)?.font;
            TMP_Text morale = EnsureText(root.transform, "MoraleValue", "100", 48, TextAlignmentOptions.Center, font, out bool moraleCreated);
            if (moraleCreated) SetRect(morale.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(180f, 62f));
            TMP_Text stage = EnsureText(root.transform, "Stage", "▲ 完整｜安定", 20, TextAlignmentOptions.Center, font, out bool stageCreated);
            if (stageCreated) SetRect(stage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(300f, 34f));

            Image[] pyramid = new Image[3];
            for (int index = 0; index < pyramid.Length; index++)
            {
                GameObject layer = EnsureObject(root.transform, "PyramidLayer" + index, out bool layerCreated);
                pyramid[index] = GetOrAdd<Image>(layer);
                pyramid[index].raycastTarget = false;
                if (layerCreated)
                    SetRect(layer.GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
                        new Vector2((index - 1) * 38f, -110f - index * 10f), new Vector2(32f + index * 12f, 12f));
            }

            TMP_Text moraleWarning = EnsureText(root.transform, "MoraleWarning", string.Empty, 17, TextAlignmentOptions.Center, font, out bool moraleWarningCreated);
            moraleWarning.color = new Color32(244, 194, 91, 255);
            if (moraleWarningCreated) SetRect(moraleWarning.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(410f, 62f));

            TMP_Text foodWarning = EnsureAttachedWarning(basePresenter, "foodText", "PB_P0_014E_FoodWarning", font, -4f);
            TMP_Text resourceWarning = EnsureAttachedWarning(basePresenter, "resourceText", "PB_P0_014E_ResourceWarning", font, -50f);
            TMP_Text combatWarning = EnsureAttachedWarning(basePresenter, "combatText", "PB_P0_014E_CombatWarning", font, -96f);

            GameObject detail = EnsurePanel(canvas.transform, "PB_P0_014E_MoraleDetail", new Vector2(520f, 350f), new Vector2(290f, -245f));
            detail.GetComponent<Image>().raycastTarget = false;
            TMP_Text detailText = EnsureText(detail.transform, "Body", string.Empty, 18, TextAlignmentOptions.TopLeft, font, out bool detailTextCreated);
            if (detailTextCreated) Stretch(detailText.rectTransform, 24f);
            detail.SetActive(false);

            GameObject modal = EnsureModal(canvas.transform, font, out TMP_Text modalBody, out Button returnButton, out Button continueButton);
            modal.SetActive(false);

            SerializedObject serialized = new SerializedObject(presenter);
            Set(serialized, "runPhaseController", phase);
            Set(serialized, "basePresenter", basePresenter);
            Set(serialized, "moraleText", morale);
            Set(serialized, "stageText", stage);
            SerializedProperty layers = serialized.FindProperty("pyramidLayers");
            layers.arraySize = 3;
            for (int i = 0; i < 3; i++) layers.GetArrayElementAtIndex(i).objectReferenceValue = pyramid[i];
            Set(serialized, "detailRoot", detail);
            Set(serialized, "detailText", detailText);
            Set(serialized, "moraleWarningText", moraleWarning);
            Set(serialized, "foodWarningText", foodWarning);
            Set(serialized, "resourceWarningText", resourceWarning);
            Set(serialized, "combatWarningText", combatWarning);
            Set(serialized, "warningCanvasGroup", warningGroup);
            Set(serialized, "confirmationRoot", modal);
            Set(serialized, "confirmationBody", modalBody);
            Set(serialized, "returnButton", returnButton);
            Set(serialized, "continueButton", continueButton);
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(root.scene);
            if (activateAfterSetup) root.SetActive(true);
            Selection.activeGameObject = root;
            Debug.Log("Morale E1 HUD applied to the current scene. Review layout, then save manually.", root);
        }

        [MenuItem("Tools/Zombie Card Survive/Gameplay HUD/QA/Set Food Shortage Warning")]
        private static void SetFoodShortageWarning()
        {
            FindQaController()?.SetFoodShortageWarning();
        }

        [MenuItem("Tools/Zombie Card Survive/Gameplay HUD/QA/Set Stage Crossing Risk")]
        private static void SetStageCrossingRisk()
        {
            FindQaController()?.SetStageCrossingRisk();
        }

        [MenuItem("Tools/Zombie Card Survive/Gameplay HUD/QA/Restore Demo Values")]
        private static void RestoreDemoValues()
        {
            FindQaController()?.RestoreDemoValues();
        }

        private static P0GameplayHudSliceController FindQaController()
        {
            P0GameplayHudSliceController controller = Object.FindObjectOfType<P0GameplayHudSliceController>(true);
            if (controller == null)
                Debug.LogError("The loaded scene does not contain P0GameplayHudSliceController.");
            return controller;
        }

        private static TMP_Text EnsureAttachedWarning(
            GameplayHudRuntimePresenter presenter,
            string field,
            string name,
            TMP_FontAsset font,
            float verticalOffset)
        {
            SerializedObject serialized = new SerializedObject(presenter);
            TMP_Text anchorText = serialized.FindProperty(field)?.objectReferenceValue as TMP_Text;
            Transform parent = anchorText != null ? anchorText.transform : presenter.transform;
            TMP_Text existingWarning = FindTextByName(presenter.transform, name);
            bool warningMoved = existingWarning != null && existingWarning.transform.parent != parent;
            if (warningMoved) existingWarning.transform.SetParent(parent, false);
            TMP_Text warning = EnsureText(parent, name, string.Empty, 14, TextAlignmentOptions.Center, font, out bool warningCreated);
            warning.color = new Color32(244, 194, 91, 255);
            RectTransform rect = warning.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, verticalOffset);
            rect.sizeDelta = new Vector2(240f, 42f);
            return warning;
        }

        private static TMP_Text FindTextByName(Transform root, string name)
        {
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
                if (texts[index] != null && texts[index].name == name) return texts[index];
            return null;
        }

        private static GameObject EnsureModal(Transform canvas, TMP_FontAsset font, out TMP_Text body, out Button back, out Button proceed)
        {
            GameObject modal = EnsureObject(canvas, "PB_P0_014E_DangerConfirmation", out bool modalCreated);
            if (modalCreated) Stretch(modal.GetComponent<RectTransform>(), 0f);
            Image blocker = GetOrAdd<Image>(modal);
            blocker.color = new Color32(13, 12, 11, 210);
            blocker.raycastTarget = true;
            GameObject paper = EnsurePanel(modal.transform, "Paper", new Vector2(720f, 420f), Vector2.zero);
            TMP_Text title = EnsureText(paper.transform, "Title", "仍要結束本輪嗎？", 34, TextAlignmentOptions.Center, font, out bool titleCreated);
            if (titleCreated) SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(660f, 56f));
            body = EnsureText(paper.transform, "Body", string.Empty, 21, TextAlignmentOptions.TopLeft, font, out bool bodyCreated);
            if (bodyCreated) SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(620f, 210f));
            back = EnsureButton(paper.transform, "ReturnButton", "返回處理", new Vector2(-170f, 42f), font);
            proceed = EnsureButton(paper.transform, "ContinueButton", "仍然結束本輪", new Vector2(170f, 42f), font);
            return modal;
        }

        private static GameObject EnsurePanel(Transform parent, string name, Vector2 size, Vector2 position)
        {
            GameObject panel = EnsureObject(parent, name, out bool panelCreated);
            if (panelCreated) SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, size);
            Image image = GetOrAdd<Image>(panel);
            image.color = new Color32(54, 51, 46, 250);
            return panel;
        }

        private static Button EnsureButton(Transform parent, string name, string label, Vector2 position, TMP_FontAsset font)
        {
            GameObject root = EnsureObject(parent, name, out bool buttonCreated);
            if (buttonCreated) SetRect(root.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), position, new Vector2(260f, 62f));
            Image image = GetOrAdd<Image>(root);
            image.color = new Color32(91, 84, 72, 255);
            Button button = GetOrAdd<Button>(root);
            TMP_Text text = EnsureText(root.transform, "Label", label, 20, TextAlignmentOptions.Center, font, out bool labelCreated);
            if (labelCreated) Stretch(text.rectTransform, 4f);
            return button;
        }

        private static TMP_Text EnsureText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            return EnsureText(parent, name, value, size, alignment, font, out _);
        }

        private static TMP_Text EnsureText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment, TMP_FontAsset font, out bool created)
        {
            GameObject root = EnsureObject(parent, name, out created);
            bool componentMissing = root.GetComponent<TMP_Text>() == null;
            TMP_Text text = GetOrAdd<TextMeshProUGUI>(root);
            if (created || componentMissing)
            {
                text.text = value;
                text.fontSize = size;
                text.alignment = alignment;
                text.color = new Color32(235, 230, 214, 255);
                text.enableWordWrapping = true;
                text.raycastTarget = false;
                if (font != null) text.font = font;
            }
            return text;
        }

        private static GameObject EnsureObject(Transform parent, string name)
        {
            return EnsureObject(parent, name, out _);
        }

        private static GameObject EnsureObject(Transform parent, string name, out bool created)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                created = false;
                return child.gameObject;
            }
            GameObject result = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(result, "Create " + name);
            result.transform.SetParent(parent, false);
            created = true;
            return result;
        }

        private static GameObject CreateUiObject(string name, Transform parent) => EnsureObject(parent, name);
        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }
        private static void Set(SerializedObject target, string name, Object value)
        {
            SerializedProperty property = target.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }
        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
#endif
