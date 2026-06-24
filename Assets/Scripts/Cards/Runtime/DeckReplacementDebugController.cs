using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    public class DeckReplacementDebugController : MonoBehaviour
    {
        [SerializeField] private CardController cardController;
        [SerializeField] private CardInventory inventory;
        [SerializeField] private DeckReplacementReason reason = DeckReplacementReason.Debug;
        [SerializeField] private int replacementCount = 1;
        [SerializeField] private bool useInventoryCards = true;
        [SerializeField] private List<CardBase> candidateCards = new List<CardBase>();
        [SerializeField] private List<DeckSlotType> allowedSlotTypes = new List<DeckSlotType>();

        private DeckReplacementSession activeSession;

        [ContextMenu("Start Replacement Session")]
        public void StartReplacementSession()
        {
            activeSession = new DeckReplacementSession(cardController, ResolveInventoryForReplacement(), ResolveDeckLayout(), CreateRequest());
            activeSession.Changed += LogSessionSummary;
            LogSessionSummary();
        }

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

        private DeckReplacementRequest CreateRequest()
        {
            return new DeckReplacementRequest(
                reason,
                replacementCount,
                cardController != null ? cardController.GetReplaceableRegularCards() : null,
                ResolveCandidateEntries(),
                allowedSlotTypes);
        }

        private IEnumerable<CardInventoryEntry> ResolveCandidateEntries()
        {
            if (useInventoryCards && inventory != null)
            {
                return inventory.Entries;
            }

            return CreateEntriesFromCardData(candidateCards);
        }

        private DeckLayoutDefinition ResolveDeckLayout()
        {
            return cardController != null ? cardController.DeckLayout : null;
        }

        private DeckReplacementSession GetOrCreateSession()
        {
            if (activeSession == null || activeSession.IsComplete)
            {
                StartReplacementSession();
            }

            return activeSession;
        }

        private CardInventory ResolveInventoryForReplacement()
        {
            return useInventoryCards ? inventory : null;
        }

        private void LogSessionSummary()
        {
            if (activeSession == null)
            {
                return;
            }

            Debug.Log($"Replacement session: {activeSession.CompletedReplacementCount}/{activeSession.Request.ReplacementCount}, Remaining: {activeSession.RemainingReplacementCount}, Options: {activeSession.Options.Count}");
        }

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
