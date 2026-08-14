using UnityEngine;
using System.Collections.Generic;

namespace BackpackPrototype
{
    /// <summary>
    /// Calculates the grid cell used to place an item when its lower geometric
    /// center is held by the pointer.
    /// </summary>
    public static class ItemGrabOffsetCalculator
    {
        public static Vector2Int Calculate(
            IReadOnlyList<Vector2Int> shapeOffsets)
        {
            Vector2 anchor = CalculateVisualAnchor(shapeOffsets);
            return new Vector2Int(
                Mathf.RoundToInt(anchor.x),
                Mathf.RoundToInt(anchor.y));
        }

        /// <summary>
        /// Returns the exact logical location of the drag anchor. Its x value is
        /// the image's geometric center; its y value is the lowest occupied row.
        /// ItemView uses this row to position the anchor on that row's bottom edge.
        /// </summary>
        public static Vector2 CalculateVisualAnchor(
            IReadOnlyList<Vector2Int> shapeOffsets)
        {
            if (shapeOffsets == null || shapeOffsets.Count == 0)
            {
                return Vector2Int.zero;
            }

            int bottomRow = shapeOffsets[0].y;

            for (int index = 1; index < shapeOffsets.Count; index++)
            {
                bottomRow = Mathf.Max(bottomRow, shapeOffsets[index].y);
            }

            return new Vector2(
                ItemShapeGeometry.CalculateCenter(shapeOffsets).x,
                bottomRow);
        }

        /// <summary>
        /// A horizontal two-cell item preserves the cell the player pressed as
        /// its drag anchor. This lets either cell be held from its bottom edge.
        /// </summary>
        public static bool TryCalculateTwoByOneAnchor(
            IReadOnlyList<Vector2Int> shapeOffsets,
            Vector2Int grabbedCell,
            out Vector2 anchor)
        {
            anchor = Vector2.zero;
            if (shapeOffsets == null || shapeOffsets.Count != 2 ||
                (shapeOffsets[0].y != shapeOffsets[1].y ||
                 Mathf.Abs(shapeOffsets[0].x - shapeOffsets[1].x) != 1))
            {
                return false;
            }

            if (grabbedCell != shapeOffsets[0] &&
                grabbedCell != shapeOffsets[1])
            {
                return false;
            }

            anchor = grabbedCell;
            return true;
        }
    }
}
