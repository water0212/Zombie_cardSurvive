using System;
using System.Collections.Generic;
using ZombieCardSurvive.Events.Data;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class RunFlagRuntimeStore
    {
        private readonly Dictionary<string, MutableFlagEntry> entries =
            new Dictionary<string, MutableFlagEntry>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<RunFlagRuntimeSnapshot> Entries
        {
            get
            {
                List<RunFlagRuntimeSnapshot> snapshot = new List<RunFlagRuntimeSnapshot>(entries.Count);
                foreach (KeyValuePair<string, MutableFlagEntry> pair in entries)
                {
                    snapshot.Add(new RunFlagRuntimeSnapshot(
                        pair.Key,
                        pair.Value.Value,
                        pair.Value.FirstSetDay,
                        pair.Value.LastChangedDay,
                        pair.Value.ChangedCount));
                }

                return snapshot;
            }
        }

        public void Reset()
        {
            entries.Clear();
        }

        public int GetValue(string flagId)
        {
            return TryGetEntry(flagId, out MutableFlagEntry entry) ? entry.Value : 0;
        }

        public int GetValue(RunFlagData flagData)
        {
            if (flagData == null || string.IsNullOrWhiteSpace(flagData.FlagId))
            {
                return 0;
            }

            return TryGetEntry(flagData.FlagId, out MutableFlagEntry entry)
                ? entry.Value
                : flagData.DefaultValue;
        }

        public bool IsActive(string flagId)
        {
            return GetValue(flagId) > 0;
        }

        public bool IsActive(RunFlagData flagData)
        {
            return GetValue(flagData) > 0;
        }

        public int GetDaysSinceFirstSet(string flagId, int currentDay)
        {
            return TryGetEntry(flagId, out MutableFlagEntry entry) && entry.FirstSetDay > 0
                ? Math.Max(0, Math.Max(1, currentDay) - entry.FirstSetDay)
                : 0;
        }

        public int GetDaysSinceFirstSet(RunFlagData flagData, int currentDay)
        {
            return flagData != null
                ? GetDaysSinceFirstSet(flagData.FlagId, currentDay)
                : 0;
        }

        public int SetValue(string flagId, int value, int day)
        {
            return SetValue(flagId, value, Math.Max(1, day), null, true);
        }

        public int SetValue(RunFlagData flagData, int value, int day, bool clampToDefinition = true)
        {
            string flagId = flagData != null ? flagData.FlagId : null;
            return SetValue(flagId, value, Math.Max(1, day), flagData, clampToDefinition);
        }

        public int AddValue(string flagId, int amount, int day)
        {
            return SetValue(flagId, GetValue(flagId) + amount, day);
        }

        public int AddValue(RunFlagData flagData, int amount, int day, bool clampToDefinition = true)
        {
            int current = GetValue(flagData);
            return SetValue(flagData, current + amount, day, clampToDefinition);
        }

        public void Clear(string flagId, int day)
        {
            SetValue(flagId, 0, day);
        }

        public void Clear(RunFlagData flagData, int day)
        {
            if (flagData != null)
            {
                SetValue(flagData.FlagId, 0, day, null, false);
            }
        }

        private int SetValue(
            string flagId,
            int value,
            int day,
            RunFlagData flagData,
            bool clampToDefinition)
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                return 0;
            }

            string normalizedId = flagId.Trim();
            int finalValue = clampToDefinition && flagData != null ? flagData.Clamp(value) : Math.Max(0, value);
            if (!entries.TryGetValue(normalizedId, out MutableFlagEntry entry))
            {
                entry = new MutableFlagEntry();
                entries.Add(normalizedId, entry);
            }

            bool wasInactive = entry.Value <= 0;
            entry.Value = finalValue;
            entry.LastChangedDay = day;
            entry.ChangedCount++;
            if (wasInactive && finalValue > 0)
            {
                entry.FirstSetDay = day;
            }

            if (finalValue <= 0)
            {
                entry.FirstSetDay = 0;
            }

            return entry.Value;
        }

        private bool TryGetEntry(string flagId, out MutableFlagEntry entry)
        {
            entry = null;
            return !string.IsNullOrWhiteSpace(flagId)
                && entries.TryGetValue(flagId.Trim(), out entry);
        }

        private sealed class MutableFlagEntry
        {
            public int Value;
            public int FirstSetDay;
            public int LastChangedDay;
            public int ChangedCount;
        }
    }

    public readonly struct RunFlagRuntimeSnapshot
    {
        public RunFlagRuntimeSnapshot(
            string flagId,
            int value,
            int firstSetDay,
            int lastChangedDay,
            int changedCount)
        {
            FlagId = flagId;
            Value = value;
            FirstSetDay = firstSetDay;
            LastChangedDay = lastChangedDay;
            ChangedCount = changedCount;
        }

        public string FlagId { get; }
        public int Value { get; }
        public int FirstSetDay { get; }
        public int LastChangedDay { get; }
        public int ChangedCount { get; }
    }
}
