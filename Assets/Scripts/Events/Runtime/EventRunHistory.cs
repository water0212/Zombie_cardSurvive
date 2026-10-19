using System;
using System.Collections.Generic;
using ZombieCardSurvive.Events.Data;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class EventRunHistory
    {
        private readonly Dictionary<string, MutableEntry> entries =
            new Dictionary<string, MutableEntry>(StringComparer.OrdinalIgnoreCase);

        public int? LastNegativeEventDay { get; private set; }

        public IReadOnlyList<EventRunHistoryEntry> Entries
        {
            get
            {
                List<EventRunHistoryEntry> snapshot = new List<EventRunHistoryEntry>(entries.Count);
                foreach (KeyValuePair<string, MutableEntry> pair in entries)
                {
                    snapshot.Add(new EventRunHistoryEntry(pair.Key, pair.Value.AppearanceCount, pair.Value.LastAppearanceDay));
                }

                return snapshot;
            }
        }

        public void Reset()
        {
            entries.Clear();
            LastNegativeEventDay = null;
        }

        public void RecordAppearance(EventData eventData, int day)
        {
            if (eventData == null || string.IsNullOrWhiteSpace(eventData.EventId))
            {
                return;
            }

            string eventId = eventData.EventId.Trim();
            if (!entries.TryGetValue(eventId, out MutableEntry entry))
            {
                entry = new MutableEntry();
                entries.Add(eventId, entry);
            }

            entry.AppearanceCount++;
            entry.LastAppearanceDay = Math.Max(1, day);

            if (eventData.MoodCategory == EventMoodCategory.Negative)
            {
                LastNegativeEventDay = entry.LastAppearanceDay;
            }
        }

        public int GetAppearanceCount(string eventId)
        {
            return TryGetEntry(eventId, out MutableEntry entry) ? entry.AppearanceCount : 0;
        }

        public int? GetLastAppearanceDay(string eventId)
        {
            return TryGetEntry(eventId, out MutableEntry entry) ? entry.LastAppearanceDay : (int?)null;
        }

        public int GetDaysSinceLastNegativeEvent(int currentDay)
        {
            return LastNegativeEventDay.HasValue
                ? Math.Max(0, currentDay - LastNegativeEventDay.Value)
                : Math.Max(0, currentDay - 1);
        }

        private bool TryGetEntry(string eventId, out MutableEntry entry)
        {
            entry = null;
            return !string.IsNullOrWhiteSpace(eventId)
                && entries.TryGetValue(eventId.Trim(), out entry);
        }

        private sealed class MutableEntry
        {
            public int AppearanceCount;
            public int LastAppearanceDay;
        }
    }

    public readonly struct EventRunHistoryEntry
    {
        public EventRunHistoryEntry(string eventId, int appearanceCount, int lastAppearanceDay)
        {
            EventId = eventId;
            AppearanceCount = appearanceCount;
            LastAppearanceDay = lastAppearanceDay;
        }

        public string EventId { get; }
        public int AppearanceCount { get; }
        public int LastAppearanceDay { get; }
    }
}
