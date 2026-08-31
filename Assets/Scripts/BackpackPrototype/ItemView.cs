using System;
using System.Collections.Generic;
using DG.Tweening;
using BackpackHero.Battle;
using BackpackHero.Audio;
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
        [SerializeField] private ItemBaseShape itemBaseShape =
            ItemBaseShape.Square;

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

        private static readonly int BottomPlateDisplayModeId =
            Shader.PropertyToID("_BottomPlateDisplayMode");

        private static readonly int AircraftGlowRenderModeId =
            Shader.PropertyToID("_AircraftGlowRenderMode");

        private static readonly int AircraftGlowColorId =
            Shader.PropertyToID("_AircraftGlowColor");

        private static readonly int AircraftGlowMinimumIntensityId =
            Shader.PropertyToID("_AircraftGlowMinimumIntensity");

        private static readonly int AircraftGlowMaximumIntensityId =
            Shader.PropertyToID("_AircraftGlowMaximumIntensity");

        private static readonly int AircraftGlowCycleDurationId =
            Shader.PropertyToID("_AircraftGlowCycleDuration");

        private static readonly int AircraftGlowPaddingId =
            Shader.PropertyToID("_AircraftGlowPadding");

        private static readonly int AircraftGlowRadiusUvId =
            Shader.PropertyToID("_AircraftGlowRadiusUV");

        private static readonly int AircraftGlowUvBoundsId =
            Shader.PropertyToID("_AircraftGlowUvBounds");

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
        private Tween shopAppearanceTween;
        private Tween aircraftQualityPulseTween;
        private bool mergeHighlightActive;
        private bool isDeletePreviewActive;
        private float cooldownFlashAmount;
        private float mergeFlashAmount;
        private bool isDragging;
        private bool canDeleteFromTrash;
        private bool interactionEnabled;
        private bool shopAppearanceActive;
        private float shopAppearanceAlpha = 1f;
        private Vector2 shapeCellSize;
        private Vector2 shapeSpacing;
        private Vector2 placementFeedbackAnchoredPosition;
        private Quaternion placementFeedbackRotation;
        private Vector3 placementFeedbackScale;
        private Vector3 placementFeedbackCenterWorldPosition;
        private bool placementFeedbackActive;
        private Image[] trashPreviewImages;
        private Color[] trashPreviewColors;
        private Color defaultIconColor;
        private float defaultLevelLabelFontSize;
        private bool levelLabelFontSizeCached;
        private bool deletePreviewColorsCached;
        
        private Material originalBackgroundMaterial;
        private Material originalIconMaterial;
        private Material backgroundCooldownMaterial;
        private Material iconCooldownMaterial;
        private Image aircraftGlowImage;
        private Material aircraftGlowMaterial;
        private Image aircraftQualityPulseImage;
        private Sprite style1BackgroundSprite;

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
        public ItemBaseShape ItemBaseShape => itemBaseShape;

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

            style1BackgroundSprite = background != null
                ? background.sprite
                : null;
            ArtAssetDebugRuntime.SettingsChanged +=
                HandleArtAssetSettingsChanged;
            ApplyItemBaseStyle();

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

        private void LateUpdate()
        {
            if (aircraftGlowImage != null &&
                aircraftGlowImage.gameObject.activeSelf)
            {
                SyncAircraftGlowTransform();
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
            Vector2 spacing,
            BackpackCombatController combatController = null)
        {
            BackpackVisualDebugRuntime.SettingsChanged -=
                HandleBackpackVisualSettingsChanged;
            BattleFlowController.PhaseChanged -= HandleBattlePhaseChanged;
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
            isDeletePreviewActive = false;
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
            ApplyItemBaseStyle();

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

            CacheLevelLabelFontSize();
            RefreshLevelLabel();
            CacheDeletePreviewColors();
            RefreshBottomPlateVisual();
            BackpackVisualDebugRuntime.SettingsChanged +=
                HandleBackpackVisualSettingsChanged;
            BattleFlowController.PhaseChanged += HandleBattlePhaseChanged;
            RefreshAircraftGlow();
            RefreshAircraftQualityPulse();
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
            RefreshAircraftGlow();
            RefreshAircraftQualityPulse();
        }

        /// <summary>
        /// 新生成的商店候选统一使用延迟渐显，避免背包缩放动画完成时
        /// 物品尺寸瞬间变化。出现期间不接收交互。
        /// </summary>
        public void PlayShopAppearance(
            float delay,
            float fadeDuration)
        {
            CancelShopAppearance(restoreVisibility: false);
            shopAppearanceActive = true;
            SetShopAppearanceAlpha(0f);
            RefreshInteractionState();

            shopAppearanceTween = DOTween.Sequence()
                .AppendInterval(Mathf.Max(0f, delay))
                .Append(DOTween.To(
                    () => shopAppearanceAlpha,
                    SetShopAppearanceAlpha,
                    1f,
                    Mathf.Max(.01f, fadeDuration))
                    .SetEase(Ease.OutCubic))
                .OnComplete(CompleteShopAppearance);
        }

        public void TweenToShopPosition(
            RectTransform shopContainer,
            Vector2 anchoredPosition,
            Vector3 scale,
            Vector3? startWorldPosition = null)
        {
            CancelShopAppearance();
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
            RefreshAircraftGlow();
            RefreshAircraftQualityPulse();

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

        public void PlayMergeFeedback(bool playBackpackItemSfx = false)
        {
            PlayPlacedFeedback();
            if (playBackpackItemSfx)
            {
                PlayBackpackItemSfx();
            }
        }

        /// <summary>供非交互式背包操作者播放一次完整的移动反馈。</summary>
        public void AnimateToBackpackPosition(
            Vector2Int anchorCell,
            Action completed = null,
            bool playBackpackItemSfx = false)
        {
            if (playBackpackItemSfx)
            {
                PlayBackpackItemSfx();
            }

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
                    if (playBackpackItemSfx)
                    {
                        PlayBackpackItemSfx();
                    }
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
        public void AnimateRemoval(
            Action completed = null,
            bool playBackpackItemSfx = false)
        {
            if (playBackpackItemSfx)
            {
                PlayBackpackItemSfx();
            }

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
            ReleaseAircraftGlow();
            ReleaseAircraftQualityPulse();
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
            InitializeAircraftGlow();
            RefreshAircraftQualityPulse();
        }

        private void InitializeAircraftGlow()
        {
            if (background == null || backgroundCooldownMaterial == null ||
                background.sprite == null)
            {
                return;
            }

            GameObject glowObject = new("Aircraft Glow", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            Transform glowParent = transform.parent != null
                ? transform.parent
                : transform;
            glowObject.transform.SetParent(glowParent, false);
            glowObject.transform.SetSiblingIndex(transform.GetSiblingIndex());
            aircraftGlowImage = glowObject.GetComponent<Image>();
            aircraftGlowImage.sprite = background.sprite;
            aircraftGlowImage.type = background.type;
            aircraftGlowImage.preserveAspect = background.preserveAspect;
            aircraftGlowImage.raycastTarget = false;
            aircraftGlowImage.color = Color.white;
            aircraftGlowMaterial = new Material(backgroundCooldownMaterial)
            {
                name = $"{backgroundCooldownMaterial.name} (Aircraft Glow Runtime)"
            };
            aircraftGlowMaterial.SetFloat(AircraftGlowRenderModeId, 1f);
            aircraftGlowImage.material = aircraftGlowMaterial;
            RefreshAircraftGlow();
        }

        private void RefreshAircraftGlow()
        {
            if (aircraftGlowImage == null || aircraftGlowMaterial == null)
            {
                return;
            }

            BackpackVisualSettings settings =
                BackpackVisualDebugRuntime.CurrentSettings;
            ItemType itemType = Instance != null && Instance.Data != null
                ? Instance.Data.ItemType
                : ItemType.Equipment;
            BattleFaction faction = CombatController == null
                ? BattleFaction.Player
                : CombatController.Faction;
            bool showGlow = !isDragging && IsAircraftGlowEligible(settings.OverridesEnabled,
                settings.AircraftGlowEnabled, itemType, IsPlacedInBackpack,
                faction, BattleFlowController.CurrentPhase);
            aircraftGlowImage.gameObject.SetActive(showGlow);
            if (!showGlow)
            {
                return;
            }

            aircraftGlowMaterial.SetColor(AircraftGlowColorId,
                settings.AircraftGlowColor);
            aircraftGlowMaterial.SetFloat(AircraftGlowMinimumIntensityId,
                settings.AircraftGlowMinimumIntensity);
            aircraftGlowMaterial.SetFloat(AircraftGlowMaximumIntensityId,
                settings.AircraftGlowMaximumIntensity);
            aircraftGlowMaterial.SetFloat(AircraftGlowCycleDurationId,
                settings.AircraftGlowCycleDuration);

            float edgeWidth = settings.AircraftGlowEdgeWidth;
            SyncAircraftGlowTransform();
            RectTransform glowTransform = aircraftGlowImage.rectTransform;
            Vector2 baseSize = rectTransform.rect.size;
            Vector2 expandedSize = baseSize +
                new Vector2(edgeWidth * 2f, edgeWidth * 2f);
            aircraftGlowMaterial.SetVector(AircraftGlowPaddingId, new Vector4(
                expandedSize.x > 0f ? edgeWidth / expandedSize.x : 0f,
                expandedSize.y > 0f ? edgeWidth / expandedSize.y : 0f,
                0f, 0f));
            aircraftGlowMaterial.SetVector(AircraftGlowRadiusUvId, new Vector4(
                baseSize.x > 0f ? edgeWidth / baseSize.x : 0f,
                baseSize.y > 0f ? edgeWidth / baseSize.y : 0f,
                0f, 0f));
            aircraftGlowMaterial.SetVector(AircraftGlowUvBoundsId,
                GetSpriteUvBounds(background.sprite));
        }

        private static Vector4 GetSpriteUvBounds(Sprite sprite)
        {
            if (sprite == null || sprite.uv == null || sprite.uv.Length == 0)
            {
                return new Vector4(0f, 0f, 1f, 1f);
            }

            Vector2 min = sprite.uv[0];
            Vector2 max = min;
            foreach (Vector2 uv in sprite.uv)
            {
                min = Vector2.Min(min, uv);
                max = Vector2.Max(max, uv);
            }

            return new Vector4(min.x, min.y, max.x, max.y);
        }

        private void SyncAircraftGlowTransform()
        {
            if (aircraftGlowImage == null || rectTransform == null)
            {
                return;
            }

            RectTransform glowTransform = aircraftGlowImage.rectTransform;
            Transform targetParent = rectTransform.parent;
            if (targetParent != null && glowTransform.parent != targetParent)
            {
                glowTransform.SetParent(targetParent, false);
                glowTransform.SetSiblingIndex(rectTransform.GetSiblingIndex());
            }

            float edgeWidth = BackpackVisualDebugRuntime.CurrentSettings
                .AircraftGlowEdgeWidth;
            Vector2 expansion = new Vector2(edgeWidth * 2f, edgeWidth * 2f);
            glowTransform.anchorMin = rectTransform.anchorMin;
            glowTransform.anchorMax = rectTransform.anchorMax;
            glowTransform.pivot = rectTransform.pivot;
            // ItemView 常用左上 Pivot。仅扩大 sizeDelta 会让光晕只向右下
            // 偏移；根据 Pivot 平移半个扩展尺寸，使原底板仍位于光晕中心。
            glowTransform.anchoredPosition = rectTransform.anchoredPosition +
                Vector2.Scale(rectTransform.pivot * 2f - Vector2.one,
                    expansion * .5f);
            glowTransform.sizeDelta = rectTransform.sizeDelta + expansion;
            glowTransform.localScale = rectTransform.localScale;
            glowTransform.localRotation = rectTransform.localRotation;
        }

        public static bool IsAircraftGlowEligible(
            bool overridesEnabled,
            bool aircraftGlowEnabled,
            ItemType itemType,
            bool isPlacedInBackpack,
            BattleFaction faction,
            BattlePhase phase) =>
            overridesEnabled && aircraftGlowEnabled &&
            itemType == ItemType.Aircraft && isPlacedInBackpack &&
            faction == BattleFaction.Player && phase == BattlePhase.Preparation;

        public static bool IsAircraftQualityPulseEligible(
            bool colorQualityModeActive,
            bool aircraftQualityPulseEnabled,
            ItemType itemType,
            bool isPlacedInBackpack,
            BattleFaction faction,
            BattlePhase phase,
            bool isDragging) =>
            colorQualityModeActive && aircraftQualityPulseEnabled &&
            itemType == ItemType.Aircraft &&
            isPlacedInBackpack && faction == BattleFaction.Player &&
            phase == BattlePhase.Preparation && !isDragging;

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

                BackpackVisualSettings settings =
                    BackpackVisualDebugRuntime.CurrentSettings;
                bool shouldUseEquipmentGlow = Instance != null &&
                    Instance.Data != null &&
                    Instance.Data.ItemType == ItemType.Equipment &&
                    Instance.Data.BottomPlateDisplayMode ==
                        BottomPlateDisplayMode.Glow &&
                    settings.OverridesEnabled &&
                    settings.EquipmentBottomPlateGlowEnabled &&
                    !IsColorQualityModeActive(settings);
                float bottomPlateDisplayMode = shouldUseEquipmentGlow
                    ? (float)BottomPlateDisplayMode.Glow
                    : (float)BottomPlateDisplayMode.Normal;
                backgroundCooldownMaterial.SetFloat(
                    BottomPlateDisplayModeId,
                    bottomPlateDisplayMode);
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
                ApplyItemBaseStyle();
                RefreshLevelLabel();
                RefreshBottomPlateVisual();
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
        
        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;
            if (!enabled && isDragging)
            {
                isDragging = false;
                dragPositionTween?.Kill();
                dragPositionTween = null;
                GridView?.ClearPlacementPreview();
                SetTrashPreview(false);
                canDeleteFromTrash = false;
                DragStateChanged?.Invoke(this, false);
            }

            RefreshInteractionState();
        }

        private void CompleteShopAppearance()
        {
            shopAppearanceTween = null;
            shopAppearanceActive = false;
            SetShopAppearanceAlpha(1f);
            RefreshInteractionState();
        }

        private void CancelShopAppearance(
            bool restoreVisibility = true)
        {
            shopAppearanceTween?.Kill();
            shopAppearanceTween = null;
            if (!shopAppearanceActive)
            {
                return;
            }

            shopAppearanceActive = false;
            if (restoreVisibility)
            {
                SetShopAppearanceAlpha(1f);
            }

            RefreshInteractionState();
        }

        private void SetShopAppearanceAlpha(float alpha)
        {
            shopAppearanceAlpha = Mathf.Clamp01(alpha);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = shopAppearanceAlpha;
            }

            if (aircraftGlowImage != null)
            {
                Color glowColor = aircraftGlowImage.color;
                glowColor.a = shopAppearanceAlpha;
                aircraftGlowImage.color = glowColor;
            }
        }

        private void RefreshInteractionState()
        {
            if (canvasGroup == null)
            {
                return;
            }

            bool enabled = interactionEnabled && !shopAppearanceActive;
            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
            if (!shopAppearanceActive)
            {
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

        private void ReleaseAircraftGlow()
        {
            if (aircraftGlowMaterial != null)
            {
                Destroy(aircraftGlowMaterial);
                aircraftGlowMaterial = null;
            }

            if (aircraftGlowImage != null)
            {
                Destroy(aircraftGlowImage.gameObject);
                aircraftGlowImage = null;
            }
        }

        private void RefreshAircraftQualityPulse()
        {
            StopAircraftQualityPulse();

            BackpackVisualSettings settings =
                BackpackVisualDebugRuntime.CurrentSettings;
            ItemType itemType = Instance != null && Instance.Data != null
                ? Instance.Data.ItemType
                : ItemType.Equipment;
            BattleFaction faction = CombatController == null
                ? BattleFaction.Player
                : CombatController.Faction;
            if (!IsAircraftQualityPulseEligible(
                    IsColorQualityModeActive(settings),
                    settings.AircraftQualityPulseEnabled, itemType,
                    IsPlacedInBackpack, faction,
                    BattleFlowController.CurrentPhase, isDragging) ||
                !EnsureAircraftQualityPulseImage())
            {
                return;
            }

            RectTransform pulseTransform =
                aircraftQualityPulseImage.rectTransform;
            aircraftQualityPulseTween = DOTween.Sequence()
                .AppendInterval(settings.AircraftQualityPulseInterval)
                .AppendCallback(PrepareAircraftQualityPulse)
                .Append(pulseTransform.DOScale(
                    rectTransform.localScale *
                    settings.AircraftQualityPulseScaleMultiplier,
                    settings.AircraftQualityPulseTweenDuration)
                    .SetEase(Ease.OutCubic))
                .Join(DOTween.To(
                    () => aircraftQualityPulseImage.color.a,
                    SetAircraftQualityPulseAlpha,
                    0f,
                    settings.AircraftQualityPulseTweenDuration)
                    .SetEase(Ease.OutCubic))
                .OnComplete(() =>
                {
                    aircraftQualityPulseTween = null;
                    ResetAircraftQualityPulseImage();
                    RefreshAircraftQualityPulse();
                });
        }

        private bool EnsureAircraftQualityPulseImage()
        {
            if (aircraftQualityPulseImage != null)
            {
                return true;
            }

            if (background == null || background.sprite == null ||
                rectTransform == null)
            {
                return false;
            }

            GameObject pulseObject = new("Aircraft Quality Pulse",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Transform pulseParent = transform.parent != null
                ? transform.parent
                : transform;
            pulseObject.transform.SetParent(pulseParent, false);
            pulseObject.transform.SetSiblingIndex(transform.GetSiblingIndex());
            aircraftQualityPulseImage = pulseObject.GetComponent<Image>();
            aircraftQualityPulseImage.raycastTarget = false;
            ResetAircraftQualityPulseImage();
            return true;
        }

        private void PrepareAircraftQualityPulse()
        {
            if (aircraftQualityPulseImage == null)
            {
                return;
            }

            SyncAircraftQualityPulseImage();
            aircraftQualityPulseImage.gameObject.SetActive(true);
        }

        private void SetAircraftQualityPulseAlpha(float value)
        {
            if (aircraftQualityPulseImage == null)
            {
                return;
            }

            Color color = aircraftQualityPulseImage.color;
            color.a = value;
            aircraftQualityPulseImage.color = color;
        }

        private void StopAircraftQualityPulse()
        {
            aircraftQualityPulseTween?.Kill();
            aircraftQualityPulseTween = null;
            ResetAircraftQualityPulseImage();
        }

        private void ResetAircraftQualityPulseImage()
        {
            if (aircraftQualityPulseImage == null)
            {
                return;
            }

            SyncAircraftQualityPulseImage();
            aircraftQualityPulseImage.gameObject.SetActive(false);
        }

        private void SyncAircraftQualityPulseImage()
        {
            if (aircraftQualityPulseImage == null || background == null ||
                rectTransform == null)
            {
                return;
            }

            RectTransform pulseTransform =
                aircraftQualityPulseImage.rectTransform;
            Transform targetParent = rectTransform.parent;
            if (targetParent != null && pulseTransform.parent != targetParent)
            {
                pulseTransform.SetParent(targetParent, false);
            }

            pulseTransform.SetSiblingIndex(rectTransform.GetSiblingIndex());
            pulseTransform.anchorMin = rectTransform.anchorMin;
            pulseTransform.anchorMax = rectTransform.anchorMax;
            Vector2 pulsePivot = new(.5f, .5f);
            Vector2 scaledRectSize = Vector2.Scale(
                rectTransform.rect.size,
                new Vector2(rectTransform.localScale.x,
                    rectTransform.localScale.y));
            Vector2 pivotToCenter = Vector2.Scale(
                pulsePivot - rectTransform.pivot,
                scaledRectSize);
            Vector3 rotatedPivotToCenter = rectTransform.localRotation *
                new Vector3(pivotToCenter.x, pivotToCenter.y, 0f);
            // 背包物品通常使用左上 Pivot。脉冲层改为中心 Pivot 后，
            // 先补偿到原底板的几何中心，放大才会向四周均匀扩张。
            pulseTransform.pivot = pulsePivot;
            pulseTransform.anchoredPosition = rectTransform.anchoredPosition +
                new Vector2(rotatedPivotToCenter.x, rotatedPivotToCenter.y);
            pulseTransform.sizeDelta = rectTransform.sizeDelta;
            pulseTransform.localScale = rectTransform.localScale;
            pulseTransform.localRotation = rectTransform.localRotation;
            aircraftQualityPulseImage.sprite = background.sprite;
            aircraftQualityPulseImage.type = background.type;
            aircraftQualityPulseImage.preserveAspect = background.preserveAspect;
            aircraftQualityPulseImage.material = background.material;
            aircraftQualityPulseImage.color = background.color;
        }

        private void ReleaseAircraftQualityPulse()
        {
            aircraftQualityPulseTween?.Kill();
            aircraftQualityPulseTween = null;
            if (aircraftQualityPulseImage != null)
            {
                Destroy(aircraftQualityPulseImage.gameObject);
                aircraftQualityPulseImage = null;
            }
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

            CancelShopAppearance();
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
            RefreshAircraftGlow();
            RefreshAircraftQualityPulse();
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
            PlayBackpackItemSfx();
            RefreshAircraftGlow();
            RefreshAircraftQualityPulse();
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
            RefreshAircraftGlow();
            RefreshAircraftQualityPulse();
            dragPositionTween?.Kill();
            dragPositionTween = null;
            DragStateChanged?.Invoke(this, false);
            RefreshInteractionState();
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
            PlayBackpackItemSfx();
            PlacedSuccessfully?.Invoke(this);
            return true;
        }

        private static void PlayBackpackItemSfx()
        {
            GameSfxService.Instance?.Play(GameSfxId.BackpackItem);
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
            defaultIconColor = icon != null
                ? icon.color
                : Color.white;
            deletePreviewColorsCached = true;
        }

        private void SetItemDeletePreview(bool deleting)
        {
            if (!deletePreviewColorsCached)
            {
                return;
            }

            isDeletePreviewActive = deleting;
            RefreshBottomPlateVisual();

            if (icon != null)
            {
                icon.color = deleting
                    ? new Color(1f, 0.32f, 0.32f,
                        defaultIconColor.a)
                    : defaultIconColor;
            }

            RefreshLevelLabel();
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
            shopAppearanceTween?.Kill();
            ReleaseAircraftQualityPulse();
            BackpackVisualDebugRuntime.SettingsChanged -=
                HandleBackpackVisualSettingsChanged;
            ArtAssetDebugRuntime.SettingsChanged -=
                HandleArtAssetSettingsChanged;
            BattleFlowController.PhaseChanged -= HandleBattlePhaseChanged;
            ReleaseAircraftGlow();
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
            ApplyItemBaseStyle();
            ApplyItemVisualStyle();
            RefreshLevelLabel();
            RefreshBottomPlateVisual();
            RefreshAircraftGlow();
            RefreshAircraftQualityPulse();
            if (mergeHighlightActive)
            {
                SetMergeHighlight(true);
            }
        }

        private void HandleArtAssetSettingsChanged(
            ArtAssetDebugSettingsValue _)
        {
            ApplyItemBaseStyle();
        }

        private void ApplyItemBaseStyle()
        {
            if (background == null)
            {
                return;
            }

            Sprite fallback = ArtAssetDebugRuntime.GetCurrentItemBaseSprite(
                itemBaseShape, style1BackgroundSprite);
            Sprite sprite = ResolveBottomPlateSprite(fallback);
            if (sprite == null)
            {
                return;
            }

            background.sprite = sprite;
            if (aircraftGlowImage != null)
            {
                aircraftGlowImage.sprite = sprite;
                SyncAircraftGlowTransform();
                RefreshAircraftGlow();
            }
            if (aircraftQualityPulseImage != null)
            {
                RefreshAircraftQualityPulse();
            }
        }

        private Sprite ResolveBottomPlateSprite(Sprite fallback)
        {
            BackpackVisualSettings settings =
                BackpackVisualDebugRuntime.CurrentSettings;
            if (!IsColorQualityModeActive(settings) || Instance == null)
            {
                return fallback;
            }

            return settings.ItemQualityPalette.GetSpriteForLevel(
                Instance.Level, itemBaseShape) ?? fallback;
        }

        private static bool IsColorQualityModeActive(
            BackpackVisualSettings settings) =>
            settings.OverridesEnabled && settings.ColorQualityModeEnabled &&
            settings.ItemQualityPalette != null;

        private void HandleBattlePhaseChanged(BattlePhase _)
        {
            RefreshAircraftGlow();
            RefreshAircraftQualityPulse();
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
            Color factionColor = isPlayer
                ? new Color(0.28f, 0.78f, 1f, 1f)
                : new Color(1f, 0.34f, 0.12f, 1f);
            BackpackVisualSettings settings =
                BackpackVisualDebugRuntime.CurrentSettings;
            bool bottomPlateHighlightEnabled =
                settings.OverridesEnabled && settings.AircraftGlowEnabled;
            levelLabel.enabled = !ShouldHideEquipmentLevelLabel(
                bottomPlateHighlightEnabled, Instance.Data.ItemType);
            bool useFactionColor = settings.UsesFactionLevelColor;
            levelLabel.color = useFactionColor
                ? factionColor
                : settings.LevelFontColor;
            if (settings.OverridesEnabled)
            {
                levelLabel.fontSize = settings.LevelFontSize;
            }
            else if (levelLabelFontSizeCached)
            {
                levelLabel.fontSize = defaultLevelLabelFontSize;
            }
            levelLabel.outlineColor = isPlayer
                ? new Color(0.015f, 0.04f, 0.09f, 0.9f)
                : new Color(0.12f, 0.015f, 0.005f, 0.9f);
            if (isDeletePreviewActive)
            {
                levelLabel.color = new Color(1f, 0.76f, 0.76f,
                    levelLabel.color.a);
            }
        }

        private void RefreshBottomPlateVisual()
        {
            if (background == null || Instance == null ||
                Instance.Data == null)
            {
                return;
            }

            BackpackVisualSettings settings =
                BackpackVisualDebugRuntime.CurrentSettings;
            Color baseColor = GetBottomPlateTint(
                Instance.Data.BackgroundColor,
                Instance.Level,
                settings.OverridesEnabled && settings.AircraftGlowEnabled,
                IsColorQualityModeActive(settings));
            background.color = isDeletePreviewActive
                ? new Color(1f, 0.08f, 0.08f, baseColor.a)
                : baseColor;
        }

        public static Color GetBottomPlateColor(
            Color originalColor,
            int itemLevel,
            bool bottomPlateHighlightEnabled)
        {
            if (!bottomPlateHighlightEnabled)
            {
                return originalColor;
            }

            return Mathf.Clamp(itemLevel, ItemInstance.DefaultLevel,
                ItemInstance.MaximumLevel) switch
            {
                1 => new Color(1f, .8f, .5019608f, 1f),
                2 => new Color(.5058824f, .7803922f, .5176471f, 1f),
                _ => new Color(.65882355f, .33333334f, .96862745f, 1f)
            };
        }

        public static Color GetBottomPlateTint(
            Color originalColor,
            int itemLevel,
            bool bottomPlateHighlightEnabled,
            bool colorQualityModeEnabled) =>
            colorQualityModeEnabled
                ? Color.white
                : GetBottomPlateColor(originalColor, itemLevel,
                    bottomPlateHighlightEnabled);

        public static bool ShouldHideEquipmentLevelLabel(
            bool bottomPlateHighlightEnabled,
            ItemType itemType) =>
            bottomPlateHighlightEnabled && itemType == ItemType.Equipment;

        private void CacheLevelLabelFontSize()
        {
            if (levelLabel == null || levelLabelFontSizeCached)
            {
                return;
            }

            defaultLevelLabelFontSize = levelLabel.fontSize;
            levelLabelFontSizeCached = true;
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
