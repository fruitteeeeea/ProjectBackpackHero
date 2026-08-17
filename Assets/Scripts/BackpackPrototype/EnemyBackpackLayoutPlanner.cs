using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>敌人用的中心优先、邻接飞机优先的随机布局器。</summary>
    public static class EnemyBackpackLayoutPlanner
    {
        private const int CandidateCount = 24;

        public static bool TryBuild(
            IReadOnlyList<ItemData> deck,
            int width,
            int height,
            out List<BackpackLayoutItem> layout)
        {
            layout = null;
            if (deck == null)
            {
                return false;
            }

            List<ItemData> items = new();
            foreach (ItemData item in deck)
            {
                if (item != null)
                {
                    items.Add(item);
                }
            }

            if (items.Count == 0)
            {
                layout = new List<BackpackLayoutItem>();
                return true;
            }

            List<BackpackLayoutItem> best = null;
            int bestScore = int.MinValue;
            for (int attempt = 0; attempt < CandidateCount; attempt++)
            {
                if (!TryBuildCandidate(items, width, height, out List<BackpackLayoutItem> candidate))
                {
                    continue;
                }

                int score = Score(candidate, width, height);
                if (best == null || score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            layout = best;
            return layout != null;
        }

        public static bool TryFindPreferredCell(
            BackpackController backpack,
            ItemInstance item,
            ItemInstance ignore,
            out Vector2Int cell)
        {
            cell = default;
            if (backpack == null || item?.Data == null)
            {
                return false;
            }

            List<Vector2Int> cells = GetCenterFirstCells(backpack.Width, backpack.Height);
            foreach (Vector2Int candidate in cells)
            {
                if (backpack.CanPlace(item, candidate, ignore))
                {
                    cell = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryBuildCandidate(
            List<ItemData> source,
            int width,
            int height,
            out List<BackpackLayoutItem> layout)
        {
            List<ItemData> items = new(source);
            Shuffle(items);
            BackpackController validation = new(width, height);
            layout = new List<BackpackLayoutItem>(items.Count);
            int id = 0;
            foreach (ItemData item in items)
            {
                bool placed = false;
                foreach (Vector2Int cell in GetCenterFirstCells(width, height))
                {
                    ItemInstance candidate = new($"enemy-layout-{++id}", item, cell);
                    if (!validation.PlaceItem(candidate, cell))
                    {
                        continue;
                    }

                    layout.Add(new BackpackLayoutItem(item, cell));
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    layout = null;
                    return false;
                }
            }

            return true;
        }

        private static int Score(
            IReadOnlyList<BackpackLayoutItem> layout,
            int width,
            int height)
        {
            BackpackController validation = new(width, height);
            int id = 0;
            foreach (BackpackLayoutItem placement in layout)
            {
                validation.PlaceItem(new ItemInstance($"enemy-score-{++id}", placement.Data, placement.AnchorCell), placement.AnchorCell);
            }

            int aircraftCovered = 0;
            int adjacencyLinks = 0;
            int centerPenalty = 0;
            foreach (ItemInstance item in validation.Items)
            {
                centerPenalty += Mathf.RoundToInt((Mathf.Abs(item.AnchorCell.x - (width - 1) * .5f) + Mathf.Abs(item.AnchorCell.y - (height - 1) * .5f)) * 10f);
                if (item.Data.ItemType != ItemType.Equipment)
                {
                    continue;
                }

                List<ItemInstance> aircraft = validation.GetAdjacentAircraftItems(item);
                aircraftCovered += aircraft.Count;
                adjacencyLinks += aircraft.Count;
            }

            // 覆盖飞机数的权重最高；中心只作为相同战术布局的平局规则。
            return aircraftCovered * 10000 + adjacencyLinks * 100 - centerPenalty;
        }

        private static List<Vector2Int> GetCenterFirstCells(int width, int height)
        {
            List<Vector2Int> cells = new(width * height);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                cells.Add(new Vector2Int(x, y));
            float centerX = (width - 1) * .5f;
            float centerY = (height - 1) * .5f;
            cells.Sort((left, right) =>
            {
                float leftDistance = Mathf.Abs(left.x - centerX) + Mathf.Abs(left.y - centerY);
                float rightDistance = Mathf.Abs(right.x - centerX) + Mathf.Abs(right.y - centerY);
                int comparison = leftDistance.CompareTo(rightDistance);
                if (comparison != 0) return comparison;
                // 排序比较器必须保持传递性；布局差异由物品顺序产生，
                // 同距离格子保持稳定的坐标顺序。
                comparison = left.y.CompareTo(right.y);
                return comparison != 0 ? comparison : left.x.CompareTo(right.x);
            });
            return cells;
        }

        private static void Shuffle<T>(IList<T> values)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int swap = UnityEngine.Random.Range(0, index + 1);
                (values[index], values[swap]) = (values[swap], values[index]);
            }
        }
    }
}
