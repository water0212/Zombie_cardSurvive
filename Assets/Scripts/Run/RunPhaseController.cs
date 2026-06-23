using System;
using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Cards.UI.Replacement;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Run
{
    public class RunPhaseController : MonoBehaviour
    {
        [SerializeField] private CardController cardController;
        [SerializeField] private DeckReplacementView deckReplacementView;
        [SerializeField] private bool openExhaustionReplacementBeforeRound = true;
        private EventFlowController eventFlowController;
        [SerializeField] private bool startRunOnAwake = true;
        private bool waitingForReplacement;

        public event Action<RunPhase> PhaseChanged;

        public RunPhase CurrentPhase { get; private set; } = RunPhase.Event;
        public int RoundIndex { get; private set; }
        public int TurnIndex { get; private set; }

        private void Awake()
        {
            if (cardController == null)
            {
                cardController = FindObjectOfType<CardController>();
            }

            if (eventFlowController == null)
            {
                eventFlowController = FindObjectOfType<EventFlowController>();
            }

            if (deckReplacementView == null)
            {
                deckReplacementView = FindObjectOfType<DeckReplacementView>(true);
            }

            if (startRunOnAwake)
            {
                StartRun();
            }
        }

        [ContextMenu("Start Run")]
        public void StartRun()
        {
            RoundIndex = 0;
            TurnIndex = 0;

            if (cardController != null)
            {
                cardController.StartDemoRun();
            }

            EnterEventPhase();
        }

        [ContextMenu("Confirm Event Choice")]
        public void ConfirmEventChoice()
        {
            if (eventFlowController != null)
            {
                eventFlowController.EndEvent();
                return;
            }

            StartRoundFromEvent();
        }

        public void StartRoundFromEvent()
        {
            if (CurrentPhase != RunPhase.Event)
            {
                return;
            }

            if (waitingForReplacement)
            {
                return;
            }

            if (TryOpenExhaustionReplacement())
            {
                return;
            }

            StartRoundAfterReplacement();
        }

        private void StartRoundAfterReplacement()
        {
            waitingForReplacement = false;
            RoundIndex++;

            if (cardController != null)
            {
                cardController.StartNewRound();
            }

            EnterTurnPhase();
        }

        private void OnDestroy()
        {
            UnhookReplacementView();
        }

        private bool TryOpenExhaustionReplacement()
        {
            if (!openExhaustionReplacementBeforeRound)
            {
                Debug.Log("Exhaustion replacement skipped: openExhaustionReplacementBeforeRound is disabled.");
                return false;
            }

            if (cardController == null)
            {
                Debug.LogWarning("Exhaustion replacement skipped: CardController is not assigned.");
                return false;
            }

            if (deckReplacementView == null)
            {
                Debug.LogWarning("Exhaustion replacement skipped: DeckReplacementView is not assigned or could not be found.");
                return false;
            }

            if (!cardController.HasPendingExhaustionRefill)
            {
                return false;
            }

            waitingForReplacement = true;
            deckReplacementView.Completed += HandleReplacementFinished;
            deckReplacementView.Cancelled += HandleReplacementFinished;

            if (deckReplacementView.OpenExhaustionRefill(cardController.GetExhaustedRefillTargets()))
            {
                return true;
            }

            Debug.LogWarning("Exhaustion replacement view did not open. Check candidate cards, default slot cards, and popup UI references.");
            UnhookReplacementView();
            waitingForReplacement = false;
            return false;
        }

        private void HandleReplacementFinished()
        {
            if (!waitingForReplacement)
            {
                return;
            }

            UnhookReplacementView();
            StartRoundAfterReplacement();
        }

        private void UnhookReplacementView()
        {
            if (deckReplacementView == null)
            {
                return;
            }

            deckReplacementView.Completed -= HandleReplacementFinished;
            deckReplacementView.Cancelled -= HandleReplacementFinished;
        }

        [ContextMenu("End Turn")]
        public void EndTurn()
        {
            if (CurrentPhase != RunPhase.Turn)
            {
                return;
            }

            if (cardController == null)
            {
                EnterEventPhase();
                return;
            }

            cardController.EndTurnCleanup();

            if (cardController.IsDrawPileEmpty)
            {
                EnterEventPhase();
            }
            else
            {
                EnterTurnPhase();
            }
        }

        public void EnterEventPhase()
        {
            CurrentPhase = RunPhase.Event;
            PhaseChanged?.Invoke(CurrentPhase);

            if (eventFlowController != null)
            {
                eventFlowController.BeginEventPhase();
            }
        }

        public void EnterTurnPhase()
        {
            CurrentPhase = RunPhase.Turn;
            TurnIndex++;

            if (cardController != null)
            {
                cardController.StartTurn();
            }

            PhaseChanged?.Invoke(CurrentPhase);
        }
    }
}
