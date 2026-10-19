using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Events.Data
{
    [Serializable]
    public class EventChoiceData
    {
        [SerializeField] private string choiceText;
        [SerializeField] private string resultTitle;
        [TextArea]
        [SerializeField] private string resultDescription;
        [SerializeField] private Sprite resultArtwork;
        [SerializeField] private List<GameEffectData> effects = new List<GameEffectData>();
        [SerializeField] private List<EventForcedLossConsequenceData> forcedLossConsequences =
            new List<EventForcedLossConsequenceData>();

        public string ChoiceText => choiceText;
        public string ResultTitle => resultTitle;
        public string ResultDescription => resultDescription;
        public Sprite ResultArtwork => resultArtwork;
        public IReadOnlyList<GameEffectData> Effects => effects;
        public IReadOnlyList<EventForcedLossConsequenceData> ForcedLossConsequences => forcedLossConsequences;

        public void ApplyEffects()
        {
            GameEffectContext context = GameEffectContext.ForEvent();
            ApplyEffects(context);
        }

        public void ApplyEffects(GameEffectContext context)
        {
            foreach (GameEffectData effect in effects)
            {
                if (effect != null && !effect.CanApply(context))
                {
                    return;
                }
            }

            MoraleBatchRequest moraleBatch = new MoraleBatchRequest("LegacyEventChoice");
            for (int index = 0; index < effects.Count; index++)
            {
                GameEffectData effect = effects[index];
                if (effect == null || !effect.ShouldApplyForCurrentState())
                {
                    continue;
                }

                if (effect.IsMoraleEffect)
                {
                    moraleBatch.Add(
                        $"EventEffect[{index}]",
                        effect.ResolveMoraleDelta(effect.Amount));
                }
                else
                {
                    effect.Apply(context);
                }
            }

            if (moraleBatch.Count > 0)
            {
                MoraleSystem.CommitBatch(moraleBatch);
            }
        }
    }
}
