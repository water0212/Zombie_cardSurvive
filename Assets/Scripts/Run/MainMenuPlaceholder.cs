using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Run
{
    [DefaultExecutionOrder(-10000)]
    public sealed class MainMenuPlaceholder : MonoBehaviour
    {
        [SerializeField] private MoralePyramidProfileData[] profiles;
        [SerializeField] private TMP_Dropdown profileDropdown;
        [SerializeField] private Button startButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private TMP_Text statusText;
        private RunLifecycleController flow;
        private string selectionError;
        private void Awake() { flow = RunLifecycleController.Ensure(); }
        private void OnEnable()
        {
            flow.Changed += Refresh;
            startButton.onClick.AddListener(StartSelected);
            retryButton.onClick.AddListener(Retry);
            profileDropdown.ClearOptions();
            var names = new System.Collections.Generic.List<string>();
            if (profiles != null) foreach (var profile in profiles) names.Add(profile == null ? "Missing profile" : profile.DisplayName);
            profileDropdown.AddOptions(names);
            profileDropdown.value = 0;
            Refresh();
        }
        private void Start() { flow.EnterMenu(); }
        private void OnDisable()
        {
            if (flow != null) flow.Changed -= Refresh;
            startButton.onClick.RemoveListener(StartSelected);
            retryButton.onClick.RemoveListener(Retry);
        }
        private void StartSelected()
        {
            var selection = profiles != null && profileDropdown.value < profiles.Length ? profiles[profileDropdown.value] : null;
            if (!flow.TryStart(selection, out selectionError)) Refresh();
        }
        private void Retry() { selectionError = null; flow.RetryMenu(); }
        private void Refresh()
        {
            bool ready = flow.State == RunLifecycleState.MenuReady;
            profileDropdown.interactable = ready;
            startButton.interactable = ready;
            retryButton.gameObject.SetActive(flow.State == RunLifecycleState.Error);
            statusText.text = string.IsNullOrEmpty(selectionError) ? flow.Status : selectionError;
        }
    }
}
