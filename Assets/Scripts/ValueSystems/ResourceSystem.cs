using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;

namespace ZombieCardSurvive.Systems
{
    public static class ResourceSystem
    {
        private static readonly List<TurnProductionEffect> productionEffects = new List<TurnProductionEffect>();
        private static int resource;

        public static event Action StateChanged;

        public static int Resource => resource;
        public static int AvailableResource => ResourceReservationService.AvailableResource;
        public static int ReservedResource => ResourceReservationService.ReservedResource;
        public static IReadOnlyList<TurnProductionEffect> ProductionEffects => productionEffects;

        public static int GetResource()
        {
            return resource;
        }

        public static void AddResource(int amount)
        {
            resource = AddClamped(resource, amount);
            NotifyStateChanged();
        }

        public static bool SpendResource(int amount)
        {
            int cost = Mathf.Max(0, amount);
            if (AvailableResource < cost)
            {
                return false;
            }

            resource -= cost;
            NotifyStateChanged();
            return true;
        }

        public static ValueConsumptionResult ConsumeResourceAllowShortage(int amount)
        {
            int requested = Mathf.Max(0, amount);
            int consumed = Mathf.Min(AvailableResource, requested);
            resource -= consumed;
            NotifyStateChanged();
            return new ValueConsumptionResult(requested, consumed);
        }

        internal static bool SpendReservedResource(int amount, bool notifyStateChanged = true)
        {
            int cost = Mathf.Max(0, amount);
            if (resource < cost)
            {
                return false;
            }

            resource -= cost;
            if (notifyStateChanged)
            {
                NotifyStateChanged();
            }
            return true;
        }

        public static void Reset(int startingResource)
        {
            resource = Mathf.Max(0, startingResource);
            productionEffects.Clear();
            NotifyStateChanged();
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
                resource = AddClamped(resource, effect.AmountPerTurn);
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
