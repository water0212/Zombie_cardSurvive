using UnityEngine;

namespace ZombieCardSurvive.Events.Data
{
    [CreateAssetMenu(fileName = "EventMagnitudeSettings", menuName = "Zombie Card Survive/Events/Magnitude Settings")]
    public sealed class EventMagnitudeSettingsData : ScriptableObject
    {
        [Min(1)] [SerializeField] private int daysPerTimeTier = 10;
        [Min(1)] [SerializeField] private int developmentPointsPerSettlementTier = 3;
        [Tooltip("How much food-storage bonus produces one additional storage scale tier.")]
        [Min(1)] [SerializeField] private int foodStorageBonusPerTier = 10;

        public EventMagnitudeSettingsSnapshot CreateSnapshot()
        {
            return new EventMagnitudeSettingsSnapshot(
                daysPerTimeTier,
                developmentPointsPerSettlementTier,
                foodStorageBonusPerTier);
        }
    }

    public readonly struct EventMagnitudeSettingsSnapshot
    {
        public static EventMagnitudeSettingsSnapshot Default => new EventMagnitudeSettingsSnapshot(10, 3, 10);

        public EventMagnitudeSettingsSnapshot(
            int daysPerTimeTier,
            int developmentPointsPerSettlementTier,
            int foodStorageBonusPerTier)
        {
            DaysPerTimeTier = Mathf.Max(1, daysPerTimeTier);
            DevelopmentPointsPerSettlementTier = Mathf.Max(1, developmentPointsPerSettlementTier);
            FoodStorageBonusPerTier = Mathf.Max(1, foodStorageBonusPerTier);
        }

        public int DaysPerTimeTier { get; }
        public int DevelopmentPointsPerSettlementTier { get; }
        public int FoodStorageBonusPerTier { get; }
    }
}
