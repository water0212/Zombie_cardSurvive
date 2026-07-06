using System;
using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Cards.UI.Replacement;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Run
{
    public class RunPhaseController : MonoBehaviour
    {
        [SerializeField] private CardController cardController;
        [SerializeField] private DeckReplacementView deckReplacementView;
        [SerializeField] private RoundEndResolver roundEndResolver;
        [SerializeField] private BuildingSystem buildingSystem;
        [SerializeField] private bool openExhaustionReplacementBeforeRound = true;
        private EventFlowController eventFlowController;
        [SerializeField] private bool startRunOnAwake = true;
        private bool waitingForReplacement;
        private int postEventReplacementStep;
        private bool postEventResolved;

        public event Action<RunPhase> PhaseChanged;

        public RunPhase CurrentPhase { get; private set; } = RunPhase.Event;
        public int RoundIndex { get; private set; }
        public int TurnIndex { get; private set; }
        public RoundEndReport LastRoundEndReport => roundEndResolver != null ? roundEndResolver.LastReport : null;

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

            if (roundEndResolver == null)
            {
                roundEndResolver = FindObjectOfType<RoundEndResolver>();
            }

            if (buildingSystem == null)
            {
                buildingSystem = FindObjectOfType<BuildingSystem>();
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

            if (buildingSystem != null)
            {
                buildingSystem.InitializeRun();
            }

            if (roundEndResolver != null)
            {
                roundEndResolver.PrepareRun();
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
            if (CurrentPhase != RunPhase.Event && CurrentPhase != RunPhase.BuildingResolution)
            {
                return;
            }

            if (waitingForReplacement)
            {
                return;
            }

            if (!postEventResolved)
            {
                EnterBuildingResolutionPhase();
                buildingSystem?.ResolveAfterEvent();
                postEventResolved = true;
                postEventReplacementStep = 0;
            }

            ContinuePostEventFlow();
        }

        private void StartRoundAfterReplacement()
        {
            waitingForReplacement = false;
            postEventResolved = false;
            postEventReplacementStep = 0;
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

        private void ContinuePostEventFlow()
        {
            if (waitingForReplacement)
            {
                return;
            }

            if (postEventReplacementStep == 0)
            {
                postEventReplacementStep = 1;
                if (TryOpenVacancyReplacement())
                {
                    return;
                }
            }

            if (postEventReplacementStep == 1)
            {
                postEventReplacementStep = 2;
                if (TryOpenConstructionIntegration())
                {
                    return;
                }
            }

            StartRoundAfterReplacement();
        }

        private bool TryOpenVacancyReplacement()
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

            if (!cardController.HasPendingVacancyRefill)
            {
                return false;
            }

            waitingForReplacement = true;
            deckReplacementView.Completed += HandleReplacementFinished;
            deckReplacementView.Cancelled += HandleReplacementFinished;

            if (deckReplacementView.OpenVacancyRefill(cardController.GetVacancyRefillTargets()))
            {
                return true;
            }

            Debug.LogWarning("Exhaustion replacement view did not open. Check candidate cards, default slot cards, and popup UI references.");
            UnhookReplacementView();
            waitingForReplacement = false;
            return false;
        }

        private bool TryOpenConstructionIntegration()
        {
            if (buildingSystem == null || deckReplacementView == null)
            {
                return false;
            }

            if (!buildingSystem.TryCreateConstructionIntegrationRequest(out DeckReplacementRequest request))
            {
                return false;
            }

            waitingForReplacement = true;
            deckReplacementView.Completed += HandleReplacementFinished;
            deckReplacementView.Cancelled += HandleReplacementFinished;
            if (deckReplacementView.OpenWithRequest(request))
            {
                buildingSystem.ConsumeConstructionIntegrationOffers();
                return true;
            }

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
            waitingForReplacement = false;
            ContinuePostEventFlow();
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
                EnterRoundEndPhase();
                EnterEventPhase();
            }
            else
            {
                EnterTurnPhase();
            }
        }

        public bool TrySetFoodPlan(FoodPlanLevel plan)
        {
            if (CurrentPhase != RunPhase.Turn)
            {
                return false;
            }

            return FoodPlanSystem.TrySetPendingPlan(plan);
        }

        public void EnterRoundEndPhase()
        {
            CurrentPhase = RunPhase.RoundEnd;
            PhaseChanged?.Invoke(CurrentPhase);

            if (roundEndResolver != null)
            {
                roundEndResolver.ResolveRoundEnd(cardController, RoundIndex, TurnIndex);
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

        private void EnterBuildingResolutionPhase()
        {
            CurrentPhase = RunPhase.BuildingResolution;
            PhaseChanged?.Invoke(CurrentPhase);
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
