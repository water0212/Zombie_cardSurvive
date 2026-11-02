using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.UI.Morale;
using ZombieCardSurvive.UI.Morale.Editor;
using ZombieCardSurvive.UI.World;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class MoraleHudE1SetupUtilityTests
    {
        [Test]
        public void ApplyTwice_RepairsMissingChildAndPreservesExistingLayout()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            GameObject baseHud = new GameObject("BaseHud", typeof(RectTransform));
            baseHud.transform.SetParent(canvasObject.transform, false);
            GameplayHudRuntimePresenter basePresenter = baseHud.AddComponent<GameplayHudRuntimePresenter>();
            GameObject moralePanel = new GameObject("MoralePanel", typeof(RectTransform));
            moralePanel.transform.SetParent(baseHud.transform, false);
            TMP_Text oldMorale = CreateText(moralePanel.transform, "Morale");
            TMP_Text oldThreshold = CreateText(moralePanel.transform, "Threshold");
            GameObject resourceBar = new GameObject("ResourceBar", typeof(RectTransform));
            resourceBar.transform.SetParent(baseHud.transform, false);
            TMP_Text food = CreateText(resourceBar.transform, "Food");
            TMP_Text resource = CreateText(resourceBar.transform, "Resource");
            TMP_Text combat = CreateText(resourceBar.transform, "Combat");
            SerializedObject serializedBase = new SerializedObject(basePresenter);
            serializedBase.FindProperty("moraleText").objectReferenceValue = oldMorale;
            serializedBase.FindProperty("moraleThresholdText").objectReferenceValue = oldThreshold;
            serializedBase.FindProperty("foodText").objectReferenceValue = food;
            serializedBase.FindProperty("resourceText").objectReferenceValue = resource;
            serializedBase.FindProperty("combatText").objectReferenceValue = combat;
            serializedBase.ApplyModifiedPropertiesWithoutUndo();
            GameObject phaseOwner = new GameObject("RunPhase", typeof(RectTransform));
            phaseOwner.transform.SetParent(canvasObject.transform, false);
            phaseOwner.SetActive(false);
            RunPhaseController phase = phaseOwner.AddComponent<RunPhaseController>();
            SerializedObject serializedPhase = new SerializedObject(phase);
            serializedPhase.FindProperty("startRunOnAwake").boolValue = false;
            serializedPhase.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                MoraleHudE1SetupUtility.ApplyToCurrentScene();
                Transform root = moralePanel.transform.Find("PB_P0_014E_E1_MoraleHUD");
                Assert.That(root, Is.Not.Null);
                Assert.That(oldMorale.gameObject.activeSelf, Is.False);
                Assert.That(oldThreshold.gameObject.activeSelf, Is.False);
                Assert.That(food.transform.Find("PB_P0_014E_FoodWarning"), Is.Not.Null);
                Assert.That(resource.transform.Find("PB_P0_014E_ResourceWarning"), Is.Not.Null);
                Assert.That(combat.transform.Find("PB_P0_014E_CombatWarning"), Is.Not.Null);
                Assert.That(
                    food.transform.Find("PB_P0_014E_FoodWarning").GetComponent<RectTransform>().anchoredPosition.y,
                    Is.Not.EqualTo(resource.transform.Find("PB_P0_014E_ResourceWarning").GetComponent<RectTransform>().anchoredPosition.y));
                Transform detail = canvasObject.transform.Find("PB_P0_014E_MoraleDetail");
                Assert.That(detail.GetComponent<Image>().raycastTarget, Is.False);
                RectTransform rootRect = (RectTransform)root;
                rootRect.anchoredPosition = new Vector2(123f, -77f);

                Transform confirmation = canvasObject.transform.Find("PB_P0_014E_DangerConfirmation");
                Transform continueButton = confirmation.Find("Paper/ContinueButton");
                Object.DestroyImmediate(continueButton.gameObject);

                MoraleHudE1SetupUtility.ApplyToCurrentScene();

                Assert.That(canvasObject.transform.Find("PB_P0_014E_DangerConfirmation/Paper/ContinueButton"), Is.Not.Null);
                Assert.That(rootRect.anchoredPosition, Is.EqualTo(new Vector2(123f, -77f)));
                MoraleHudPresenter presenter = root.GetComponent<MoraleHudPresenter>();
                Assert.That(presenter, Is.Not.Null);
                SerializedObject serializedPresenter = new SerializedObject(presenter);
                Assert.That(serializedPresenter.FindProperty("continueButton").objectReferenceValue, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void PresenterWithoutCompleteModal_DoesNotClaimConfirmationCapability()
        {
            GameObject owner = new GameObject("IncompleteMoralePresenter");
            owner.SetActive(false);
            RunPhaseController phase = owner.AddComponent<RunPhaseController>();
            MoraleHudPresenter presenter = owner.AddComponent<MoraleHudPresenter>();
            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("runPhaseController").objectReferenceValue = phase;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                Assert.That(presenter.CanHandleRoundEndConfirmation, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static TMP_Text CreateText(Transform parent, string name)
        {
            GameObject owner = new GameObject(name, typeof(RectTransform));
            owner.transform.SetParent(parent, false);
            return owner.AddComponent<TextMeshProUGUI>();
        }
    }
}
