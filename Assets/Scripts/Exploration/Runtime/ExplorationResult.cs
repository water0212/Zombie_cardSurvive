using System.Collections.Generic;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Exploration.Data;

namespace ZombieCardSurvive.Exploration.Runtime
{
    public class ExplorationResult
    {
        private readonly List<CardPackData> openedPacks = new List<CardPackData>();
        private readonly List<CardBase> storedCards = new List<CardBase>();
        private readonly List<ZombieCardData> zombieCardsAddedToDiscard = new List<ZombieCardData>();
        private readonly List<string> messages = new List<string>();

        public IReadOnlyList<CardPackData> OpenedPacks => openedPacks;
        public IReadOnlyList<CardBase> StoredCards => storedCards;
        public IReadOnlyList<ZombieCardData> ZombieCardsAddedToDiscard => zombieCardsAddedToDiscard;
        public IReadOnlyList<string> Messages => messages;

        public void AddOpenedPack(CardPackData pack)
        {
            if (pack != null)
            {
                openedPacks.Add(pack);
            }
        }

        public void AddStoredCard(CardBase card)
        {
            if (card != null)
            {
                storedCards.Add(card);
            }
        }

        public void AddZombieCard(ZombieCardData card)
        {
            if (card != null)
            {
                zombieCardsAddedToDiscard.Add(card);
            }
        }

        public void AddMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                messages.Add(message);
            }
        }

        public string BuildSummary()
        {
            List<string> lines = new List<string>();

            if (openedPacks.Count > 0)
            {
                lines.Add("開啟卡包：" + JoinPackNames());
            }

            if (storedCards.Count > 0)
            {
                lines.Add("加入倉庫：" + JoinCardNames(storedCards));
            }

            if (zombieCardsAddedToDiscard.Count > 0)
            {
                lines.Add("污染進入棄牌堆：" + JoinZombieNames());
            }

            foreach (string message in messages)
            {
                lines.Add(message);
            }

            return lines.Count > 0 ? string.Join("\n", lines) : "沒有獲得任何結果。";
        }

        private string JoinPackNames()
        {
            List<string> names = new List<string>();
            foreach (CardPackData pack in openedPacks)
            {
                names.Add(!string.IsNullOrWhiteSpace(pack.DisplayName) ? pack.DisplayName : pack.name);
            }

            return string.Join("、", names);
        }

        private static string JoinCardNames(IEnumerable<CardBase> cards)
        {
            List<string> names = new List<string>();
            foreach (CardBase card in cards)
            {
                names.Add(!string.IsNullOrWhiteSpace(card.DisplayName) ? card.DisplayName : card.name);
            }

            return string.Join("、", names);
        }

        private string JoinZombieNames()
        {
            List<CardBase> cards = new List<CardBase>();
            foreach (ZombieCardData zombieCard in zombieCardsAddedToDiscard)
            {
                cards.Add(zombieCard);
            }

            return JoinCardNames(cards);
        }
    }

    public enum ExplorationFlowState
    {
        Hidden,
        ChoosingRegion,
        Result
    }
}
