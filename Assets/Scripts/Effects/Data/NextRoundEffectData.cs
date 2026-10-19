using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ZombieCardSurvive.Effects.Data
{
    public enum NextRoundEffectType
    {
        IncreaseNextRoundFoodDemand
    }

    [Serializable]
    public sealed class NextRoundEffectEntry
    {
        [SerializeField] private NextRoundEffectType effectType;
        [Min(0)]
        [SerializeField] private int amount;

        public NextRoundEffectType EffectType => effectType;
        public int Amount => Mathf.Max(0, amount);
    }

    /// <summary>
    /// 可由殭屍、卡牌或事件引用的下輪效果定義。
    /// Asset 只描述效果內容，不保存本局排程或來源狀態。
    /// </summary>
    [CreateAssetMenu(
        fileName = "NextRoundEffect",
        menuName = "Zombie Card Survive/Effects/Next Round Effect")]
    public sealed class NextRoundEffectData : ScriptableObject
    {
        [FormerlySerializedAs("consequenceId")]
        [SerializeField] private string nextRoundEffectId;
        [SerializeField] private List<NextRoundEffectEntry> effects =
            new List<NextRoundEffectEntry>();

        public string NextRoundEffectId => string.IsNullOrWhiteSpace(nextRoundEffectId)
            ? name
            : nextRoundEffectId;
        public IReadOnlyList<NextRoundEffectEntry> Effects => effects;

        public int GetNextRoundFoodDemandModifier()
        {
            long total = 0;
            if (effects == null)
            {
                return 0;
            }

            foreach (NextRoundEffectEntry effect in effects)
            {
                if (effect == null
                    || effect.EffectType != NextRoundEffectType.IncreaseNextRoundFoodDemand)
                {
                    continue;
                }

                total += effect.Amount;
                if (total >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }

            return (int)total;
        }

        public bool HasExecutableEffects => GetNextRoundFoodDemandModifier() > 0;
    }
}
