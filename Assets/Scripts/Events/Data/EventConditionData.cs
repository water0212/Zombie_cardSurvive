using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieCardSurvive.Events.Data
{
    [Serializable]
    public sealed class EventConditionEntry
    {
        [SerializeField] private string displayName;
        [SerializeField] private EventConditionType conditionType;
        [SerializeField] private EventValueComparison comparison = EventValueComparison.AtLeast;
        [Tooltip("Building ID, Event ID, run flag, or custom value key. Simple value conditions do not use this field.")]
        [SerializeField] private string targetId;
        [Tooltip("Optional Run Flag asset. When assigned, it is used before Target ID.")]
        [SerializeField] private RunFlagData runFlag;
        [SerializeField] private int requiredValue;

        public string DisplayName => displayName;
        public EventConditionType ConditionType => conditionType;
        public EventValueComparison Comparison => comparison;
        public string TargetId => targetId;
        public RunFlagData RunFlag => runFlag;
        public string TargetKey => runFlag != null ? runFlag.FlagId : targetId;
        public int RequiredValue => requiredValue;
        public bool RequiresTargetId => conditionType == EventConditionType.BuildingLevel
            || conditionType == EventConditionType.EventAppearanceCount
            || conditionType == EventConditionType.RunFlag
            || conditionType == EventConditionType.CustomValue
            || conditionType == EventConditionType.RunFlagValue
            || conditionType == EventConditionType.RunFlagDaysSinceSet;

        public bool IsConfigured => !RequiresTargetId || !string.IsNullOrWhiteSpace(TargetKey);
    }

    [Serializable]
    public sealed class EventConditionGroup
    {
        [SerializeField] private EventConditionMatchMode matchMode = EventConditionMatchMode.All;
        [SerializeField] private bool invertResult;
        [SerializeField] private List<EventConditionEntry> conditions = new List<EventConditionEntry>();

        public EventConditionMatchMode MatchMode => matchMode;
        public bool InvertResult => invertResult;
        public IReadOnlyList<EventConditionEntry> Conditions => conditions;
        public bool HasConditions => conditions != null && conditions.Count > 0;
    }
}
