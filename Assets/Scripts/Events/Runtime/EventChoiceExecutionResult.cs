using System.Collections.Generic;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class EventChoiceExecutionResult
    {
        private EventChoiceExecutionResult(
            bool succeeded,
            EventChoiceAvailability availability,
            IReadOnlyList<EventForcedLossResult> forcedLosses,
            IReadOnlyList<ResolvedGameEffectRuntime> appliedEffects,
            string resultTitle,
            string resultDescription,
            MoraleBatchResult moraleSettlement,
            EventValueSnapshot valueChanges = null)
        {
            Succeeded = succeeded;
            Availability = availability;
            ForcedLosses = forcedLosses ?? new List<EventForcedLossResult>();
            AppliedEffects = appliedEffects ?? new List<ResolvedGameEffectRuntime>();
            ResultTitle = resultTitle ?? string.Empty;
            ResultDescription = resultDescription ?? string.Empty;
            MoraleSettlement = moraleSettlement;
            ValueChanges = valueChanges;
        }

        public bool Succeeded { get; }
        public EventChoiceAvailability Availability { get; }
        public IReadOnlyList<EventForcedLossResult> ForcedLosses { get; }
        public IReadOnlyList<ResolvedGameEffectRuntime> AppliedEffects { get; }
        public string ResultTitle { get; }
        public string ResultDescription { get; }
        public MoraleBatchResult MoraleSettlement { get; }
        public EventValueSnapshot ValueChanges { get; }

        public static EventChoiceExecutionResult Failure(EventChoiceAvailability availability)
        {
            return new EventChoiceExecutionResult(
                false,
                availability,
                new List<EventForcedLossResult>(),
                new List<ResolvedGameEffectRuntime>(),
                string.Empty,
                string.Empty,
                null);
        }

        public static EventChoiceExecutionResult Success(
            EventChoiceAvailability availability,
            IReadOnlyList<EventForcedLossResult> forcedLosses,
            IReadOnlyList<ResolvedGameEffectRuntime> appliedEffects,
            string resultTitle,
            string resultDescription,
            MoraleBatchResult moraleSettlement = null,
            EventValueSnapshot valueChanges = null)
        {
            return new EventChoiceExecutionResult(
                true,
                availability,
                forcedLosses,
                appliedEffects,
                resultTitle,
                resultDescription,
                moraleSettlement,
                valueChanges);
        }
    }
}
