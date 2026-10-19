using System;
using UnityEngine;

namespace ZombieCardSurvive.Systems
{
    public static class EnergySystem
    {
        private static int currentEnergy;
        private static int baseMaxEnergy;
        private static int profileMaximumEnergyModifier;

        public static event Action StateChanged;

        public static int CurrentEnergy => currentEnergy;
        public static int BaseMaxEnergy => baseMaxEnergy;
        public static int ProfileMaximumEnergyModifier => profileMaximumEnergyModifier;
        public static int MaxEnergy => CalculateEffectiveMaximumEnergy();

        public static void Reset(int maxEnergy)
        {
            baseMaxEnergy = Mathf.Max(0, maxEnergy);
            profileMaximumEnergyModifier = 0;
            currentEnergy = MaxEnergy;
            NotifyStateChanged();
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
            NotifyStateChanged();
            return true;
        }

        public static void AddEnergy(int amount)
        {
            currentEnergy = Mathf.Clamp(currentEnergy + Mathf.Max(0, amount), 0, MaxEnergy);
            NotifyStateChanged();
        }

        public static void IncreaseMaxEnergy(int amount)
        {
            baseMaxEnergy = AddClamped(baseMaxEnergy, Mathf.Max(0, amount));
            currentEnergy = Mathf.Clamp(currentEnergy, 0, MaxEnergy);
            NotifyStateChanged();
        }

        public static void DecreaseMaxEnergy(int amount)
        {
            baseMaxEnergy = Mathf.Max(0, baseMaxEnergy - Mathf.Max(0, amount));
            currentEnergy = Mathf.Clamp(currentEnergy, 0, MaxEnergy);
            NotifyStateChanged();
        }

        public static void RefillToMax()
        {
            currentEnergy = MaxEnergy;
            NotifyStateChanged();
        }

        internal static void SetProfileMaximumEnergyModifier(int amount)
        {
            profileMaximumEnergyModifier = amount;
            currentEnergy = Mathf.Clamp(currentEnergy, 0, MaxEnergy);
            NotifyStateChanged();
        }

        private static int CalculateEffectiveMaximumEnergy()
        {
            long value = (long)baseMaxEnergy + profileMaximumEnergyModifier;
            return (int)Math.Max(0, Math.Min(int.MaxValue, value));
        }

        private static int AddClamped(int value, int amount)
        {
            long total = (long)value + amount;
            return (int)Math.Max(0, Math.Min(int.MaxValue, total));
        }

        private static void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
