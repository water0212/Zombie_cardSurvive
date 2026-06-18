using System;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    [Serializable]
    public class CardRuntime
    {
        public CardRuntime(CardBase data)
        {
            RuntimeId = Guid.NewGuid().ToString("N");
            Data = data;
        }

        public string RuntimeId { get; }
        public CardBase Data { get; }
    }
}
