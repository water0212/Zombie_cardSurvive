using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Events.Data
{
    [CreateAssetMenu(fileName = "EventData", menuName = "Zombie Card Survive/Events/Event")]
    public class EventData : ScriptableObject, IAutoIdAsset
    {
        [SerializeField] private string eventId;
        [SerializeField] private string displayTitle;
        [TextArea]
        [SerializeField] private string description;
        [Tooltip("Optional notebook-style background, witness account, or contextual information shown beside the event artwork.")]
        [TextArea]
        [SerializeField] private string journalText;
        [SerializeField] private Sprite artwork;
        [SerializeField] private List<EventChoiceData> choices = new List<EventChoiceData>();

        [Header("Schedule classification")]
        [SerializeField] private EventMoodCategory moodCategory = EventMoodCategory.Mixed;
        [SerializeField] private EventContentCategory contentCategory = EventContentCategory.Resource;

        [Header("Content implementation")]
        [SerializeField] private EventImplementationStatus implementationStatus = EventImplementationStatus.Ready;
        [Tooltip("Design-only impact label copied from the Event Schedule source.")]
        [SerializeField] private string impactLevel;
        [TextArea]
        [SerializeField] private string specialRules;
        [TextArea]
        [SerializeField] private string implementationNotes;

        [Header("Schedule eligibility")]
        [Min(1)] [SerializeField] private int earliestDay = 1;
        [Tooltip("0 means this event has no latest day.")]
        [Min(0)] [SerializeField] private int latestDay;
        [Min(0f)] [SerializeField] private float baseWeight = 100f;
        [SerializeField] private bool canRepeat = true;
        [Tooltip("0 means unlimited when Can Repeat is enabled.")]
        [Min(0)] [SerializeField] private int maximumAppearancesPerRun;
        [Tooltip("Number of complete days that must exist between two appearances.")]
        [Min(0)] [SerializeField] private int minimumDaysBetweenAppearances = 1;
        [SerializeField] private EventConditionGroup eligibilityConditions = new EventConditionGroup();
        [SerializeField] private List<EventWeightModifierEntry> weightModifiers =
            new List<EventWeightModifierEntry>();

        public string EventId => eventId;
        public string DisplayTitle => displayTitle;
        public string Description => description;
        public string JournalText => journalText;
        public Sprite Artwork => artwork;
        public IReadOnlyList<EventChoiceData> Choices => choices;
        public EventMoodCategory MoodCategory => moodCategory;
        public EventContentCategory ContentCategory => contentCategory;
        public EventImplementationStatus ImplementationStatus => implementationStatus;
        public bool IsImplementationReady => implementationStatus == EventImplementationStatus.Ready;
        public string ImpactLevel => impactLevel;
        public string SpecialRules => specialRules;
        public string ImplementationNotes => implementationNotes;
        public int EarliestDay => Mathf.Max(1, earliestDay);
        public int LatestDay => Mathf.Max(0, latestDay);
        public float BaseWeight => Mathf.Max(0f, baseWeight);
        public bool CanRepeat => canRepeat;
        public int MaximumAppearancesPerRun => canRepeat ? Mathf.Max(0, maximumAppearancesPerRun) : 1;
        public int MinimumDaysBetweenAppearances => Mathf.Max(0, minimumDaysBetweenAppearances);
        public EventConditionGroup EligibilityConditions => eligibilityConditions;
        public IReadOnlyList<EventWeightModifierEntry> WeightModifiers => weightModifiers;
        public string Id => eventId;

        private void OnValidate()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                eventId = AutoIdUtility.CreateId("event");
            }
        }

        public void RegenerateId()
        {
            eventId = AutoIdUtility.CreateId("event");
        }
    }
}
