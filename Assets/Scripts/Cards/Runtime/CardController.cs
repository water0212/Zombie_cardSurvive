using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.Runtime
{
    public class CardController : MonoBehaviour
    {
        [Header("Deck")]
        [SerializeField] private List<CardBase> startingDeck = new List<CardBase>();
        [SerializeField] private DeckLayoutDefinition deckLayout;
        [SerializeField] private int drawCountPerTurn = 5;

        [Header("Run Settings")]
        [SerializeField] private int maxEnergy = 3;
        [SerializeField] private int startingFood;
        [SerializeField] private int startingResource;
        [SerializeField] private bool startDemoOnAwake = true;

        private readonly List<CardRuntime> drawPile = new List<CardRuntime>();
        private readonly List<CardRuntime> handCards = new List<CardRuntime>();
        private readonly List<CardRuntime> discardPile = new List<CardRuntime>();
        private readonly List<CardRuntime> playedCards = new List<CardRuntime>();
        private readonly List<CardRuntime> exhaustedCards = new List<CardRuntime>();
        private readonly List<CardExhaustionRecord> exhaustionRecords = new List<CardExhaustionRecord>();

        public event Action StateChanged;

        public IReadOnlyList<CardRuntime> DrawPile => drawPile;
        public IReadOnlyList<CardRuntime> HandCards => handCards;
        public IReadOnlyList<CardRuntime> DiscardPile => discardPile;
        public IReadOnlyList<CardRuntime> PlayedCards => playedCards;
        public IReadOnlyList<CardRuntime> ExhaustedCards => exhaustedCards;
        public IReadOnlyList<CardExhaustionRecord> ExhaustionRecords => exhaustionRecords;
        public DeckLayoutDefinition DeckLayout => deckLayout;

        public int DrawCountPerTurn => deckLayout != null ? deckLayout.DrawCountPerTurn : Mathf.Max(0, drawCountPerTurn);
        public int MaxEnergy => EnergySystem.MaxEnergy;
        public int CurrentEnergy => EnergySystem.CurrentEnergy;
        public int PendingDamage => CombatSystem.PendingDamage;
        public int ZombieThreatCount => CombatSystem.ZombieThreatCount;
        public int RegularDrawPileCount => CountCardsInDrawPile(false);
        public int AdditiveDrawPileCount => CountCardsInDrawPile(true);
        public bool IsDrawPileEmpty => RegularDrawPileCount == 0;
        public bool HasPendingExhaustionRefill => exhaustionRecords.Count > 0;
        public int PendingExhaustionRefillCount => exhaustionRecords.Count;

        private void Awake()
        {
            if (startDemoOnAwake)
            {
                StartDemoRun();
            }
        }

        [ContextMenu("Start Demo Run")]
        public void StartDemoRun()
        {
            drawPile.Clear();
            handCards.Clear();
            discardPile.Clear();
            playedCards.Clear();
            exhaustedCards.Clear();
            exhaustionRecords.Clear();

            FoodSystem.Reset(startingFood);
            ResourceSystem.Reset(startingResource);
            CombatSystem.Reset();
            EnergySystem.Reset(maxEnergy);

            DeckBuildResult buildResult = DeckBuildSystem.Build(deckLayout, startingDeck);
            foreach (CardRuntime card in buildResult.RegularCards)
            {
                AddToDrawPile(card);
            }

            foreach (CardRuntime card in buildResult.AdditiveCards)
            {
                AddToDrawPile(card);
            }

            Shuffle(drawPile);
            NotifyStateChanged();
        }

        [ContextMenu("Draw Turn Cards")]
        public void DrawTurnCards()
        {
            StartTurn();
        }

        [ContextMenu("Start New Round")]
        public void StartNewRound()
        {
            MoveCardsToDrawPile(handCards);
            MoveCardsToDrawPile(discardPile);
            MoveCardsToDrawPile(playedCards);

            handCards.Clear();
            discardPile.Clear();
            playedCards.Clear();

            RefillExhaustedSlotsWithDefaults();
            Shuffle(drawPile);
            NotifyStateChanged();
        }

        [ContextMenu("Start Turn")]
        public void StartTurn()
        {
            FoodSystem.ApplyTurnStartProduction();
            ResourceSystem.ApplyTurnStartProduction();
            EnergySystem.RefillToMax();
            DrawCards(DrawCountPerTurn);
            NotifyStateChanged();
        }

        public void DrawCards(int amount)
        {
            int drawAmount = Mathf.Max(0, amount);
            int regularCardsDrawn = 0;

            while (regularCardsDrawn < drawAmount && drawPile.Count > 0)
            {
                CardRuntime topCard = drawPile[0];
                drawPile.RemoveAt(0);

                if (topCard == null || topCard.Data == null)
                {
                    continue;
                }

                if (topCard.Data.EffectivePlayMode == CardPlayMode.AutoResolveOnDraw)
                {
                    bool countsAsReplacementDraw = topCard.Data.DrawReplacementOnAutoResolve;
                    ResolveAutoDrawCard(topCard);

                    if (!countsAsReplacementDraw)
                    {
                        regularCardsDrawn++;
                    }

                    continue;
                }

                topCard.SetZone(DeckZone.Hand);
                handCards.Add(topCard);
                regularCardsDrawn++;
            }

            NotifyStateChanged();
        }

        public bool RequestPlayCard(CardRuntime card)
        {
            if (card == null || card.Data == null)
            {
                return false;
            }

            if (!handCards.Contains(card))
            {
                return false;
            }

            if (card.Data.EffectivePlayMode != CardPlayMode.Playable)
            {
                NotifyStateChanged();
                return false;
            }

            if (!card.HasRemainingUses)
            {
                NotifyStateChanged();
                return false;
            }

            if (!EnergySystem.CanSpend(card.Data.EnergyCost))
            {
                NotifyStateChanged();
                return false;
            }

            CardPlayContext context = new CardPlayContext(this, card);

            if (!card.Data.CanPayCosts(context))
            {
                NotifyStateChanged();
                return false;
            }

            if (!card.Data.PayCosts(context))
            {
                NotifyStateChanged();
                return false;
            }

            EnergySystem.SpendEnergy(card.Data.EnergyCost);
            handCards.Remove(card);
            card.Data.Resolve(context);

            if (card.ConsumeUse())
            {
                MoveToExhausted(card);
            }
            else
            {
                MoveToDiscardPile(card);
            }

            NotifyStateChanged();
            return true;
        }

        public bool DiscardCard(CardRuntime card)
        {
            if (card == null)
            {
                return false;
            }

            bool removed = handCards.Remove(card) || playedCards.Remove(card);
            if (!removed)
            {
                return false;
            }

            MoveToDiscardPile(card);
            NotifyStateChanged();
            return true;
        }

        [ContextMenu("Discard Hand")]
        public void DiscardHand()
        {
            MoveCardsToDiscardPile(handCards);
            handCards.Clear();
            NotifyStateChanged();
        }

        [ContextMenu("End Turn")]
        public void EndTurn()
        {
            EndTurnCleanup();
        }

        [ContextMenu("End Turn Cleanup")]
        public void EndTurnCleanup()
        {
            MoveCardsToDiscardPile(handCards);
            MoveCardsToDiscardPile(playedCards);
            handCards.Clear();
            playedCards.Clear();

            if (IsDrawPileEmpty)
            {
                RevealRemainingAdditiveCardsForRound();
            }

            NotifyStateChanged();
        }

        public void RevealRemainingAdditiveCardsForRound()
        {
            for (int i = drawPile.Count - 1; i >= 0; i--)
            {
                CardRuntime card = drawPile[i];
                if (card == null || !card.IsAdditiveInstance)
                {
                    continue;
                }

                drawPile.RemoveAt(i);
                ResolveAutoDrawCard(card);
            }

            NotifyStateChanged();
        }

        public bool SwapCards(int firstHandIndex, int secondHandIndex)
        {
            if (!IsValidHandIndex(firstHandIndex) || !IsValidHandIndex(secondHandIndex))
            {
                return false;
            }

            (handCards[firstHandIndex], handCards[secondHandIndex]) = (handCards[secondHandIndex], handCards[firstHandIndex]);
            NotifyStateChanged();
            return true;
        }

        public List<CardRuntime> GetReplaceableRegularCards()
        {
            List<CardRuntime> cards = new List<CardRuntime>();
            AddReplaceableCards(drawPile, cards);
            AddReplaceableCards(handCards, cards);
            AddReplaceableCards(discardPile, cards);
            AddReplaceableCards(playedCards, cards);
            return cards;
        }

        public List<CardRuntime> GetExhaustedRefillTargets()
        {
            List<CardRuntime> cards = new List<CardRuntime>();
            foreach (CardExhaustionRecord record in exhaustionRecords)
            {
                if (record != null && record.Card != null)
                {
                    cards.Add(record.Card);
                }
            }

            return cards;
        }

        public bool TryReplaceRuntimeCard(CardRuntime target, CardBase replacementData, out CardRuntime replacementRuntime)
        {
            replacementRuntime = null;

            if (target == null || replacementData == null || target.IsAdditiveInstance)
            {
                return false;
            }

            List<CardRuntime> zoneList = GetZoneList(target);
            if (zoneList == null)
            {
                return false;
            }

            int index = zoneList.IndexOf(target);
            if (index < 0)
            {
                return false;
            }

            replacementRuntime = replacementData.CreateRuntimeCard();
            replacementRuntime.SetAssignedSlotType(target.AssignedSlotType);
            replacementRuntime.SetZone(target.Zone);
            zoneList[index] = replacementRuntime;
            target.SetZone(DeckZone.Removed);

            NotifyStateChanged();
            return true;
        }

        public bool TryRefillExhaustedSlot(CardRuntime exhaustedCard, CardBase replacementData, out CardRuntime replacementRuntime)
        {
            replacementRuntime = null;

            if (exhaustedCard == null || replacementData == null)
            {
                return false;
            }

            CardExhaustionRecord record = FindExhaustionRecord(exhaustedCard);
            if (record == null)
            {
                return false;
            }

            replacementRuntime = replacementData.CreateRuntimeCard();
            replacementRuntime.SetAssignedSlotType(record.SlotType);
            AddToDrawPile(replacementRuntime);
            exhaustionRecords.Remove(record);

            NotifyStateChanged();
            return true;
        }

        private bool IsValidHandIndex(int index)
        {
            return index >= 0 && index < handCards.Count;
        }

        private static void AddReplaceableCards(IEnumerable<CardRuntime> source, List<CardRuntime> target)
        {
            foreach (CardRuntime card in source)
            {
                if (card != null && !card.IsAdditiveInstance && !card.IsExhausted)
                {
                    target.Add(card);
                }
            }
        }

        private List<CardRuntime> GetZoneList(CardRuntime card)
        {
            if (drawPile.Contains(card))
            {
                return drawPile;
            }

            if (handCards.Contains(card))
            {
                return handCards;
            }

            if (discardPile.Contains(card))
            {
                return discardPile;
            }

            if (playedCards.Contains(card))
            {
                return playedCards;
            }

            return null;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        private void ResolveAutoDrawCard(CardRuntime card)
        {
            if (card == null || card.Data == null)
            {
                return;
            }

            CardPlayContext context = new CardPlayContext(this, card);
            card.Data.Resolve(context);
            card.SetZone(DeckZone.Removed);
        }

        private void MoveCardsToDrawPile(List<CardRuntime> cards)
        {
            foreach (CardRuntime card in cards)
            {
                if (card == null || card.IsExhausted)
                {
                    continue;
                }

                AddToDrawPile(card);
            }
        }

        private void AddToDrawPile(CardRuntime card)
        {
            if (card == null)
            {
                return;
            }

            card.SetZone(DeckZone.DrawPile);
            drawPile.Add(card);
        }

        private void MoveCardsToDiscardPile(List<CardRuntime> cards)
        {
            foreach (CardRuntime card in cards)
            {
                MoveToDiscardPile(card);
            }
        }

        private void MoveToDiscardPile(CardRuntime card)
        {
            if (card == null || card.IsExhausted)
            {
                return;
            }

            card.SetZone(DeckZone.DiscardPile);
            discardPile.Add(card);
        }

        private void MoveToExhausted(CardRuntime card)
        {
            if (card == null)
            {
                return;
            }

            card.MarkExhausted();
            exhaustedCards.Add(card);
            exhaustionRecords.Add(new CardExhaustionRecord(card, card.AssignedSlotType));
        }

        public void RefillExhaustedSlotsWithDefaults()
        {
            if (deckLayout == null || exhaustionRecords.Count == 0)
            {
                return;
            }

            foreach (CardExhaustionRecord record in exhaustionRecords)
            {
                if (record == null)
                {
                    continue;
                }

                CardBase defaultCard = deckLayout.GetDefaultCardForSlot(record.SlotType);
                if (defaultCard == null)
                {
                    Debug.LogWarning($"No default card configured for exhausted slot {record.SlotType}.");
                    continue;
                }

                CardRuntime replacement = defaultCard.CreateRuntimeCard();
                replacement.SetAssignedSlotType(record.SlotType);
                AddToDrawPile(replacement);
            }

            exhaustionRecords.Clear();
        }

        private CardExhaustionRecord FindExhaustionRecord(CardRuntime exhaustedCard)
        {
            foreach (CardExhaustionRecord record in exhaustionRecords)
            {
                if (record != null && ReferenceEquals(record.Card, exhaustedCard))
                {
                    return record;
                }
            }

            return null;
        }

        private int CountCardsInDrawPile(bool additive)
        {
            int count = 0;
            foreach (CardRuntime card in drawPile)
            {
                if (card != null && card.IsAdditiveInstance == additive)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
