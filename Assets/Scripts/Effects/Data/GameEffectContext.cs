using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Effects.Data
{
    public readonly struct GameEffectContext
    {
        private GameEffectContext(GameEffectCarrier carrier, CardPlayContext cardContext)
            : this(carrier, cardContext, null, null, 0)
        {
        }

        private GameEffectContext(GameEffectCarrier carrier, CardPlayContext cardContext, CardController eventCardController)
            : this(carrier, cardContext, eventCardController, null, 0)
        {
        }

        private GameEffectContext(
            GameEffectCarrier carrier,
            CardPlayContext cardContext,
            CardController eventCardController,
            RunFlagRuntimeStore runFlags,
            int eventDay)
        {
            Carrier = carrier;
            CardContext = cardContext;
            EventCardController = eventCardController;
            RunFlags = runFlags;
            EventDay = eventDay;
        }

        public GameEffectCarrier Carrier { get; }
        public CardPlayContext CardContext { get; }
        public CardController EventCardController { get; }
        public RunFlagRuntimeStore RunFlags { get; }
        public int EventDay { get; }
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

        public static GameEffectContext ForEvent(RunFlagRuntimeStore runFlags, int eventDay)
        {
            return new GameEffectContext(GameEffectCarrier.Event, default, null, runFlags, eventDay);
        }

        public static GameEffectContext ForEvent(CardController cardController, RunFlagRuntimeStore runFlags, int eventDay)
        {
            return new GameEffectContext(GameEffectCarrier.Event, default, cardController, runFlags, eventDay);
        }

        public static GameEffectContext ForOther()
        {
            return new GameEffectContext(GameEffectCarrier.Other, default);
        }
    }
}
