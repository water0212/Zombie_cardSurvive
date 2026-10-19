using UnityEngine;

namespace ZombieCardSurvive.Cards.UI.Zombies
{
    public static class ZombieThreatLayoutCalculator
    {
        public static ZombieThreatLayoutSlot CalculateSlot(
            int index,
            int columns,
            Vector2 origin,
            Vector2 spacing,
            float overflowRowScale)
        {
            int safeIndex = Mathf.Max(0, index);
            int safeColumns = Mathf.Max(1, columns);
            int row = safeIndex / safeColumns;
            int column = safeIndex % safeColumns;
            int columnOffset = GetCenterOutColumnOffset(column);
            float scale = row == 0 ? 1f : Mathf.Max(0.1f, overflowRowScale);
            Vector2 position = origin + new Vector2(columnOffset * spacing.x, row * spacing.y);

            return new ZombieThreatLayoutSlot(position, scale, row, columnOffset);
        }

        private static int GetCenterOutColumnOffset(int column)
        {
            if (column <= 0)
            {
                return 0;
            }

            int distance = (column + 1) / 2;
            return column % 2 == 1 ? -distance : distance;
        }
    }
}
