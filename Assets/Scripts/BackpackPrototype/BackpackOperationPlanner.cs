using System;
using System.Collections.Generic;
using BackpackHero.Battle;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
#endif

namespace BackpackPrototype
{
    /// <summary>玩家与敌人共用的准备阶段背包操作评分器。</summary>
    public enum BackpackOperationKind { RollShop, MoveItem, AddShopItem, RemoveItem, MergeItems, MergeShopItem, ReplaceItem }

    public enum BackpackOperationSelectionMode { GainOnly, ExploreNonDecreasing }

    public sealed class BackpackOperation
    {
        public BackpackOperationKind Kind;
        public ItemInstance Item;
        public ItemInstance SecondaryItem;
        public ItemData ShopItem;
        public Vector2Int Destination;
        public bool IsExploration;
    }

    public static class BackpackOperationPlanner
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly ProfilerMarker SelectionMarker =
            new("SampleScenePerf.BackpackOperationPlanner.TrySelectBest");
#endif

        public static bool TrySelectBest(BackpackController backpack,
            IReadOnlyList<ItemData> shopItems, int remainingRolls,
            out BackpackOperation selected)
        {
            return TrySelectBest(backpack, shopItems, remainingRolls,
                BackpackOperationSelectionMode.GainOnly, null, out selected);
        }

        public static bool TrySelectBest(BackpackController backpack,
            IReadOnlyList<ItemData> shopItems, int remainingRolls,
            BackpackOperationSelectionMode mode,
            ISet<string> visitedLayouts,
            out BackpackOperation selected)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (SelectionMarker.Auto())
            {
                return TrySelectBestCore(backpack, shopItems, remainingRolls,
                    mode, visitedLayouts, out selected);
            }
#else
            return TrySelectBestCore(backpack, shopItems, remainingRolls,
                mode, visitedLayouts, out selected);
#endif
        }

        private static bool TrySelectBestCore(BackpackController backpack,
            IReadOnlyList<ItemData> shopItems, int remainingRolls,
            BackpackOperationSelectionMode mode,
            ISet<string> visitedLayouts,
            out BackpackOperation selected)
        {
            selected = null;
            if (backpack == null) return false;
            float current = BackpackStrengthCalculator.Calculate(backpack).TotalScore;
            float bestGain = float.NegativeInfinity;
            List<BackpackOperation> gainTies = new();
            List<SimulatedCandidate> neutralCandidates = new();
            List<SimulatedCandidate> evaluatedCandidates = new();
            foreach (BackpackOperation candidate in BuildCandidates(backpack, shopItems, remainingRolls))
            {
                if (!TrySimulate(backpack, candidate, out BackpackController simulated, out float score)) continue;
                SimulatedCandidate simulatedCandidate = new(candidate, simulated, score);
                evaluatedCandidates.Add(simulatedCandidate);
                float gain = score - current;
                if (gain > .001f && gain > bestGain + .001f)
                {
                    bestGain = gain;
                    gainTies.Clear();
                    gainTies.Add(candidate);
                }
                else if (gain > .001f && Mathf.Abs(gain - bestGain) <= .001f)
                {
                    gainTies.Add(candidate);
                }

                if (gain >= -.001f)
                {
                    neutralCandidates.Add(simulatedCandidate);
                }
            }

            if (TrySelectShopPlacementOrMerge(
                    evaluatedCandidates, current, out selected))
            {
                return true;
            }

            if (gainTies.Count > 0)
            {
                selected = gainTies[UnityEngine.Random.Range(0, gainTies.Count)];
                return true;
            }

            if (mode == BackpackOperationSelectionMode.GainOnly)
            {
                return false;
            }

            // 不加分时，先选择能够在下一步产生直接增益的零损失操作。
            float bestFutureGain = .001f;
            List<BackpackOperation> setupTies = new();
            foreach (SimulatedCandidate candidate in neutralCandidates)
            {
                if (candidate.Operation.Kind == BackpackOperationKind.RollShop) continue;
                float futureGain = FindBestDirectGain(candidate.Backpack, ApplyShopChange(shopItems, candidate.Operation));
                if (futureGain > bestFutureGain + .001f)
                {
                    bestFutureGain = futureGain;
                    setupTies.Clear();
                    setupTies.Add(candidate.Operation);
                }
                else if (futureGain > .001f && Mathf.Abs(futureGain - bestFutureGain) <= .001f)
                {
                    setupTies.Add(candidate.Operation);
                }
            }
            if (setupTies.Count > 0)
            {
                selected = setupTies[UnityEngine.Random.Range(0, setupTies.Count)];
                selected.IsExploration = true;
                return true;
            }

            // Roll 的结果无法预判；在所有当前操作都不增益时，用剩余 Roll 探索新商店。
            if (remainingRolls > 0)
            {
                selected = new BackpackOperation { Kind = BackpackOperationKind.RollShop, IsExploration = true };
                return true;
            }

            List<BackpackOperation> unseenTies = new();
            foreach (SimulatedCandidate candidate in neutralCandidates)
            {
                if (candidate.Operation.Kind == BackpackOperationKind.RollShop ||
                    (visitedLayouts != null && visitedLayouts.Contains(GetLayoutFingerprint(candidate.Backpack))))
                {
                    continue;
                }
                unseenTies.Add(candidate.Operation);
            }
            if (unseenTies.Count == 0) return false;
            selected = unseenTies[UnityEngine.Random.Range(0, unseenTies.Count)];
            selected.IsExploration = true;
            return true;
        }

        public static string GetLayoutFingerprint(BackpackController backpack)
        {
            if (backpack == null) return string.Empty;
            List<string> entries = new();
            foreach (ItemInstance item in backpack.Items)
                if (item?.Data != null)
                    entries.Add($"{item.Data.name}:{item.Level}:{item.AnchorCell.x}:{item.AnchorCell.y}");
            entries.Sort(StringComparer.Ordinal);
            return string.Join("|", entries);
        }

        private static IEnumerable<BackpackOperation> BuildCandidates(BackpackController backpack, IReadOnlyList<ItemData> shop, int rolls)
        {
            if (rolls > 0) yield return new BackpackOperation { Kind = BackpackOperationKind.RollShop };
            foreach (ItemInstance item in backpack.Items)
            {
                foreach (Vector2Int cell in EnumerateCells(backpack))
                    if (cell != item.AnchorCell && backpack.CanPlace(item, cell, item))
                        yield return new BackpackOperation { Kind = BackpackOperationKind.MoveItem, Item = item, Destination = cell };
                foreach (ItemInstance target in backpack.Items)
                    if (backpack.CanMerge(item, target)) yield return new BackpackOperation { Kind = BackpackOperationKind.MergeItems, Item = item, SecondaryItem = target };
            }
            if (shop == null || shop.Count == 0) yield break;
            foreach (ItemData data in shop)
            {
                if (data == null) continue;
                ItemInstance candidate = new("planner", data, Vector2Int.zero);
                foreach (Vector2Int cell in EnumerateCells(backpack))
                    if (backpack.CanPlace(candidate, cell))
                        yield return new BackpackOperation { Kind = BackpackOperationKind.AddShopItem, ShopItem = data, Destination = cell };
                foreach (ItemInstance target in backpack.Items)
                    if (backpack.CanMerge(candidate, target))
                        yield return new BackpackOperation { Kind = BackpackOperationKind.MergeShopItem, ShopItem = data, SecondaryItem = target };
                foreach (ItemInstance old in backpack.Items)
                    if (backpack.CanPlace(new ItemInstance("planner-replace", data, old.AnchorCell), old.AnchorCell, old))
                        yield return new BackpackOperation { Kind = BackpackOperationKind.ReplaceItem, Item = old, ShopItem = data, Destination = old.AnchorCell };
            }
            bool full = true;
            for (int y = 0; y < backpack.Height; y++) for (int x = 0; x < backpack.Width; x++) if (backpack.GetItemAt(new Vector2Int(x, y)) == null) full = false;
            if (full) foreach (ItemInstance item in backpack.Items) yield return new BackpackOperation { Kind = BackpackOperationKind.RemoveItem, Item = item };
        }

        private static IEnumerable<Vector2Int> EnumerateCells(BackpackController backpack)
        {
            for (int y = 0; y < backpack.Height; y++)
                for (int x = 0; x < backpack.Width; x++)
                    yield return new Vector2Int(x, y);
        }

        private static float FindBestDirectGain(BackpackController backpack, IReadOnlyList<ItemData> shop)
        {
            float current = BackpackStrengthCalculator.Calculate(backpack).TotalScore;
            float bestGain = 0f;
            foreach (BackpackOperation candidate in BuildCandidates(backpack, shop, 0))
            {
                if (TrySimulate(backpack, candidate, out _, out float score))
                    bestGain = Mathf.Max(bestGain, score - current);
            }
            return bestGain;
        }

        private static IReadOnlyList<ItemData> ApplyShopChange(IReadOnlyList<ItemData> shop, BackpackOperation operation)
        {
            List<ItemData> result = shop != null ? new List<ItemData>(shop) : new List<ItemData>();
            if ((operation.Kind == BackpackOperationKind.AddShopItem ||
                 operation.Kind == BackpackOperationKind.MergeShopItem ||
                 operation.Kind == BackpackOperationKind.ReplaceItem) && operation.ShopItem != null)
                result.Remove(operation.ShopItem);
            return result;
        }

        private static bool TrySimulate(BackpackController source, BackpackOperation operation, out BackpackController copy, out float score)
        {
            score = 0f; copy = new BackpackController(source.Width, source.Height); Dictionary<ItemInstance, ItemInstance> map = new(); int id = 0;
            foreach (ItemInstance item in source.Items) { ItemInstance clone = new("planner-" + ++id, item.Data, item.AnchorCell, item.Level); if (!copy.PlaceItem(clone, clone.AnchorCell)) return false; map[item] = clone; }
            bool ok = operation.Kind switch
            {
                BackpackOperationKind.RollShop => true,
                BackpackOperationKind.MoveItem => map.TryGetValue(operation.Item, out ItemInstance move) && copy.MoveItem(move, operation.Destination),
                BackpackOperationKind.AddShopItem => copy.PlaceItem(new ItemInstance("planner-add", operation.ShopItem, operation.Destination), operation.Destination),
                BackpackOperationKind.RemoveItem => map.TryGetValue(operation.Item, out ItemInstance remove) && copy.RemoveItem(remove),
                BackpackOperationKind.MergeItems => map.TryGetValue(operation.Item, out ItemInstance from) && map.TryGetValue(operation.SecondaryItem, out ItemInstance to) && copy.TryMerge(from, to),
                BackpackOperationKind.MergeShopItem => operation.ShopItem != null && map.TryGetValue(operation.SecondaryItem, out ItemInstance shopMergeTarget) && copy.TryMerge(new ItemInstance("planner-shop-merge", operation.ShopItem, Vector2Int.zero), shopMergeTarget),
                BackpackOperationKind.ReplaceItem => map.TryGetValue(operation.Item, out ItemInstance old) && copy.RemoveItem(old) && copy.PlaceItem(new ItemInstance("planner-replace", operation.ShopItem, operation.Destination), operation.Destination),
                _ => false
            };
            if (!ok) { copy = null; return false; } score = BackpackStrengthCalculator.Calculate(copy).TotalScore; return true;
        }

        private static bool TrySelectShopPlacementOrMerge(
            IReadOnlyList<SimulatedCandidate> candidates,
            float currentScore,
            out BackpackOperation selected)
        {
            selected = null;
            bool canPlaceShopItem = false;
            foreach (SimulatedCandidate candidate in candidates)
            {
                if (candidate.Operation.Kind == BackpackOperationKind.AddShopItem)
                {
                    canPlaceShopItem = true;
                    break;
                }
            }

            List<SimulatedCandidate> eligible = new();
            foreach (SimulatedCandidate candidate in candidates)
            {
                bool isPlacement = candidate.Operation.Kind == BackpackOperationKind.AddShopItem;
                bool isMerge = candidate.Operation.Kind == BackpackOperationKind.MergeShopItem;
                if ((canPlaceShopItem && (isPlacement || isMerge)) ||
                    (!canPlaceShopItem && isMerge))
                {
                    eligible.Add(candidate);
                }
            }
            if (eligible.Count == 0) return false;

            float bestGain = float.NegativeInfinity;
            List<BackpackOperation> ties = new();
            foreach (SimulatedCandidate candidate in eligible)
            {
                float gain = candidate.Score - currentScore;
                if (gain > bestGain + .001f)
                {
                    bestGain = gain;
                    ties.Clear();
                    ties.Add(candidate.Operation);
                }
                else if (Mathf.Abs(gain - bestGain) <= .001f)
                {
                    ties.Add(candidate.Operation);
                }
            }
            selected = ties[UnityEngine.Random.Range(0, ties.Count)];
            return true;
        }

        private readonly struct SimulatedCandidate
        {
            public SimulatedCandidate(BackpackOperation operation, BackpackController backpack, float score)
            {
                Operation = operation;
                Backpack = backpack;
                Score = score;
            }
            public BackpackOperation Operation { get; }
            public BackpackController Backpack { get; }
            public float Score { get; }
        }
    }

    /// <summary>统一输出真实背包操作的运行时调试记录。</summary>
    public static class BackpackOperationDebugLogger
    {
        public static string Describe(BackpackOperation operation) =>
            operation?.Kind switch
            {
                BackpackOperationKind.RollShop => "刷新商店",
                BackpackOperationKind.MoveItem => "移动物品",
                BackpackOperationKind.AddShopItem => "从商店放置",
                BackpackOperationKind.RemoveItem => "移除物品",
                BackpackOperationKind.MergeItems => "合成物品",
                BackpackOperationKind.MergeShopItem => "商店物品合成升级",
                BackpackOperationKind.ReplaceItem => "替换物品",
                _ => "未知操作",
            };

        public static void Log(BattleFaction faction, BackpackOperation operation,
            float scoreBefore, BackpackController backpack,
            IReadOnlyList<ItemData> shopItems, int remainingRolls,
            UnityEngine.Object context)
        {
            Log(faction, Describe(operation), scoreBefore, backpack, shopItems,
                remainingRolls, context);
        }

        public static void Log(BattleFaction faction, string action,
            float scoreBefore, BackpackController backpack,
            IReadOnlyList<ItemData> shopItems, int remainingRolls,
            UnityEngine.Object context)
        {
            float scoreAfter = BackpackStrengthCalculator.Calculate(backpack)
                .TotalScore;
            List<string> names = new();
            if (shopItems != null)
            {
                foreach (ItemData item in shopItems)
                {
                    if (item != null) names.Add(item.ItemName);
                }
            }

            Debug.Log($"[BackpackOperation] {faction} | {action} | " +
                $"分数 {scoreBefore:0.##} → {scoreAfter:0.##} " +
                $"({scoreAfter - scoreBefore:+0.##;-0.##;0}) | " +
                $"商店 [{string.Join(", ", names)}] | Roll {remainingRolls}",
                context);
        }
    }
}
