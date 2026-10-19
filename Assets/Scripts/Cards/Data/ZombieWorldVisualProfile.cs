using UnityEngine;

namespace ZombieCardSurvive.Cards.Data
{
    /// <summary>殭屍卡圖以外、專供世界物件使用的視覺資料。</summary>
    [CreateAssetMenu(fileName = "ZombieWorldVisualProfile", menuName = "Zombie Card Survive/Cards/Zombie World Visual Profile")]
    public sealed class ZombieWorldVisualProfile : ScriptableObject
    {
        [SerializeField] private ZombieCardData sourceCard;
        [SerializeField] private Sprite worldSprite;
        [SerializeField] private Vector2 labelOffset = new Vector2(0f, 1.15f);
        [SerializeField] private float worldScale = 1f;

        public ZombieCardData SourceCard => sourceCard;
        public Sprite WorldSprite => worldSprite;
        public Vector2 LabelOffset => labelOffset;
        public float WorldScale => Mathf.Max(0.01f, worldScale);
    }
}
