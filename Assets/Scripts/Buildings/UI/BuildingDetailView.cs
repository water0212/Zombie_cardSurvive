using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;

namespace ZombieCardSurvive.Buildings.UI
{
    public class BuildingDetailView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text requirementText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Button constructButton;
        [SerializeField] private Button cancelConstructionButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform interactionRoot;
        [SerializeField] private CardCraftView cardCraftPrefab;

        private readonly List<CardCraftView> generatedCraftViews = new List<CardCraftView>();
        private BuildingSystem system;
        private BuildingRuntime runtime;

        private void Awake()
        {
            constructButton?.onClick.AddListener(StartConstruction);
            cancelConstructionButton?.onClick.AddListener(CancelConstruction);
            closeButton?.onClick.AddListener(Close);
            SetVisible(false);
        }

        private void OnDestroy()
        {
            constructButton?.onClick.RemoveListener(StartConstruction);
            cancelConstructionButton?.onClick.RemoveListener(CancelConstruction);
            closeButton?.onClick.RemoveListener(Close);
            UnhookSystem();
        }

        public void Bind(BuildingSystem owner, BuildingRuntime buildingRuntime)
        {
            UnhookSystem();
            system = owner;
            runtime = buildingRuntime;
            if (system != null)
            {
                system.BuildingsChanged += Refresh;
            }

            SetVisible(runtime != null);
            Refresh();
        }

        public void RefreshIfBound(BuildingRuntime target)
        {
            if (ReferenceEquals(runtime, target))
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            if (runtime?.Data == null)
            {
                Close();
                return;
            }

            if (titleText != null)
            {
                titleText.text = runtime.Data.DisplayName;
            }

            if (statusText != null)
            {
                statusText.text = $"狀態：{GetStateLabel(runtime.State)}　等級：{runtime.CurrentLevel}";
            }

            BuildingLevelData next = runtime.GetNextLevel();
            if (requirementText != null)
            {
                requirementText.text = next != null
                    ? $"下一級：進度 {next.RequiredConstructionProgress}　食物 {next.ConstructionCost.Food}　資源 {next.ConstructionCost.Resource}"
                    : "已達最高等級";
            }

            if (rewardText != null)
            {
                BuildingLevelData current = runtime.Data.GetLevel(runtime.CurrentLevel);
                rewardText.text = current != null ? current.RewardDescription : "解鎖後可開始施工";
            }

            bool canStart = system != null && system.CanSubmitActions
                && next != null
                && (runtime.State == BuildingState.Vacant || runtime.State == BuildingState.Active);
            if (constructButton != null)
            {
                constructButton.gameObject.SetActive(next != null && (runtime.State == BuildingState.Vacant || runtime.State == BuildingState.Active));
                constructButton.interactable = canStart;
            }

            bool canCancel = system != null && system.CanSubmitActions
                && (runtime.State == BuildingState.Constructing || runtime.State == BuildingState.Upgrading);
            if (cancelConstructionButton != null)
            {
                cancelConstructionButton.gameObject.SetActive(runtime.State == BuildingState.Constructing || runtime.State == BuildingState.Upgrading);
                cancelConstructionButton.interactable = canCancel;
            }

            RebuildInteractions();
        }

        public void Close()
        {
            UnhookSystem();
            runtime = null;
            system = null;
            SetVisible(false);
            DestroyCraftViews();
        }

        private void StartConstruction()
        {
            if (runtime?.Data != null)
            {
                system?.TryStartConstruction(runtime.Data.BuildingId);
            }
        }

        private void CancelConstruction()
        {
            if (runtime?.Data != null)
            {
                system?.RequestCancelConstruction(runtime.Data.BuildingId);
            }
        }

        private void RebuildInteractions()
        {
            DestroyCraftViews();
            if (interactionRoot == null || cardCraftPrefab == null || runtime == null)
            {
                return;
            }

            foreach (CraftLineRuntime line in runtime.CraftLines)
            {
                CardCraftView view = Instantiate(cardCraftPrefab, interactionRoot);
                view.Bind(system, runtime, line);
                generatedCraftViews.Add(view);
            }
        }

        private void DestroyCraftViews()
        {
            foreach (CardCraftView view in generatedCraftViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            generatedCraftViews.Clear();
        }

        private void UnhookSystem()
        {
            if (system != null)
            {
                system.BuildingsChanged -= Refresh;
            }
        }

        private void SetVisible(bool visible)
        {
            (root != null ? root : gameObject).SetActive(visible);
        }

        private static string GetStateLabel(BuildingState state)
        {
            switch (state)
            {
                case BuildingState.Vacant:
                    return "空地";
                case BuildingState.Constructing:
                    return "施工中";
                case BuildingState.Active:
                    return "運作中";
                case BuildingState.Upgrading:
                    return "升級中";
                case BuildingState.CancelPending:
                    return "等待取消";
                default:
                    return state.ToString();
            }
        }
    }
}
