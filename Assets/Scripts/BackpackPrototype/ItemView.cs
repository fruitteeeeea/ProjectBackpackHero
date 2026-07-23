using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackPrototype
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Text label;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 originalAnchoredPosition;
        private Tween scaleTween;
        private Tween feedbackTween;
        private bool isDragging;

        public ItemInstance Instance { get; private set; }
        public BackpackController Backpack { get; private set; }
        public BackpackGridView GridView { get; private set; }
        public RectTransform BackpackItemLayer { get; private set; }
        public RectTransform DragLayer { get; private set; }
        public RectTransform TrashZone { get; private set; }
        public Vector2 DragVisualOffset { get; private set; }
        public Vector2Int GrabCellOffset { get; private set; }
        public Vector2Int? CandidateAnchorCell { get; private set; }
        public bool IsPlacedInBackpack { get; private set; }
        
        public string DisplayName =>
            Instance != null && Instance.Data != null
                ? Instance.Data.ItemName
                : name;

        public string LocationName =>
            IsPlacedInBackpack
                ? "Backpack"
                : "Shop";
        
        public bool TryGetBackpackAnchor(out Vector2Int anchorCell)
        {
            if (IsPlacedInBackpack && Instance != null)
            {
                anchorCell = Instance.AnchorCell;
                return true;
            }

            anchorCell = default;
            return false;
        }
        
        public event Action<ItemView> PlacedSuccessfully;
        public event Action<ItemView> DeletedSuccessfully;
        public event Action<ItemView> SelectionRequested;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvasGroup = GetComponent<CanvasGroup>();

            if (background == null)
            {
                background = GetComponent<Image>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<Text>();
            }
        }

        public void Bind(
            ItemInstance instance,
            BackpackController backpack,
            BackpackGridView gridView,
            RectTransform backpackItemLayer,
            RectTransform dragLayer,
            RectTransform trashZone,
            Vector2 cellSize,
            Vector2 spacing)
        {
            Instance = instance;
            Backpack = backpack;
            GridView = gridView;
            BackpackItemLayer = backpackItemLayer;
            DragLayer = dragLayer;
            TrashZone = trashZone;

            ResizeToShape(cellSize, spacing);

            if (background != null &&
                instance != null &&
                instance.Data != null)
            {
                background.sprite = instance.Data.Icon;
                background.color = Color.white;
                background.type = Image.Type.Simple;
                background.preserveAspect = false;
            }

            if (label != null)
            {
                label.text = instance.Data.ItemName;
            }
        }

        public void SetBackpackPosition(Vector2Int anchorCell)
        {
            if (GridView == null)
            {
                return;
            }

            Instance.AnchorCell = anchorCell;
            IsPlacedInBackpack = true;
            rectTransform.SetParent(BackpackItemLayer, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = GridView.GetItemAnchoredPosition(anchorCell);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            RequestSelection();
            isDragging = true;
            originalParent = rectTransform.parent;
            originalSiblingIndex = rectTransform.GetSiblingIndex();
            originalAnchoredPosition = rectTransform.anchoredPosition;
            CandidateAnchorCell = null;
            GridView?.ClearPlacementPreview();

            var itemScreenPosition = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, rectTransform.position);
            DragVisualOffset = eventData.position - itemScreenPosition;
            GrabCellOffset = CalculateGrabCellOffset(eventData.position, eventData.pressEventCamera);

            if (DragLayer != null)
            {
                rectTransform.SetParent(DragLayer, true);
                rectTransform.SetAsLastSibling();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.86f;
            TweenScale(1.05f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var targetScreenPosition = eventData.position - DragVisualOffset;

            if (rectTransform.parent is RectTransform parentRect &&
                RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, targetScreenPosition, eventData.pressEventCamera, out var worldPoint))
            {
                rectTransform.position = worldPoint;
            }

            if (GridView != null && GridView.TryGetCellAtScreenPosition(eventData.position, eventData.pressEventCamera, out var pointerCell))
            {
                CandidateAnchorCell = BackpackGridView.CalculateAnchorCell(pointerCell, GrabCellOffset);
                GridView.ShowPlacementPreview(Backpack, Instance, CandidateAnchorCell.Value, IsPlacedInBackpack ? Instance : null);
            }
            else
            {
                CandidateAnchorCell = null;
                GridView?.ClearPlacementPreview();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            GridView?.ClearPlacementPreview();

            if (IsOverTrash(eventData.position, eventData.pressEventCamera))
            {
                DeleteItem();
                return;
            }

            if (CandidateAnchorCell.HasValue && TryPlaceAt(CandidateAnchorCell.Value))
            {
                return;
            }

            if (originalParent != null)
            {
                rectTransform.SetParent(originalParent, true);
                rectTransform.SetSiblingIndex(originalSiblingIndex);
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }

            CandidateAnchorCell = null;
            PlayFailedFeedback();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isDragging)
            {
                TweenScale(1.08f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isDragging)
            {
                TweenScale(1f);
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            RequestSelection();
        }
        
        private void RequestSelection()
        {
            SelectionRequested?.Invoke(this);
        }

        private bool TryPlaceAt(Vector2Int anchorCell)
        {
            if (Backpack == null)
            {
                return false;
            }

            var moved = IsPlacedInBackpack
                ? Backpack.MoveItem(Instance, anchorCell)
                : Backpack.PlaceItem(Instance, anchorCell);

            if (!moved)
            {
                return false;
            }

            SetBackpackPosition(anchorCell);
            CandidateAnchorCell = null;
            PlayPlacedFeedback();
            PlacedSuccessfully?.Invoke(this);
            return true;
        }

        private bool IsOverTrash(Vector2 screenPosition, Camera eventCamera)
        {
            return TrashZone != null &&
                RectTransformUtility.RectangleContainsScreenPoint(TrashZone, screenPosition, eventCamera);
        }

        private void DeleteItem()
        {
            if (IsPlacedInBackpack && Backpack != null)
            {
                Backpack.RemoveItem(Instance);
            }

            DeletedSuccessfully?.Invoke(this);
            Destroy(gameObject);
        }

        private void PlayPlacedFeedback()
        {
            feedbackTween?.Kill();
            rectTransform.localScale = Vector3.one;
            feedbackTween = rectTransform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.22f, 7, 0.65f);
        }

        private void PlayFailedFeedback()
        {
            feedbackTween?.Kill();
            rectTransform.localScale = Vector3.one;
            feedbackTween = rectTransform.DOShakePosition(0.18f, new Vector3(12f, 0f, 0f), 12, 90f, false, true);
            TweenScale(1f);
        }

        private void TweenScale(float targetScale)
        {
            scaleTween?.Kill();
            scaleTween = rectTransform.DOScale(targetScale, 0.12f).SetEase(Ease.OutQuad);
        }

        private void OnDestroy()
        {
            scaleTween?.Kill();
            feedbackTween?.Kill();
        }

        private void ResizeToShape(Vector2 cellSize, Vector2 spacing)
        {
            var bounds = GetShapeBounds(Instance.Data.ShapeOffsets);
            var width = (bounds.x + 1) * cellSize.x + bounds.x * spacing.x;
            var height = (bounds.y + 1) * cellSize.y + bounds.y * spacing.y;
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        private Vector2Int CalculateGrabCellOffset(Vector2 screenPosition, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, eventCamera, out var localPoint))
            {
                return Vector2Int.zero;
            }

            var rect = rectTransform.rect;
            var fromTopLeft = new Vector2(localPoint.x - rect.xMin, rect.yMax - localPoint.y);
            var grid = GridView != null ? GridView : null;
            var cellSize = grid != null ? grid.CellSize : new Vector2(80f, 80f);
            var spacing = grid != null ? grid.Spacing : new Vector2(8f, 8f);
            var x = Mathf.FloorToInt(fromTopLeft.x / (cellSize.x + spacing.x));
            var y = Mathf.FloorToInt(fromTopLeft.y / (cellSize.y + spacing.y));
            var candidate = new Vector2Int(Mathf.Max(0, x), Mathf.Max(0, y));

            foreach (var offset in Instance.Data.ShapeOffsets)
            {
                if (offset == candidate)
                {
                    return candidate;
                }
            }

            return FindNearestShapeOffset(candidate, Instance.Data.ShapeOffsets);
        }

        private static Vector2Int GetShapeBounds(IReadOnlyList<Vector2Int> offsets)
        {
            var max = Vector2Int.zero;

            foreach (var offset in offsets)
            {
                max.x = Mathf.Max(max.x, offset.x);
                max.y = Mathf.Max(max.y, offset.y);
            }

            return max;
        }

        private static Vector2Int FindNearestShapeOffset(Vector2Int candidate, IReadOnlyList<Vector2Int> offsets)
        {
            var nearest = offsets.Count > 0 ? offsets[0] : Vector2Int.zero;
            var nearestDistance = int.MaxValue;

            foreach (var offset in offsets)
            {
                var distance = Mathf.Abs(offset.x - candidate.x) + Mathf.Abs(offset.y - candidate.y);
                if (distance < nearestDistance)
                {
                    nearest = offset;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}
