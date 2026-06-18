using UnityEngine;

namespace ZombieCardSurvive.Systems
{
    public static class CombatSystem
    {
        private static int pendingDamage;
        private static int zombieThreatCount;

        public static int PendingDamage => pendingDamage;
        public static int ZombieThreatCount => zombieThreatCount;

        public static void AddPendingDamage(int amount)
        {
            pendingDamage = Mathf.Max(0, pendingDamage + Mathf.Max(0, amount));
        }

        public static bool SpendPendingDamage(int amount)
        {
            int cost = Mathf.Max(0, amount);
            if (pendingDamage < cost)
            {
                return false;
            }

            pendingDamage -= cost;
            return true;
        }

        public static void ClearPendingDamage()
        {
            pendingDamage = 0;
        }

        public static void AddZombieThreat(int amount)
        {
            zombieThreatCount = Mathf.Max(0, zombieThreatCount + Mathf.Max(0, amount));
        }

        public static bool RemoveZombieThreat(int amount)
        {
            int value = Mathf.Max(0, amount);
            if (zombieThreatCount < value)
            {
                return false;
            }

            zombieThreatCount -= value;
            return true;
        }

        public static void Reset()
        {
            pendingDamage = 0;
            zombieThreatCount = 0;
        }
    }
}
