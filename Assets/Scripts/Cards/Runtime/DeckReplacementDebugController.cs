using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    /// <summary>
    /// 牌組替換系統的 Inspector 除錯控制器。
    /// 不屬於正式遊戲流程，用來手動建立替換 Session、查看候選與套用第一個合法替換。
    /// </summary>
    public class DeckReplacementDebugController : MonoBehaviour
    {
        // 被測試的卡牌控制器，提供目前牌組中的可替換目標。
        [SerializeField] private CardController cardController;
        // 若 useInventoryCards 為 true，替換候選會從這個倉庫取得。
        [SerializeField] private CardInventory inventory;
        // 本次替換的原因標籤，方便替換系統與 log 區分來源。
        [SerializeField] private DeckReplacementReason reason = DeckReplacementReason.Debug;
        // 本次 Session 最多允許替換幾張卡。
        [SerializeField] private int replacementCount = 1;
        // true 時使用 CardInventory；false 時改用 candidateCards 這份手動清單。
        [SerializeField] private bool useInventoryCards = true;
        // 不使用倉庫時的手動候選卡清單。
        [SerializeField] private List<CardBase> candidateCards = new List<CardBase>();
        // 限制哪些正規欄位可以被替換；空清單代表不額外限制。
        [SerializeField] private List<DeckSlotType> allowedSlotTypes = new List<DeckSlotType>();

        // 目前正在檢查的替換 Session；完成後下次會重新建立。
        private DeckReplacementSession activeSession;

        /// <summary>
        /// 建立新的替換 Session，並把 Session 摘要輸出到 Console。
        /// </summary>
        [ContextMenu("Start Replacement Session")]
        public void StartReplacementSession()
        {
            activeSession = new DeckReplacementSession(cardController, ResolveInventoryForReplacement(), ResolveDeckLayout(), CreateRequest());
            activeSession.Changed += LogSessionSummary;
            LogSessionSummary();
        }

        /// <summary>
        /// 將目前所有可替換目標與候選數量輸出到 Console，方便檢查資料設定。
        /// </summary>
        [ContextMenu("Log Replacement Options")]
        public void LogReplacementOptions()
        {
            DeckReplacementSession session = GetOrCreateSession();

            Debug.Log($"Replacement options: {session.Options.Count}");
            foreach (DeckReplacementOption option in session.Options)
            {
                string targetName = option.TargetCard != null && option.TargetCard.Data != null
                    ? option.TargetCard.Data.DisplayName
                    : "(None)";
                Debug.Log($"Target: {targetName} Slot: {option.SlotType} Candidates: {option.CandidateCards.Count}");
            }
        }

        /// <summary>
        /// 直接套用第一個合法替換選項，用於快速確認替換流程是否可運作。
        /// </summary>
        [ContextMenu("Replace First Valid Option")]
        public void ReplaceFirstValidOption()
        {
            DeckReplacementSession session = GetOrCreateSession();
            bool replaced = session.TryApplyFirstValid(out CardRuntime replacementRuntime);
            string replacementName = replacementRuntime != null && replacementRuntime.Data != null
                ? replacementRuntime.Data.DisplayName
                : "(None)";
            Debug.Log($"Replace first valid option result: {replaced}, New card: {replacementName}");
        }

        // 將 Inspector 設定包成正式的替換請求。
        private DeckReplacementRequest CreateRequest()
        {
            return new DeckReplacementRequest(
                reason,
                replacementCount,
                cardController != null ? cardController.GetReplaceableRegularCards() : null,
                ResolveCandidateEntries(),
                allowedSlotTypes);
        }

        // 依 useInventoryCards 決定候選卡來源：玩家倉庫或手動清單。
        private IEnumerable<CardInventoryEntry> ResolveCandidateEntries()
        {
            if (useInventoryCards && inventory != null)
            {
                return inventory.Entries;
            }

            return CreateEntriesFromCardData(candidateCards);
        }

        // 從 CardController 取得目前牌組版型，讓替換規則知道欄位限制。
        private DeckLayoutDefinition ResolveDeckLayout()
        {
            return cardController != null ? cardController.DeckLayout : null;
        }

        // 若目前沒有可用 Session，就自動建立新的除錯 Session。
        private DeckReplacementSession GetOrCreateSession()
        {
            if (activeSession == null || activeSession.IsComplete)
            {
                StartReplacementSession();
            }

            return activeSession;
        }

        // 正式替換 Session 可選擇是否帶入 CardInventory。
        private CardInventory ResolveInventoryForReplacement()
        {
            return useInventoryCards ? inventory : null;
        }

        // 將目前替換進度與選項數量輸出到 Console。
        private void LogSessionSummary()
        {
            if (activeSession == null)
            {
                return;
            }

            Debug.Log($"Replacement session: {activeSession.CompletedReplacementCount}/{activeSession.Request.ReplacementCount}, Remaining: {activeSession.RemainingReplacementCount}, Options: {activeSession.Options.Count}");
        }

        // 將手動填入的 CardBase 清單包成和倉庫相同格式的候選 Entry。
        private static IEnumerable<CardInventoryEntry> CreateEntriesFromCardData(IEnumerable<CardBase> source)
        {
            List<CardInventoryEntry> entries = new List<CardInventoryEntry>();
            if (source == null)
            {
                return entries;
            }

            foreach (CardBase card in source)
            {
                if (card != null)
                {
                    entries.Add(new CardInventoryEntry(card));
                }
            }

            return entries;
        }
    }
}
