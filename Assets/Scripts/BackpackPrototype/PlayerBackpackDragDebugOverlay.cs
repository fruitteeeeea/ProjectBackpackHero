using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    /// <summary>
    /// Runtime-only marker layer for inspecting player backpack drag placement.
    /// It intentionally renders independently from slot backgrounds so the
    /// normal placement preview remains unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerBackpackDragDebugOverlay : MonoBehaviour
    {
        private static readonly Color EmptyCellColor =
            new(0.45f, 0.48f, 0.53f, 0.95f);

        private static readonly Color OccupiedCellColor =
            new(1f, 0.57f, 0.08f, 1f);

        private static readonly Color LegalPreviewColor =
            new(0.74f, 0.25f, 1f, 1f);

        private static readonly Color IllegalPreviewColor =
            new(1f, 0.08f, 0.12f, 1f);

        private static readonly Color DraggedCellColor =
            new(0.12f, 0.63f, 1f, 1f);

        private static readonly Color GrabAnchorColor =
            new(0.37f, 1f, 0.13f, 1f);

        private const float GridMarkerSize = 11f;
        private const float DragMarkerSize = 13f;

        private readonly List<Image> markerPool = new();

        private BackpackGridView gridView;
        private RectTransform overlayRect;
        private Sprite dotSprite;
        private int activeMarkerCount;

        public static PlayerBackpackDragDebugOverlay Create(
            BackpackGridView gridView)
        {
            if (gridView == null)
            {
                return null;
            }

            PlayerBackpackDragDebugOverlay existing =
                gridView.GetComponentInChildren<
                    PlayerBackpackDragDebugOverlay>(true);
            if (existing != null)
            {
                existing.Initialize(gridView);
                return existing;
            }

            Canvas canvas = gridView.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            var overlayObject = new GameObject(
                "Player Backpack Drag Debug Overlay",
                typeof(RectTransform),
                typeof(PlayerBackpackDragDebugOverlay));
            overlayObject.transform.SetParent(canvas.transform, false);
            overlayObject.transform.SetAsLastSibling();

            var overlay = overlayObject.GetComponent<
                PlayerBackpackDragDebugOverlay>();
            overlay.Initialize(gridView);
            return overlay;
        }

        public void Refresh(
            BackpackController backpack,
            ItemView draggingItem,
            bool enabled)
        {
            ClearMarkers();

            if (!enabled ||
                backpack == null ||
                draggingItem == null ||
                !draggingItem.IsDragging ||
                gridView == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            DrawBackpackOccupancy(backpack);
            DrawPlacementPreview(backpack, draggingItem);
            DrawDraggedItem(draggingItem);
        }

        public void Clear()
        {
            ClearMarkers();
            gameObject.SetActive(false);
        }

        private void Initialize(BackpackGridView value)
        {
            gridView = value;
            overlayRect = (RectTransform)transform;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.SetAsLastSibling();
            gameObject.SetActive(false);
        }

        private void DrawBackpackOccupancy(BackpackController backpack)
        {
            for (int y = 0; y < backpack.Height; y++)
            {
                for (int x = 0; x < backpack.Width; x++)
                {
                    Vector2Int cell = new(x, y);
                    DrawMarker(
                        gridView.GetCellCenterWorldPosition(cell),
                        backpack.GetItemAt(cell) == null
                            ? EmptyCellColor
                            : OccupiedCellColor,
                        GridMarkerSize);
                }
            }
        }

        private void DrawPlacementPreview(
            BackpackController backpack,
            ItemView draggingItem)
        {
            if (!draggingItem.CandidateAnchorCell.HasValue ||
                draggingItem.Instance == null)
            {
                return;
            }

            PlacementPreview preview =
                BackpackGridView.BuildPlacementPreview(
                    backpack,
                    draggingItem.Instance,
                    draggingItem.CandidateAnchorCell.Value,
                    draggingItem.IsPlacedInBackpack
                        ? draggingItem.Instance
                        : null);

            Color previewColor = preview.IsLegal
                ? LegalPreviewColor
                : IllegalPreviewColor;
            foreach (Vector2Int cell in preview.VisibleCells)
            {
                DrawMarker(
                    gridView.GetCellCenterWorldPosition(cell),
                    previewColor,
                    GridMarkerSize + 4f);
            }
        }

        private void DrawDraggedItem(ItemView draggingItem)
        {
            if (draggingItem.Instance?.Data == null)
            {
                return;
            }

            foreach (Vector2Int offset in
                     draggingItem.Instance.Data.ShapeOffsets)
            {
                DrawMarker(
                    draggingItem.GetShapeCellCenterWorldPosition(offset),
                    offset == draggingItem.GrabCellOffset
                        ? GrabAnchorColor
                        : DraggedCellColor,
                    DragMarkerSize);
            }
        }

        private void DrawMarker(
            Vector3 worldPosition,
            Color color,
            float size)
        {
            Image marker = GetMarker(activeMarkerCount++);
            RectTransform markerRect = marker.rectTransform;
            markerRect.localPosition =
                overlayRect.InverseTransformPoint(worldPosition);
            markerRect.sizeDelta = new Vector2(size, size);
            marker.color = color;
            marker.gameObject.SetActive(true);
        }

        private Image GetMarker(int index)
        {
            while (markerPool.Count <= index)
            {
                var markerObject = new GameObject(
                    "Debug Dot",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                markerObject.transform.SetParent(transform, false);

                Image marker = markerObject.GetComponent<Image>();
                marker.sprite = GetDotSprite();
                marker.raycastTarget = false;
                marker.rectTransform.anchorMin = new Vector2(.5f, .5f);
                marker.rectTransform.anchorMax = new Vector2(.5f, .5f);
                marker.rectTransform.pivot = new Vector2(.5f, .5f);
                markerPool.Add(marker);
            }

            return markerPool[index];
        }

        private void ClearMarkers()
        {
            activeMarkerCount = 0;
            foreach (Image marker in markerPool)
            {
                if (marker != null)
                {
                    marker.gameObject.SetActive(false);
                }
            }
        }

        private Sprite GetDotSprite()
        {
            if (dotSprite != null)
            {
                return dotSprite;
            }

            const int size = 32;
            const float radius = 15f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Player Backpack Debug Dot",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(
                        new Vector2(x, y),
                        new Vector2(15.5f, 15.5f));
                    float alpha = Mathf.Clamp01(radius - distance + 1f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            dotSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(.5f, .5f),
                size);
            dotSprite.name = texture.name;
            dotSprite.hideFlags = HideFlags.HideAndDontSave;
            return dotSprite;
        }

        private void OnDestroy()
        {
            if (dotSprite != null)
            {
                Destroy(dotSprite.texture);
                Destroy(dotSprite);
            }
        }
    }
}
