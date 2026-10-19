using UnityEngine;

namespace ZombieCardSurvive.Events.Data
{
    [CreateAssetMenu(fileName = "EventSchedulerSettings", menuName = "Zombie Card Survive/Events/Scheduler Settings")]
    public sealed class EventSchedulerSettingsData : ScriptableObject
    {
        [Header("Morale bands")]
        [Range(0, 100)] [SerializeField] private int lowMoraleThreshold = 30;
        [Range(0, 100)] [SerializeField] private int highMoraleThreshold = 70;
        [Min(0f)] [SerializeField] private float lowMoralePositiveMultiplier = 1.25f;
        [Min(0f)] [SerializeField] private float lowMoraleNegativeMultiplier = 0.75f;
        [Min(0f)] [SerializeField] private float highMoralePositiveMultiplier = 0.85f;
        [Min(0f)] [SerializeField] private float highMoraleNegativeMultiplier = 1.15f;

        [Header("Negative-event pacing")]
        [Min(0)] [SerializeField] private int recentNegativeProtectionDays = 1;
        [Min(0f)] [SerializeField] private float recentNegativeMultiplier = 0.5f;
        [Min(0)] [SerializeField] private int negativePityStartDays = 3;
        [Min(0f)] [SerializeField] private float negativePityPerExtraDay = 0.25f;
        [Min(1f)] [SerializeField] private float maximumNegativePityMultiplier = 2f;

        public EventSchedulerSettingsSnapshot CreateSnapshot()
        {
            return new EventSchedulerSettingsSnapshot(
                lowMoraleThreshold,
                highMoraleThreshold,
                lowMoralePositiveMultiplier,
                lowMoraleNegativeMultiplier,
                highMoralePositiveMultiplier,
                highMoraleNegativeMultiplier,
                recentNegativeProtectionDays,
                recentNegativeMultiplier,
                negativePityStartDays,
                negativePityPerExtraDay,
                maximumNegativePityMultiplier);
        }
    }

    public readonly struct EventSchedulerSettingsSnapshot
    {
        public static EventSchedulerSettingsSnapshot Default => new EventSchedulerSettingsSnapshot(
            30, 70, 1.25f, 0.75f, 0.85f, 1.15f, 1, 0.5f, 3, 0.25f, 2f);

        public EventSchedulerSettingsSnapshot(
            int lowMoraleThreshold,
            int highMoraleThreshold,
            float lowMoralePositiveMultiplier,
            float lowMoraleNegativeMultiplier,
            float highMoralePositiveMultiplier,
            float highMoraleNegativeMultiplier,
            int recentNegativeProtectionDays,
            float recentNegativeMultiplier,
            int negativePityStartDays,
            float negativePityPerExtraDay,
            float maximumNegativePityMultiplier)
        {
            LowMoraleThreshold = Mathf.Max(0, lowMoraleThreshold);
            HighMoraleThreshold = Mathf.Max(LowMoraleThreshold, highMoraleThreshold);
            LowMoralePositiveMultiplier = Mathf.Max(0f, lowMoralePositiveMultiplier);
            LowMoraleNegativeMultiplier = Mathf.Max(0f, lowMoraleNegativeMultiplier);
            HighMoralePositiveMultiplier = Mathf.Max(0f, highMoralePositiveMultiplier);
            HighMoraleNegativeMultiplier = Mathf.Max(0f, highMoraleNegativeMultiplier);
            RecentNegativeProtectionDays = Mathf.Max(0, recentNegativeProtectionDays);
            RecentNegativeMultiplier = Mathf.Max(0f, recentNegativeMultiplier);
            NegativePityStartDays = Mathf.Max(0, negativePityStartDays);
            NegativePityPerExtraDay = Mathf.Max(0f, negativePityPerExtraDay);
            MaximumNegativePityMultiplier = Mathf.Max(1f, maximumNegativePityMultiplier);
        }

        public int LowMoraleThreshold { get; }
        public int HighMoraleThreshold { get; }
        public float LowMoralePositiveMultiplier { get; }
        public float LowMoraleNegativeMultiplier { get; }
        public float HighMoralePositiveMultiplier { get; }
        public float HighMoraleNegativeMultiplier { get; }
        public int RecentNegativeProtectionDays { get; }
        public float RecentNegativeMultiplier { get; }
        public int NegativePityStartDays { get; }
        public float NegativePityPerExtraDay { get; }
        public float MaximumNegativePityMultiplier { get; }

        public float GetMoraleMultiplier(EventMoodCategory mood, int morale)
        {
            if (morale <= LowMoraleThreshold)
            {
                if (mood == EventMoodCategory.Positive)
                {
                    return LowMoralePositiveMultiplier;
                }

                if (mood == EventMoodCategory.Negative)
                {
                    return LowMoraleNegativeMultiplier;
                }
            }

            if (morale >= HighMoraleThreshold)
            {
                if (mood == EventMoodCategory.Positive)
                {
                    return HighMoralePositiveMultiplier;
                }

                if (mood == EventMoodCategory.Negative)
                {
                    return HighMoraleNegativeMultiplier;
                }
            }

            return 1f;
        }

        public float GetNegativePacingMultiplier(EventMoodCategory mood, int day, int? lastNegativeEventDay)
        {
            if (mood != EventMoodCategory.Negative)
            {
                return 1f;
            }

            int daysSinceNegative = lastNegativeEventDay.HasValue
                ? Mathf.Max(0, day - lastNegativeEventDay.Value)
                : Mathf.Max(0, day - 1);

            if (lastNegativeEventDay.HasValue && daysSinceNegative <= RecentNegativeProtectionDays)
            {
                return RecentNegativeMultiplier;
            }

            if (daysSinceNegative <= NegativePityStartDays)
            {
                return 1f;
            }

            float pity = 1f + (daysSinceNegative - NegativePityStartDays) * NegativePityPerExtraDay;
            return Mathf.Min(MaximumNegativePityMultiplier, pity);
        }
    }
}
