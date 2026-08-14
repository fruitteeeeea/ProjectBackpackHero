using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>
    /// 计算物品图像与视觉效果使用的形状中心，坐标以格子中心为单位。
    /// </summary>
    public static class ItemShapeGeometry
    {
        public static Vector2 CalculateCenter(
            IReadOnlyList<Vector2Int> shapeOffsets)
        {
            if (shapeOffsets == null || shapeOffsets.Count == 0)
            {
                return Vector2.zero;
            }

            if (TryGetLCorner(shapeOffsets, out Vector2Int corner))
            {
                return corner;
            }

            Vector2Int min = shapeOffsets[0];
            Vector2Int max = shapeOffsets[0];
            for (int index = 1; index < shapeOffsets.Count; index++)
            {
                Vector2Int offset = shapeOffsets[index];
                min = Vector2Int.Min(min, offset);
                max = Vector2Int.Max(max, offset);
            }

            return new Vector2(
                (min.x + max.x) * .5f,
                (min.y + max.y) * .5f);
        }

        private static bool TryGetLCorner(
            IReadOnlyList<Vector2Int> shapeOffsets,
            out Vector2Int corner)
        {
            corner = Vector2Int.zero;
            if (shapeOffsets.Count != 3)
            {
                return false;
            }

            foreach (Vector2Int candidate in shapeOffsets)
            {
                int horizontalNeighbors = 0;
                int verticalNeighbors = 0;

                foreach (Vector2Int offset in shapeOffsets)
                {
                    if (offset == candidate)
                    {
                        continue;
                    }

                    if (offset.y == candidate.y &&
                        Mathf.Abs(offset.x - candidate.x) == 1)
                    {
                        horizontalNeighbors++;
                    }

                    if (offset.x == candidate.x &&
                        Mathf.Abs(offset.y - candidate.y) == 1)
                    {
                        verticalNeighbors++;
                    }
                }

                if (horizontalNeighbors == 1 && verticalNeighbors == 1)
                {
                    corner = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
