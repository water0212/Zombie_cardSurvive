using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.Runtime
{
    public readonly struct CardPlayContext
    {
        public CardPlayContext(CardController controller, CardRuntime card)
        {
            Controller = controller;
            Card = card;
        }

        public CardController Controller { get; }
        public CardRuntime Card { get; }
        public int Food => FoodSystem.Food;
        public int Resource => ResourceSystem.Resource;
        public int CurrentEnergy => EnergySystem.CurrentEnergy;
        public int MaxEnergy => EnergySystem.MaxEnergy;
        public int PendingDamage => CombatSystem.PendingDamage;
        public int ZombieThreatCount => CombatSystem.ZombieThreatCount;
        public int Morale => MoraleSystem.Morale;

        public void AddFood(int amount)
        {
            FoodSystem.AddFood(amount);
        }

        public void AddResource(int amount)
        {
            ResourceSystem.AddResource(amount);
        }

        public void AddPendingDamage(int amount)
        {
            CombatSystem.AddPendingDamage(amount);
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
