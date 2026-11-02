#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieCardSurvive.Cards.UI;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.UI.Settlement;
using ZombieCardSurvive.UI.Settlement.Editor;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class SampleE1E2IntegrationTests
    {
        [Test]
        public void ResourceSynchronizeBeforeAwake_PreservesVisibleAuthoredColour()
        {
            var go = new GameObject("Late Awake presenter"); go.SetActive(false);
            var number = new GameObject("Number", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var authored = new Color(.8f, .9f, .7f, 1f); number.color = authored;
            var view = go.AddComponent<ResourceValuePresenter>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("valueText").objectReferenceValue = number;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                view.Synchronize(ZombieCardSurvive.Systems.Transactions.ValueSnapshot.Capture());
                Assert.That(number.color, Is.EqualTo(authored));
                typeof(ResourceValuePresenter).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(view, null);
                view.Synchronize(ZombieCardSurvive.Systems.Transactions.ValueSnapshot.Capture());
                Assert.That(number.color, Is.EqualTo(authored));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(number.gameObject); }
        }
        [Test]
        public void LegacyNumericPolling_DoesNotOverwriteOwnedPresentation()
        {
            var go = new GameObject("Ownership test");
            var number = new GameObject("Number", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var debug = go.AddComponent<ResourceDebugView>();
            var serialized = new SerializedObject(debug);
            serialized.FindProperty("foodText").objectReferenceValue = number;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                debug.SetPrimaryValuesOwnedExternally(true);
                number.text = "animated intermediate value";
                debug.Refresh();
                Assert.That(number.text, Is.EqualTo("animated intermediate value"));
                debug.SetPrimaryValuesOwnedExternally(false);
                Assert.That(number.text, Is.Not.EqualTo("animated intermediate value"));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(number.gameObject); }
        }
        [Test]
        public void SavedIntegrationScene_PreservesRealHand_AndAllE1E2Bindings()
        {
            var scene = SceneManager.GetSceneByPath(SampleE1E2IntegrationSetupUtility.TestScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SampleE1E2IntegrationSetupUtility.TestScenePath, OpenSceneMode.Additive);
            try
            {
                Assert.That(SampleE1E2IntegrationSetupUtility.ValidateScene(scene), Is.Empty);
                Assert.That(SampleE1E2IntegrationSetupUtility.InScene<CardHandView>(scene).Length, Is.EqualTo(1));
                Assert.That(SampleE1E2IntegrationSetupUtility.InScene<ResourceValuePresenter>(scene).Select(view => view.Id).Distinct().Count(), Is.EqualTo(4));
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        [Test]
        public void ApplyingInstallerAgain_DoesNotDuplicateViewsOrControllers()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(SampleE1E2IntegrationSetupUtility.TestScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SampleE1E2IntegrationSetupUtility.TestScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                SampleE1E2IntegrationSetupUtility.ApplyToCurrentScene();
                SampleE1E2IntegrationSetupUtility.ApplyToCurrentScene();
                Assert.That(SampleE1E2IntegrationSetupUtility.ValidateScene(scene), Is.Empty);
                Assert.That(SampleE1E2IntegrationSetupUtility.InScene<SettlementPresenter>(scene).Length, Is.EqualTo(1));
                Assert.That(SampleE1E2IntegrationSetupUtility.InScene<ResourceValuePresenter>(scene).Length, Is.EqualTo(4));
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
#endif
