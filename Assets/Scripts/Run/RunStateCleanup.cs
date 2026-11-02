using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Systems.Transactions;

namespace ZombieCardSurvive.Run
{
    public static class RunStateCleanup
    {
        // Called after the old scene is unloaded, with the lifecycle owner's lock still held.
        // Reset clears runtime state only; asset definitions and persisted history are untouched.
        public static void ClearStatics()
        {
            MoralePyramidSystem.Reset();
            NextRoundEffectSystem.Reset();
            ResourceReservationService.Reset();
            FoodSystem.Reset(0);
            FoodSystem.SetStorageCapacity(0);
            ResourceSystem.Reset(0);
            CombatSystem.Reset();
            EnergySystem.Reset(0);
            FoodPlanSystem.Reset();
            MoraleSystem.ClearRunState();
            ValueTransactions.ResetPresentation();
            GameplayInteractionLock.Reset();
            if (RunLifecycleController.Instance != null) GameplayInteractionLock.Acquire(RunLifecycleController.Instance);
        }
    }
}
