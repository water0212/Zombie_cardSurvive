using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Events.Runtime
{
    /// <summary>事件交易邊界的四種核心數值；結果保存差值，UI 不重讀目前狀態。</summary>
    public sealed class EventValueSnapshot
    {
        public long Food { get; }
        public long Resource { get; }
        public long Combat { get; }
        public long Energy { get; }

        public EventValueSnapshot(long food, long resource, long combat, long energy)
        {
            Food = food; Resource = resource; Combat = combat; Energy = energy;
        }

        public static EventValueSnapshot Capture() => new EventValueSnapshot(
            FoodSystem.Food, ResourceSystem.GetResource(), CombatSystem.CombatValue, EnergySystem.CurrentEnergy);

        public EventValueSnapshot DifferenceFrom(EventValueSnapshot before) => new EventValueSnapshot(
            Food - before.Food, Resource - before.Resource, Combat - before.Combat, Energy - before.Energy);
    }
}
