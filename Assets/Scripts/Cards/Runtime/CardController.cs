using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Buildings.Runtime;
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
        private readonly List<DeckVacancyRecord> vacancyRecords = new List<DeckVacancyRecord>();
        private int drawOperationDepth;

        public event Action StateChanged;

        public IReadOnlyList<CardRuntime> DrawPile => drawPile;
        public IReadOnlyList<CardRuntime> HandCards => handCards;
        public IReadOnlyList<CardRuntime> DiscardPile => discardPile;
        public IReadOnlyList<CardRuntime> PlayedCards => playedCards;
        public IReadOnlyList<CardRuntime> ExhaustedCards => exhaustedCards;
        public IReadOnlyList<DeckVacancyRecord> VacancyRecords => vacancyRecords;
        public DeckLayoutDefinition DeckLayout => deckLayout;

        public int DrawCountPerTurn => deckLayout != null ? deckLayout.DrawCountPerTurn : Mathf.Max(0, drawCountPerTurn);
        public int MaxEnergy => EnergySystem.MaxEnergy;
        public int CurrentEnergy => EnergySystem.CurrentEnergy;
        public int PendingDamage => CombatSystem.PendingDamage;
        public int ZombieThreatCount => CombatSystem.ZombieThreatCount;
        public int RegularDrawPileCount => CountCardsInDrawPile(false);
        public int AdditiveDrawPileCount => CountCardsInDrawPile(true);
        public bool IsDrawPileEmpty => RegularDrawPileCount == 0;
        public bool HasPendingVacancyRefill => vacancyRecords.Count > 0;
        public int PendingVacancyRefillCount => vacancyRecords.Count;
        public bool HasPendingExhaustionRefill => HasPendingVacancyRefill;
        public int PendingExhaustionRefillCount => PendingVacancyRefillCount;

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
            vacancyRecords.Clear();

            ResourceReservationService.Reset();
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

            RefillVacantSlotsWithDefaults();
            Shuffle(drawPile);
            NotifyStateChanged();
        }

        [ContextMenu("Start Turn")]
        public void StartTurn()
        {
            FoodPlanSystem.BeginTurn();
            FoodSystem.ApplyTurnStartProduction();
            ResourceSystem.ApplyTurnStartProduction();
            EnergySystem.RefillToMax();
            DrawCards(DrawCountPerTurn);
            NotifyStateChanged();
        }

        public void DrawCards(int amount)
        {
            drawOperationDepth++;
            int drawAmount = Mathf.Max(0, amount);
            int regularCardsDrawn = 0;

            try
            {
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
                        bool countsAsReplacementDraw = topCard.Data.EffectiveDrawReplacementOnAutoResolve;
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
            }
            finally
            {
                drawOperationDepth--;
            }

            if (drawOperationDepth == 0)
            {
                NotifyStateChanged();
            }
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

        public bool AddCardToDiscardPile(CardBase cardData)
        {
            if (cardData == null)
            {
                return false;
            }

            CardRuntime runtime = cardData.CreateRuntimeCard();
            MoveToDiscardPile(runtime);
            NotifyStateChanged();
            return true;
        }

        public int RemoveRuntimeCardsByInstanceIds(IEnumerable<string> instanceIds, DeckVacancyReason reason)
        {
            if (instanceIds == null)
            {
                return 0;
            }

            HashSet<string> ids = new HashSet<string>(instanceIds);
            int removed = 0;
            removed += RemoveMatchingCards(drawPile, ids, reason);
            removed += RemoveMatchingCards(handCards, ids, reason);
            removed += RemoveMatchingCards(discardPile, ids, reason);
            removed += RemoveMatchingCards(playedCards, ids, reason);
            removed += RemoveMatchingCards(exhaustedCards, ids, reason);

            if (removed > 0)
            {
                NotifyStateChanged();
            }

            return removed;
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

        public List<CardRuntime> GetVacancyRefillTargets()
        {
            List<CardRuntime> cards = new List<CardRuntime>();
            foreach (DeckVacancyRecord record in vacancyRecords)
            {
                if (record != null && record.SourceCard != null)
                {
                    cards.Add(record.SourceCard);
                }
            }

            return cards;
        }

        public List<CardRuntime> GetExhaustedRefillTargets()
        {
            return GetVacancyRefillTargets();
        }

        public bool TryReplaceRuntimeCard(CardRuntime target, CardBase replacementData, out CardRuntime replacementRuntime)
        {
            return TryReplaceRuntimeCard(target, replacementData != null ? new CardInventoryEntry(replacementData) : null, out replacementRuntime);
        }

        public bool TryReplaceRuntimeCard(CardRuntime target, CardInventoryEntry replacementEntry, out CardRuntime replacementRuntime)
        {
            replacementRuntime = null;

            if (target == null || replacementEntry == null || replacementEntry.Data == null || target.IsAdditiveInstance)
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

            replacementRuntime = replacementEntry.CreateRuntimeCard();
            replacementRuntime.SetAssignedSlotType(target.AssignedSlotType);
            replacementRuntime.SetZone(target.Zone);
            zoneList[index] = replacementRuntime;
            target.SetZone(DeckZone.Removed);

            NotifyStateChanged();
            return true;
        }

        public bool TryFillVacantSlot(CardRuntime sourceCard, CardBase replacementData, out CardRuntime replacementRuntime)
        {
            return TryFillVacantSlot(sourceCard, replacementData != null ? new CardInventoryEntry(replacementData) : null, out replacementRuntime);
        }

        public bool TryFillVacantSlot(CardRuntime sourceCard, CardInventoryEntry replacementEntry, out CardRuntime replacementRuntime)
        {
            replacementRuntime = null;

            if (sourceCard == null || replacementEntry == null || replacementEntry.Data == null)
            {
                return false;
            }

            DeckVacancyRecord record = FindVacancyRecord(sourceCard);
            if (record == null)
            {
                return false;
            }

            replacementRuntime = replacementEntry.CreateRuntimeCard();
            replacementRuntime.SetAssignedSlotType(record.SlotType);
            AddToDrawPile(replacementRuntime);
            vacancyRecords.Remove(record);

            NotifyStateChanged();
            return true;
        }

        public bool TryRefillExhaustedSlot(CardRuntime exhaustedCard, CardBase replacementData, out CardRuntime replacementRuntime)
        {
            return TryFillVacantSlot(exhaustedCard, replacementData, out replacementRuntime);
        }

        public bool TryRefillExhaustedSlot(CardRuntime exhaustedCard, CardInventoryEntry replacementEntry, out CardRuntime replacementRuntime)
        {
            return TryFillVacantSlot(exhaustedCard, replacementEntry, out replacementRuntime);
        }

        public bool HasVacancyFor(CardRuntime sourceCard)
        {
            return FindVacancyRecord(sourceCard) != null;
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
            if (card.Data == null || card.Data.CreatesExhaustionRefillRequest)
            {
                vacancyRecords.Add(new DeckVacancyRecord(card, card.AssignedSlotType, DeckVacancyReason.Exhausted));
            }
        }

        public void RefillVacantSlotsWithDefaults()
        {
            if (deckLayout == null || vacancyRecords.Count == 0)
            {
                return;
            }

            foreach (DeckVacancyRecord record in vacancyRecords)
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

            vacancyRecords.Clear();
        }

        public void RefillExhaustedSlotsWithDefaults()
        {
            RefillVacantSlotsWithDefaults();
        }

        private DeckVacancyRecord FindVacancyRecord(CardRuntime sourceCard)
        {
            foreach (DeckVacancyRecord record in vacancyRecords)
            {
                if (record != null && ReferenceEquals(record.SourceCard, sourceCard))
                {
                    return record;
                }
            }

            return null;
        }

        private int RemoveMatchingCards(List<CardRuntime> cards, HashSet<string> instanceIds, DeckVacancyReason reason)
        {
            int removed = 0;
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                CardRuntime card = cards[i];
                if (card == null || !instanceIds.Contains(card.RuntimeId))
                {
                    continue;
                }

                cards.RemoveAt(i);
                if (!card.IsAdditiveInstance && !HasVacancyFor(card))
                {
                    vacancyRecords.Add(new DeckVacancyRecord(card, card.AssignedSlotType, reason));
                }

                card.SetZone(DeckZone.Removed);
                removed++;
            }

            return removed;
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
