using UnityEngine;

namespace ZombieCardSurvive.Cards.UI.Zombies
{
    public readonly struct ZombieThreatLayoutSlot
    {
        public ZombieThreatLayoutSlot(Vector2 anchoredPosition, float scale, int row, int columnOffset)
        {
            AnchoredPosition = anchoredPosition;
            Scale = scale;
            Row = row;
            ColumnOffset = columnOffset;
        }

        public Vector2 AnchoredPosition { get; }
        public float Scale { get; }
        public int Row { get; }
        public int ColumnOffset { get; }
        public bool IsOverflowRow => Row > 0;
    }
}
