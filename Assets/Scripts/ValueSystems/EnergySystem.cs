using UnityEngine;

namespace ZombieCardSurvive.Systems
{
    public static class EnergySystem
    {
        private static int currentEnergy;
        private static int maxEnergy;

        public static int CurrentEnergy => currentEnergy;
        public static int MaxEnergy => maxEnergy;

        public static void Reset(int maxEnergy)
        {
            EnergySystem.maxEnergy = Mathf.Max(0, maxEnergy);
            currentEnergy = EnergySystem.maxEnergy;
        }

        public static bool CanSpend(int amount)
        {
            return currentEnergy >= Mathf.Max(0, amount);
        }

        public static bool SpendEnergy(int amount)
        {
            int cost = Mathf.Max(0, amount);
            if (!CanSpend(cost))
            {
                return false;
            }

            currentEnergy -= cost;
            return true;
        }

        public static void AddEnergy(int amount)
        {
            currentEnergy = Mathf.Clamp(currentEnergy + Mathf.Max(0, amount), 0, maxEnergy);
        }

        public static void IncreaseMaxEnergy(int amount)
        {
            maxEnergy = Mathf.Max(0, maxEnergy + Mathf.Max(0, amount));
            currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        }

        public static void DecreaseMaxEnergy(int amount)
        {
            maxEnergy = Mathf.Max(0, maxEnergy - Mathf.Max(0, amount));
            currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        }

        public static void RefillToMax()
        {
            currentEnergy = maxEnergy;
        }
    }
}
