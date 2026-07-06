using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;

namespace ZombieCardSurvive.Systems
{
    public static class FoodSystem
    {
        private static readonly List<TurnProductionEffect> productionEffects = new List<TurnProductionEffect>();
        private static int food;
        private static int temporaryFood;
        private static int storageCapacity = 50;

        public static int Food => food;
        public static int TemporaryFood => temporaryFood;
        public static int AvailableFood => ResourceReservationService.AvailableFood;
        public static int ReservedFood => ResourceReservationService.ReservedFood;
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

        public static void AddTemporaryFood(int amount)
        {
            temporaryFood = Mathf.Max(0, temporaryFood + Mathf.Max(0, amount));
        }

        public static void SetStorageCapacity(int capacity)
        {
            storageCapacity = Mathf.Max(0, capacity);
        }

        public static bool SpendFood(int amount)
        {
            int cost = Mathf.Max(0, amount);
            if (AvailableFood < cost)
            {
                return false;
            }

            food -= cost;
            return true;
        }

        public static void Reset(int startingFood)
        {
            food = Mathf.Max(0, startingFood);
            temporaryFood = 0;
            productionEffects.Clear();
        }

        public static RoundFoodConsumptionResult PreviewRoundFoodConsumption(int amount)
        {
            int requested = Mathf.Max(0, amount);
            int temporaryBefore = temporaryFood;
            int temporaryConsumed = Mathf.Min(temporaryBefore, requested);
            int remaining = requested - temporaryConsumed;
            int permanentConsumed = Mathf.Min(AvailableFood, remaining);
            int shortage = remaining - permanentConsumed;
            int temporaryDiscarded = temporaryBefore - temporaryConsumed;

            return new RoundFoodConsumptionResult(
                requested,
                food,
                food - permanentConsumed,
                temporaryBefore,
                temporaryConsumed,
                permanentConsumed,
                shortage,
                temporaryDiscarded);
        }

        public static RoundFoodConsumptionResult ConsumeRoundFoodAllowShortage(int amount)
        {
            RoundFoodConsumptionResult result = PreviewRoundFoodConsumption(amount);
            food -= result.PermanentConsumed;
            temporaryFood = 0;
            return result;
        }

        public static int ConsumeFoodAllowShortage(int amount)
        {
            int cost = Mathf.Max(0, amount);
            int paid = Mathf.Min(AvailableFood, cost);
            food -= paid;
            return cost - paid;
        }

        public static int TrimToStorageCapacity()
        {
            int protectedCapacity = Mathf.Max(storageCapacity, ReservedFood);
            if (food <= protectedCapacity)
            {
                return 0;
            }

            int removed = food - protectedCapacity;
            food = protectedCapacity;
            return removed;
        }

        public static int PreviewFoodAfterConsumption(int amount)
        {
            int paid = Mathf.Min(AvailableFood, Mathf.Max(0, amount));
            return Mathf.Max(0, food - paid);
        }

        internal static bool SpendReservedFood(int amount)
        {
            int cost = Mathf.Max(0, amount);
            if (food < cost)
            {
                return false;
            }

            food -= cost;
            return true;
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
