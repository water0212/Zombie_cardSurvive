using System.Collections.Generic;
using ZombieCardSurvive.Events.Data;

namespace ZombieCardSurvive.Events.Runtime
{
    public enum EventRejectionReason
    {
        None,
        MissingId,
        BeforeEarliestDay,
        AfterLatestDay,
        RepeatLimitReached,
        CooldownActive,
        ConditionFailed,
        NoPositiveWeight
    }

    public readonly struct EventCandidateEvaluation
    {
        public EventCandidateEvaluation(
            EventData eventData,
            bool isEligible,
            float finalWeight,
            EventRejectionReason rejectionReason,
            string detail)
        {
            EventData = eventData;
            IsEligible = isEligible;
            FinalWeight = finalWeight;
            RejectionReason = rejectionReason;
            Detail = detail;
        }

        public EventData EventData { get; }
        public bool IsEligible { get; }
        public float FinalWeight { get; }
        public EventRejectionReason RejectionReason { get; }
        public string Detail { get; }
    }

    public sealed class EventSelectionResult
    {
        public EventSelectionResult(
            int day,
            EventData selectedEvent,
            IReadOnlyList<EventCandidateEvaluation> evaluations)
        {
            Day = day;
            SelectedEvent = selectedEvent;
            Evaluations = evaluations ?? new List<EventCandidateEvaluation>();

            int eligibleCount = 0;
            foreach (EventCandidateEvaluation evaluation in Evaluations)
            {
                if (evaluation.IsEligible)
                {
                    eligibleCount++;
                }
            }

            EligibleCandidateCount = eligibleCount;
        }

        public int Day { get; }
        public EventData SelectedEvent { get; }
        public IReadOnlyList<EventCandidateEvaluation> Evaluations { get; }
        public int EligibleCandidateCount { get; }
        public bool HasSelection => SelectedEvent != null;
    }
}
