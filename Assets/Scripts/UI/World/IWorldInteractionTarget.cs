using UnityEngine;

namespace ZombieCardSurvive.UI.World
{
    /// <summary>由世界物件提供給單一輸入路由器的互動介面。</summary>
    public interface IWorldInteractionTarget
    {
        int InteractionPriority { get; }
        bool CanInteract { get; }
        GameObject InteractionObject { get; }
        void SetHovered(bool hovered);
        void OnPrimaryInteract();
        void OnSecondaryInteract();
    }
}
