using System;
using UnityEngine;
using ZombieCardSurvive.Effects.Data;

namespace ZombieCardSurvive.Events.Data
{
    [Serializable]
    public sealed class EventForcedLossConsequenceData
    {
        [Tooltip("Zero-based index of the ForcedLoss entry in this choice's Effects list.")]
        [Min(0)] [SerializeField] private int sourceEffectIndex;
        [SerializeField] private EventShortageConsequenceAmountMode amountMode =
            EventShortageConsequenceAmountMode.PerMissingUnit;
        [SerializeField] private GameEffectData consequenceEffect = new GameEffectData();

        public int SourceEffectIndex => Mathf.Max(0, sourceEffectIndex);
        public EventShortageConsequenceAmountMode AmountMode => amountMode;
        public GameEffectData ConsequenceEffect => consequenceEffect;
    }
}
