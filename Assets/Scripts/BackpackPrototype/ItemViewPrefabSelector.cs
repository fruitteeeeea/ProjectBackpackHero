using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>Selects a visual shell from shape offsets, never from an item-specific catalog.</summary>
    public static class ItemViewPrefabSelector
    {
        public static ItemView Select(
            ItemData data,
            ItemView fallback,
            ItemView oneByTwo,
            ItemView twoByOne,
            ItemView missingBottomLeft,
            ItemView missingBottomRight,
            ItemView missingTopLeft,
            ItemView missingTopRight)
        {
            IReadOnlyList<Vector2Int> offsets = data?.ShapeOffsets;
            if (offsets == null) return fallback;
            if (Has(offsets, new Vector2Int(0, 0)) && Has(offsets, new Vector2Int(0, 1)) && offsets.Count == 2)
                return oneByTwo != null ? oneByTwo : fallback;
            if (Has(offsets, new Vector2Int(0, 0)) && Has(offsets, new Vector2Int(1, 0)) && offsets.Count == 2)
                return twoByOne != null ? twoByOne : fallback;
            if (offsets.Count != 3) return fallback;
            if (!Has(offsets, new Vector2Int(0, 0))) return missingTopLeft != null ? missingTopLeft : fallback;
            if (!Has(offsets, new Vector2Int(1, 0))) return missingTopRight != null ? missingTopRight : fallback;
            if (!Has(offsets, new Vector2Int(0, 1))) return missingBottomLeft != null ? missingBottomLeft : fallback;
            if (!Has(offsets, new Vector2Int(1, 1))) return missingBottomRight != null ? missingBottomRight : fallback;
            return fallback;
        }

        private static bool Has(IReadOnlyList<Vector2Int> offsets, Vector2Int cell)
        {
            foreach (Vector2Int offset in offsets)
                if (offset == cell) return true;
            return false;
        }
    }
}
