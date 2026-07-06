using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;

namespace ZombieCardSurvive.Buildings.UI
{
    public class BuildingWorldView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Button button;
        [SerializeField] private Image artworkImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Color vacantColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color constructionColor = new Color(0.85f, 0.65f, 0.25f, 1f);

        private BuildingSystem system;
        private BuildingRuntime runtime;

        public RectTransform RectTransform => rectTransform != null ? rectTransform : transform as RectTransform;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button?.onClick.AddListener(OpenDetails);
        }

        private void OnDestroy()
        {
            button?.onClick.RemoveListener(OpenDetails);
        }

        public void Bind(BuildingSystem owner, BuildingRuntime buildingRuntime)
        {
            system = owner;
            runtime = buildingRuntime;
            Refresh();
        }

        public void Refresh()
        {
            if (runtime?.Data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (nameText != null)
            {
                nameText.text = runtime.Data.DisplayName;
            }

            if (statusText != null)
            {
                statusText.text = BuildStatusText(runtime);
            }

            if (artworkImage != null)
            {
                BuildingLevelData level = runtime.Data.GetLevel(runtime.CurrentLevel);
                artworkImage.sprite = level?.Artwork != null ? level.Artwork : runtime.Data.Artwork;
                artworkImage.color = ResolveColor(level);
                artworkImage.enabled = artworkImage.sprite != null;
            }
        }

        private void OpenDetails()
        {
            system?.OpenDetails(runtime);
        }

        private Color ResolveColor(BuildingLevelData level)
        {
            switch (runtime.State)
            {
                case BuildingState.Vacant:
                    return vacantColor;
                case BuildingState.Constructing:
                case BuildingState.Upgrading:
                case BuildingState.CancelPending:
                    return constructionColor;
                default:
                    return level != null ? level.Color : runtime.Data.Color;
            }
        }

        private static string BuildStatusText(BuildingRuntime target)
        {
            switch (target.State)
            {
                case BuildingState.Vacant:
                    return "空地";
                case BuildingState.Constructing:
                    return BuildProgressText("施工中", target.ConstructionJob);
                case BuildingState.Upgrading:
                    return BuildProgressText($"Lv.{target.CurrentLevel} 升級中", target.ConstructionJob);
                case BuildingState.CancelPending:
                    return "等待取消";
                default:
                    return $"Lv.{target.CurrentLevel}";
            }
        }

        private static string BuildProgressText(string prefix, ConstructionJobRuntime job)
        {
            return job?.TargetLevel != null
                ? $"{prefix} {job.CurrentProgress}/{job.TargetLevel.RequiredConstructionProgress}"
                : prefix;
        }
    }
}
