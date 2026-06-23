using System.Collections.Generic;

namespace ZombieCardSurvive.Cards.Runtime
{
    public class DeckBuildResult
    {
        public DeckBuildResult(List<CardRuntime> regularCards, List<CardRuntime> additiveCards)
        {
            RegularCards = regularCards ?? new List<CardRuntime>();
            AdditiveCards = additiveCards ?? new List<CardRuntime>();
        }

        public List<CardRuntime> RegularCards { get; }
        public List<CardRuntime> AdditiveCards { get; }
    }
}
