using System;
using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Run
{
    public class RunPhaseController : MonoBehaviour
    {
        [SerializeField] private CardController cardController;
        [SerializeField] private EventFlowController eventFlowController;
        [SerializeField] private bool startRunOnAwake = true;

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

            RoundIndex++;

            if (cardController != null)
            {
                cardController.StartNewRound();
            }

            EnterTurnPhase();
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
