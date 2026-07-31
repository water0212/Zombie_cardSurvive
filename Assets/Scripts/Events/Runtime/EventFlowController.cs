using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Run;

namespace ZombieCardSurvive.Events.Runtime
{
    /// <summary>
    /// 事件階段控制器。
    /// 負責抽出事件、等待玩家選項、套用事件效果，並在事件結束後通知 RunPhaseController 進入下一段流程。
    /// </summary>
    public class EventFlowController : MonoBehaviour
    {
        // 回合階段控制器；事件結束後會交還流程控制權。
        [SerializeField] private RunPhaseController runPhaseController;
        // 卡牌控制器；事件效果需要透過它取得食物、資源、戰鬥與卡牌上下文。
        [SerializeField] private CardController cardController;
        // 可被抽中的事件資產清單；V1 使用簡單隨機。
        [SerializeField] private List<EventData> eventPool = new List<EventData>();

        /// <summary>
        /// 事件狀態、目前事件或選項結果改變時觸發，事件 UI 會用它刷新。
        /// </summary>
        public event Action EventChanged;

        /// <summary>目前事件 UI 狀態：隱藏、選擇中或顯示結果。</summary>
        public EventFlowState State { get; private set; } = EventFlowState.Hidden;
        /// <summary>本次事件階段抽到的事件資料。</summary>
        public EventData CurrentEvent { get; private set; }
        /// <summary>玩家已選擇的事件選項；尚未選擇時為 null。</summary>
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

            if (cardController == null)
            {
                cardController = FindObjectOfType<CardController>();
            }
        }

        /// <summary>
        /// 開始事件階段：從事件池抽一個事件，並切到選項畫面。
        /// </summary>
        [ContextMenu("Begin Event Phase")]
        public void BeginEventPhase()
        {
            CurrentEvent = SelectRandomEvent();
            SelectedChoice = null;
            State = CurrentEvent == null ? EventFlowState.Result : EventFlowState.Choosing;
            NotifyChanged();
        }

        /// <summary>
        /// 嘗試選擇事件選項並立即套用該選項的效果。
        /// </summary>
        /// <param name="optionIndex">玩家選擇的選項索引。</param>
        /// <returns>目前可選、索引合法且成功套用時回傳 true。</returns>
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
            SelectedChoice?.ApplyEffects(GameEffectContext.ForEvent(cardController));
            State = EventFlowState.Result;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 結束事件結果畫面，並請 RunPhaseController 接續事件後流程。
        /// </summary>
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

        /// <summary>
        /// 隱藏事件 UI 狀態並清掉目前事件資料。
        /// </summary>
        public void Hide()
        {
            State = EventFlowState.Hidden;
            CurrentEvent = null;
            SelectedChoice = null;
            NotifyChanged();
        }

        // V1 事件抽選：先過濾掉空資料，再從可用事件中等機率抽一個。
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

        // 統一通知事件 UI 刷新目前事件狀態。
        private void NotifyChanged()
        {
            EventChanged?.Invoke();
        }
    }
}
