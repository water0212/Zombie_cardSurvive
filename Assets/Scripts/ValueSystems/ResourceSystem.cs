using System.Collections.Generic;
using UnityEngine;

namespace ZombieCardSurvive.Systems
{
    public static class ResourceSystem
    {
        private static readonly List<TurnProductionEffect> productionEffects = new List<TurnProductionEffect>();
        private static int resource;

        public static int Resource => resource;
        public static IReadOnlyList<TurnProductionEffect> ProductionEffects => productionEffects;

        public static int GetResource()
        {
            return resource;
        }

        public static void AddResource(int amount)
        {
            resource = Mathf.Max(0, resource + amount);
        }

        public static bool SpendResource(int amount)
        {
            int cost = Mathf.Max(0, amount);
            if (resource < cost)
            {
                return false;
            }

            resource -= cost;
            return true;
        }

        public static void Reset(int startingResource)
        {
            resource = Mathf.Max(0, startingResource);
            productionEffects.Clear();
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
                AddResource(effect.AmountPerTurn);
                effect.Tick();

                if (effect.IsExpired)
                {
                    productionEffects.RemoveAt(i);
                }
            }
        }
    }
}
