using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Run;

namespace ZombieCardSurvive.Events.Runtime
{
    public class EventFlowController : MonoBehaviour
    {
        [SerializeField] private RunPhaseController runPhaseController;
        [SerializeField] private List<EventData> eventPool = new List<EventData>();

        public event Action EventChanged;

        public EventFlowState State { get; private set; } = EventFlowState.Hidden;
        public EventData CurrentEvent { get; private set; }
        public EventChoiceData SelectedChoice { get; private set; }

        private void Reset()
        {
            runPhaseController = FindObjectOfType<RunPhaseController>();
        }

        private void Awake()
        {
            if (runPhaseController == null)
            {
                runPhaseController = FindObjectOfType<RunPhaseController>();
            }
        }

        [ContextMenu("Begin Event Phase")]
        public void BeginEventPhase()
        {
            CurrentEvent = SelectRandomEvent();
            SelectedChoice = null;
            State = CurrentEvent == null ? EventFlowState.Result : EventFlowState.Choosing;
            NotifyChanged();
        }

        public bool ChooseOption(int optionIndex)
        {
            if (State != EventFlowState.Choosing || CurrentEvent == null)
            {
                return false;
            }

            if (optionIndex < 0 || optionIndex >= CurrentEvent.Choices.Count)
            {
                return false;
            }

            SelectedChoice = CurrentEvent.Choices[optionIndex];
            SelectedChoice?.ApplyEffects();
            State = EventFlowState.Result;
            NotifyChanged();
            return true;
        }

        [ContextMenu("End Event")]
        public void EndEvent()
        {
            if (State != EventFlowState.Result)
            {
                return;
            }

            Hide();

            if (runPhaseController != null)
            {
                runPhaseController.StartRoundFromEvent();
            }
        }

        public void Hide()
        {
            State = EventFlowState.Hidden;
            CurrentEvent = null;
            SelectedChoice = null;
            NotifyChanged();
        }

        private EventData SelectRandomEvent()
        {
            List<EventData> availableEvents = new List<EventData>();
            foreach (EventData eventData in eventPool)
            {
                if (eventData != null)
                {
                    availableEvents.Add(eventData);
                }
            }

            if (availableEvents.Count == 0)
            {
                return null;
            }

            int index = UnityEngine.Random.Range(0, availableEvents.Count);
            return availableEvents[index];
        }

        private void NotifyChanged()
        {
            EventChanged?.Invoke();
        }
    }
}
