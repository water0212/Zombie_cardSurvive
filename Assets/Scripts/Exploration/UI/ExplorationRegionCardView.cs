using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ZombieCardSurvive.Exploration.Data;
using ZombieCardSurvive.Exploration.Runtime;

namespace ZombieCardSurvive.Exploration.UI
{
    public class ExplorationRegionCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image artworkImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text dangerText;
        [SerializeField] private TMP_Text rewardsText;

        private UnityAction currentClickAction;

        public Button Button => button;

        private void Reset()
        {
            button = GetComponent<Button>();
            artworkImage = GetComponentInChildren<Image>(true);
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0)
            {
                nameText = texts[0];
            }

            if (texts.Length > 1)
            {
                descriptionText = texts[1];
            }

            if (texts.Length > 2)
            {
                dangerText = texts[2];
            }

            if (texts.Length > 3)
            {
                rewardsText = texts[3];
            }
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        public void Bind(ExplorationRegionData region, UnityAction clickAction)
        {
            Bind(region, null, clickAction);
        }

        public void Bind(ExplorationRegionData region, RegionRuntime runtime, UnityAction clickAction)
        {
            SetVisible(region != null);
            ClearClickAction();

            if (region == null)
            {
                return;
            }

            SetText(nameText, !string.IsNullOrWhiteSpace(region.DisplayName) ? region.DisplayName : region.name);
            SetText(descriptionText, region.Description);
            SetText(dangerText, BuildStatusText(region, runtime));
            SetText(rewardsText, "預期收益：" + region.BuildExpectedRewardsText());
            SetArtwork(region.Artwork);
            SetClickAction(clickAction);
        }

        private static string BuildStatusText(ExplorationRegionData region, RegionRuntime runtime)
        {
            if (runtime != null && ReferenceEquals(runtime.Data, region))
            {
                return ExplorationTextUtility.FormatRegionStatus(
                    runtime.ResourceState,
                    runtime.RelativeDangerLevel);
            }

            return "物資：未知｜危險：" + ExplorationTextUtility.GetDangerLabel(region.DangerLevel);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void ClearClickAction()
        {
            if (button != null && currentClickAction != null)
            {
                button.onClick.RemoveListener(currentClickAction);
            }

            currentClickAction = null;
        }

        private void SetClickAction(UnityAction action)
        {
            currentClickAction = action;
            if (button != null && currentClickAction != null)
            {
                button.onClick.AddListener(currentClickAction);
            }
        }

        private void SetArtwork(Sprite artwork)
        {
            if (artworkImage == null)
            {
                return;
            }

            artworkImage.sprite = artwork;
            artworkImage.enabled = artwork != null;
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
