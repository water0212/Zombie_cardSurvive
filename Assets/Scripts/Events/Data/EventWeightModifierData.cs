using System;
using UnityEngine;

namespace ZombieCardSurvive.Events.Data
{
    public enum EventWeightModifierSource
    {
        RunFlagDaysSinceSet
    }

    [Serializable]
    public sealed class EventWeightModifierEntry
    {
        [SerializeField] private string displayName;
        [SerializeField] private EventWeightModifierSource source;
        [SerializeField] private RunFlagData runFlag;
        [Tooltip("Fallback key used when Run Flag is empty.")]
        [SerializeField] private string targetId;
        [Tooltip("How many source units are ignored before the bonus starts.")]
        [Min(0)]
        [SerializeField] private int ignoredUnitsBeforeBonus;
        [SerializeField] private float weightPerUnit;
        [SerializeField] private float minimumBonus;
        [Tooltip("0 means this modifier has no maximum bonus.")]
        [Min(0f)]
        [SerializeField] private float maximumBonus;

        public string DisplayName => displayName;
        public EventWeightModifierSource Source => source;
        public RunFlagData RunFlag => runFlag;
        public string TargetId => targetId;
        public string TargetKey => runFlag != null ? runFlag.FlagId : targetId;
        public int IgnoredUnitsBeforeBonus => Mathf.Max(0, ignoredUnitsBeforeBonus);
        public float WeightPerUnit => weightPerUnit;
        public float MinimumBonus => minimumBonus;
        public float MaximumBonus => Mathf.Max(0f, maximumBonus);
        public bool IsConfigured => !string.IsNullOrWhiteSpace(TargetKey);
    }
}
