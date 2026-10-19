using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
namespace ZombieCardSurvive.Buildings.UI
{
    public class CraftJobRowView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Image artwork;
        [SerializeField] private TMP_Text hintText;
        private BuildingSystem system;
        private BuildingRuntime building;
        private CraftLineRuntime line;
        private CraftJobRuntime job;
        private Action<string> selected;
        public void Bind(BuildingSystem owner, BuildingRuntime target, CraftLineRuntime craftLine, CraftJobRuntime value)
        { Bind(owner, target, craftLine, value, null, false); }
        public void Bind(BuildingSystem owner, BuildingRuntime target, CraftLineRuntime craftLine, CraftJobRuntime value, Action<string> onSelect, bool isSelected)
        {
            system = owner; building = target; line = craftLine; job = value; selected = onSelect;
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (descriptionText != null) descriptionText.text = job == null ? "製作工作" : $"{(isSelected ? "▶ " : "")}{job.Recipe?.DisplayName}\n{job.CurrentProduction}/{job.RequiredProduction}\n{(job.State == BuildingJobState.CancelPending ? "待取消" : StateLabel(job.State))}";
            if (hintText != null) hintText.text = "左鍵查看／右鍵取消";
            if (artwork != null) { artwork.sprite = job?.Recipe?.Artwork != null ? job.Recipe.Artwork : job?.Recipe?.OutputCard?.Artwork; artwork.gameObject.SetActive(artwork.sprite != null); }
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (job == null) return;
            if (eventData.button == PointerEventData.InputButton.Left) selected?.Invoke(job.JobId);
            else if (eventData.button == PointerEventData.InputButton.Right && system != null && system.CanSubmitActions && building?.Data != null && line?.Data != null && job.State != BuildingJobState.CancelPending)
                system.RequestCancelCraft(building.Data.BuildingId, line.Data.InteractionId, job.JobId);
        }
        public static string StateLabel(BuildingJobState state)
        {
            switch (state)
            {
                case BuildingJobState.CancelPending: return "待取消（結算時解除保留）";
                case BuildingJobState.InProgress: return "製作中";
                case BuildingJobState.Completed: return "完成";
                case BuildingJobState.Cancelled: return "已取消";
                default: return "等待製作";
            }
        }
    }
}
