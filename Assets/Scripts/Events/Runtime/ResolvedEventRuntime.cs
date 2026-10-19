using System.Collections.Generic;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Threats.Runtime;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class ResolvedEventRuntime
    {
        private readonly List<ResolvedEventChoiceRuntime> choices;

        private ResolvedEventRuntime(
            EventData source,
            EventMagnitudeSnapshot magnitudeSnapshot,
            List<ResolvedEventChoiceRuntime> choices)
        {
            Source = source;
            MagnitudeSnapshot = magnitudeSnapshot;
            this.choices = choices;
        }

        public EventData Source { get; }
        public EventMagnitudeSnapshot MagnitudeSnapshot { get; }
        public IReadOnlyList<ResolvedEventChoiceRuntime> Choices => choices;

        public static ResolvedEventRuntime Create(
            EventData source,
            EventMagnitudeSnapshot magnitudeSnapshot)
        {
            int eventDay = magnitudeSnapshot != null ? magnitudeSnapshot.Day : 1;
            return Create(
                source,
                magnitudeSnapshot,
                null,
                runSeed: 0,
                eventDay: eventDay);
        }

        public static ResolvedEventRuntime Create(
            EventData source,
            EventMagnitudeSnapshot magnitudeSnapshot,
            ThreatSnapshot threatSnapshot,
            int runSeed,
            int eventDay)
        {
            if (source == null)
            {
                return null;
            }

            int choiceCount = source.Choices != null ? source.Choices.Count : 0;
            List<ResolvedEventChoiceRuntime> resolvedChoices =
                new List<ResolvedEventChoiceRuntime>(choiceCount);
            for (int index = 0; index < choiceCount; index++)
            {
                resolvedChoices.Add(ResolvedEventChoiceRuntime.Create(
                    source.Choices[index],
                    magnitudeSnapshot,
                    threatSnapshot,
                    runSeed,
                    eventDay,
                    source.EventId,
                    index));
            }

            return new ResolvedEventRuntime(source, magnitudeSnapshot, resolvedChoices);
        }
    }
}
