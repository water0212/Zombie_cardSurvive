using UnityEngine;

namespace ZombieCardSurvive.Buildings.Data
{
    /// <summary>建築卡圖以外、專供 SpriteRenderer 世界呈現的視覺資料。</summary>
    [CreateAssetMenu(fileName = "BuildingWorldVisualProfile", menuName = "Zombie Card Survive/Buildings/World Visual Profile")]
    public sealed class BuildingWorldVisualProfile : ScriptableObject
    {
        [SerializeField] private string buildingId;
        [SerializeField] private Sprite vacantSprite;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite constructingSprite;
        [SerializeField] private Vector2 labelOffset = new Vector2(0f, 1.2f);
        [SerializeField] private float worldScale = 1f;

        public string BuildingId => buildingId;
        public Vector2 LabelOffset => labelOffset;
        public float WorldScale => Mathf.Max(0.01f, worldScale);

        public Sprite ResolveSprite(BuildingState state)
        {
            if ((state == BuildingState.Constructing || state == BuildingState.Upgrading) && constructingSprite != null)
            {
                return constructingSprite;
            }
            if (state == BuildingState.Vacant && vacantSprite != null)
            {
                return vacantSprite;
            }
            return activeSprite != null ? activeSprite : vacantSprite;
        }
    }
}
