using System;
using UnityEngine;

namespace ZombieCardSurvive.Events.Data
{
    [Serializable]
    public sealed class EventEffectScalingData
    {
        [SerializeField] private EventScaleSource scaleSource = EventScaleSource.Fixed;
        [Min(0f)] [SerializeField] private float scalingStrength = 1f;
        [Min(0)] [SerializeField] private int minimumFinalMagnitude;
        [Tooltip("0 means no maximum.")]
        [Min(0)] [SerializeField] private int maximumFinalMagnitude;
        [SerializeField] private EventScaleRoundingMode roundingMode = EventScaleRoundingMode.Round;

        public EventScaleSource ScaleSource => scaleSource;
        public float ScalingStrength => Mathf.Max(0f, scalingStrength);
        public int MinimumFinalMagnitude => Mathf.Max(0, minimumFinalMagnitude);
        public int MaximumFinalMagnitude => Mathf.Max(0, maximumFinalMagnitude);
        public EventScaleRoundingMode RoundingMode => roundingMode;
    }
}
