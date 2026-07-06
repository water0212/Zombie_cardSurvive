using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
                    regionCard.Bind(explorationSystem.CurrentOffers[i], () => ChooseRegion(index));
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
            SetText(descriptionText, explorationSystem.LastResult != null ? explorationSystem.LastResult.BuildSummary() : "探索已結束。");
            SetCloseVisible(true);
            SetRegionCardsVisible(false);
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
