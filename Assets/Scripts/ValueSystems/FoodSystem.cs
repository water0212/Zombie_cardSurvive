using System.Collections.Generic;
using UnityEngine;

namespace ZombieCardSurvive.Systems
{
    public static class FoodSystem
    {
        private static readonly List<TurnProductionEffect> productionEffects = new List<TurnProductionEffect>();
        private static int food;
        private static int storageCapacity = 50;

        public static int Food => food;
        public static int StorageCapacity => storageCapacity;
        public static IReadOnlyList<TurnProductionEffect> ProductionEffects => productionEffects;

        public static int GetFood()
        {
            return food;
        }

        public static void AddFood(int amount)
        {
            food = Mathf.Max(0, food + amount);
        }

        public static void SetStorageCapacity(int capacity)
        {
            storageCapacity = Mathf.Max(0, capacity);
        }

        public static bool SpendFood(int amount)
        {
            int cost = Mathf.Max(0, amount);
            if (food < cost)
            {
                return false;
            }

            food -= cost;
            return true;
        }

        public static void Reset(int startingFood)
        {
            food = Mathf.Max(0, startingFood);
            productionEffects.Clear();
        }

        public static int ConsumeFoodAllowShortage(int amount)
        {
            int cost = Mathf.Max(0, amount);
            int paid = Mathf.Min(food, cost);
            food -= paid;
            return cost - paid;
        }

        public static int TrimToStorageCapacity()
        {
            if (food <= storageCapacity)
            {
                return 0;
            }

            int removed = food - storageCapacity;
            food = storageCapacity;
            return removed;
        }

        public static int PreviewFoodAfterConsumption(int amount)
        {
            return Mathf.Max(0, food - Mathf.Max(0, amount));
        }

        public static TurnProductionEffect AddProductionEffect(string id, int amountPerTurn, bool isPermanent = true, int remainingTurns = 0)
        {
            TurnProductionEffect effect = new TurnProductionEffect(id, amountPerTurn, isPermanent, remainingTurns);
            productionEffects.Add(effect);
            return effect;
        }

        public static bool RemoveProductionEffect(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            int removedCount = productionEffects.RemoveAll(effect => effect.Id == id);
            return removedCount > 0;
        }

        public static void ApplyTurnStartProduction()
        {
            for (int i = productionEffects.Count - 1; i >= 0; i--)
            {
                TurnProductionEffect effect = productionEffects[i];
                AddFood(effect.AmountPerTurn);
                effect.Tick();

                if (effect.IsExpired)
                {
                    productionEffects.RemoveAt(i);
                }
            }
        }
    }
}
