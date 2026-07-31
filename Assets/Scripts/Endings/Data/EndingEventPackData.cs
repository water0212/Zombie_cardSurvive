using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Endings.Data
{
    [CreateAssetMenu(fileName = "EndingEventPack", menuName = "Zombie Card Survive/Endings/Event Pack")]
    public sealed class EndingEventPackData : ScriptableObject, IAutoIdAsset
    {
        [SerializeField] private string eventPackId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;

        [Header("Normal Events")]
        [SerializeField] private EndingEventPackOrderMode orderMode = EndingEventPackOrderMode.ShuffleWithoutReplacement;
        [SerializeField] private List<EventData> events = new List<EventData>();

        [Header("Fixed Final Sequence")]
        [Tooltip("These events always run after the normal event list has been exhausted.")]
        [SerializeField] private List<EventData> fixedFinalSequence = new List<EventData>();

        [Header("After Every Event Has Run")]
        [SerializeField] private EndingEventPackExhaustionMode exhaustionMode = EndingEventPackExhaustionMode.Stop;

        public string EventPackId => eventPackId;
        public string DisplayName => displayName;
        public string Description => description;
        public EndingEventPackOrderMode OrderMode => orderMode;
        public IReadOnlyList<EventData> Events => events;
        public IReadOnlyList<EventData> FixedFinalSequence => fixedFinalSequence;
        public EndingEventPackExhaustionMode ExhaustionMode => exhaustionMode;
        public int ConfiguredEventCount => CountNonNull(events) + CountNonNull(fixedFinalSequence);
        public bool HasEvents => ConfiguredEventCount > 0;
        public string Id => eventPackId;

        private void OnValidate()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(eventPackId))
            {
                eventPackId = AutoIdUtility.CreateId("ending_event_pack");
            }
        }

        public void RegenerateId()
        {
            eventPackId = AutoIdUtility.CreateId("ending_event_pack");
        }

        public List<string> GetValidationIssues()
        {
            List<string> issues = new List<string>();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                issues.Add("Event pack display name is empty.");
            }

            if (!HasEvents)
            {
                issues.Add("Event pack does not contain any events.");
            }

            if (exhaustionMode != EndingEventPackExhaustionMode.Stop && CountNonNull(events) == 0)
            {
                issues.Add("A repeating exhaustion mode requires at least one normal event.");
            }

            return issues;
        }

        private static int CountNonNull(IReadOnlyList<EventData> source)
        {
            if (source == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
