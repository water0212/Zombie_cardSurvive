using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Effects.Data;

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

        public string ChoiceText => choiceText;
        public string ResultTitle => resultTitle;
        public string ResultDescription => resultDescription;
        public Sprite ResultArtwork => resultArtwork;
        public IReadOnlyList<GameEffectData> Effects => effects;

        public void ApplyEffects()
        {
            GameEffectContext context = GameEffectContext.ForEvent();
            foreach (GameEffectData effect in effects)
            {
                effect?.Apply(context);
            }
        }
    }
}
