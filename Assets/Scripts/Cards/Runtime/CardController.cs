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

        public event Action StateChanged;

        public IReadOnlyList<CardRuntime> DrawPile => drawPile;
        public IReadOnlyList<CardRuntime> HandCards => handCards;
        public IReadOnlyList<CardRuntime> DiscardPile => discardPile;
        public IReadOnlyList<CardRuntime> PlayedCards => playedCards;

        public int DrawCountPerTurn => Mathf.Max(0, drawCountPerTurn);
        public int MaxEnergy => EnergySystem.MaxEnergy;
        public int CurrentEnergy => EnergySystem.CurrentEnergy;
        public int PendingDamage => CombatSystem.PendingDamage;
        public int ZombieThreatCount => CombatSystem.ZombieThreatCount;
        public bool IsDrawPileEmpty => drawPile.Count == 0;

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

            FoodSystem.Reset(startingFood);
            ResourceSystem.Reset(startingResource);
            CombatSystem.Reset();
            EnergySystem.Reset(maxEnergy);

            foreach (CardBase cardData in startingDeck)
            {
                if (cardData == null)
                {
                    continue;
                }

                drawPile.Add(cardData.CreateRuntimeCard());
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
            drawPile.AddRange(handCards);
            drawPile.AddRange(discardPile);
            drawPile.AddRange(playedCards);

            handCards.Clear();
            discardPile.Clear();
            playedCards.Clear();

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

            for (int i = 0; i < drawAmount; i++)
            {
                if (drawPile.Count == 0)
                {
                    break;
                }

                CardRuntime topCard = drawPile[0];
                drawPile.RemoveAt(0);
                handCards.Add(topCard);
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

            discardPile.Add(card);
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

            discardPile.Add(card);
            NotifyStateChanged();
            return true;
        }

        [ContextMenu("Discard Hand")]
        public void DiscardHand()
        {
            discardPile.AddRange(handCards);
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
            discardPile.AddRange(handCards);
            discardPile.AddRange(playedCards);
            handCards.Clear();
            playedCards.Clear();
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

        private bool IsValidHandIndex(int index)
        {
            return index >= 0 && index < handCards.Count;
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
    }
}
