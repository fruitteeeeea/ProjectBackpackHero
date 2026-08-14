using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    public readonly struct PlacementPreview
    {
        public PlacementPreview(bool isLegal, IReadOnlyList<Vector2Int> visibleCells)
        {
            IsLegal = isLegal;
            VisibleCells = visibleCells;
        }

        public bool IsLegal { get; }
        public IReadOnlyList<Vector2Int> VisibleCells { get; }
    }

    public sealed class BackpackGridView : MonoBehaviour
    {
        [SerializeField] private RectTransform gridRect;
        [SerializeField] private Vector2 cellSize = new(80f, 80f);
        [SerializeField] private Vector2 spacing = new(8f, 8f);

        [SerializeField, Min(0f)]
        private float dropHitPadding = 32f;

        [SerializeField, Min(1)]
        private int columns = 7;

        [SerializeField, Min(1)]
        private int rows = 4;

        [SerializeField]
        private List<BackpackSlotView> slots = new();

        public RectTransform GridRect => gridRect;
        public Vector2 CellSize => cellSize;
        public Vector2 Spacing => spacing;
        public int Columns => columns;
        public int Rows => rows;
        public IReadOnlyList<BackpackSlotView> Slots => slots;

        private void Awake()
        {
            if (gridRect == null)
            {
                gridRect = (RectTransform)transform;
            }
        }

        public void Initialize(RectTransform rect, Vector2 slotSize, Vector2 slotSpacing, IReadOnlyList<BackpackSlotView> slotViews)
        {
            gridRect = rect;
            cellSize = slotSize;
            spacing = slotSpacing;
            slots = new List<BackpackSlotView>(slotViews);
        }

        public bool TryGetCellAtScreenPosition(Vector2 screenPosition, Camera eventCamera, out Vector2Int cell)
        {
            cell = default;

            if (gridRect == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRect, screenPosition, eventCamera, out var localPoint))
            {
                return false;
            }

            var rect = gridRect.rect;
            float clampedLocalX = Mathf.Clamp(
                localPoint.x,
                rect.xMin,
                rect.xMax);
            float clampedLocalY = Mathf.Clamp(
                localPoint.y,
                rect.yMin,
                rect.yMax);
            bool isInsideExpandedBounds =
                localPoint.x >= rect.xMin - dropHitPadding &&
                localPoint.x <= rect.xMax + dropHitPadding &&
                localPoint.y >= rect.yMin - dropHitPadding &&
                localPoint.y <= rect.yMax + dropHitPadding;
            if (!isInsideExpandedBounds)
            {
                return false;
            }

            var fromTopLeft = new Vector2(
                clampedLocalX - rect.xMin,
                rect.yMax - clampedLocalY);
            var stepX = cellSize.x + spacing.x;
            var stepY = cellSize.y + spacing.y;

            if (stepX <= 0f ||
                stepY <= 0f)
            {
                return false;
            }

            var x =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        (fromTopLeft.x + spacing.x * 0.5f) /
                        stepX),
                    0,
                    columns - 1);
            var y =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        (fromTopLeft.y + spacing.y * 0.5f) /
                        stepY),
                    0,
                    rows - 1);

            cell = new Vector2Int(x, y);
            return true;
        }

        public void ShowPlacementPreview(
            BackpackController backpack,
            ItemInstance item,
            Vector2Int anchorCell,
            ItemInstance ignoreItem = null)
        {
            ClearPlacementPreview();

            var preview = BuildPlacementPreview(backpack, item, anchorCell, ignoreItem);
            foreach (var visibleCell in preview.VisibleCells)
            {
                var slot = GetSlot(visibleCell);
                if (slot != null)
                {
                    slot.SetPreview(preview.IsLegal);
                }
            }
        }

        public void ClearPlacementPreview()
        {
            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    slot.ClearPreview();
                }
            }
        }

        public Vector2 GetItemAnchoredPosition(Vector2Int anchorCell)
        {
            return new Vector2(
                anchorCell.x * (cellSize.x + spacing.x),
                -anchorCell.y * (cellSize.y + spacing.y));
        }

        /// <summary>Returns the world-space center of a logical backpack cell.</summary>
        public Vector3 GetCellCenterWorldPosition(Vector2Int cell)
        {
            if (gridRect == null)
            {
                return transform.position;
            }

            Rect rect = gridRect.rect;
            Vector2 pitch = cellSize + spacing;
            Vector3 localCellCenter = new(
                rect.xMin + cell.x * pitch.x + cellSize.x * .5f,
                rect.yMax - cell.y * pitch.y - cellSize.y * .5f);

            return gridRect.TransformPoint(localCellCenter);
        }

        public static Vector2Int CalculateAnchorCell(Vector2Int pointerCell, Vector2Int grabCellOffset)
        {
            return pointerCell - grabCellOffset;
        }

        public static PlacementPreview BuildPlacementPreview(
            BackpackController backpack,
            ItemInstance item,
            Vector2Int anchorCell,
            ItemInstance ignoreItem = null)
        {
            var visibleCells = new List<Vector2Int>();
            var isLegal = backpack.CanPlace(item, anchorCell, ignoreItem) ||
                backpack.CanMergeAt(item, anchorCell);

            foreach (var cell in backpack.GetOccupiedCells(item, anchorCell))
            {
                if (backpack.IsInside(cell))
                {
                    visibleCells.Add(cell);
                }
            }

            return new PlacementPreview(isLegal, visibleCells);
        }

        private BackpackSlotView GetSlot(Vector2Int cell)
        {
            foreach (var slot in slots)
            {
                if (slot != null && slot.Cell == cell)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
