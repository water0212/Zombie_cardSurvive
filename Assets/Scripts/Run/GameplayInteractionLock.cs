using System.Collections.Generic;

namespace ZombieCardSurvive.Run
{
    /// <summary>跨 Canvas 與世界互動共用的輕量鎖；擁有者必須對稱釋放。</summary>
    public static class GameplayInteractionLock
    {
        private static readonly HashSet<object> Owners = new HashSet<object>();
        public static bool IsLocked => Owners.Count > 0;

        public static void Acquire(object owner)
        {
            if (owner != null) Owners.Add(owner);
        }

        public static void Release(object owner)
        {
            if (owner != null) Owners.Remove(owner);
        }

        public static void Reset()
        {
            Owners.Clear();
        }
    }
}
