using System.Collections.Generic;

namespace HY3RIDOrigins.UI
{
    /// Tracks which touch IDs are currently owned by a UI consumer.
    /// Prevents two consumers from reading the same finger simultaneously.
    /// All methods are O(1) via HashSet. Not thread-safe — call from main thread only.
    internal static class TouchOwnership
    {
        private static readonly HashSet<int> s_owned = new HashSet<int>();

        internal static void Claim(int touchId)   => s_owned.Add(touchId);
        internal static void Release(int touchId) => s_owned.Remove(touchId);
        internal static bool IsOwned(int touchId) => s_owned.Contains(touchId);
    }
}
