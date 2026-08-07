using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    public sealed class BackpackController
    {
        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
        };

        private readonly ItemInstance[,] occupied;
        private readonly List<ItemInstance> items = new();

        public BackpackController(int width = 4, int height = 4)
        {
            Width = width;
            Height = height;
            occupied = new ItemInstance[width, height];
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<ItemInstance> Items => items;

        public event Action<ItemInstance> ItemAdded;
        public event Action<ItemInstance> ItemMoved;
        public event Action<ItemInstance> ItemRemoved;
        public event Action<ItemInstance> ItemLevelChanged;
        public event Action Cleared;

        public bool Contains(ItemInstance item)
        {
            return item != null && items.Contains(item);
        }

        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }

        public ItemInstance GetItemAt(Vector2Int cell)
        {
            return IsInside(cell) ? occupied[cell.x, cell.y] : null;
        }

        public List<Vector2Int> GetOccupiedCells(ItemInstance item, Vector2Int anchorCell)
        {
            var cells = new List<Vector2Int>();

            foreach (var offset in item.Data.ShapeOffsets)
            {
                cells.Add(anchorCell + offset);
            }

            return cells;
        }

        public bool CanPlace(ItemInstance item, Vector2Int anchorCell, ItemInstance ignoreItem = null)
        {
            foreach (var cell in GetOccupiedCells(item, anchorCell))
            {
                if (!IsInside(cell))
                {
                    return false;
                }

                var occupyingItem = occupied[cell.x, cell.y];
                if (occupyingItem != null && occupyingItem != ignoreItem)
                {
                    return false;
                }
            }

            return true;
        }

        public bool PlaceItem(ItemInstance item, Vector2Int anchorCell)
        {
            if (!CanPlace(item, anchorCell))
            {
                return false;
            }

            item.AnchorCell = anchorCell;
            if (!items.Contains(item))
            {
                items.Add(item);
            }

            FillCells(item, anchorCell);
            ItemAdded?.Invoke(item);
            return true;
        }

        public bool CanMerge(ItemInstance source, ItemInstance target)
        {
            return source != null &&
                target != null &&
                source != target &&
                source.Data != null &&
                source.Data == target.Data &&
                source.Level == ItemInstance.DefaultLevel &&
                target.Level == ItemInstance.DefaultLevel &&
                items.Contains(target);
        }

        public bool TryGetMergeTarget(
            ItemInstance source,
            Vector2Int anchorCell,
            out ItemInstance target)
        {
            target = null;

            if (source?.Data == null)
            {
                return false;
            }

            foreach (Vector2Int cell in GetOccupiedCells(source, anchorCell))
            {
                if (!IsInside(cell))
                {
                    return false;
                }

                ItemInstance occupyingItem = GetItemAt(cell);
                if (occupyingItem == null)
                {
                    return false;
                }

                if (target == null)
                {
                    target = occupyingItem;
                }
                else if (target != occupyingItem)
                {
                    target = null;
                    return false;
                }
            }

            return CanMerge(source, target);
        }

        public bool CanMergeAt(
            ItemInstance source,
            Vector2Int anchorCell)
        {
            return TryGetMergeTarget(source, anchorCell, out _);
        }

        public bool TryMerge(
            ItemInstance source,
            ItemInstance target)
        {
            if (!CanMerge(source, target) || !target.TryUpgrade())
            {
                return false;
            }

            if (items.Contains(source))
            {
                RemoveItem(source);
            }
            else
            {
                source.ResetCooldown();
            }

            ItemLevelChanged?.Invoke(target);
            return true;
        }

        public bool MoveItem(ItemInstance item, Vector2Int anchorCell)
        {
            if (!items.Contains(item) || !CanPlace(item, anchorCell, item))
            {
                return false;
            }

            ClearCells(item);
            item.AnchorCell = anchorCell;
            FillCells(item, anchorCell);
            ItemMoved?.Invoke(item);
            return true;
        }

        public bool RemoveItem(ItemInstance item)
        {
            if (!items.Remove(item))
            {
                return false;
            }

            ClearCells(item);
            item.ResetCooldown();
            ItemRemoved?.Invoke(item);
            return true;
        }

        public void Clear()
        {
            if (items.Count == 0)
            {
                return;
            }

            ItemInstance[] removedItems =
                items.ToArray();

            items.Clear();
            Array.Clear(
                occupied,
                0,
                occupied.Length);

            foreach (ItemInstance item in removedItems)
            {
                item?.ResetCooldown();
                ItemRemoved?.Invoke(item);
            }

            Cleared?.Invoke();
        }

        public List<ItemInstance> GetAdjacentItems(
            ItemInstance item)
        {
            var adjacentItems =
                new List<ItemInstance>();

            if (item == null ||
                item.Data == null ||
                !items.Contains(item))
            {
                return adjacentItems;
            }

            var uniqueItems =
                new HashSet<ItemInstance>();

            foreach (Vector2Int occupiedCell
                     in GetOccupiedCells(
                         item,
                         item.AnchorCell))
            {
                foreach (Vector2Int direction
                         in CardinalDirections)
                {
                    ItemInstance adjacentItem =
                        GetItemAt(
                            occupiedCell + direction);

                    if (adjacentItem == null ||
                        adjacentItem == item ||
                        !uniqueItems.Add(adjacentItem))
                    {
                        continue;
                    }

                    adjacentItems.Add(adjacentItem);
                }
            }

            return adjacentItems;
        }

        public List<ItemInstance> GetAdjacentEquipmentItems(
            ItemInstance aircraft)
        {
            var equipmentItems =
                new List<ItemInstance>();

            if (aircraft == null ||
                aircraft.Data == null ||
                aircraft.Data.ItemType != ItemType.Aircraft)
            {
                return equipmentItems;
            }

            foreach (ItemInstance adjacentItem
                     in GetAdjacentItems(aircraft))
            {
                if (adjacentItem.Data != null &&
                    adjacentItem.Data.ItemType ==
                    ItemType.Equipment)
                {
                    equipmentItems.Add(adjacentItem);
                }
            }

            return equipmentItems;
        }

        public List<ItemInstance> GetAdjacentAircraftItems(
            ItemInstance equipment)
        {
            var aircraftItems = new List<ItemInstance>();

            if (equipment == null ||
                equipment.Data == null ||
                equipment.Data.ItemType != ItemType.Equipment)
            {
                return aircraftItems;
            }

            foreach (ItemInstance adjacentItem in GetAdjacentItems(equipment))
            {
                if (adjacentItem.Data != null &&
                    adjacentItem.Data.ItemType == ItemType.Aircraft)
                {
                    aircraftItems.Add(adjacentItem);
                }
            }

            return aircraftItems;
        }

        private void FillCells(ItemInstance item, Vector2Int anchorCell)
        {
            foreach (var cell in GetOccupiedCells(item, anchorCell))
            {
                occupied[cell.x, cell.y] = item;
            }
        }

        private void ClearCells(ItemInstance item)
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (occupied[x, y] == item)
                    {
                        occupied[x, y] = null;
                    }
                }
            }
        }
    }
}
