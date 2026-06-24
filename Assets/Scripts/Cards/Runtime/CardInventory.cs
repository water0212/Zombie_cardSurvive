using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    [Serializable]
    public class CardInventoryEntry
    {
        [SerializeField] private CardBase data;
        [SerializeField] private string instanceId;
        [SerializeField] private int remainingUses = -1;
        [SerializeField] private bool isReusable;

        public CardInventoryEntry()
        {
        }

        public CardInventoryEntry(CardBase data)
            : this(data, GetDefaultRemainingUses(data), Guid.NewGuid().ToString("N"))
        {
        }

        public CardInventoryEntry(CardRuntime runtime)
            : this(
                runtime != null ? runtime.Data : null,
                runtime != null ? runtime.RemainingUses : -1,
                runtime != null ? runtime.RuntimeId : Guid.NewGuid().ToString("N"))
        {
        }

        public CardInventoryEntry(CardBase data, int remainingUses, string instanceId)
            : this(data, remainingUses, instanceId, false)
        {
        }

        public CardInventoryEntry(CardBase data, int remainingUses, string instanceId, bool isReusable)
        {
            this.data = data;
            this.instanceId = !string.IsNullOrWhiteSpace(instanceId) ? instanceId : Guid.NewGuid().ToString("N");
            this.remainingUses = NormalizeRemainingUses(data, remainingUses);
            this.isReusable = isReusable;
        }

        public CardBase Data => data;
        public string InstanceId => instanceId;
        public int RemainingUses => NormalizeRemainingUses(data, remainingUses);
        public bool IsReusable => isReusable;
        public bool HasRemainingUses => data == null || !data.HasLimitedUses || RemainingUses > 0;

        public CardRuntime CreateRuntimeCard()
        {
            return new CardRuntime(data, RemainingUses, isReusable ? null : instanceId);
        }

        private static int GetDefaultRemainingUses(CardBase data)
        {
            return data != null && data.HasLimitedUses ? data.MaxUsesPerRun : -1;
        }

        private static int NormalizeRemainingUses(CardBase data, int remainingUses)
        {
            if (data == null || !data.HasLimitedUses)
            {
                return -1;
            }

            if (remainingUses < 0)
            {
                return data.MaxUsesPerRun;
            }

            return Mathf.Min(data.MaxUsesPerRun, Mathf.Max(0, remainingUses));
        }
    }

    public class CardInventory : MonoBehaviour
    {
        [SerializeField] private List<CardBase> startingCards = new List<CardBase>();

        private readonly List<CardInventoryEntry> entries = new List<CardInventoryEntry>();
        private readonly List<CardBase> cards = new List<CardBase>();

        public IReadOnlyList<CardInventoryEntry> Entries => entries;
        public IReadOnlyList<CardBase> Cards => cards;
        public int Count => entries.Count;

        private void Awake()
        {
            ResetInventory();
        }

        [ContextMenu("Reset Inventory")]
        public void ResetInventory()
        {
            entries.Clear();
            cards.Clear();

            foreach (CardBase card in startingCards)
            {
                if (card != null)
                {
                    AddCard(card);
                }
            }
        }

        public void AddCard(CardBase card)
        {
            if (card != null)
            {
                AddEntry(new CardInventoryEntry(card));
            }
        }

        public void AddRuntimeCard(CardRuntime card)
        {
            if (card != null && card.Data != null)
            {
                AddEntry(new CardInventoryEntry(card));
            }
        }

        public void AddEntry(CardInventoryEntry entry)
        {
            if (entry != null && entry.Data != null)
            {
                entries.Add(entry);
                cards.Add(entry.Data);
            }
        }

        public bool RemoveCard(CardBase card)
        {
            if (card == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                CardInventoryEntry entry = entries[i];
                if (entry != null && ReferenceEquals(entry.Data, card))
                {
                    RemoveEntryAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool RemoveEntry(CardInventoryEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (ReferenceEquals(entries[i], entry))
                {
                    RemoveEntryAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool Contains(CardBase card)
        {
            return card != null && cards.Contains(card);
        }

        public bool ContainsEntry(CardInventoryEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            foreach (CardInventoryEntry candidate in entries)
            {
                if (ReferenceEquals(candidate, entry))
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveEntryAt(int index)
        {
            entries.RemoveAt(index);

            if (index >= 0 && index < cards.Count)
            {
                cards.RemoveAt(index);
            }
            else
            {
                RebuildCardDataCache();
            }
        }

        private void RebuildCardDataCache()
        {
            cards.Clear();
            foreach (CardInventoryEntry entry in entries)
            {
                if (entry != null && entry.Data != null)
                {
                    cards.Add(entry.Data);
                }
            }
        }
    }
}
