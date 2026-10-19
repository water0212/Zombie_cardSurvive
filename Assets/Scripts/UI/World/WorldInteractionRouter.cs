using UnityEngine;
using UnityEngine.EventSystems;

namespace ZombieCardSurvive.UI.World
{
    /// <summary>
    /// 世界物件唯一的滑鼠入口。先讓 Canvas UI 取得輸入，再依排序與優先度挑選世界物件。
    /// </summary>
    public sealed class WorldInteractionRouter : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private LayerMask interactionLayers = ~0;
        [SerializeField] private bool inputLocked;

        private readonly Collider2D[] hits = new Collider2D[32];
        private IWorldInteractionTarget hovered;

        public static WorldInteractionRouter Current { get; private set; }
        public bool IsInputLocked => inputLocked || FlowInspectionController.IsInspectingGameplay;

        public void Configure(Camera cameraValue, LayerMask layers)
        {
            worldCamera = cameraValue;
            interactionLayers = layers;
        }

        private void Awake()
        {
            Current = this;
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            if (IsInputLocked)
            {
                SetHovered(null);
            }
        }

        private void Update()
        {
            if (IsInputLocked || worldCamera == null || IsPointerOverCanvas())
            {
                SetHovered(null);
                return;
            }

            Vector3 screen = Input.mousePosition;
            Vector3 world = worldCamera.ScreenToWorldPoint(screen);
            IWorldInteractionTarget target = FindTarget(world);
            SetHovered(target);

            if (target == null || !target.CanInteract)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                target.OnPrimaryInteract();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                target.OnSecondaryInteract();
            }
        }

        private bool IsPointerOverCanvas()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private IWorldInteractionTarget FindTarget(Vector2 worldPoint)
        {
            int count = Physics2D.OverlapPointNonAlloc(worldPoint, hits, interactionLayers);
            IWorldInteractionTarget best = null;
            int bestPriority = int.MinValue;

            for (int index = 0; index < count; index++)
            {
                Collider2D hit = hits[index];
                if (hit == null)
                {
                    continue;
                }

                IWorldInteractionTarget candidate = hit.GetComponentInParent<IWorldInteractionTarget>();
                if (candidate == null || !candidate.CanInteract || candidate.InteractionPriority < bestPriority)
                {
                    continue;
                }

                best = candidate;
                bestPriority = candidate.InteractionPriority;
            }

            return best;
        }

        private void SetHovered(IWorldInteractionTarget next)
        {
            if (ReferenceEquals(hovered, next))
            {
                return;
            }

            hovered?.SetHovered(false);
            hovered = next;
            hovered?.SetHovered(true);
        }
    }
}
