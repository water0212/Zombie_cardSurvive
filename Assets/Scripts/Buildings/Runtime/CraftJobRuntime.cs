using System;
using ZombieCardSurvive.Buildings.Data;

namespace ZombieCardSurvive.Buildings.Runtime
{
    [Serializable]
    public class CraftJobRuntime
    {
        public CraftJobRuntime(CardCraftRecipeData recipe, ResourceReservationToken reservation)
        {
            JobId = Guid.NewGuid().ToString("N");
            Recipe = recipe;
            Reservation = reservation;
            State = BuildingJobState.Queued;
        }

        public string JobId { get; }
        public CardCraftRecipeData Recipe { get; }
        public ResourceReservationToken Reservation { get; }
        public BuildingJobState State { get; private set; }
        public int CurrentProduction { get; private set; }
        public int RequiredProduction => Recipe != null ? Recipe.RequiredProduction : 1;
        public int RemainingProduction => Math.Max(0, RequiredProduction - CurrentProduction);

        public int ApplyProduction(int amount)
        {
            if (State == BuildingJobState.Queued)
            {
                State = BuildingJobState.InProgress;
            }

            if (State != BuildingJobState.InProgress)
            {
                return Math.Max(0, amount);
            }

            int production = Math.Max(0, amount);
            int used = Math.Min(RemainingProduction, production);
            CurrentProduction += used;
            return production - used;
        }

        public void RequestCancel()
        {
            if (State == BuildingJobState.Queued || State == BuildingJobState.InProgress)
            {
                State = BuildingJobState.CancelPending;
            }
        }

        public void Complete()
        {
            State = BuildingJobState.Completed;
        }

        public void Cancel()
        {
            State = BuildingJobState.Cancelled;
        }
    }
}
