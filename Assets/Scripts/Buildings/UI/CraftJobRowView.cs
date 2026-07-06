using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;

namespace ZombieCardSurvive.Buildings.UI
{
    public class CraftJobRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button cancelButton;

        private BuildingSystem system;
        private BuildingRuntime building;
        private CraftLineRuntime line;
        private CraftJobRuntime job;

        private void Awake()
        {
            cancelButton?.onClick.AddListener(Cancel);
        }

        private void OnDestroy()
        {
            cancelButton?.onClick.RemoveListener(Cancel);
        }

        public void Bind(BuildingSystem owner, BuildingRuntime buildingRuntime, CraftLineRuntime lineRuntime, CraftJobRuntime jobRuntime)
        {
            system = owner;
            building = buildingRuntime;
            line = lineRuntime;
            job = jobRuntime;

            if (descriptionText != null)
            {
                string name = job?.Recipe != null && !string.IsNullOrWhiteSpace(job.Recipe.DisplayName)
                    ? job.Recipe.DisplayName
                    : "製作工作";
                descriptionText.text = job != null
                    ? $"{name}  {job.CurrentProduction}/{job.RequiredProduction}"
                    : name;
            }

            if (cancelButton != null)
            {
                cancelButton.interactable = system != null && system.CanSubmitActions && job?.State != BuildingJobState.CancelPending;
            }
        }

        private void Cancel()
        {
            if (building?.Data != null && line?.Data != null && job != null)
            {
                system?.RequestCancelCraft(building.Data.BuildingId, line.Data.InteractionId, job.JobId);
            }
        }
    }
}
