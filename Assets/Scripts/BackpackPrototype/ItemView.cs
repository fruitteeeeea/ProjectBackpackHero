using System;
using System.Collections.Generic;
using DG.Tweening;
using BackpackHero.Battle;
using BackpackHero.Debugging;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackPrototype
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, ICanvasRaycastFilter
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TMP_Text itemLabel;

        [SerializeField, Min(0f)]
        private float shopDropHitPadding = 32f;

        private static readonly int CooldownProgressId =
            Shader.PropertyToID("_CooldownProgress");

        private static readonly int FlashAmountId =
            Shader.PropertyToID("_FlashAmount");

        private static readonly int ItemVisualStyleId =
            Shader.PropertyToID("_ItemVisualStyle");

        private static readonly int PatternTilingId =
            Shader.PropertyToID("_PatternTiling");

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 originalAnchoredPosition;
        private Tween dragPositionTween;
        private Tween feedbackTween;
        private Tween cooldownFlashTween;
        private Tween mergeFlashTween;
        private Tween shopTransitionTween;
        private bool mergeHighlightActive;
        private float cooldownFlashAmount;
        private float mergeFlashAmount;
        private bool isDragging;
        private bool canDeleteFromTrash;
        private Vector2 shapeCellSize;
        private Vector2 shapeSpacing;
        private Vector2 placementFeedbackAnchoredPosition;
        private Quaternion placementFeedbackRotation;
        private Vector3 placementFeedbackScale;
        private Vector3 placementFeedbackCenterWorldPosition;
        private bool placementFeedbackActive;
        private Image[] trashPreviewImages;
        private Color[] trashPreviewColors;
        private Color defaultBackgroundColor;
        private Color defaultIconColor;
        private Color defaultLevelLabelColor;
        private bool deletePreviewColorsCached;
        
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
        public RectTransform ShopDropZone { get; private set; }
        public Vector2Int GrabCellOffset { get; private set; }
        public Vector2 GrabAnchorOffset { get; private set; }
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

        public Sprite IconSprite => icon != null ? icon.sprite : null;

        public RectTransform RectTransform => rectTransform;
        public bool IsDragging => isDragging;

        /// <summary>
        /// Returns the current world-space center of one logical item cell.
        /// This stays correct while the item is in either the backpack or drag layer.
        /// </summary>
        public Vector3 GetShapeCellCenterWorldPosition(
            Vector2Int cellOffset)
        {
            if (rectTransform == null)
            {
                return transform.position;
            }

            Vector2 pitch = shapeCellSize + shapeSpacing;
            Rect rect = rectTransform.rect;
            Vector3 localCellCenter = new(
                rect.xMin + cellOffset.x * pitch.x +
                    shapeCellSize.x * .5f,
                rect.yMax - cellOffset.y * pitch.y -
                    shapeCellSize.y * .5f);

            return rectTransform.TransformPoint(localCellCenter);
        }

        /// <summary>
        /// 物品图片的几何中心。用于图标、弹道和命中特效的统一锚点。
        /// </summary>
        public Vector3 GetImageGeometricCenterWorldPosition()
        {
            return rectTransform != null
                ? rectTransform.TransformPoint(
                    GetImageGeometricCenterLocalPosition())
                : transform.position;
        }
        
        
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
        public event Action<ItemView> DragPreviewChanged;
        public event Func<ItemView, bool> ShopDropRequested;
        public event Func<ItemView, Vector2Int, bool> SqueezeRequested;

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

            if (itemLabel == null)
            {
                foreach (TMP_Text candidate in GetComponentsInChildren<TMP_Text>(true))
                {
                    if (candidate.gameObject.name != "ItemLabel") continue;
                    itemLabel = candidate;
                    break;
                }
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
            BackpackVisualDebugRuntime.SettingsChanged -=
                HandleBackpackVisualSettingsChanged;
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

            CacheTrashPreviewImages();

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

            UpdateIconGeometricCenter();

            InitializeCooldownMaterials();

            if (label != null)
            {
                label.text = instance.Data.ItemName;
            }

            if (itemLabel != null)
            {
                itemLabel.text = instance.Data.ItemName;
            }

            RefreshLevelLabel();
            CacheDeletePreviewColors();
            BackpackVisualDebugRuntime.SettingsChanged +=
                HandleBackpackVisualSettingsChanged;
        }

        public void SetShopDropZone(RectTransform shopDropZone)
        {
            ShopDropZone = shopDropZone;
        }

        public void SetShopPosition(
            RectTransform shopContainer,
            Vector2 anchoredPosition,
            Vector3 scale)
        {
            shopTransitionTween?.Kill();
            rectTransform.SetParent(shopContainer, false);
            rectTransform.anchorMin = new Vector2(.5f, .5f);
            rectTransform.anchorMax = new Vector2(.5f, .5f);
            rectTransform.pivot = new Vector2(.5f, .5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = scale;
            rectTransform.localRotation = Quaternion.identity;
            IsPlacedInBackpack = false;
        }

        public void TweenToShopPosition(
            RectTransform shopContainer,
            Vector2 anchoredPosition,
            Vector3 scale,
            Vector3? startWorldPosition = null)
        {
            shopTransitionTween?.Kill();
            float duration = GetBackpackVisualSettings().ShopFlightDuration;
            if (startWorldPosition.HasValue)
            {
                rectTransform.position = startWorldPosition.Value;
            }
            rectTransform.SetParent(shopContainer, true);
            rectTransform.anchorMin = new Vector2(.5f, .5f);
            rectTransform.anchorMax = new Vector2(.5f, .5f);
            rectTransform.pivot = new Vector2(.5f, .5f);
            IsPlacedInBackpack = false;

            shopTransitionTween = DOTween.Sequence()
                .Join(rectTransform.DOAnchorPos(anchoredPosition, duration)
                    .SetEase(Ease.OutCubic))
                .Join(rectTransform.DOScale(scale, duration)
                    .SetEase(Ease.OutBack))
                .Join(rectTransform.DOLocalRotate(
                    Vector3.zero,
                    duration,
                    RotateMode.FastBeyond360)
                    .SetEase(Ease.OutCubic));
        }

        public void SetMergeHighlight(bool highlighted)
        {
            mergeHighlightActive = highlighted;
            mergeFlashTween?.Kill();

            if (!highlighted)
            {
                mergeFlashAmount = 0f;
                ApplyFlashAmount();
                return;
            }

            BackpackVisualSettings settings = GetBackpackVisualSettings();
            mergeFlashAmount = settings.MergeFlashMinimum;
            ApplyFlashAmount();
            mergeFlashTween = DOTween.To(
                    () => mergeFlashAmount,
                    value =>
                    {
                        mergeFlashAmount = value;
                        ApplyFlashAmount();
                    },
                    settings.MergeFlashMaximum,
                    settings.MergeFlashCycleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void PlayMergeFeedback()
        {
            PlayPlacedFeedback();
        }

        /// <summary>供非交互式背包操作者播放一次完整的移动反馈。</summary>
        public void AnimateToBackpackPosition(
            Vector2Int anchorCell,
            Action completed = null)
        {
            if (GridView == null || rectTransform == null)
            {
                completed?.Invoke();
                return;
            }

            feedbackTween?.Kill();
            rectTransform.SetParent(BackpackItemLayer, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            IsPlacedInBackpack = true;
            Instance.AnchorCell = anchorCell;
            Vector2 target = GridView.GetItemAnchoredPosition(anchorCell);
            feedbackTween = rectTransform.DOAnchorPos(target, .28f)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    SetBackpackPosition(anchorCell);
                    PlayPlacedFeedback();
                    completed?.Invoke();
                });
        }

        /// <summary>供非交互式背包操作者播放一次加入反馈。</summary>
        public void AnimateSpawnInBackpack(Action completed = null)
        {
            if (rectTransform == null)
            {
                completed?.Invoke();
                return;
            }

            feedbackTween?.Kill();
            rectTransform.localScale = Vector3.zero;
            feedbackTween = rectTransform.DOScale(Vector3.one, .24f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    PlayPlacedFeedback();
                    completed?.Invoke();
                });
        }

        /// <summary>供非交互式背包操作者播放一次移除反馈。</summary>
        public void AnimateRemoval(Action completed = null)
        {
            if (rectTransform == null)
            {
                completed?.Invoke();
                return;
            }

            feedbackTween?.Kill();
            float originalAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
            Sequence sequence = DOTween.Sequence();
            sequence.Join(rectTransform.DOScale(Vector3.zero, .22f)
                .SetEase(Ease.InBack));
            if (canvasGroup != null)
            {
                sequence.Join(canvasGroup.DOFade(0f, .18f));
            }

            feedbackTween = sequence.OnComplete(() =>
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = originalAlpha;
                }

                completed?.Invoke();
            });
        }

        /// <summary>供装备触发命中时使用的白闪，不影响冷却完成闪烁。</summary>
        public void PlayTriggeredFeedback()
        {
            cooldownFlashTween?.Kill();
            cooldownFlashAmount = 0f;
            ApplyFlashAmount();

            cooldownFlashTween = DOTween.Sequence()
                .Append(DOTween.To(
                    () => cooldownFlashAmount,
                    SetCooldownFlashAmount,
                    1f,
                    0.055f).SetEase(Ease.OutQuad))
                .Append(DOTween.To(
                    () => cooldownFlashAmount,
                    SetCooldownFlashAmount,
                    0f,
                    0.13f).SetEase(Ease.InQuad));
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

            ApplyItemVisualStyle();

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

        private void ApplyItemVisualStyle()
        {
            if (backgroundCooldownMaterial != null)
            {
                float visualStyle = 0f;

                // if (Instance != null &&
                //     Instance.Data != null)
                // {
                //     visualStyle =
                //         Instance.Data.ItemType == ItemType.Aircraft
                //             ? 1f
                //             : 2f;
                // }

                backgroundCooldownMaterial.SetFloat(
                    ItemVisualStyleId,
                    visualStyle);

                backgroundCooldownMaterial.SetVector(
                    PatternTilingId,
                    GetPatternTiling());
            }

            if (iconCooldownMaterial != null)
            {
                iconCooldownMaterial.SetFloat(
                    ItemVisualStyleId,
                    0f);
            }
        }

        private Vector4 GetPatternTiling()
        {
            if (background == null ||
                shapeCellSize.x <= 0f ||
                shapeCellSize.y <= 0f)
            {
                return new Vector4(1f, 1f, 0f, 0f);
            }

            Vector2 backgroundSize =
                background.rectTransform.rect.size;

            return new Vector4(
                Mathf.Max(1f, backgroundSize.x / shapeCellSize.x),
                Mathf.Max(1f, backgroundSize.y / shapeCellSize.y),
                0f,
                0f);
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
                dragPositionTween?.Kill();
                dragPositionTween = null;
                GridView?.ClearPlacementPreview();
                SetTrashPreview(false);
                canDeleteFromTrash = false;
                DragStateChanged?.Invoke(this, false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = interactionEnabled;
                canvasGroup.blocksRaycasts = interactionEnabled;
                canvasGroup.alpha = 1f;
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
            // 拖拽层不在背包的缩放根节点下。以 worldPositionStays
            // 重设父级会把父级缩放烘焙进物品 localScale；放回背包前
            // 必须恢复预制体基准，避免每次拖拽后叠乘放大。
            rectTransform.localScale = Vector3.one;
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

            isDragging = true;
            RequestSelection();
            canDeleteFromTrash = IsPlacedInBackpack;
            originalParent = rectTransform.parent;
            originalSiblingIndex = rectTransform.GetSiblingIndex();
            originalAnchoredPosition = rectTransform.anchoredPosition;
            CandidateAnchorCell = null;
            GridView?.ClearPlacementPreview();
            SetTrashPreview(false);

            IReadOnlyList<Vector2Int> shapeOffsets =
                Instance?.Data?.ShapeOffsets;
            GrabCellOffset = ItemGrabOffsetCalculator.Calculate(shapeOffsets);
            GrabAnchorOffset = ItemGrabOffsetCalculator.CalculateVisualAnchor(
                shapeOffsets);

            if (TryGetShapeCellAtScreenPosition(
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2Int grabbedCell) &&
                ItemGrabOffsetCalculator.TryCalculateTwoByOneAnchor(
                    shapeOffsets,
                    grabbedCell,
                    out Vector2 twoByOneAnchor))
            {
                GrabCellOffset = grabbedCell;
                GrabAnchorOffset = twoByOneAnchor;
            }

            if (DragLayer != null)
            {
                rectTransform.SetParent(DragLayer, true);
                rectTransform.SetAsLastSibling();
            }

            TweenDragVisualToPointer(
                eventData.position,
                eventData.pressEventCamera);

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = GetBackpackVisualSettings().DragOpacity;
            DragStateChanged?.Invoke(this, true);
            DragPreviewChanged?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            TweenDragVisualToPointer(
                eventData.position,
                eventData.pressEventCamera);

            bool previewsTrashDelete =
                canDeleteFromTrash &&
                IsOverTrash(
                    eventData.position,
                    eventData.pressEventCamera);
            SetTrashPreview(previewsTrashDelete);

            if (previewsTrashDelete)
            {
                CandidateAnchorCell = null;
                GridView?.ClearPlacementPreview();
                DragPreviewChanged?.Invoke(this);
                return;
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

            DragPreviewChanged?.Invoke(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            dragPositionTween?.Kill();
            dragPositionTween = null;
            DragStateChanged?.Invoke(this, false);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            GridView?.ClearPlacementPreview();

            bool deletesFromTrash =
                canDeleteFromTrash &&
                IsOverTrash(
                    eventData.position,
                    eventData.pressEventCamera);
            SetTrashPreview(false);
            canDeleteFromTrash = false;

            if (deletesFromTrash)
            {
                DeleteItem();
                return;
            }

            if (IsOverShop(
                    eventData.position,
                    eventData.pressEventCamera) &&
                ShopDropRequested?.Invoke(this) == true)
            {
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
            DragPreviewChanged?.Invoke(this);
            PlayFailedFeedback();
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

            SqueezeRequested?.Invoke(this, anchorCell);

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

        private bool IsOverShop(Vector2 screenPosition, Camera eventCamera)
        {
            if (ShopDropZone == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    ShopDropZone,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            Rect rect = ShopDropZone.rect;
            return localPoint.x >= rect.xMin - shopDropHitPadding &&
                localPoint.x <= rect.xMax + shopDropHitPadding &&
                localPoint.y >= rect.yMin - shopDropHitPadding &&
                localPoint.y <= rect.yMax + shopDropHitPadding;
        }

        private void DeleteItem()
        {
            if (!IsPlacedInBackpack || Backpack == null)
            {
                return;
            }

            if (CombatController != null)
            {
                CombatController.RemoveItem(Instance);
            }
            else
            {
                Backpack.RemoveItem(Instance);
            }

            DeletedSuccessfully?.Invoke(this);
            Destroy(gameObject);
        }

        private void CacheTrashPreviewImages()
        {
            if (TrashZone == null)
            {
                trashPreviewImages = Array.Empty<Image>();
                trashPreviewColors = Array.Empty<Color>();
                return;
            }

            trashPreviewImages = TrashZone.GetComponentsInChildren<Image>(
                true);
            trashPreviewColors = new Color[trashPreviewImages.Length];

            for (int index = 0;
                 index < trashPreviewImages.Length;
                 index++)
            {
                trashPreviewColors[index] =
                    trashPreviewImages[index].color;
            }
        }

        private void SetTrashPreview(bool deleting)
        {
            if (trashPreviewImages != null &&
                trashPreviewColors != null)
            {
                for (int index = 0;
                     index < trashPreviewImages.Length;
                     index++)
                {
                    Image previewImage = trashPreviewImages[index];
                    if (previewImage == null)
                    {
                        continue;
                    }

                    Color defaultColor = trashPreviewColors[index];
                    previewImage.color = deleting
                        ? new Color(1f, 0.18f, 0.18f, defaultColor.a)
                        : defaultColor;
                }
            }

            SetItemDeletePreview(deleting);
        }

        private void CacheDeletePreviewColors()
        {
            defaultBackgroundColor = background != null
                ? background.color
                : Color.white;
            defaultIconColor = icon != null
                ? icon.color
                : Color.white;
            defaultLevelLabelColor = levelLabel != null
                ? levelLabel.color
                : Color.white;
            deletePreviewColorsCached = true;
        }

        private void SetItemDeletePreview(bool deleting)
        {
            if (!deletePreviewColorsCached)
            {
                return;
            }

            if (background != null)
            {
                background.color = deleting
                    ? new Color(1f, 0.08f, 0.08f,
                        defaultBackgroundColor.a)
                    : defaultBackgroundColor;
            }

            if (icon != null)
            {
                icon.color = deleting
                    ? new Color(1f, 0.32f, 0.32f,
                        defaultIconColor.a)
                    : defaultIconColor;
            }

            if (levelLabel != null)
            {
                levelLabel.color = deleting
                    ? new Color(1f, 0.76f, 0.76f,
                        defaultLevelLabelColor.a)
                    : defaultLevelLabelColor;
            }
        }

        private void PlayPlacedFeedback()
        {
            StopPlacementFeedback();

            // 物品预制体的视觉基准为 1。合成目标可能尚未经过
            // SetBackpackPosition，因此在播放放置反馈前也显式归一。
            rectTransform.localScale = Vector3.one;
            placementFeedbackAnchoredPosition =
                rectTransform.anchoredPosition;
            placementFeedbackRotation = rectTransform.localRotation;
            placementFeedbackScale = rectTransform.localScale;
            placementFeedbackCenterWorldPosition =
                rectTransform.TransformPoint(rectTransform.rect.center);
            placementFeedbackActive = true;

            BackpackVisualSettings settings = GetBackpackVisualSettings();
            rectTransform.localScale = placementFeedbackScale *
                settings.PlacementScaleMultiplier;
            KeepPlacementFeedbackCenterFixed();

            float rotation = 0f;
            Sequence feedbackSequence = DOTween.Sequence();
            feedbackSequence.Append(DOTween.To(
                    () => rotation,
                    value =>
                    {
                        rotation = value;
                        SetPlacementFeedbackRotation(value);
                    },
                    settings.PlacementPositiveRotationDegrees,
                    0.04f)
                .SetEase(Ease.OutQuad));
            feedbackSequence.Append(DOTween.To(
                    () => rotation,
                    value =>
                    {
                        rotation = value;
                        SetPlacementFeedbackRotation(value);
                    },
                    settings.PlacementNegativeRotationDegrees,
                    0.05f)
                .SetEase(Ease.InOutQuad));
            feedbackSequence.Append(DOTween.To(
                    () => rotation,
                    value =>
                    {
                        rotation = value;
                        SetPlacementFeedbackRotation(value);
                    },
                    0f,
                    0.055f)
                .SetEase(Ease.OutQuad));
            feedbackSequence.Join(DOTween.To(
                    () => rectTransform.localScale,
                    value => rectTransform.localScale = value,
                    placementFeedbackScale,
                    0.17f)
                .SetEase(settings.GetPlacementScaleDotweenEase(), 0.8f, 0.28f));

            feedbackTween = feedbackSequence
                .OnUpdate(KeepPlacementFeedbackCenterFixed)
                .OnComplete(ResetPlacementFeedbackTransform)
                .OnKill(ResetPlacementFeedbackTransform);
        }

        private void PlayFailedFeedback()
        {
            feedbackTween?.Kill();
            feedbackTween = rectTransform.DOShakePosition(0.18f, new Vector3(12f, 0f, 0f), 12, 90f, false, true);
        }

        private void KeepPlacementFeedbackCenterFixed()
        {
            if (!placementFeedbackActive || rectTransform == null)
            {
                return;
            }

            Vector3 rotatedCenter = rectTransform.TransformPoint(
                rectTransform.rect.center);
            rectTransform.position =
                ItemPlacementRotation.CalculateCorrectedWorldPosition(
                    rectTransform.position,
                    placementFeedbackCenterWorldPosition,
                    rotatedCenter);
        }

        private void SetPlacementFeedbackRotation(float degrees)
        {
            rectTransform.localRotation = placementFeedbackRotation *
                Quaternion.Euler(0f, 0f, degrees);
        }

        private void StopPlacementFeedback()
        {
            feedbackTween?.Kill();
            ResetPlacementFeedbackTransform();
        }

        private void ResetPlacementFeedbackTransform()
        {
            if (!placementFeedbackActive || rectTransform == null)
            {
                return;
            }

            rectTransform.localRotation = placementFeedbackRotation;
            rectTransform.localScale = placementFeedbackScale;
            rectTransform.anchoredPosition =
                placementFeedbackAnchoredPosition;
            placementFeedbackActive = false;
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

            dragPositionTween?.Kill();
            feedbackTween?.Kill();
            cooldownFlashTween?.Kill();
            mergeFlashTween?.Kill();
            shopTransitionTween?.Kill();
            BackpackVisualDebugRuntime.SettingsChanged -=
                HandleBackpackVisualSettingsChanged;
            ReleaseCooldownMaterials();
        }

        private static BackpackVisualSettings GetBackpackVisualSettings()
        {
            BackpackVisualSettings settings =
                BackpackVisualDebugRuntime.CurrentSettings;
            return settings.OverridesEnabled
                ? settings
                : BackpackVisualSettings.Default;
        }

        private void HandleBackpackVisualSettingsChanged(
            BackpackVisualSettings _)
        {
            if (mergeHighlightActive)
            {
                SetMergeHighlight(true);
            }
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

        private void UpdateIconGeometricCenter()
        {
            if (icon == null)
            {
                return;
            }

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(.5f, .5f);
            iconRect.anchorMax = new Vector2(.5f, .5f);
            iconRect.pivot = new Vector2(.5f, .5f);
            iconRect.anchoredPosition =
                GetImageGeometricCenterLocalPosition();
        }

        private Vector2 GetImageGeometricCenterLocalPosition()
        {
            if (Instance?.Data == null ||
                shapeCellSize.x <= 0f ||
                shapeCellSize.y <= 0f ||
                rectTransform == null)
            {
                return Vector2.zero;
            }

            Vector2 center = ItemShapeGeometry.CalculateCenter(
                Instance.Data.ShapeOffsets);
            Vector2 pitch = shapeCellSize + shapeSpacing;
            Rect rect = rectTransform.rect;
            return new Vector2(
                rect.xMin + center.x * pitch.x +
                    shapeCellSize.x * .5f,
                rect.yMax - center.y * pitch.y -
                    shapeCellSize.y * .5f);
        }

        public bool IsRaycastLocationValid(
            Vector2 screenPoint,
            Camera eventCamera)
        {
            return TryGetShapeCellAtScreenPosition(
                screenPoint,
                eventCamera,
                out _);
        }

        private bool TryGetShapeCellAtScreenPosition(
            Vector2 screenPoint,
            Camera eventCamera,
            out Vector2Int shapeCell)
        {
            shapeCell = default;
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
                    shapeCell = offset;
                    return true;
                }
            }

            return false;
        }

        private void TweenDragVisualToPointer(
            Vector2 screenPosition,
            Camera eventCamera)
        {
            if (!(rectTransform.parent is RectTransform parentRect) ||
                !RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parentRect,
                    screenPosition,
                    eventCamera,
                    out Vector3 pointerWorldPosition))
            {
                return;
            }

            Vector3 targetPosition = pointerWorldPosition -
                GetGrabAnchorWorldOffset();
            dragPositionTween?.Kill();
            dragPositionTween = rectTransform.DOMove(
                    targetPosition,
                    0.12f)
                .SetEase(Ease.OutQuad);
        }

        private Vector3 GetGrabAnchorWorldOffset()
        {
            Vector2 pitch = shapeCellSize + shapeSpacing;
            Rect rect = rectTransform.rect;
            Vector3 localAnchor = new Vector3(
                rect.xMin + GrabAnchorOffset.x * pitch.x +
                    shapeCellSize.x * 0.5f,
                rect.yMax - GrabAnchorOffset.y * pitch.y -
                    shapeCellSize.y);

            return rectTransform.TransformVector(localAnchor);
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
    }
}
