using ZombieCardSurvive.Cards.Runtime;

namespace ZombieCardSurvive.Effects.Data
{
    public readonly struct GameEffectContext
    {
        private GameEffectContext(GameEffectCarrier carrier, CardPlayContext cardContext)
        {
            Carrier = carrier;
            CardContext = cardContext;
        }

        public GameEffectCarrier Carrier { get; }
        public CardPlayContext CardContext { get; }
        public CardController CardController => CardContext.Controller;
        public CardRuntime Card => CardContext.Card;

        public static GameEffectContext ForCard(CardPlayContext cardContext)
        {
            return new GameEffectContext(GameEffectCarrier.Card, cardContext);
        }

        public static GameEffectContext ForEvent()
        {
            return new GameEffectContext(GameEffectCarrier.Event, default);
        }

        public static GameEffectContext ForOther()
        {
            return new GameEffectContext(GameEffectCarrier.Other, default);
        }
    }
}
