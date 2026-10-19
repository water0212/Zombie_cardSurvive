namespace ZombieCardSurvive.Events.Runtime
{
    /// <summary>建立跨執行環境一致的事件屍群亂數種子。</summary>
    public static class EventHordeSeedUtility
    {
        public static int CreateSeed(
            int runSeed,
            int eventDay,
            string eventId,
            int choiceIndex,
            int effectIndex)
        {
            unchecked
            {
                int seed = runSeed;
                seed = seed * 397 ^ eventDay;
                seed = seed * 397 ^ StableStringHash(eventId);
                seed = seed * 397 ^ choiceIndex;
                seed = seed * 397 ^ effectIndex;
                return seed;
            }
        }

        public static int StableStringHash(string value)
        {
            unchecked
            {
                const uint offsetBasis = 2166136261u;
                const uint prime = 16777619u;
                uint hash = offsetBasis;
                if (!string.IsNullOrEmpty(value))
                {
                    for (int index = 0; index < value.Length; index++)
                    {
                        hash ^= value[index];
                        hash *= prime;
                    }
                }

                return (int)hash;
            }
        }
    }
}
