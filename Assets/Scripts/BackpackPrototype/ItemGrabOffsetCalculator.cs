using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>
    /// Chooses the occupied item cell that remains under the pointer while dragging.
    /// </summary>
    public static class ItemGrabOffsetCalculator
    {
        public static Vector2Int Calculate(
            IReadOnlyList<Vector2Int> shapeOffsets)
        {
            Vector2Int topLeft = FindTopLeft(shapeOffsets);

            if (shapeOffsets == null || shapeOffsets.Count != 3)
            {
                return topLeft;
            }

            var uniqueOffsets = new HashSet<Vector2Int>(shapeOffsets);
            if (uniqueOffsets.Count != 3)
            {
                return topLeft;
            }

            foreach (Vector2Int candidate in uniqueOffsets)
            {
                bool hasHorizontalNeighbor = false;
                bool hasVerticalNeighbor = false;

                foreach (Vector2Int offset in uniqueOffsets)
                {
                    if (offset == candidate)
                    {
                        continue;
                    }

                    if (offset.y == candidate.y &&
                        Mathf.Abs(offset.x - candidate.x) == 1)
                    {
                        hasHorizontalNeighbor = true;
                    }

                    if (offset.x == candidate.x &&
                        Mathf.Abs(offset.y - candidate.y) == 1)
                    {
                        hasVerticalNeighbor = true;
                    }
                }

                if (hasHorizontalNeighbor && hasVerticalNeighbor)
                {
                    return candidate;
                }
            }

            return topLeft;
        }

        private static Vector2Int FindTopLeft(
            IReadOnlyList<Vector2Int> shapeOffsets)
        {
            if (shapeOffsets == null || shapeOffsets.Count == 0)
            {
                return Vector2Int.zero;
            }

            Vector2Int topLeft = shapeOffsets[0];

            for (int index = 1; index < shapeOffsets.Count; index++)
            {
                Vector2Int candidate = shapeOffsets[index];
                if (candidate.y < topLeft.y ||
                    candidate.y == topLeft.y && candidate.x < topLeft.x)
                {
                    topLeft = candidate;
                }
            }

            return topLeft;
        }
    }
}
