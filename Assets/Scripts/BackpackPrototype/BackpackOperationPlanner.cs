using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>玩家与敌人共用的准备阶段背包操作评分器。</summary>
    public enum BackpackOperationKind { RollShop, MoveItem, AddShopItem, RemoveItem, MergeItems, ReplaceItem }

    public sealed class BackpackOperation
    {
        public BackpackOperationKind Kind;
        public ItemInstance Item;
        public ItemInstance SecondaryItem;
        public ItemData ShopItem;
        public Vector2Int Destination;
    }

    public static class BackpackOperationPlanner
    {
        public static bool TrySelectBest(BackpackController backpack,
            IReadOnlyList<ItemData> shopItems, int remainingRolls,
            out BackpackOperation selected)
        {
            selected = null;
            if (backpack == null) return false;
            float current = BackpackStrengthCalculator.Calculate(backpack).TotalScore;
            float bestGain = float.NegativeInfinity;
            List<BackpackOperation> ties = new();
            foreach (BackpackOperation candidate in BuildCandidates(backpack, shopItems, remainingRolls))
            {
                if (!TrySimulate(backpack, candidate, out float score)) continue;
                float gain = score - current;
                if (gain > bestGain + .001f) { bestGain = gain; ties.Clear(); ties.Add(candidate); }
                else if (Mathf.Abs(gain - bestGain) <= .001f) ties.Add(candidate);
            }
            if (ties.Count == 0 || bestGain < 0f) return false;
            selected = ties[UnityEngine.Random.Range(0, ties.Count)];
            return true;
        }

        private static IEnumerable<BackpackOperation> BuildCandidates(BackpackController backpack, IReadOnlyList<ItemData> shop, int rolls)
        {
            if (shop == null || shop.Count == 0) { if (rolls > 0) yield return new BackpackOperation { Kind = BackpackOperationKind.RollShop }; yield break; }
            foreach (ItemInstance item in backpack.Items)
            {
                foreach (Vector2Int cell in EnumerateCells(backpack))
                    if (cell != item.AnchorCell && backpack.CanPlace(item, cell, item))
                        yield return new BackpackOperation { Kind = BackpackOperationKind.MoveItem, Item = item, Destination = cell };
                foreach (ItemInstance target in backpack.Items)
                    if (backpack.CanMerge(item, target)) yield return new BackpackOperation { Kind = BackpackOperationKind.MergeItems, Item = item, SecondaryItem = target };
            }
            foreach (ItemData data in shop)
            {
                if (data == null) continue;
                ItemInstance candidate = new("planner", data, Vector2Int.zero);
                foreach (Vector2Int cell in EnumerateCells(backpack))
                    if (backpack.CanPlace(candidate, cell))
                        yield return new BackpackOperation { Kind = BackpackOperationKind.AddShopItem, ShopItem = data, Destination = cell };
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

        private static bool TrySimulate(BackpackController source, BackpackOperation operation, out float score)
        {
            score = 0f; BackpackController copy = new(source.Width, source.Height); Dictionary<ItemInstance, ItemInstance> map = new(); int id = 0;
            foreach (ItemInstance item in source.Items) { ItemInstance clone = new("planner-" + ++id, item.Data, item.AnchorCell, item.Level); if (!copy.PlaceItem(clone, clone.AnchorCell)) return false; map[item] = clone; }
            bool ok = operation.Kind switch
            {
                BackpackOperationKind.RollShop => true,
                BackpackOperationKind.MoveItem => map.TryGetValue(operation.Item, out ItemInstance move) && copy.MoveItem(move, operation.Destination),
                BackpackOperationKind.AddShopItem => copy.PlaceItem(new ItemInstance("planner-add", operation.ShopItem, operation.Destination), operation.Destination),
                BackpackOperationKind.RemoveItem => map.TryGetValue(operation.Item, out ItemInstance remove) && copy.RemoveItem(remove),
                BackpackOperationKind.MergeItems => map.TryGetValue(operation.Item, out ItemInstance from) && map.TryGetValue(operation.SecondaryItem, out ItemInstance to) && copy.TryMerge(from, to),
                BackpackOperationKind.ReplaceItem => map.TryGetValue(operation.Item, out ItemInstance old) && copy.RemoveItem(old) && copy.PlaceItem(new ItemInstance("planner-replace", operation.ShopItem, operation.Destination), operation.Destination),
                _ => false
            };
            if (!ok) return false; score = BackpackStrengthCalculator.Calculate(copy).TotalScore; return true;
        }
    }
}
