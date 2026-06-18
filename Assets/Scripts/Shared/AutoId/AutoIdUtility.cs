using System;

namespace ZombieCardSurvive.Shared.AutoId
{
    public static class AutoIdUtility
    {
        public static string CreateId(string prefix)
        {
            string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "id" : prefix.Trim();
            return $"{safePrefix}_{Guid.NewGuid():N}";
        }
    }
}
