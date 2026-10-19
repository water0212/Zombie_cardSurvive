using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Threats.Runtime;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class ResolvedEventChoiceRuntime
    {
        private readonly List<ResolvedGameEffectRuntime> effects;
        private readonly List<ResolvedForcedLossConsequenceRuntime> forcedLossConsequences;

        private ResolvedEventChoiceRuntime(
            EventChoiceData source,
            List<ResolvedGameEffectRuntime> effects,
            List<ResolvedForcedLossConsequenceRuntime> forcedLossConsequences)
        {
            Source = source;
            this.effects = effects;
            this.forcedLossConsequences = forcedLossConsequences;
            ChoiceText = EventTextFormatter.Resolve(source != null ? source.ChoiceText : string.Empty, effects);
            ResultTitle = EventTextFormatter.Resolve(source != null ? source.ResultTitle : string.Empty, effects);
            ResultDescription = EventTextFormatter.Resolve(source != null ? source.ResultDescription : string.Empty, effects);
            ResultArtwork = source != null ? source.ResultArtwork : null;
        }

        public EventChoiceData Source { get; }
        public string ChoiceText { get; }
        public string ResultTitle { get; }
        public string ResultDescription { get; }
        public Sprite ResultArtwork { get; }
        public IReadOnlyList<ResolvedGameEffectRuntime> Effects => effects;
        public IReadOnlyList<ResolvedForcedLossConsequenceRuntime> ForcedLossConsequences =>
            forcedLossConsequences;

        public static ResolvedEventChoiceRuntime Create(
            EventChoiceData source,
            EventMagnitudeSnapshot magnitudeSnapshot)
        {
            int eventDay = magnitudeSnapshot != null ? magnitudeSnapshot.Day : 1;
            return Create(
                source,
                magnitudeSnapshot,
                null,
                runSeed: 0,
                eventDay: eventDay,
                eventId: string.Empty,
                choiceIndex: 0);
        }

        public static ResolvedEventChoiceRuntime Create(
            EventChoiceData source,
            EventMagnitudeSnapshot magnitudeSnapshot,
            ThreatSnapshot threatSnapshot,
            int runSeed,
            int eventDay,
            string eventId,
            int choiceIndex)
        {
            if (source == null)
            {
                return null;
            }

            int effectCount = source.Effects != null ? source.Effects.Count : 0;
            List<ResolvedGameEffectRuntime> resolvedEffects =
                new List<ResolvedGameEffectRuntime>(effectCount);
            for (int index = 0; index < effectCount; index++)
            {
                resolvedEffects.Add(ResolvedGameEffectRuntime.Create(
                    source.Effects[index],
                    magnitudeSnapshot,
                    threatSnapshot,
                    runSeed,
                    eventDay,
                    eventId,
                    choiceIndex,
                    index));
            }

            int consequenceCount = source.ForcedLossConsequences != null
                ? source.ForcedLossConsequences.Count
                : 0;
            List<ResolvedForcedLossConsequenceRuntime> resolvedConsequences =
                new List<ResolvedForcedLossConsequenceRuntime>(consequenceCount);
            for (int index = 0; index < consequenceCount; index++)
            {
                resolvedConsequences.Add(ResolvedForcedLossConsequenceRuntime.Create(
                    source.ForcedLossConsequences[index],
                    magnitudeSnapshot));
            }

            return new ResolvedEventChoiceRuntime(source, resolvedEffects, resolvedConsequences);
        }

        public bool CanApply(GameEffectContext context)
        {
            return EvaluateAvailability(context).IsAvailable;
        }

        public bool TryApply(GameEffectContext context)
        {
            return TryExecute(context, out EventChoiceExecutionResult result) && result.Succeeded;
        }

        public EventChoiceAvailability EvaluateAvailability(GameEffectContext context)
        {
            return EventChoiceExecutionPlan.Create(this, context).Availability;
        }

        public bool TryExecute(
            GameEffectContext context,
            out EventChoiceExecutionResult result)
        {
            EventChoiceExecutionPlan plan = EventChoiceExecutionPlan.Create(this, context);
            result = plan.Execute();
            return result.Succeeded;
        }
    }
}
