using System.Collections.Generic;

namespace ZombieCardSurvive.Cards.Runtime
{
    public sealed class CardSelectionRequest
    {
        private readonly List<CardRuntime> sourceCards = new List<CardRuntime>();

        public CardSelectionRequest(
            string title,
            int selectionCount,
            IEnumerable<CardRuntime> sourceCards,
            CardSelectionFilter filter)
        {
            Title = string.IsNullOrWhiteSpace(title) ? "選擇卡牌" : title;
            SelectionCount = selectionCount < 1 ? 1 : selectionCount;
            Filter = filter;

            if (sourceCards == null)
            {
                return;
            }

            foreach (CardRuntime card in sourceCards)
            {
                if (card != null && !this.sourceCards.Contains(card))
                {
                    this.sourceCards.Add(card);
                }
            }
        }

        public string Title { get; }
        public int SelectionCount { get; }
        public IReadOnlyList<CardRuntime> SourceCards => sourceCards;
        public CardSelectionFilter Filter { get; }

        public List<CardRuntime> GetValidCards()
        {
            List<CardRuntime> validCards = new List<CardRuntime>();
            foreach (CardRuntime card in sourceCards)
            {
                if (Filter == null || Filter.Allows(card))
                {
                    validCards.Add(card);
                }
            }

            return validCards;
        }
    }
}
