using ZombieCardSurvive.Systems.Transactions;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.Runtime
{
    public readonly struct CardPlayContext
    {
        public CardPlayContext(CardController controller, CardRuntime card, ValueTransaction transaction = null)
        {
            Controller = controller;
            Card = card; Transaction = transaction;
        }

        public CardController Controller { get; }
        public CardRuntime Card { get; }
        public ValueTransaction Transaction { get; }
        public int Food => FoodSystem.AvailableFood;
        public int TemporaryFood => FoodSystem.TemporaryFood;
        public int Resource => ResourceSystem.AvailableResource;
        public int CurrentEnergy => EnergySystem.CurrentEnergy;
        public int MaxEnergy => EnergySystem.MaxEnergy;
        public int PendingDamage => CombatSystem.PendingDamage;
        public int ZombieThreatCount => CombatSystem.ZombieThreatCount;
        public int Morale => MoraleSystem.Morale;

        public void AddFood(int amount)
        {
            if (Transaction != null) GameValues.Food.Change(amount, Transaction); else FoodSystem.AddFood(amount);
        }

        public void AddTemporaryFood(int amount)
        {
            FoodSystem.AddTemporaryFood(amount);
        }

        public void AddResource(int amount)
        {
            if (Transaction != null) GameValues.Resource.Change(amount, Transaction); else ResourceSystem.AddResource(amount);
        }

        public void AddPendingDamage(int amount)
        {
            if (Transaction != null) GameValues.Combat.Change(amount, Transaction); else CombatSystem.AddPendingDamage(amount);
        }

        public void AddZombieThreat(int amount)
        {
            CombatSystem.AddZombieThreat(amount);
        }

        public void AddMorale(int amount)
        {
            MoraleSystem.AddMorale(amount);
        }

        public void ReduceMorale(int amount)
        {
            MoraleSystem.ReduceMorale(amount);
        }
    }
}
