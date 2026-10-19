using TMPro;
using UnityEngine;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;

namespace ZombieCardSurvive.UI.World
{
    /// <summary>P0 SpriteRenderer 建築表現；正式資料綁定會在後續切片接到 BuildingRuntime。</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class BuildingWorldSpriteView : MonoBehaviour, IWorldInteractionTarget
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer selection;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private string buildingName = "情報中心";
        private BuildingRuntime runtime;

        public int InteractionPriority => 10;
        public bool CanInteract => isActiveAndEnabled;
        public GameObject InteractionObject => gameObject;

        public void Configure(SpriteRenderer bodyRenderer, SpriteRenderer selectionRenderer, TMP_Text details, string displayName)
        {
            body = bodyRenderer;
            selection = selectionRenderer;
            detailText = details;
            buildingName = displayName;
            SetHovered(false);
        }

        public void Bind(BuildingRuntime value, BuildingWorldVisualProfile profile)
        {
            runtime = value;
            if (runtime?.Data != null)
            {
                buildingName = runtime.Data.DisplayName;
            }
            if (profile != null)
            {
                if (body != null)
                {
                    body.sprite = profile.ResolveSprite(runtime != null ? runtime.State : BuildingState.Active);
                }
                transform.localScale = Vector3.one * profile.WorldScale;
            }
        }

        public void SetHovered(bool hovered)
        {
            if (selection != null)
            {
                selection.enabled = hovered;
            }
        }

        public void OnPrimaryInteract()
        {
            if (detailText != null)
            {
                detailText.text = runtime == null
                    ? $"{buildingName}\n左鍵：開啟建築操作（P0 驗證）"
                    : $"{buildingName}\n等級 {runtime.CurrentLevel}｜狀態 {runtime.State}";
            }
        }

        public void OnSecondaryInteract()
        {
            if (detailText != null)
            {
                detailText.text = $"{buildingName}\n世界物件：SpriteRenderer\n標籤與操作：Canvas UI";
            }
        }
    }
}
