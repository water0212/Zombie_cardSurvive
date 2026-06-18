using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Run;

namespace ZombieCardSurvive.Cards.Demo
{
    public class CardDemoBootstrap : MonoBehaviour
    {
        [SerializeField] private RunPhaseController runPhaseController;
        [SerializeField] private CardController cardController;

        private void Reset()
        {
            runPhaseController = FindObjectOfType<RunPhaseController>();
            cardController = FindObjectOfType<CardController>();
        }

        [ContextMenu("Start Demo")]
        public void StartDemo()
        {
            if (runPhaseController != null)
            {
                runPhaseController.StartRun();
            }
            else if (cardController != null)
            {
                cardController.StartDemoRun();
            }
        }

        [ContextMenu("Draw Cards")]
        public void DrawCards()
        {
            if (cardController != null)
            {
                cardController.DrawTurnCards();
            }
        }

        [ContextMenu("Discard Hand")]
        public void DiscardHand()
        {
            if (cardController != null)
            {
                cardController.DiscardHand();
            }
        }

        [ContextMenu("End Turn")]
        public void EndTurn()
        {
            if (runPhaseController != null)
            {
                runPhaseController.EndTurn();
            }
            else if (cardController != null)
            {
                cardController.EndTurn();
            }
        }
    }
}
