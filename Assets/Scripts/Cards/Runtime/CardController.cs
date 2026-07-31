using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.UI.Piles;
using ZombieCardSurvive.Cards.UI.Replacement;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.Runtime
{
    /// <summary>
    /// 卡牌回合的核心控制器：管理抽牌堆、手牌、棄牌堆、耗盡卡、卡位空缺與替換請求。
    /// 其他系統若要打牌、抽牌、補牌或移除 Runtime 卡，通常會從這裡進出。
    /// </summary>
    public class CardController : MonoBehaviour
    {
        [Header("Deck")]
        // 舊版或測試用起始牌組清單；有 DeckLayout 時會交給 DeckBuildSystem 一起正規化。
        [SerializeField] private List<CardBase> startingDeck = new List<CardBase>();
        // 正規牌組版型，定義各欄位數量、每回合抽牌數與預設補位卡。
        [SerializeField] private DeckLayoutDefinition deckLayout;
        // 沒有 DeckLayout 時使用的備援抽牌數。
        [SerializeField] private int drawCountPerTurn = 5;

        [Header("Run Settings")]
        // 每回合能量上限；StartDemoRun 時會重設 EnergySystem。
        [SerializeField] private int maxEnergy = 3;
        // 測試開局的永久食物數量。
        [SerializeField] private int startingFood;
        // 測試開局的資源數量。
        [SerializeField] private int startingResource;
        // 場景啟動時是否自動建立一局測試流程。
        [SerializeField] private bool startDemoOnAwake = true;

        [Header("Selection UI")]
        // 通用牌堆查看／選牌視窗；調度工具只借它選卡，真正抽牌仍由 CardController 處理。
        [SerializeField] private CardPileViewSystem cardPileViewSystem;

        // 尚未抽出的 Runtime 卡；包含正規卡與附加卡，例如殭屍。
        private readonly List<CardRuntime> drawPile = new List<CardRuntime>();
        // 玩家目前可操作的手牌。
        private readonly List<CardRuntime> handCards = new List<CardRuntime>();
        // 已打出或棄掉、之後會回到抽牌堆的卡。
        private readonly List<CardRuntime> discardPile = new List<CardRuntime>();
        // 本回合已打出但尚未進棄牌堆的卡。
        private readonly List<CardRuntime> playedCards = new List<CardRuntime>();
        // 使用次數歸零且離開循環的卡。
        private readonly List<CardRuntime> exhaustedCards = new List<CardRuntime>();
        // 正規卡被耗盡、建築完成或取消移除後留下的卡位空缺紀錄。
        private readonly List<DeckVacancyRecord> vacancyRecords = new List<DeckVacancyRecord>();
        // 情報中心等效果給予的「一般替換」次數，事件後才會開視窗處理。
        private int pendingPlanningReplacementCount;
        // 抽牌遞迴保護：自動觸發卡可能補抽，最外層抽牌結束後才刷新 UI。
        private int drawOperationDepth;

        /// <summary>
        /// 任何牌堆、手牌、能量或替換狀態改變時觸發，UI 會用它重繪。
        /// </summary>
        public event Action StateChanged;

        /// <summary>目前抽牌堆，只讀給 UI 與除錯工具使用。</summary>
        public IReadOnlyList<CardRuntime> DrawPile => drawPile;
        /// <summary>目前手牌，只讀給 UI 顯示與互動判斷使用。</summary>
        public IReadOnlyList<CardRuntime> HandCards => handCards;
        /// <summary>目前棄牌堆，只讀給牌堆檢視器使用。</summary>
        public IReadOnlyList<CardRuntime> DiscardPile => discardPile;
        /// <summary>本回合已打出的卡，只讀給狀態顯示或結算流程使用。</summary>
        public IReadOnlyList<CardRuntime> PlayedCards => playedCards;
        /// <summary>已耗盡的卡，只讀給替換與除錯流程使用。</summary>
        public IReadOnlyList<CardRuntime> ExhaustedCards => exhaustedCards;
        /// <summary>尚未補回的正規卡位空缺。</summary>
        public IReadOnlyList<DeckVacancyRecord> VacancyRecords => vacancyRecords;
        /// <summary>目前使用的牌組版型。</summary>
        public DeckLayoutDefinition DeckLayout => deckLayout;

        /// <summary>本回合應抽的正規卡數，優先讀 DeckLayout，沒有時用備援欄位。</summary>
        public int DrawCountPerTurn => deckLayout != null ? deckLayout.DrawCountPerTurn : Mathf.Max(0, drawCountPerTurn);
        /// <summary>目前能量上限，由 EnergySystem 保存。</summary>
        public int MaxEnergy => EnergySystem.MaxEnergy;
        /// <summary>目前剩餘能量，由 EnergySystem 保存。</summary>
        public int CurrentEnergy => EnergySystem.CurrentEnergy;
        /// <summary>尚未結算的戰鬥力，用於抵銷殭屍威脅。</summary>
        public int PendingDamage => CombatSystem.PendingDamage;
        /// <summary>場上目前存在的殭屍威脅數量。</summary>
        public int ZombieThreatCount => CombatSystem.ZombieThreatCount;
        /// <summary>抽牌堆中的正規卡數量；決定本輪是否還有下一個 Turn。</summary>
        public int RegularDrawPileCount => CountCardsInDrawPile(false);
        /// <summary>抽牌堆中的附加卡數量；附加卡不會增加回合數。</summary>
        public int AdditiveDrawPileCount => CountCardsInDrawPile(true);
        /// <summary>正規抽牌堆是否已空；附加卡不算作延長回合。</summary>
        public bool IsDrawPileEmpty => RegularDrawPileCount == 0;
        /// <summary>是否有耗盡、建築完成或取消造成的卡位需要補回。</summary>
        public bool HasPendingVacancyRefill => vacancyRecords.Count > 0;
        /// <summary>是否有待處理的一般替換權利。</summary>
        public bool HasPendingPlanningReplacement => pendingPlanningReplacementCount > 0;
        /// <summary>待處理的一般替換次數。</summary>
        public int PendingPlanningReplacementCount => pendingPlanningReplacementCount;
        /// <summary>待補回的正規卡位數量。</summary>
        public int PendingVacancyRefillCount => vacancyRecords.Count;
        /// <summary>舊 API 名稱，保留給既有 UI 或測試使用；語意等同 HasPendingVacancyRefill。</summary>
        public bool HasPendingExhaustionRefill => HasPendingVacancyRefill;
        /// <summary>舊 API 名稱，保留給既有 UI 或測試使用；語意等同 PendingVacancyRefillCount。</summary>
        public int PendingExhaustionRefillCount => PendingVacancyRefillCount;

        private void Awake()
        {
            if (cardPileViewSystem == null)
            {
                cardPileViewSystem = FindObjectOfType<CardPileViewSystem>(true);
            }

            if (startDemoOnAwake)
            {
                StartDemoRun();
            }
        }

        /// <summary>
        /// 重新建立一局測試用牌局：清空所有 Runtime 牌堆，重設資源／戰鬥／能量，再依 DeckLayout 生成牌組。
        /// </summary>
        [ContextMenu("Start Demo Run")]
        public void StartDemoRun()
        {
            drawPile.Clear();
            handCards.Clear();
            discardPile.Clear();
            playedCards.Clear();
            exhaustedCards.Clear();
            vacancyRecords.Clear();
            pendingPlanningReplacementCount = 0;

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

        /// <summary>
        /// Inspector 除錯入口：等同開始一個 Turn 並抽牌。
        /// </summary>
        [ContextMenu("Draw Turn Cards")]
        public void DrawTurnCards()
        {
            StartTurn();
        }

        /// <summary>
        /// 開始新一輪：回收手牌、棄牌、已打出牌，補預設空位，洗牌並刷新 UI。
        /// </summary>
        [ContextMenu("Start New Round")]
        public void StartNewRound()
        {
            RemoveCardsExpiringBeforeNextRound(handCards);
            RemoveCardsExpiringBeforeNextRound(discardPile);
            RemoveCardsExpiringBeforeNextRound(playedCards);
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

        /// <summary>
        /// 開始一個玩家 Turn：套用回合起始產出、補滿能量，抽出本 Turn 的正規卡。
        /// </summary>
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

        /// <summary>
        /// 從抽牌堆抽指定數量的正規卡；自動觸發卡會立即結算，並依卡牌設定決定是否補抽。
        /// </summary>
        /// <param name="amount">本次要抽出的正規卡張數。</param>
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

        /// <summary>
        /// 嘗試打出一張手牌。成功時會支付費用、執行效果，並依剩餘使用次數放入棄牌堆或耗盡區。
        /// </summary>
        /// <param name="card">玩家想打出的手牌 Runtime。</param>
        /// <returns>成功打出回傳 true；費用不足、不是手牌或不可打出時回傳 false。</returns>
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

            int effectiveEnergyCost = card.EffectiveEnergyCost;
            if (!EnergySystem.CanSpend(effectiveEnergyCost))
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

            EnergySystem.SpendEnergy(effectiveEnergyCost);
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

        /// <summary>
        /// 將一張手牌或已打出牌直接棄掉。
        /// </summary>
        /// <param name="card">要棄掉的 Runtime 卡。</param>
        /// <returns>成功移入棄牌堆時回傳 true。</returns>
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

        /// <summary>
        /// 以卡牌資產建立一張新的 Runtime 卡並加入棄牌堆，常用於探索風險加入殭屍卡。
        /// </summary>
        /// <param name="cardData">要建立的卡牌資料。</param>
        /// <returns>成功建立並加入棄牌堆時回傳 true。</returns>
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

        /// <summary>
        /// 給予事件後的一般替換次數，例如情報中心卡牌提供的替換權利。
        /// </summary>
        /// <param name="count">增加的替換次數；小於 0 會視為 0。</param>
        public void GrantPlanningReplacement(int count)
        {
            pendingPlanningReplacementCount += Mathf.Max(0, count);
            NotifyStateChanged();
        }

        /// <summary>
        /// 嘗試把目前累積的一般替換權利轉成 DeckReplacementRequest，供替換 UI 開啟。
        /// </summary>
        /// <param name="inventory">玩家卡牌倉庫，作為可換入候選來源。</param>
        /// <param name="request">成功時輸出的替換請求。</param>
        /// <returns>有替換次數、可換目標與候選卡時回傳 true。</returns>
        public bool TryCreatePlanningReplacementRequest(CardInventory inventory, out DeckReplacementRequest request)
        {
            request = null;
            if (inventory == null || pendingPlanningReplacementCount <= 0)
            {
                return false;
            }

            List<CardRuntime> targets = GetReplaceableRegularCards();
            List<CardInventoryEntry> candidates = new List<CardInventoryEntry>();
            foreach (CardInventoryEntry entry in inventory.Entries)
            {
                if (entry != null && entry.Data != null && entry.HasRemainingUses)
                {
                    candidates.Add(entry);
                }
            }

            int replacementCount = Mathf.Min(pendingPlanningReplacementCount, targets.Count, candidates.Count);
            if (replacementCount <= 0)
            {
                return false;
            }

            request = new DeckReplacementRequest(
                DeckReplacementReason.Planning,
                replacementCount,
                targets,
                candidates);
            return true;
        }

        /// <summary>
        /// 消耗已經開啟給玩家的一般替換次數；視窗成功開啟後才呼叫。
        /// </summary>
        /// <param name="count">要消耗的替換次數。</param>
        public void ConsumePlanningReplacementOffers(int count = 1)
        {
            int consumed = Mathf.Max(0, count);
            if (pendingPlanningReplacementCount > 0 && consumed > 0)
            {
                pendingPlanningReplacementCount = Mathf.Max(0, pendingPlanningReplacementCount - consumed);
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 依 Runtime ID 從所有牌堆與耗盡區移除卡牌，並替正規卡建立卡位空缺。
        /// </summary>
        /// <param name="instanceIds">要移除的 RuntimeId 清單。</param>
        /// <param name="reason">移除原因，會寫進 DeckVacancyRecord。</param>
        /// <returns>實際移除的卡牌數量。</returns>
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

        /// <summary>
        /// 將所有引用同一張 CardBase 資產的 Runtime 卡替換成另一張 CardBase。
        /// </summary>
        /// <param name="source">要被替換掉的原始卡牌資料。</param>
        /// <param name="replacement">要換入的新卡牌資料。</param>
        /// <returns>實際替換的 Runtime 卡數量。</returns>
        public int ReplaceRuntimeCardData(CardBase source, CardBase replacement)
        {
            if (source == null || replacement == null || ReferenceEquals(source, replacement))
            {
                return 0;
            }

            int replaced = 0;
            replaced += ReplaceMatchingCardData(drawPile, source, replacement);
            replaced += ReplaceMatchingCardData(handCards, source, replacement);
            replaced += ReplaceMatchingCardData(discardPile, source, replacement);
            replaced += ReplaceMatchingCardData(playedCards, source, replacement);
            replaced += ReplaceMatchingCardData(exhaustedCards, source, replacement);

            if (replaced > 0)
            {
                NotifyStateChanged();
            }

            return replaced;
        }

        /// <summary>
        /// 將整副手牌移入棄牌堆，主要用於除錯或回合清理。
        /// </summary>
        [ContextMenu("Discard Hand")]
        public void DiscardHand()
        {
            MoveCardsToDiscardPile(handCards);
            handCards.Clear();
            NotifyStateChanged();
        }

        /// <summary>
        /// Inspector 除錯入口：執行回合結束清理。
        /// </summary>
        [ContextMenu("End Turn")]
        public void EndTurn()
        {
            EndTurnCleanup();
        }

        /// <summary>
        /// 回合結束清理：手牌與已打出牌進棄牌堆；若正規抽牌堆空了，揭露剩餘附加卡。
        /// </summary>
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

        /// <summary>
        /// 本輪正規卡抽完時，將抽牌堆中剩餘的附加卡全部自動結算。
        /// </summary>
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

        /// <summary>
        /// 交換兩張手牌的位置，供拖曳排序或除錯使用。
        /// </summary>
        /// <param name="firstHandIndex">第一張手牌索引。</param>
        /// <param name="secondHandIndex">第二張手牌索引。</param>
        /// <returns>兩個索引都合法時回傳 true。</returns>
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

        /// <summary>
        /// 取得目前可被一般替換的正規卡。附加卡與耗盡卡不會列入。
        /// </summary>
        /// <returns>可替換的 Runtime 卡清單。</returns>
        public List<CardRuntime> GetReplaceableRegularCards()
        {
            List<CardRuntime> cards = new List<CardRuntime>();
            AddReplaceableCards(drawPile, cards);
            AddReplaceableCards(handCards, cards);
            AddReplaceableCards(discardPile, cards);
            AddReplaceableCards(playedCards, cards);
            return cards;
        }

        /// <summary>
        /// 取得所有待補位空缺的來源卡，供 DeckReplacementView 顯示「要補哪個位置」。
        /// </summary>
        /// <returns>每筆空缺對應的來源 Runtime 卡。</returns>
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

        /// <summary>
        /// 舊 API 名稱，保留給既有 UI 或測試使用；語意等同 GetVacancyRefillTargets。
        /// </summary>
        public List<CardRuntime> GetExhaustedRefillTargets()
        {
            return GetVacancyRefillTargets();
        }

        /// <summary>
        /// 用卡牌資料替換指定 Runtime 卡。
        /// </summary>
        /// <param name="target">被替換的正規 Runtime 卡。</param>
        /// <param name="replacementData">換入的卡牌資料。</param>
        /// <param name="replacementRuntime">成功時輸出的新 Runtime 卡。</param>
        /// <returns>成功替換時回傳 true。</returns>
        public bool TryReplaceRuntimeCard(CardRuntime target, CardBase replacementData, out CardRuntime replacementRuntime)
        {
            return TryReplaceRuntimeCard(target, replacementData != null ? new CardInventoryEntry(replacementData) : null, out replacementRuntime);
        }

        /// <summary>
        /// 用倉庫 Entry 替換指定 Runtime 卡，並保留原本的正規欄位與所在牌堆。
        /// </summary>
        /// <param name="target">被替換的正規 Runtime 卡。</param>
        /// <param name="replacementEntry">換入來源，通常來自 CardInventory。</param>
        /// <param name="replacementRuntime">成功時輸出的新 Runtime 卡。</param>
        /// <returns>成功替換時回傳 true。</returns>
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

        /// <summary>
        /// 以卡牌資料補回一個正規卡位空缺，補入後會放回抽牌堆。
        /// </summary>
        /// <param name="sourceCard">造成空缺的來源卡。</param>
        /// <param name="replacementData">補入的卡牌資料。</param>
        /// <param name="replacementRuntime">成功時輸出的新 Runtime 卡。</param>
        /// <returns>找到對應空缺並成功補入時回傳 true。</returns>
        public bool TryFillVacantSlot(CardRuntime sourceCard, CardBase replacementData, out CardRuntime replacementRuntime)
        {
            return TryFillVacantSlot(sourceCard, replacementData != null ? new CardInventoryEntry(replacementData) : null, out replacementRuntime);
        }

        /// <summary>
        /// 以倉庫 Entry 補回一個正規卡位空缺，補入後會放回抽牌堆。
        /// </summary>
        /// <param name="sourceCard">造成空缺的來源卡。</param>
        /// <param name="replacementEntry">補入來源，通常來自 CardInventory。</param>
        /// <param name="replacementRuntime">成功時輸出的新 Runtime 卡。</param>
        /// <returns>找到對應空缺並成功補入時回傳 true。</returns>
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

        /// <summary>
        /// 舊 API 名稱，保留給耗盡補位流程使用；內部改走通用空位補位。
        /// </summary>
        public bool TryRefillExhaustedSlot(CardRuntime exhaustedCard, CardBase replacementData, out CardRuntime replacementRuntime)
        {
            return TryFillVacantSlot(exhaustedCard, replacementData, out replacementRuntime);
        }

        /// <summary>
        /// 舊 API 名稱，保留給耗盡補位流程使用；內部改走通用空位補位。
        /// </summary>
        public bool TryRefillExhaustedSlot(CardRuntime exhaustedCard, CardInventoryEntry replacementEntry, out CardRuntime replacementRuntime)
        {
            return TryFillVacantSlot(exhaustedCard, replacementEntry, out replacementRuntime);
        }

        /// <summary>
        /// 檢查指定來源卡是否已經建立空位紀錄，避免重複建立補位需求。
        /// </summary>
        /// <param name="sourceCard">造成空缺的來源 Runtime 卡。</param>
        /// <returns>已有空位紀錄時回傳 true。</returns>
        public bool HasVacancyFor(CardRuntime sourceCard)
        {
            return FindVacancyRecord(sourceCard) != null;
        }

        /// <summary>
        /// 開始「從抽牌堆調度指定卡牌」流程。此方法只建立請求與開選牌 UI；真正搬牌在玩家選完後才做。
        /// </summary>
        public bool BeginDispatchValidDrawPileCard(
            CardBase restCardData,
            int amount,
            CardSelectionFilterData filterData)
        {
            return BeginDispatchValidDrawPileCard(
                restCardData,
                amount,
                filterData?.CreateRuntimeFilter());
        }

        /// <summary>
        /// 舊版參數式入口，保留給既有程式與資產設定工具使用。
        /// 新卡牌應優先傳入 CardSelectionFilterData。
        /// </summary>
        public bool BeginDispatchValidDrawPileCard(
            CardBase restCardData,
            int amount,
            bool cardTypeFilter,
            List<CardType> cardTypes,
            bool cardRarityFilter,
            List<CardRarity> cardRarities,
            bool cardCostFilter,
            CardCostFilterMode costFilterMode,
            int cost)
        {
            CardSelectionFilter filter = new CardSelectionFilter(
                cardTypeFilter,
                cardTypes,
                cardRarityFilter,
                cardRarities,
                cardCostFilter,
                costFilterMode,
                cost);

            return BeginDispatchValidDrawPileCard(restCardData, amount, filter);
        }

        private bool BeginDispatchValidDrawPileCard(
            CardBase restCardData,
            int amount,
            CardSelectionFilter filter)
        {
            int requestedAmount = Mathf.Max(0, amount);
            if (requestedAmount == 0)
            {
                return false;
            }

            CardSelectionRequest request = new CardSelectionRequest(
                "調度卡牌",
                requestedAmount,
                GetDispatchableDrawPileCards(),
                filter);

            return ChooseValidPileCards(
                request,
                selectedCards => DispatchValidDrawPileCard(restCardData, selectedCards),
                () => NotifyStateChanged());
        }

        /// <summary>
        /// 通用選牌入口：只負責決定候選與取得玩家選擇，不負責抽牌、刪牌或替換。
        /// </summary>
        public bool ChooseValidPileCards(
            CardSelectionRequest request,
            Action<IReadOnlyList<CardRuntime>> onCompleted,
            Action onCancelled = null)
        {
            if (request == null || onCompleted == null)
            {
                return false;
            }

            List<CardRuntime> validCards = request.GetValidCards();
            if (validCards.Count == 0)
            {
                return false;
            }

            if (validCards.Count <= request.SelectionCount)
            {
                onCompleted(validCards);
                return true;
            }

            if (cardPileViewSystem != null && cardPileViewSystem.OpenSelection(request, onCompleted, onCancelled))
            {
                return true;
            }

            Debug.LogWarning("Card selection UI is unavailable. Falling back to the first valid cards.", this);
            onCompleted(TakeFirstCards(validCards, request.SelectionCount));
            return true;
        }

        /// <summary>
        /// 將玩家選中的抽牌堆卡牌移到手牌，並為每張成功抽出的卡建立一張 restCardData 補回抽牌堆。
        /// </summary>
        public bool DispatchValidDrawPileCard(CardBase restCardData, IReadOnlyList<CardRuntime> dispatchedCards)
        {
            if (dispatchedCards == null || dispatchedCards.Count == 0)
            {
                return false;
            }

            int dispatchedCount = 0;
            for (int i = 0; i < dispatchedCards.Count; i++)
            {
                CardRuntime card = dispatchedCards[i];
                if (card == null)
                {
                    continue;
                }

                int drawPileIndex = drawPile.IndexOf(card);
                if (drawPileIndex < 0)
                {
                    continue;
                }

                drawPile.RemoveAt(drawPileIndex);
                card.SetZone(DeckZone.Hand);
                handCards.Add(card);
                dispatchedCount++;

                if (restCardData != null)
                {
                    CardRuntime restCard = restCardData.CreateRuntimeCard();
                    restCard.SetAssignedSlotType(card.AssignedSlotType);
                    restCard.SetZone(DeckZone.DrawPile);
                    drawPile.Insert(Mathf.Min(drawPileIndex, drawPile.Count), restCard);
                }
                else
                {
                    Debug.LogWarning("Dispatch draw pile card succeeded, but restCardData is not assigned. Draw pile size will shrink.", this);
                }
            }

            if (dispatchedCount <= 0)
            {
                return false;
            }

            NotifyStateChanged();
            return true;
        }
        /// <summary>
        /// 降低目前其他合法手牌的費用，不開啟選牌視窗。
        /// </summary>
        public int ReduceOtherHandEnergyCosts(CardSelectionFilter filter, CardRuntime sourceCard, int amount)
        {
            List<CardRuntime> targets = new List<CardRuntime>();
            foreach (CardRuntime handCard in handCards)
            {
                if (handCard == null || ReferenceEquals(handCard, sourceCard) || handCard.Data == null)
                {
                    continue;
                }

                if (handCard.Data.EffectivePlayMode != CardPlayMode.Playable)
                {
                    continue;
                }

                if (filter != null && !filter.Allows(handCard))
                {
                    continue;
                }

                targets.Add(handCard);
            }

            return ReduceEnergyCosts(targets, amount);
        }

        /// <summary>
        /// 從抽牌堆開啟通用選牌介面，選完後降低指定卡牌費用。
        /// </summary>
        public bool ReduceDrawPileEnergyCosts(
            CardSelectionFilter filter,
            CardRuntime sourceCard,
            int selectionCount,
            int amount)
        {
            int requestedCount = Mathf.Max(0, selectionCount);
            int reduction = Mathf.Max(0, amount);
            if (requestedCount == 0 || reduction == 0)
            {
                return false;
            }

            CardSelectionRequest request = new CardSelectionRequest(
                "選擇要降低費用的卡牌",
                requestedCount,
                GetReducibleDrawPileCards(sourceCard),
                filter);

            return ChooseValidPileCards(
                request,
                selectedCards =>
                {
                    ReduceEnergyCosts(selectedCards, reduction);
                },
                () => NotifyStateChanged());
        }

        /// <summary>
        /// 對指定 Runtime 卡牌套用同一份臨時費用減免，回傳實際影響張數。
        /// </summary>
        public int ReduceEnergyCosts(IEnumerable<CardRuntime> cards, int amount)
        {
            if (cards == null)
            {
                return 0;
            }

            int reduction = Mathf.Max(0, amount);
            if (reduction == 0)
            {
                return 0;
            }

            int affectedCount = 0;
            HashSet<CardRuntime> affectedCards = new HashSet<CardRuntime>();
            foreach (CardRuntime target in cards)
            {
                if (target == null || target.Data == null || !affectedCards.Add(target))
                {
                    continue;
                }

                target.AddTurnEnergyCostModifier(-reduction);
                affectedCount++;
            }

            if (affectedCount > 0)
            {
                NotifyStateChanged();
            }

            return affectedCount;
        }

        // 調度工具只允許從抽牌堆挑可正常打出的正規卡，避免把殭屍或傷口直接塞進手牌。
        private IEnumerable<CardRuntime> GetDispatchableDrawPileCards()
        {
            foreach (CardRuntime card in drawPile)
            {
                if (card == null || card.Data == null)
                {
                    continue;
                }

                if (card.IsAdditiveInstance || card.Data.EffectivePlayMode != CardPlayMode.Playable)
                {
                    continue;
                }

                yield return card;
            }
        }

        private IEnumerable<CardRuntime> GetReducibleDrawPileCards(CardRuntime excludedCard)
        {
            foreach (CardRuntime card in GetDispatchableDrawPileCards())
            {
                if (!ReferenceEquals(card, excludedCard))
                {
                    yield return card;
                }
            }
        }

        // 沒有選牌 UI 可用時的保底路徑：直接取前 N 張合法卡，避免卡牌效果卡死。
        private static IReadOnlyList<CardRuntime> TakeFirstCards(IReadOnlyList<CardRuntime> source, int count)
        {
            List<CardRuntime> result = new List<CardRuntime>();
            if (source == null)
            {
                return result;
            }

            int targetCount = Mathf.Max(0, count);
            for (int i = 0; i < source.Count && result.Count < targetCount; i++)
            {
                if (source[i] != null)
                {
                    result.Add(source[i]);
                }
            }

            return result;
        }

        // 檢查手牌索引是否合法，避免 UI 拖曳排序時越界。
        private bool IsValidHandIndex(int index)
        {
            return index >= 0 && index < handCards.Count;
        }

        // 從指定牌堆收集可被一般替換的正規卡。
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

        // 找出某張 Runtime 卡目前屬於哪個牌堆清單。
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

        // Fisher-Yates 洗牌，讓抽牌堆順序每輪隨機。
        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

        // 統一通知 UI 與除錯工具刷新狀態。
        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        // 結算抽到即自動觸發的卡，例如附加殭屍卡。
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

        // 將一批卡回收到抽牌堆；耗盡卡不會回到循環。
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

        // 單張卡進抽牌堆時，一定同步更新 Runtime Zone。
        private void AddToDrawPile(CardRuntime card)
        {
            if (card == null)
            {
                return;
            }

            card.SetZone(DeckZone.DrawPile);
            drawPile.Add(card);
        }

        // 將一批卡移入棄牌堆，實際防呆交給 MoveToDiscardPile。
        private void MoveCardsToDiscardPile(List<CardRuntime> cards)
        {
            foreach (CardRuntime card in cards)
            {
                MoveToDiscardPile(card);
            }
        }

        // 單張卡進棄牌堆時，一定同步更新 Runtime Zone；耗盡卡不可再進棄牌循環。
        private void MoveToDiscardPile(CardRuntime card)
        {
            if (card == null || card.IsExhausted)
            {
                return;
            }

            card.ResetTurnModifiers();
            card.SetZone(DeckZone.DiscardPile);
            discardPile.Add(card);
        }

        // 卡牌使用次數耗盡後移入耗盡區，並視卡牌設定決定是否建立正規卡位空缺。
        private void MoveToExhausted(CardRuntime card)
        {
            if (card == null)
            {
                return;
            }

            card.ResetTurnModifiers();
            card.MarkExhausted();
            exhaustedCards.Add(card);
            if (card.Data == null || card.Data.CreatesExhaustionRefillRequest)
            {
                vacancyRecords.Add(new DeckVacancyRecord(card, card.AssignedSlotType, DeckVacancyReason.Exhausted));
            }
        }

        /// <summary>
        /// 用 DeckLayout 的預設卡補齊所有空位，通常在玩家取消補位時維持正規牌組合法性。
        /// </summary>
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

        /// <summary>
        /// 舊 API 名稱，保留給既有呼叫端；語意等同 RefillVacantSlotsWithDefaults。
        /// </summary>
        public void RefillExhaustedSlotsWithDefaults()
        {
            RefillVacantSlotsWithDefaults();
        }

        // 從空缺清單找到指定來源卡的紀錄，用來補位或避免重複建立空缺。
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

        // 從指定牌堆刪除符合 RuntimeId 的卡；正規卡被刪除時會留下補位空缺。
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
        // 移除所有設定為「本輪結束前過期」的 Runtime 卡，通常在回合結束時呼叫。
        private static void RemoveCardsExpiringBeforeNextRound(List<CardRuntime> cards)
        {
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                CardRuntime card = cards[i];
                if (card?.Data != null && card.Data.ExpiresBeforeNextRound)
                {
                    cards.RemoveAt(i);
                }
            }
        }

        // 批次替換同一張 CardBase 資產的所有 Runtime 實例，常用於卡牌升級或建築強化。
        private static int ReplaceMatchingCardData(List<CardRuntime> cards, CardBase source, CardBase replacement)
        {
            int replaced = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                CardRuntime card = cards[i];
                if (card == null || !ReferenceEquals(card.Data, source))
                {
                    continue;
                }

                CardRuntime replacementRuntime = replacement.CreateRuntimeCard();
                replacementRuntime.SetAssignedSlotType(card.AssignedSlotType);
                replacementRuntime.SetZone(card.Zone);
                cards[i] = replacementRuntime;
                card.SetZone(DeckZone.Removed);
                replaced++;
            }

            return replaced;
        }

        // 計算抽牌堆中的正規卡或附加卡數量。
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
