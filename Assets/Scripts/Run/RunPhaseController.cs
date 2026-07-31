using System;
using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Cards.UI.Replacement;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Run
{
    /// <summary>
    /// 一局遊戲的階段控制器。
    /// 負責在 Event、BuildingResolution、RoundEnd、Turn 之間切換，並串接事件後補位與替換流程。
    /// </summary>
    public class RunPhaseController : MonoBehaviour
    {
        // 卡牌流程控制器；負責抽牌、回合清理、補位與替換目標。
        [SerializeField] private CardController cardController;
        // 牌組替換視窗；事件後補空位、建造卡整合與一般替換都共用它。
        [SerializeField] private DeckReplacementView deckReplacementView;
        // 輪末固定消耗與報表結算器。
        [SerializeField] private RoundEndResolver roundEndResolver;
        // 建築系統；事件後會在這裡結算施工、取消與製作列。
        [SerializeField] private BuildingSystem buildingSystem;
        // 是否在新一輪開始前先開啟耗盡／空位補位視窗。
        [SerializeField] private bool openExhaustionReplacementBeforeRound = true;
        // 事件流程控制器；Awake 時自動尋找，事件階段會交給它抽事件。
        private EventFlowController eventFlowController;
        // 場景啟動時是否自動開始一局。
        [SerializeField] private bool startRunOnAwake = true;
        // 替換視窗開啟時鎖住事件後流程，等玩家完成或取消才繼續。
        private bool waitingForReplacement;
        // 事件後替換流程目前走到哪一步：0 空位、1 建造卡、2 一般替換、3 完成。
        private int postEventReplacementStep;
        // 確保同一次事件後只執行一次建築結算。
        private bool postEventResolved;

        /// <summary>
        /// 階段切換時觸發，HUD 會用它更新目前 Phase 顯示。
        /// </summary>
        public event Action<RunPhase> PhaseChanged;

        /// <summary>目前所處遊戲階段。</summary>
        public RunPhase CurrentPhase { get; private set; } = RunPhase.Event;
        /// <summary>目前第幾輪；新一輪真正開始時才加 1。</summary>
        public int RoundIndex { get; private set; }
        /// <summary>目前第幾個 Turn；每次 EnterTurnPhase 時加 1。</summary>
        public int TurnIndex { get; private set; }
        /// <summary>最近一次輪末結算報表，供 UI 或除錯查看食物、士氣等結果。</summary>
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

        /// <summary>
        /// 重新開始一局：重設回合索引、卡牌、建築與輪末系統，然後進入事件階段。
        /// </summary>
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

        /// <summary>
        /// 確認事件結果；若有 EventFlowController，交給事件流程關閉，否則直接進入事件後流程。
        /// </summary>
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

        /// <summary>
        /// 從事件階段進入事件後流程：先做建築結算，再依序處理空位補位、建造卡整合與一般替換。
        /// </summary>
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

        // 所有事件後替換都處理完後，真正開始新一輪並進入第一個 Turn。
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

        // 事件後固定順序：空位補位 -> 建造卡整合 -> 一般替換 -> 新一輪。
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

            if (postEventReplacementStep == 2)
            {
                postEventReplacementStep = 3;
                if (TryOpenPlanningReplacement())
                {
                    return;
                }
            }

            StartRoundAfterReplacement();
        }

        // 嘗試開啟正規卡位空缺補位視窗；沒有空缺或 UI 不可用時跳過。
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

        // 嘗試開啟建造卡一般整合視窗，讓玩家把倉庫中的建造卡換進正規牌組。
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

        // 嘗試開啟情報中心等效果給予的一般替換視窗。
        private bool TryOpenPlanningReplacement()
        {
            if (cardController == null || deckReplacementView == null)
            {
                return false;
            }

            CardInventory inventory = FindObjectOfType<CardInventory>();
            if (!cardController.TryCreatePlanningReplacementRequest(inventory, out DeckReplacementRequest request))
            {
                return false;
            }

            waitingForReplacement = true;
            deckReplacementView.Completed += HandleReplacementFinished;
            deckReplacementView.Cancelled += HandleReplacementFinished;
            if (deckReplacementView.OpenWithRequest(request))
            {
                cardController.ConsumePlanningReplacementOffers(request.ReplacementCount);
                return true;
            }

            UnhookReplacementView();
            waitingForReplacement = false;
            return false;
        }

        // 替換視窗完成或取消後解除等待，並繼續下一段事件後流程。
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

        // 移除替換視窗事件訂閱，避免同一個 callback 被重複註冊。
        private void UnhookReplacementView()
        {
            if (deckReplacementView == null)
            {
                return;
            }

            deckReplacementView.Completed -= HandleReplacementFinished;
            deckReplacementView.Cancelled -= HandleReplacementFinished;
        }

        /// <summary>
        /// 結束目前 Turn。若正規抽牌堆已空，會進入輪末與下一次事件；否則開始下一個 Turn。
        /// </summary>
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

        /// <summary>
        /// 嘗試設定本輪食物計畫；V1 限制只能在玩家 Turn 階段操作。
        /// </summary>
        /// <param name="plan">要切換成的食物計畫。</param>
        /// <returns>目前在 Turn 階段且 FoodPlanSystem 接受時回傳 true。</returns>
        public bool TrySetFoodPlan(FoodPlanLevel plan)
        {
            if (CurrentPhase != RunPhase.Turn)
            {
                return false;
            }

            return FoodPlanSystem.TrySetPendingPlan(plan);
        }

        /// <summary>
        /// 進入輪末階段並執行輪末固定消耗結算。
        /// </summary>
        public void EnterRoundEndPhase()
        {
            CurrentPhase = RunPhase.RoundEnd;
            PhaseChanged?.Invoke(CurrentPhase);

            if (roundEndResolver != null)
            {
                roundEndResolver.ResolveRoundEnd(cardController, RoundIndex, TurnIndex);
            }
        }

        /// <summary>
        /// 進入事件階段，並要求 EventFlowController 抽出新事件。
        /// </summary>
        public void EnterEventPhase()
        {
            CurrentPhase = RunPhase.Event;
            PhaseChanged?.Invoke(CurrentPhase);

            if (eventFlowController != null)
            {
                eventFlowController.BeginEventPhase();
            }
        }

        // 事件結束後的短暫階段，用來執行建築取消、施工完成與製作推進。
        private void EnterBuildingResolutionPhase()
        {
            CurrentPhase = RunPhase.BuildingResolution;
            PhaseChanged?.Invoke(CurrentPhase);
        }

        /// <summary>
        /// 進入玩家 Turn，增加 TurnIndex，並要求 CardController 抽本 Turn 手牌。
        /// </summary>
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
