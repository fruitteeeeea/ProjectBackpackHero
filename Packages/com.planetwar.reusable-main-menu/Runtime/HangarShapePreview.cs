using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>Renders a backpack item's occupied cells in the detail panel.</summary>
    public sealed class HangarShapePreview : MonoBehaviour
    {
        private const float DefaultCellSize = 100f;
        private const float CellSpacing = 6f;
        private const float SafePadding = 20f;
        // block.png was authored for the lighter backpack grid. Tint it here so occupied
        // cells remain legible over the spell detail panel's dark SkillBg.
        private static readonly Color DetailCellColor = new(.42f, .88f, 1f, 1f);

        [SerializeField] private Sprite cellSprite;
        [SerializeField] private RectTransform previewContainer;

        public int CellCount
        {
            get
            {
                int count = 0;
                foreach (Transform child in transform)
                    if (child.gameObject.activeSelf) count++;
                return count;
            }
        }

        public void Configure(Sprite sprite, RectTransform container = null)
        {
            cellSprite = sprite;
            previewContainer = container != null ? container : transform as RectTransform;
        }

        public void Show(IReadOnlyList<Vector2Int> shapeOffsets)
        {
            ClearCells();
            if (shapeOffsets == null || shapeOffsets.Count == 0 || cellSprite == null)
            {
                gameObject.SetActive(false);
                return;
            }

            var uniqueOffsets = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            foreach (Vector2Int offset in shapeOffsets)
                if (seen.Add(offset)) uniqueOffsets.Add(offset);
            if (uniqueOffsets.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            Vector2Int min = uniqueOffsets[0];
            Vector2Int max = uniqueOffsets[0];
            foreach (Vector2Int offset in uniqueOffsets)
            {
                min = Vector2Int.Min(min, offset);
                max = Vector2Int.Max(max, offset);
            }

            int columns = max.x - min.x + 1;
            int rows = max.y - min.y + 1;
            float cellSize = CalculateCellSize(columns, rows);
            float step = cellSize + CellSpacing;
            float centerX = (min.x + max.x) * .5f;
            float centerY = (min.y + max.y) * .5f;

            foreach (Vector2Int offset in uniqueOffsets)
            {
                GameObject cell = GetOrCreateCell();
                cell.SetActive(true);
                var rect = cell.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(.5f, .5f);
                // ItemShapeData uses y=0 as its top row. UGUI grows upward, hence the inversion.
                rect.anchoredPosition = new Vector2(
                    (offset.x - centerX) * step,
                    -(offset.y - centerY) * step);
                rect.sizeDelta = new Vector2(cellSize, cellSize);
                var image = cell.GetComponent<Image>();
                image.sprite = cellSprite;
                image.color = DetailCellColor;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }

        private float CalculateCellSize(int columns, int rows)
        {
            RectTransform container = previewContainer != null ? previewContainer : transform as RectTransform;
            Vector2 available = container != null ? container.rect.size : Vector2.zero;
            if (available.x <= 0f || available.y <= 0f) return DefaultCellSize;
            float widthLimit = (available.x - SafePadding * 2f - CellSpacing * (columns - 1)) / columns;
            float heightLimit = (available.y - SafePadding * 2f - CellSpacing * (rows - 1)) / rows;
            return Mathf.Max(1f, Mathf.Min(DefaultCellSize, widthLimit, heightLimit));
        }

        private void ClearCells()
        {
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                GameObject cell = transform.GetChild(index).gameObject;
                // Delayed Destroy leaves the old cells visible until the end of the frame when
                // a player switches cards quickly. Pool them in play mode instead.
                if (Application.isPlaying) cell.SetActive(false);
                else DestroyImmediate(cell);
            }
        }

        private GameObject GetOrCreateCell()
        {
            foreach (Transform child in transform)
                if (!child.gameObject.activeSelf) return child.gameObject;
            var cell = new GameObject("ShapeCell", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            cell.layer = gameObject.layer;
            cell.transform.SetParent(transform, false);
            return cell;
        }
    }
}
