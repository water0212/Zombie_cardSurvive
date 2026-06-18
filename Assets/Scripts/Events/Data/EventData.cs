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
        [SerializeField] private Sprite artwork;
        [SerializeField] private List<EventChoiceData> choices = new List<EventChoiceData>();

        public string EventId => eventId;
        public string DisplayTitle => displayTitle;
        public string Description => description;
        public Sprite Artwork => artwork;
        public IReadOnlyList<EventChoiceData> Choices => choices;
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
