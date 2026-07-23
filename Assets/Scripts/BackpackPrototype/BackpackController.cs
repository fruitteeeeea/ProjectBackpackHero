using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    public sealed class BackpackController
    {
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
            return true;
        }

        public bool RemoveItem(ItemInstance item)
        {
            if (!items.Remove(item))
            {
                return false;
            }

            ClearCells(item);
            return true;
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
