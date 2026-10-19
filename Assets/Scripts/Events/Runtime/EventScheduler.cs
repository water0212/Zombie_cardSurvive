using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Events.Data;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class EventScheduler
    {
        private readonly List<EventData> eventCatalog = new List<EventData>();
        private EventSchedulerSettingsSnapshot settings = EventSchedulerSettingsSnapshot.Default;
        private System.Random random = new System.Random(0);

        public EventRunHistory History { get; } = new EventRunHistory();
        public EventSelectionResult LastSelectionResult { get; private set; }
        public int Seed { get; private set; }

        public void InitializeRun(
            IEnumerable<EventData> events,
            EventSchedulerSettingsData settingsData,
            int seed)
        {
            eventCatalog.Clear();
            History.Reset();
            LastSelectionResult = null;
            Seed = seed;
            random = new System.Random(seed);
            settings = settingsData != null ? settingsData.CreateSnapshot() : EventSchedulerSettingsSnapshot.Default;

            if (events == null)
            {
                return;
            }

            HashSet<string> registeredIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (EventData eventData in events)
            {
                if (eventData == null || string.IsNullOrWhiteSpace(eventData.EventId))
                {
                    continue;
                }

                string eventId = eventData.EventId.Trim();
                if (!registeredIds.Add(eventId))
                {
                    Debug.LogWarning($"Duplicate Event ID '{eventId}' was ignored by EventScheduler.");
                    continue;
                }

                eventCatalog.Add(eventData);
            }
        }

        public bool TrySelectAndRecord(EventSelectionContext context, out EventSelectionResult result)
        {
            result = Select(context);
            LastSelectionResult = result;
            if (result == null || !result.HasSelection)
            {
                return false;
            }

            History.RecordAppearance(result.SelectedEvent, result.Day);
            return true;
        }

        public void RecordExternalAppearance(EventData eventData, int day)
        {
            History.RecordAppearance(eventData, day);
            LastSelectionResult = new EventSelectionResult(
                Mathf.Max(1, day),
                eventData,
                new List<EventCandidateEvaluation>());
        }

        private EventSelectionResult Select(EventSelectionContext context)
        {
            if (context == null)
            {
                return new EventSelectionResult(1, null, new List<EventCandidateEvaluation>());
            }

            List<EventCandidateEvaluation> evaluations = new List<EventCandidateEvaluation>(eventCatalog.Count);
            List<WeightedCandidate> candidates = new List<WeightedCandidate>();
            double totalWeight = 0d;

            foreach (EventData eventData in eventCatalog)
            {
                EventCandidateEvaluation evaluation = Evaluate(eventData, context);
                evaluations.Add(evaluation);
                if (!evaluation.IsEligible)
                {
                    continue;
                }

                totalWeight += evaluation.FinalWeight;
                candidates.Add(new WeightedCandidate(eventData, evaluation.FinalWeight));
            }

            if (candidates.Count == 0 || totalWeight <= 0d)
            {
                return new EventSelectionResult(context.Day, null, evaluations);
            }

            double roll = random.NextDouble() * totalWeight;
            double cumulative = 0d;
            EventData selectedEvent = candidates[candidates.Count - 1].EventData;
            foreach (WeightedCandidate candidate in candidates)
            {
                cumulative += candidate.Weight;
                if (roll < cumulative)
                {
                    selectedEvent = candidate.EventData;
                    break;
                }
            }

            return new EventSelectionResult(context.Day, selectedEvent, evaluations);
        }

        private EventCandidateEvaluation Evaluate(EventData eventData, EventSelectionContext context)
        {
            if (eventData == null || string.IsNullOrWhiteSpace(eventData.EventId))
            {
                return Reject(eventData, EventRejectionReason.MissingId, "Event ID is missing.");
            }

            if (context.Day < eventData.EarliestDay)
            {
                return Reject(eventData, EventRejectionReason.BeforeEarliestDay, $"Available from day {eventData.EarliestDay}.");
            }

            if (eventData.LatestDay > 0 && context.Day > eventData.LatestDay)
            {
                return Reject(eventData, EventRejectionReason.AfterLatestDay, $"Expired after day {eventData.LatestDay}.");
            }

            int appearanceCount = History.GetAppearanceCount(eventData.EventId);
            int appearanceLimit = eventData.MaximumAppearancesPerRun;
            if (appearanceLimit > 0 && appearanceCount >= appearanceLimit)
            {
                return Reject(eventData, EventRejectionReason.RepeatLimitReached, $"Appearance limit {appearanceLimit} reached.");
            }

            int? lastAppearanceDay = History.GetLastAppearanceDay(eventData.EventId);
            if (lastAppearanceDay.HasValue
                && context.Day - lastAppearanceDay.Value <= eventData.MinimumDaysBetweenAppearances)
            {
                return Reject(eventData, EventRejectionReason.CooldownActive, "Minimum day gap has not elapsed.");
            }

            if (!EvaluateConditions(eventData.EligibilityConditions, context, out string conditionFailure))
            {
                return Reject(eventData, EventRejectionReason.ConditionFailed, conditionFailure);
            }

            float modifierBonus = ResolveWeightModifierBonus(eventData.WeightModifiers, context);
            float weight = Mathf.Max(0f, eventData.BaseWeight + modifierBonus)
                * settings.GetMoraleMultiplier(eventData.MoodCategory, context.Morale)
                * settings.GetNegativePacingMultiplier(eventData.MoodCategory, context.Day, History.LastNegativeEventDay);

            if (weight <= 0f)
            {
                return Reject(eventData, EventRejectionReason.NoPositiveWeight, "Final weight is zero.");
            }

            return new EventCandidateEvaluation(eventData, true, weight, EventRejectionReason.None, string.Empty);
        }

        private bool EvaluateConditions(
            EventConditionGroup group,
            EventSelectionContext context,
            out string failure)
        {
            failure = string.Empty;
            if (group == null || !group.HasConditions)
            {
                return true;
            }

            bool aggregate = group.MatchMode == EventConditionMatchMode.All;
            foreach (EventConditionEntry condition in group.Conditions)
            {
                bool passed = condition != null
                    && condition.IsConfigured
                    && TryResolveConditionValue(condition, context, out int actualValue)
                    && Compare(actualValue, condition.RequiredValue, condition.Comparison);

                if (group.MatchMode == EventConditionMatchMode.All && !passed)
                {
                    aggregate = false;
                    failure = BuildConditionFailure(condition);
                    break;
                }

                if (group.MatchMode == EventConditionMatchMode.Any && passed)
                {
                    aggregate = true;
                    break;
                }
            }

            bool result = group.InvertResult ? !aggregate : aggregate;
            if (!result && string.IsNullOrWhiteSpace(failure))
            {
                failure = "No condition in the group passed.";
            }

            return result;
        }

        private bool TryResolveConditionValue(
            EventConditionEntry condition,
            EventSelectionContext context,
            out int value)
        {
            value = 0;
            switch (condition.ConditionType)
            {
                case EventConditionType.Morale:
                    value = context.Morale;
                    return true;
                case EventConditionType.Food:
                    value = context.Food;
                    return true;
                case EventConditionType.Resource:
                    value = context.Resource;
                    return true;
                case EventConditionType.MaxEnergy:
                    value = context.MaxEnergy;
                    return true;
                case EventConditionType.ActiveZombieCount:
                    value = context.ActiveZombieCount;
                    return true;
                case EventConditionType.BuildingLevel:
                    value = context.GetBuildingLevel(condition.TargetId);
                    return true;
                case EventConditionType.EventAppearanceCount:
                    value = History.GetAppearanceCount(condition.TargetId);
                    return true;
                case EventConditionType.DaysSinceLastNegativeEvent:
                    value = History.GetDaysSinceLastNegativeEvent(context.Day);
                    return true;
                case EventConditionType.RunFlag:
                    value = context.GetRunFlagValue(condition.RunFlag, condition.TargetId) > 0 ? 1 : 0;
                    return true;
                case EventConditionType.CustomValue:
                    value = context.GetCustomValue(condition.TargetKey);
                    return true;
                case EventConditionType.RunFlagValue:
                    value = context.GetRunFlagValue(condition.RunFlag, condition.TargetId);
                    return true;
                case EventConditionType.RunFlagDaysSinceSet:
                    value = context.GetRunFlagDaysSinceSet(condition.RunFlag, condition.TargetId);
                    return true;
                default:
                    return false;
            }
        }

        private static float ResolveWeightModifierBonus(
            IReadOnlyList<EventWeightModifierEntry> modifiers,
            EventSelectionContext context)
        {
            if (modifiers == null || modifiers.Count == 0 || context == null)
            {
                return 0f;
            }

            float total = 0f;
            for (int index = 0; index < modifiers.Count; index++)
            {
                EventWeightModifierEntry modifier = modifiers[index];
                if (modifier == null || !modifier.IsConfigured)
                {
                    continue;
                }

                float bonus = ResolveWeightModifierBonus(modifier, context);
                total += bonus;
            }

            return total;
        }

        private static float ResolveWeightModifierBonus(
            EventWeightModifierEntry modifier,
            EventSelectionContext context)
        {
            int sourceValue;
            switch (modifier.Source)
            {
                case EventWeightModifierSource.RunFlagDaysSinceSet:
                    sourceValue = context.GetRunFlagDaysSinceSet(modifier.RunFlag, modifier.TargetId);
                    break;
                default:
                    return 0f;
            }

            int effectiveUnits = Mathf.Max(0, sourceValue - modifier.IgnoredUnitsBeforeBonus);
            float bonus = effectiveUnits * modifier.WeightPerUnit;
            bonus = Mathf.Max(modifier.MinimumBonus, bonus);
            return modifier.MaximumBonus > 0f ? Mathf.Min(modifier.MaximumBonus, bonus) : bonus;
        }

        private static bool Compare(int actualValue, int requiredValue, EventValueComparison comparison)
        {
            switch (comparison)
            {
                case EventValueComparison.Equal:
                    return actualValue == requiredValue;
                case EventValueComparison.NotEqual:
                    return actualValue != requiredValue;
                case EventValueComparison.AtLeast:
                    return actualValue >= requiredValue;
                case EventValueComparison.AtMost:
                    return actualValue <= requiredValue;
                case EventValueComparison.GreaterThan:
                    return actualValue > requiredValue;
                case EventValueComparison.LessThan:
                    return actualValue < requiredValue;
                default:
                    return false;
            }
        }

        private static string BuildConditionFailure(EventConditionEntry condition)
        {
            if (condition == null)
            {
                return "A condition entry is null.";
            }

            if (!condition.IsConfigured)
            {
                return $"{condition.ConditionType} requires a target ID.";
            }

            return string.IsNullOrWhiteSpace(condition.DisplayName)
                ? $"{condition.ConditionType} condition failed."
                : $"Condition '{condition.DisplayName}' failed.";
        }

        private static EventCandidateEvaluation Reject(
            EventData eventData,
            EventRejectionReason reason,
            string detail)
        {
            return new EventCandidateEvaluation(eventData, false, 0f, reason, detail);
        }

        private readonly struct WeightedCandidate
        {
            public WeightedCandidate(EventData eventData, float weight)
            {
                EventData = eventData;
                Weight = weight;
            }

            public EventData EventData { get; }
            public float Weight { get; }
        }
    }
}
