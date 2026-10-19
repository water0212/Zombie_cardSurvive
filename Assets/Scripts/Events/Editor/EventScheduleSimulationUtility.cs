#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Events.Editor
{
    public static class EventScheduleSimulationUtility
    {
        public const string ReportAssetPath = "Assets/Docs/Playtest/EventScheduleV1_30Day_Report.md";
        private const string SchedulerSettingsPath = "Assets/ScriptObj/EventScrpitObj/EventSchedulerSettings.asset";

        [MenuItem("Tools/Zombie Card Survive/Simulate Event Content V1 (30 Days)")]
        public static void SimulateThirtyDays()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            EventContentV1SetupUtility.ValidateSpecifications(errors, warnings);
            List<EventData> events = LoadCanonicalEvents(errors);
            if (errors.Count > 0)
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"Event schedule simulation: {error}");
                }

                return;
            }

            EventSchedulerSettingsData settings =
                AssetDatabase.LoadAssetAtPath<EventSchedulerSettingsData>(SchedulerSettingsPath);
            if (settings == null)
            {
                warnings.Add("EventSchedulerSettings asset was not found; runtime defaults were used.");
            }

            EventScheduleSimulationResult canonical = Run(
                events,
                settings,
                EventContentV1Specification.CanonicalSimulationDays,
                EventContentV1Specification.CanonicalSimulationSeed);
            Dictionary<string, int> distribution = RunDistribution(events, settings);
            string report = BuildReport(canonical, distribution, warnings);

            string reportPath = Path.Combine(
                Application.dataPath,
                "Docs",
                "Playtest",
                "EventScheduleV1_30Day_Report.md");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? Application.dataPath);
            File.WriteAllText(reportPath, report, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log(
                $"Event Content V1 schedule simulation complete: " +
                $"{canonical.Days.Count} days, {canonical.FallbackCount} fallback days. " +
                $"Report: {ReportAssetPath}");
        }

        public static EventScheduleSimulationResult Run(
            IEnumerable<EventData> events,
            EventSchedulerSettingsData settings,
            int days,
            int seed)
        {
            List<EventData> catalog = events != null
                ? events.Where(eventData => eventData != null).ToList()
                : new List<EventData>();
            EventScheduler scheduler = new EventScheduler();
            scheduler.InitializeRun(catalog, settings, seed);
            EventScheduleSimulationResult simulation = new EventScheduleSimulationResult(seed, days);

            for (int day = 1; day <= Math.Max(0, days); day++)
            {
                EventSelectionContext context = CreateBaselineContext(day);
                if (!scheduler.TrySelectAndRecord(context, out EventSelectionResult selection))
                {
                    simulation.AddDay(new EventScheduleSimulationDay(
                        day,
                        null,
                        0f,
                        selection?.EligibleCandidateCount ?? 0));
                    continue;
                }

                float selectedWeight = 0f;
                foreach (EventCandidateEvaluation evaluation in selection.Evaluations)
                {
                    if (ReferenceEquals(evaluation.EventData, selection.SelectedEvent))
                    {
                        selectedWeight = evaluation.FinalWeight;
                        break;
                    }
                }

                simulation.AddDay(new EventScheduleSimulationDay(
                    day,
                    selection.SelectedEvent,
                    selectedWeight,
                    selection.EligibleCandidateCount));
            }

            return simulation;
        }

        private static Dictionary<string, int> RunDistribution(
            IReadOnlyList<EventData> events,
            EventSchedulerSettingsData settings)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (EventData eventData in events)
            {
                counts[eventData.EventId] = 0;
            }

            for (int run = 0; run < EventContentV1Specification.DistributionSimulationRuns; run++)
            {
                EventScheduleSimulationResult result = Run(
                    events,
                    settings,
                    EventContentV1Specification.CanonicalSimulationDays,
                    EventContentV1Specification.CanonicalSimulationSeed + run);
                foreach (KeyValuePair<string, int> pair in result.EventCounts)
                {
                    counts[pair.Key] = counts.TryGetValue(pair.Key, out int previous)
                        ? previous + pair.Value
                        : pair.Value;
                }
            }

            return counts;
        }

        private static EventSelectionContext CreateBaselineContext(int day)
        {
            int maxEnergy = 1 + (Math.Max(1, day) - 1) / 10;
            return new EventSelectionContext(
                day,
                morale: 50,
                food: 25,
                resource: 10,
                maxEnergy: maxEnergy,
                activeZombieCount: 0,
                buildingLevelResolver: _ => 0,
                runFlagResolver: _ => false,
                customValueResolver: _ => 0);
        }

        private static List<EventData> LoadCanonicalEvents(List<string> errors)
        {
            Dictionary<string, EventData> byId =
                new Dictionary<string, EventData>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> canonicalIds = new HashSet<string>(
                EventContentV1Specification.Events.Select(spec => spec.EventId),
                StringComparer.OrdinalIgnoreCase);

            foreach (string guid in AssetDatabase.FindAssets("t:EventData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);
                string eventId = eventData != null ? eventData.EventId?.Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(eventId) || !canonicalIds.Contains(eventId))
                {
                    continue;
                }

                if (byId.ContainsKey(eventId))
                {
                    errors.Add($"Duplicate canonical EventId '{eventId}' prevents simulation.");
                    continue;
                }

                byId.Add(eventId, eventData);
            }

            List<EventData> ordered = new List<EventData>();
            foreach (EventContentSpec spec in EventContentV1Specification.Events)
            {
                if (!byId.TryGetValue(spec.EventId, out EventData eventData))
                {
                    errors.Add($"Event asset '{spec.EventId}' is missing. Run Setup Event Content V1 first.");
                    continue;
                }

                ordered.Add(eventData);
            }

            return ordered;
        }

        private static string BuildReport(
            EventScheduleSimulationResult canonical,
            IReadOnlyDictionary<string, int> distribution,
            IReadOnlyList<string> warnings)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Event Content V1 - 30 Day Schedule Report");
            builder.AppendLine();
            builder.AppendLine("## Simulation Scope");
            builder.AppendLine();
            builder.AppendLine("- This is a schedule-only simulation. Event choices and gameplay effects are not executed.");
            builder.AppendLine($"- Canonical seed: `{canonical.Seed}`.");
            builder.AppendLine($"- Canonical duration: `{canonical.RequestedDays}` days.");
            builder.AppendLine($"- Distribution sample: `{EventContentV1Specification.DistributionSimulationRuns}` fixed-seed runs.");
            builder.AppendLine("- Baseline morale is 50, building levels are 0, and MaxEnergy is 1/2/3 across days 1-10/11-20/21-30.");
            builder.AppendLine("- Unsupported special rules and blocked event effects are intentionally ignored in this preliminary report.");
            builder.AppendLine();

            builder.AppendLine("## Canonical Sequence");
            builder.AppendLine();
            builder.AppendLine("| Day | Event ID | Event | Mood | Content | Final Weight | Eligible |");
            builder.AppendLine("|---:|---|---|---|---|---:|---:|");
            foreach (EventScheduleSimulationDay day in canonical.Days)
            {
                if (day.EventData == null)
                {
                    builder.AppendLine($"| {day.Day} | FALLBACK | 平靜的一天 | - | - | 0 | {day.EligibleCandidateCount} |");
                    continue;
                }

                builder.AppendLine(
                    $"| {day.Day} | {day.EventData.EventId} | {day.EventData.DisplayTitle} | " +
                    $"{day.EventData.MoodCategory} | {day.EventData.ContentCategory} | " +
                    $"{day.FinalWeight:0.##} | {day.EligibleCandidateCount} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Canonical Distribution");
            builder.AppendLine();
            AppendCategoryTable(builder, "Mood", canonical.MoodCounts);
            AppendCategoryTable(builder, "Content", canonical.ContentCounts);

            builder.AppendLine("## Event Frequency Across 100 Runs");
            builder.AppendLine();
            builder.AppendLine("| Event ID | Event | Appearances | Share |");
            builder.AppendLine("|---|---|---:|---:|");
            int distributionTotal = Math.Max(1, distribution.Values.Sum());
            foreach (EventContentSpec spec in EventContentV1Specification.Events)
            {
                int count = distribution.TryGetValue(spec.EventId, out int value) ? value : 0;
                builder.AppendLine(
                    $"| {spec.EventId} | {spec.Title} | {count} | {(count * 100f / distributionTotal):0.00}% |");
            }

            builder.AppendLine();
            builder.AppendLine("## Findings");
            builder.AppendLine();
            builder.AppendLine($"- Fallback days in canonical run: `{canonical.FallbackCount}`.");
            string[] neverAppeared = EventContentV1Specification.Events
                .Where(spec => !canonical.EventCounts.ContainsKey(spec.EventId))
                .Select(spec => spec.EventId)
                .ToArray();
            builder.AppendLine(
                neverAppeared.Length == 0
                    ? "- Every event appeared at least once in the canonical run."
                    : $"- Events absent from the single canonical run: `{string.Join(", ", neverAppeared)}`. This alone is not a balance failure.");

            string[] distributionMissing = EventContentV1Specification.Events
                .Where(spec => !distribution.TryGetValue(spec.EventId, out int count) || count == 0)
                .Select(spec => spec.EventId)
                .ToArray();
            builder.AppendLine(
                distributionMissing.Length == 0
                    ? "- Every event appeared in the 100-run distribution sample."
                    : $"- Events never selected in 100 runs: `{string.Join(", ", distributionMissing)}`.");
            builder.AppendLine(
                $"- Implementation-ready events: `{EventContentV1Specification.Events.Count(spec => spec.IncludeInPlayablePool)}` of " +
                $"`{EventContentV1Specification.Events.Count}`.");
            builder.AppendLine("- Economy balance, affordability, dynamic event weights, event chains, and threat outcomes remain outside PB-P1-014A.");

            if (warnings != null && warnings.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("## Validation Warnings");
                builder.AppendLine();
                foreach (string warning in warnings)
                {
                    builder.AppendLine($"- {warning}");
                }
            }

            return builder.ToString();
        }

        private static void AppendCategoryTable(
            StringBuilder builder,
            string label,
            IReadOnlyDictionary<string, int> counts)
        {
            builder.AppendLine($"### {label}");
            builder.AppendLine();
            builder.AppendLine("| Category | Count |");
            builder.AppendLine("|---|---:|");
            foreach (KeyValuePair<string, int> pair in counts.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                builder.AppendLine($"| {pair.Key} | {pair.Value} |");
            }

            builder.AppendLine();
        }
    }

    public sealed class EventScheduleSimulationResult
    {
        private readonly List<EventScheduleSimulationDay> days = new List<EventScheduleSimulationDay>();
        private readonly Dictionary<string, int> eventCounts =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> moodCounts =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> contentCounts =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public EventScheduleSimulationResult(int seed, int requestedDays)
        {
            Seed = seed;
            RequestedDays = Math.Max(0, requestedDays);
        }

        public int Seed { get; }
        public int RequestedDays { get; }
        public IReadOnlyList<EventScheduleSimulationDay> Days => days;
        public IReadOnlyDictionary<string, int> EventCounts => eventCounts;
        public IReadOnlyDictionary<string, int> MoodCounts => moodCounts;
        public IReadOnlyDictionary<string, int> ContentCounts => contentCounts;
        public int FallbackCount { get; private set; }

        public void AddDay(EventScheduleSimulationDay day)
        {
            days.Add(day);
            if (day.EventData == null)
            {
                FallbackCount++;
                return;
            }

            Increment(eventCounts, day.EventData.EventId);
            Increment(moodCounts, day.EventData.MoodCategory.ToString());
            Increment(contentCounts, day.EventData.ContentCategory.ToString());
        }

        private static void Increment(IDictionary<string, int> counts, string key)
        {
            counts[key] = counts.TryGetValue(key, out int previous) ? previous + 1 : 1;
        }
    }

    public readonly struct EventScheduleSimulationDay
    {
        public EventScheduleSimulationDay(
            int day,
            EventData eventData,
            float finalWeight,
            int eligibleCandidateCount)
        {
            Day = day;
            EventData = eventData;
            FinalWeight = finalWeight;
            EligibleCandidateCount = eligibleCandidateCount;
        }

        public int Day { get; }
        public EventData EventData { get; }
        public float FinalWeight { get; }
        public int EligibleCandidateCount { get; }
    }
}
#endif
