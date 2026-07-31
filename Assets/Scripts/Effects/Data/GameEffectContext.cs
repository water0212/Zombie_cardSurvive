using ZombieCardSurvive.Cards.Runtime;

namespace ZombieCardSurvive.Effects.Data
{
    public readonly struct GameEffectContext
    {
        private GameEffectContext(GameEffectCarrier carrier, CardPlayContext cardContext)
            : this(carrier, cardContext, null)
        {
        }

        private GameEffectContext(GameEffectCarrier carrier, CardPlayContext cardContext, CardController eventCardController)
        {
            Carrier = carrier;
            CardContext = cardContext;
            EventCardController = eventCardController;
        }

        public GameEffectCarrier Carrier { get; }
        public CardPlayContext CardContext { get; }
        public CardController EventCardController { get; }
        public CardController CardController => CardContext.Controller != null ? CardContext.Controller : EventCardController;
        public CardRuntime Card => CardContext.Card;

        public static GameEffectContext ForCard(CardPlayContext cardContext)
        {
            return new GameEffectContext(GameEffectCarrier.Card, cardContext);
        }

        public static GameEffectContext ForEvent()
        {
            return new GameEffectContext(GameEffectCarrier.Event, default);
        }

        public static GameEffectContext ForEvent(CardController cardController)
        {
            return new GameEffectContext(GameEffectCarrier.Event, default, cardController);
        }

        public static GameEffectContext ForOther()
        {
            return new GameEffectContext(GameEffectCarrier.Other, default);
        }
    }
}
