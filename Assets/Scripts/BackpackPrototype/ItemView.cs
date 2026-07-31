using System;
using System.Collections.Generic;
using DG.Tweening;
using BackpackHero.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackPrototype
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ICanvasRaycastFilter
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        [SerializeField] private TextMeshProUGUI levelLabel;

        private static readonly int CooldownProgressId =
            Shader.PropertyToID("_CooldownProgress");

        private static readonly int FlashAmountId =
            Shader.PropertyToID("_FlashAmount");

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 originalAnchoredPosition;
        private Tween scaleTween;
        private Tween feedbackTween;
        private Tween cooldownFlashTween;
        private Tween mergeFlashTween;
        private float cooldownFlashAmount;
        private float mergeFlashAmount;
        private bool isDragging;
        private Vector2 shapeCellSize;
        private Vector2 shapeSpacing;
        
        private Material originalBackgroundMaterial;
        private Material originalIconMaterial;
        private Material backgroundCooldownMaterial;
        private Material iconCooldownMaterial;

        public ItemInstance Instance { get; private set; }
        public BackpackController Backpack { get; private set; }
        public BackpackCombatController CombatController
        {
            get;
            private set;
        }
        public BackpackGridView GridView { get; private set; }
        public RectTransform BackpackItemLayer { get; private set; }
        public RectTransform DragLayer { get; private set; }
        public RectTransform TrashZone { get; private set; }
        public Vector2 DragVisualOffset { get; private set; }
        public Vector2Int GrabCellOffset { get; private set; }
        public Vector2Int? CandidateAnchorCell { get; private set; }
        public bool IsPlacedInBackpack { get; private set; }
        
        public bool IsCoolingDown =>
            Instance != null &&
            Instance.IsCoolingDown;

        public float CooldownProgress =>
            Instance != null
                ? Instance.CooldownProgress
                : 1f;

        public float RemainingCooldown =>
            Instance != null
                ? Instance.RemainingCooldown
                : 0f;
        
        
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
        public event Action<ItemView, ItemInstance> MergedSuccessfully;
        public event Action<ItemView, bool> DragStateChanged;

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

            if (icon == null)
            {
                Transform iconTransform =
                    transform.Find("ItemIcon");

                if (iconTransform != null)
                {
                    icon =
                        iconTransform.GetComponent<Image>();
                }
            }

            EnsureLevelLabel();
        }

        private void Update()
        {
            SetCooldownFloat(
                CooldownProgressId,
                CooldownProgress);
        }

        public void Bind(
            ItemInstance instance,
            BackpackController backpack,
            BackpackGridView gridView,
            RectTransform backpackItemLayer,
            RectTransform dragLayer,
            RectTransform trashZone,
            Vector2 cellSize,
            Vector2 spacing,
            BackpackCombatController combatController = null)
        {
            if (Backpack != null)
            {
                Backpack.ItemLevelChanged -= HandleItemLevelChanged;
            }

            if (CombatController != null)
            {
                CombatController.CooldownCompleted -=
                    HandleCooldownCompleted;
            }

            Instance = instance;
            Backpack = backpack;
            GridView = gridView;
            BackpackItemLayer = backpackItemLayer;
            DragLayer = dragLayer;
            TrashZone = trashZone;
            CombatController = combatController;

            if (Backpack != null)
            {
                Backpack.ItemLevelChanged += HandleItemLevelChanged;
            }

            if (CombatController != null)
            {
                CombatController.CooldownCompleted +=
                    HandleCooldownCompleted;
            }

            ResizeToShape(cellSize, spacing);

            if (background != null &&
                instance != null &&
                instance.Data != null)
            {
                background.color =
                    instance.Data.BackgroundColor;
                background.type = Image.Type.Simple;
                background.preserveAspect = false;
            }

            if (icon != null &&
                instance != null &&
                instance.Data != null)
            {
                icon.sprite = instance.Data.Icon;
                icon.color = Color.white;
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.enabled = instance.Data.Icon != null;
            }

            InitializeCooldownMaterials();

            if (label != null)
            {
                label.text = instance.Data.ItemName;
            }

            RefreshLevelLabel();
        }

        public void SetMergeHighlight(bool highlighted)
        {
            mergeFlashTween?.Kill();

            if (!highlighted)
            {
                mergeFlashAmount = 0f;
                ApplyFlashAmount();
                return;
            }

            mergeFlashAmount = 0.18f;
            ApplyFlashAmount();
            mergeFlashTween = DOTween.To(
                    () => mergeFlashAmount,
                    value =>
                    {
                        mergeFlashAmount = value;
                        ApplyFlashAmount();
                    },
                    0.62f,
                    0.7f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void PlayMergeFeedback()
        {
            PlayPlacedFeedback();
        }

        private void InitializeCooldownMaterials()
        {
            ReleaseCooldownMaterials();

            originalBackgroundMaterial =
                background != null
                    ? background.material
                    : null;

            originalIconMaterial =
                icon != null
                    ? icon.material
                    : null;

            backgroundCooldownMaterial =
                CreateCooldownMaterial(
                    background,
                    originalBackgroundMaterial);

            iconCooldownMaterial =
                CreateCooldownMaterial(
                    icon,
                    originalIconMaterial);

            SetCooldownFloat(
                CooldownProgressId,
                CooldownProgress);

            cooldownFlashAmount = 0f;
            mergeFlashAmount = 0f;
            ApplyFlashAmount();
        }

        private void HandleCooldownCompleted(
            ItemInstance completedItem)
        {
            if (completedItem != Instance)
            {
                return;
            }

            cooldownFlashTween?.Kill();
            cooldownFlashAmount = 0f;
            ApplyFlashAmount();

            Sequence flashSequence = DOTween.Sequence();
            flashSequence.Append(
                DOTween.To(
                        () => 0f,
                        value => SetCooldownFlashAmount(value),
                        1f,
                        0.06f)
                    .SetEase(Ease.OutQuad));
            flashSequence.Append(
                DOTween.To(
                        () => 1f,
                        value => SetCooldownFlashAmount(value),
                        0f,
                        0.16f)
                    .SetEase(Ease.InQuad));
            cooldownFlashTween = flashSequence;
        }

        private void SetCooldownFlashAmount(float value)
        {
            cooldownFlashAmount = value;
            ApplyFlashAmount();
        }

        private void ApplyFlashAmount()
        {
            SetCooldownFloat(
                FlashAmountId,
                Mathf.Max(cooldownFlashAmount, mergeFlashAmount));
        }

        private void HandleItemLevelChanged(ItemInstance item)
        {
            if (item == Instance)
            {
                RefreshLevelLabel();
            }
        }

        private Material CreateCooldownMaterial(
            Image targetImage,
            Material sourceMaterial)
        {
            if (targetImage == null ||
                sourceMaterial == null)
            {
                return null;
            }

            Material runtimeMaterial =
                new Material(sourceMaterial)
                {
                    name =
                        $"{sourceMaterial.name} " +
                        $"({name} Runtime)"
                };

            targetImage.material = runtimeMaterial;
            return runtimeMaterial;
        }

        private void SetCooldownFloat(
            int propertyId,
            float value)
        {
            if (backgroundCooldownMaterial != null)
            {
                backgroundCooldownMaterial.SetFloat(
                    propertyId,
                    value);
            }

            if (iconCooldownMaterial != null)
            {
                iconCooldownMaterial.SetFloat(
                    propertyId,
                    value);
            }
        }
        
        public void SetInteractionEnabled(bool interactionEnabled)
        {
            if (!interactionEnabled && isDragging)
            {
                isDragging = false;
                GridView?.ClearPlacementPreview();
                DragStateChanged?.Invoke(this, false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = interactionEnabled;
                canvasGroup.blocksRaycasts = interactionEnabled;
            }
        }
        
        private void ReleaseCooldownMaterials()
        {
            ReleaseCooldownMaterial(
                background,
                backgroundCooldownMaterial,
                originalBackgroundMaterial);

            ReleaseCooldownMaterial(
                icon,
                iconCooldownMaterial,
                originalIconMaterial);

            backgroundCooldownMaterial = null;
            iconCooldownMaterial = null;
            originalBackgroundMaterial = null;
            originalIconMaterial = null;
        }

        private static void ReleaseCooldownMaterial(
            Image targetImage,
            Material runtimeMaterial,
            Material originalMaterial)
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            if (targetImage != null &&
                targetImage.material == runtimeMaterial)
            {
                targetImage.material = originalMaterial;
            }

            Destroy(runtimeMaterial);
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
            if (BattleFlowController.IsCombatPhase ||
                canvasGroup == null ||
                !canvasGroup.interactable)
            {
                return;
            }

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
            DragStateChanged?.Invoke(this, true);
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
            DragStateChanged?.Invoke(this, false);
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

            if (Backpack.TryGetMergeTarget(
                    Instance,
                    anchorCell,
                    out ItemInstance mergeTarget) &&
                Backpack.TryMerge(Instance, mergeTarget))
            {
                CandidateAnchorCell = null;

                if (!IsPlacedInBackpack)
                {
                    DeletedSuccessfully?.Invoke(this);
                    Destroy(gameObject);
                }

                MergedSuccessfully?.Invoke(this, mergeTarget);
                return true;
            }

            bool moved;

            if (CombatController != null)
            {
                moved = IsPlacedInBackpack
                    ? CombatController.MoveItem(
                        Instance,
                        anchorCell)
                    : CombatController.PlaceItem(
                        Instance,
                        anchorCell);
            }
            else
            {
                moved = IsPlacedInBackpack
                    ? Backpack.MoveItem(
                        Instance,
                        anchorCell)
                    : Backpack.PlaceItem(
                        Instance,
                        anchorCell);
            }

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
                if (CombatController != null)
                {
                    CombatController.RemoveItem(Instance);
                }
                else
                {
                    Backpack.RemoveItem(Instance);
                }
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
            if (Backpack != null)
            {
                Backpack.ItemLevelChanged -= HandleItemLevelChanged;
            }
            if (CombatController != null)
            {
                CombatController.CooldownCompleted -=
                    HandleCooldownCompleted;
            }

            scaleTween?.Kill();
            feedbackTween?.Kill();
            cooldownFlashTween?.Kill();
            mergeFlashTween?.Kill();
            ReleaseCooldownMaterials();
        }

        private void EnsureLevelLabel()
        {
            if (levelLabel == null)
            {
                Transform existing = transform.Find("LevelLabel");
                levelLabel = existing != null
                    ? existing.GetComponent<TextMeshProUGUI>()
                    : null;
            }

            if (levelLabel != null)
            {
                return;
            }

            var labelObject = new GameObject(
                "LevelLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);
            levelLabel = labelObject.GetComponent<TextMeshProUGUI>();

            RectTransform labelRect = levelLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, -34f);
            labelRect.sizeDelta = new Vector2(80f, 22f);

            levelLabel.raycastTarget = false;
            levelLabel.alignment = TextAlignmentOptions.Center;
            levelLabel.enableWordWrapping = false;
            levelLabel.overflowMode = TextOverflowModes.Overflow;
            levelLabel.font = TMP_Settings.defaultFontAsset;
            levelLabel.fontSize = 16f;
            levelLabel.fontStyle = FontStyles.Bold;
            levelLabel.outlineWidth = 0.18f;
        }

        private void RefreshLevelLabel()
        {
            EnsureLevelLabel();

            if (levelLabel == null || Instance == null)
            {
                return;
            }

            bool isPlayer = CombatController == null ||
                CombatController.Faction == BattleFaction.Player;
            levelLabel.text = $"lv.{Instance.Level}";
            levelLabel.color = isPlayer
                ? new Color(0.28f, 0.78f, 1f, 1f)
                : new Color(1f, 0.34f, 0.12f, 1f);
            levelLabel.outlineColor = isPlayer
                ? new Color(0.015f, 0.04f, 0.09f, 0.9f)
                : new Color(0.12f, 0.015f, 0.005f, 0.9f);
        }

        private void ResizeToShape(Vector2 cellSize, Vector2 spacing)
        {
            shapeCellSize = cellSize;
            shapeSpacing = spacing;

            var bounds = GetShapeBounds(Instance.Data.ShapeOffsets);
            var width = (bounds.x + 1) * cellSize.x + bounds.x * spacing.x;
            var height = (bounds.y + 1) * cellSize.y + bounds.y * spacing.y;
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        public bool IsRaycastLocationValid(
            Vector2 screenPoint,
            Camera eventCamera)
        {
            if (Instance == null ||
                Instance.Data == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    screenPoint,
                    eventCamera,
                    out var localPoint))
            {
                return false;
            }

            var rect = rectTransform.rect;
            var fromTopLeft =
                new Vector2(
                    localPoint.x - rect.xMin,
                    rect.yMax - localPoint.y);
            var pitch = shapeCellSize + shapeSpacing;

            if (pitch.x <= 0f || pitch.y <= 0f)
            {
                return false;
            }

            var pointedCell =
                new Vector2Int(
                    Mathf.FloorToInt(fromTopLeft.x / pitch.x),
                    Mathf.FloorToInt(fromTopLeft.y / pitch.y));

            foreach (var offset in Instance.Data.ShapeOffsets)
            {
                if (offset == pointedCell)
                {
                    return true;
                }
            }

            return false;
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
