using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Exploration.Data;
using ZombieCardSurvive.Exploration.Runtime;

namespace ZombieCardSurvive.Exploration.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ExplorationFlowView : MonoBehaviour
    {
        [SerializeField] private ExplorationSystem explorationSystem;
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text resultSummaryText;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private ExplorationRegionCardView[] regionCards;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (explorationSystem == null)
            {
                explorationSystem = FindObjectOfType<ExplorationSystem>();
            }
        }

        private void Reset()
        {
            root = gameObject;
            canvasGroup = GetComponent<CanvasGroup>();
            explorationSystem = FindObjectOfType<ExplorationSystem>();
            regionCards = GetComponentsInChildren<ExplorationRegionCardView>(true);
            closeButton = GetComponentInChildren<Button>(true);
        }

        private void OnEnable()
        {
            if (explorationSystem != null)
            {
                explorationSystem.ExplorationChanged += Refresh;
            }

            HookCloseButton();
            Refresh();
        }

        private void OnDisable()
        {
            if (explorationSystem != null)
            {
                explorationSystem.ExplorationChanged -= Refresh;
            }

            UnhookCloseButton();
            ClearRegionActions();
        }

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            if (explorationSystem == null || explorationSystem.State == ExplorationFlowState.Hidden)
            {
                SetRootVisible(false);
                return;
            }

            SetRootVisible(true);
            if (explorationSystem.State == ExplorationFlowState.ChoosingRegion)
            {
                ApplyChoosingState();
                return;
            }

            ApplyResultState();
        }

        private void ApplyChoosingState()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            SetText(titleText, "選擇探索地區");
            SetText(descriptionText, "選擇一個地區，獲得對應卡包並承擔探索風險。");
            SetCloseVisible(false);

            int viewCount = regionCards != null ? regionCards.Length : 0;
            int offerCount = explorationSystem.CurrentOffers != null ? explorationSystem.CurrentOffers.Count : 0;
            for (int i = 0; i < viewCount; i++)
            {
                ExplorationRegionCardView regionCard = regionCards[i];
                if (regionCard == null)
                {
                    continue;
                }

                if (i < offerCount)
                {
                    int index = i;
                    ExplorationRegionData region = explorationSystem.CurrentOffers[i];
                    RegionRuntime runtime = explorationSystem.GetRegionRuntime(region.RegionId);
                    regionCard.Bind(region, runtime, () => ChooseRegion(index));
                }
                else
                {
                    regionCard.Bind(null, null);
                }
            }
        }

        private void ApplyResultState()
        {
            string regionName = explorationSystem.SelectedRegion != null
                ? (!string.IsNullOrWhiteSpace(explorationSystem.SelectedRegion.DisplayName) ? explorationSystem.SelectedRegion.DisplayName : explorationSystem.SelectedRegion.name)
                : "探索結果";

            SetText(titleText, regionName);
            bool hasResultPanel = resultPanel != null && resultSummaryText != null;
            if (resultPanel != null) resultPanel.SetActive(hasResultPanel);
            SetText(descriptionText, hasResultPanel ? "探索已完成，以下是本次收穫與遭遇。內容較長時可使用滾輪查看。" : BuildResultSummary());
            if (hasResultPanel) SetText(resultSummaryText, BuildResultSummary());
            SetCloseVisible(true);
            SetRegionCardsVisible(false);
        }

        private string BuildResultSummary()
        {
            string summary = explorationSystem.LastResult != null
                ? explorationSystem.LastResult.BuildSummary()
                : "探索已結束。";

            ExplorationRegionData selectedRegion = explorationSystem.SelectedRegion;
            RegionRuntime runtime = selectedRegion != null
                ? explorationSystem.GetRegionRuntime(selectedRegion.RegionId)
                : null;
            if (runtime == null || !ReferenceEquals(runtime.Data, selectedRegion))
            {
                return summary;
            }

            string status = ExplorationTextUtility.FormatRegionStatus(
                runtime.ResourceState,
                runtime.RelativeDangerLevel);
            return $"{summary}\n\n探索後狀態：{status}";
        }

        private void ChooseRegion(int index)
        {
            if (explorationSystem != null)
            {
                explorationSystem.ChooseRegion(index);
            }
        }

        private void Close()
        {
            if (explorationSystem != null)
            {
                explorationSystem.Hide();
            }
        }

        private void HookCloseButton()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        private void UnhookCloseButton()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        private void ClearRegionActions()
        {
            if (regionCards == null)
            {
                return;
            }

            foreach (ExplorationRegionCardView regionCard in regionCards)
            {
                if (regionCard != null)
                {
                    regionCard.ClearClickAction();
                }
            }
        }

        private void SetRegionCardsVisible(bool visible)
        {
            if (regionCards == null)
            {
                return;
            }

            foreach (ExplorationRegionCardView regionCard in regionCards)
            {
                if (regionCard != null)
                {
                    regionCard.SetVisible(visible);
                }
            }
        }

        private void SetCloseVisible(bool visible)
        {
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(visible);
            }
        }

        private void SetRootVisible(bool visible)
        {
            GameObject targetRoot = root != null ? root : gameObject;
            if (targetRoot != gameObject)
            {
                targetRoot.SetActive(visible);
                return;
            }

            SetAlpha(visible ? 1f : 0f);
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                canvasGroup.interactable = alpha > 0.99f;
                canvasGroup.blocksRaycasts = alpha > 0.99f;
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
