using System;
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
        private static int baseStorageCapacity = 50;
        private static int storageCapacityBonus;

        public static event Action StateChanged;

        public static int Food => food;
        public static int TemporaryFood => temporaryFood;
        public static int AvailableFood => ResourceReservationService.AvailableFood;
        public static int ReservedFood => ResourceReservationService.ReservedFood;
        public static int BaseStorageCapacity => baseStorageCapacity;
        public static int StorageCapacityBonus => storageCapacityBonus;
        public static int StorageCapacity => Mathf.Max(0, baseStorageCapacity + storageCapacityBonus);
        public static IReadOnlyList<TurnProductionEffect> ProductionEffects => productionEffects;

        public static int GetFood()
        {
            return food;
        }

        public static void AddFood(int amount)
        {
            food = AddClamped(food, amount);
            NotifyStateChanged();
        }

        public static void AddTemporaryFood(int amount)
        {
            temporaryFood = AddClamped(temporaryFood, Mathf.Max(0, amount));
            NotifyStateChanged();
        }

        public static void SetStorageCapacity(int capacity)
        {
            baseStorageCapacity = Mathf.Max(0, capacity);
            NotifyStateChanged();
        }

        public static void SetStorageCapacityBonus(int bonus)
        {
            storageCapacityBonus = Mathf.Max(0, bonus);
            NotifyStateChanged();
        }

        public static bool SpendFood(int amount)
        {
            int cost = Mathf.Max(0, amount);
            if (AvailableFood < cost)
            {
                return false;
            }

            food -= cost;
            NotifyStateChanged();
            return true;
        }

        public static void Reset(int startingFood)
        {
            food = Mathf.Max(0, startingFood);
            temporaryFood = 0;
            productionEffects.Clear();
            NotifyStateChanged();
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
            NotifyStateChanged();
            return result;
        }

        public static int ConsumeFoodAllowShortage(int amount)
        {
            return ConsumePermanentFoodAllowShortage(amount).Shortage;
        }

        public static ValueConsumptionResult ConsumePermanentFoodAllowShortage(int amount)
        {
            int requested = Mathf.Max(0, amount);
            int consumed = Mathf.Min(AvailableFood, requested);
            food -= consumed;
            NotifyStateChanged();
            return new ValueConsumptionResult(requested, consumed);
        }

        public static int TrimToStorageCapacity()
        {
            int protectedCapacity = Mathf.Max(StorageCapacity, ReservedFood);
            if (food <= protectedCapacity)
            {
                return 0;
            }

            int removed = food - protectedCapacity;
            food = protectedCapacity;
            NotifyStateChanged();
            return removed;
        }

        public static int PreviewFoodAfterConsumption(int amount)
        {
            int paid = Mathf.Min(AvailableFood, Mathf.Max(0, amount));
            return Mathf.Max(0, food - paid);
        }

        internal static bool SpendReservedFood(int amount, bool notifyStateChanged = true)
        {
            int cost = Mathf.Max(0, amount);
            if (food < cost)
            {
                return false;
            }

            food -= cost;
            if (notifyStateChanged)
            {
                NotifyStateChanged();
            }
            return true;
        }

        public static TurnProductionEffect AddProductionEffect(string id, int amountPerTurn, bool isPermanent = true, int remainingTurns = 0)
        {
            TurnProductionEffect effect = new TurnProductionEffect(id, amountPerTurn, isPermanent, remainingTurns);
            productionEffects.Add(effect);
            NotifyStateChanged();
            return effect;
        }

        public static bool RemoveProductionEffect(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            int removedCount = productionEffects.RemoveAll(effect => effect.Id == id);
            if (removedCount > 0)
            {
                NotifyStateChanged();
            }
            return removedCount > 0;
        }

        public static void ApplyTurnStartProduction()
        {
            if (productionEffects.Count == 0)
            {
                return;
            }

            for (int i = productionEffects.Count - 1; i >= 0; i--)
            {
                TurnProductionEffect effect = productionEffects[i];
                food = AddClamped(food, effect.AmountPerTurn);
                effect.Tick();

                if (effect.IsExpired)
                {
                    productionEffects.RemoveAt(i);
                }
            }

            NotifyStateChanged();
        }

        internal static void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        private static int AddClamped(int current, int delta)
        {
            long value = (long)current + delta;
            return (int)Math.Max(0, Math.Min(int.MaxValue, value));
        }
    }
}
