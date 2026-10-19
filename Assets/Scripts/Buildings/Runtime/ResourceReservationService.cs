using System;
using System.Collections.Generic;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Buildings.Runtime
{
    public readonly struct ResourceReservationToken
    {
        public ResourceReservationToken(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Id);
    }

    public static class ResourceReservationService
    {
        private static readonly Dictionary<string, ResourceCost> reservations = new Dictionary<string, ResourceCost>();

        public static event Action StateChanged;

        public static int ReservedFood { get; private set; }
        public static int ReservedResource { get; private set; }
        public static int AvailableFood => Math.Max(0, FoodSystem.GetFood() - ReservedFood);
        public static int AvailableResource => Math.Max(0, ResourceSystem.GetResource() - ReservedResource);

        public static bool TryReserve(ResourceCost cost, out ResourceReservationToken token)
        {
            token = default;
            if (AvailableFood < cost.Food || AvailableResource < cost.Resource)
            {
                return false;
            }

            string id = Guid.NewGuid().ToString("N");
            reservations[id] = cost;
            ReservedFood += cost.Food;
            ReservedResource += cost.Resource;
            token = new ResourceReservationToken(id);
            StateChanged?.Invoke();
            return true;
        }

        public static bool Commit(ResourceReservationToken token)
        {
            if (!TryGetCost(token, out ResourceCost cost))
            {
                return false;
            }

            if (FoodSystem.GetFood() < cost.Food || ResourceSystem.GetResource() < cost.Resource)
            {
                return false;
            }

            if (!FoodSystem.SpendReservedFood(cost.Food, notifyStateChanged: false)
                || !ResourceSystem.SpendReservedResource(cost.Resource, notifyStateChanged: false))
            {
                return false;
            }

            RemoveReservation(token.Id, cost, notifyStateChanged: false);
            FoodSystem.NotifyStateChanged();
            ResourceSystem.NotifyStateChanged();
            StateChanged?.Invoke();
            return true;
        }

        public static bool Release(ResourceReservationToken token)
        {
            if (!TryGetCost(token, out ResourceCost cost))
            {
                return false;
            }

            return RemoveReservation(token.Id, cost);
        }

        public static bool Contains(ResourceReservationToken token)
        {
            return token.IsValid && reservations.ContainsKey(token.Id);
        }

        public static void Reset()
        {
            reservations.Clear();
            ReservedFood = 0;
            ReservedResource = 0;
            StateChanged?.Invoke();
        }

        private static bool TryGetCost(ResourceReservationToken token, out ResourceCost cost)
        {
            cost = default;
            return token.IsValid && reservations.TryGetValue(token.Id, out cost);
        }

        private static bool RemoveReservation(
            string id,
            ResourceCost cost,
            bool notifyStateChanged = true)
        {
            if (!reservations.Remove(id))
            {
                return false;
            }

            ReservedFood = Math.Max(0, ReservedFood - cost.Food);
            ReservedResource = Math.Max(0, ReservedResource - cost.Resource);
            if (notifyStateChanged)
            {
                StateChanged?.Invoke();
            }
            return true;
        }
    }
}
