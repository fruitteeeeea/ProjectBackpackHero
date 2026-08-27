using System;
using System.Collections;
using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackHero.Input;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    [Serializable]
    public readonly struct BackpackLayoutItem
    {
        public BackpackLayoutItem(
            ItemData data,
            Vector2Int anchorCell,
            int level = ItemInstance.DefaultLevel)
        {
            Data = data;
            AnchorCell = anchorCell;
            Level = Mathf.Clamp(
                level,
                ItemInstance.DefaultLevel,
                ItemInstance.MaximumLevel);
        }

        public ItemData Data { get; }
        public Vector2Int AnchorCell { get; }
        public int Level { get; }
    }

    /// <summary>
    /// 玩家背包的正式运行时入口。统一管理背包模型、UI、商店、
    /// 战斗冷却、飞机生成和外部飞行曲线输入。
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(BackpackCombatController))]
    [RequireComponent(typeof(BackpackFighterSpawner))]
    [DisallowMultipleComponent]
    public sealed class PlayerBackpackSystem : MonoBehaviour
    {
        private const string AircraftAnchorName =
            "AircraftSpawnAnchor";
        private const string CollisionAnchorName =
            "CollisionCenterAnchor";
        private const int ShopRollItemCount = 3;
        private const int CompactShopItemCount = 4;
        private const int MaximumAircraftAppearancesPerPreparation = 2;
        private const float ShopHorizontalPadding = 24f;
        private const float CompactShopItemSpacing = 64f;
        private const float PreferredShopCompositionChance = .6f;
        private const float PreviousShopItemWeightMultiplier = .1f;

        [Header("Backpack UI")]
        [SerializeField]
        private BackpackGridView gridView;

        [SerializeField]
        private RectTransform itemLayer;

        [SerializeField]
        private RectTransform dragLayer;

        [SerializeField]
        private RectTransform trashZone;

        [SerializeField]
        private List<RectTransform> shopSlots = new();

        [Header("Item View")]
        [SerializeField]
        private ItemView itemViewPrefab;
        [SerializeField] private ItemView itemView1x2Prefab;
        [SerializeField] private ItemView itemView2x1Prefab;
        [SerializeField] private ItemView itemViewLMissingBottomLeftPrefab;
        [SerializeField] private ItemView itemViewLMissingBottomRightPrefab;
        [SerializeField] private ItemView itemViewLMissingTopLeftPrefab;
        [SerializeField] private ItemView itemViewLMissingTopRightPrefab;

        [Header("Shop Roll")]
        [SerializeField, Min(1)]
        private int rollsPerPreparation = 1;

        [Header("Optional Scene Input")]
        [SerializeField]
        private HorizontalSwipeCurveInput curveInput;

        [Header("Battle Curve")]
        [SerializeField]
        private CurvedConnectionRenderer adjustmentCurve;

        [Header("Battle World Anchors")]
        [SerializeField]
        private RectTransform aircraftSpawnAnchor;

        [SerializeField]
        private RectTransform collisionCenterAnchor;

        [SerializeField]
        private float battleWorldPlaneZ;

        private readonly List<ItemView> shopItems = new();
        private readonly List<ItemView> backpackViews = new();

        private BackpackCombatController combatController;
        private BackpackFighterSpawner fighterSpawner;
        private PlayerItemSystem playerItemSystem;
        private List<ItemData> runtimeDeckItems;
        private PlayerBackpackDragDebugOverlay dragDebugOverlay;
        private ItemView draggingItem;
        private bool isReady;
        private bool isLoadingLayout;
        private bool missingCurveWarningReported;
        private bool shopInitializedForPreparation;
        private bool shopItemScaleCached;
        private Vector3 shopItemLocalScale = Vector3.one;
        private RectTransform shopDropZone;
        private RectTransform shopContainer;
        private readonly HashSet<ItemInstance> pendingShopTransfers = new();
        private readonly Dictionary<ItemInstance, Vector3>
            pendingShopTransferStartPositions = new();
        private readonly Dictionary<string, int>
            aircraftAppearancesThisPreparation = new();
        private int nextItemId;
        private int remainingRolls;
        private int debugProgressionLevel = -1;
        private Coroutine debugAutoOperationRoutine;
        private bool debugAutoOperationRunning;
        private int debugAutoOperationSuccessCount;
        private string debugAutoOperationStatus;
        private string debugAutoOperationName;

        public static bool IsAnyDebugAutoOperationRunning { get; private set; }

        /// <summary>Roll 次数变化后通知商店 UI 刷新显示。</summary>
        public event Action RollStateChanged;

        /// <summary>拖拽物品变化后通知物品信息 UI 刷新。</summary>
        public event Action<ItemView> SelectedItemChanged;

        public BackpackController Backpack =>
            combatController != null
                ? combatController.Backpack
                : null;

        public IReadOnlyList<ItemInstance> Items =>
            Backpack != null
                ? Backpack.Items
                : Array.Empty<ItemInstance>();

        public BackpackCombatController CombatController =>
            combatController;

        public BackpackFighterSpawner FighterSpawner =>
            fighterSpawner;

        public HorizontalSwipeCurveInput FlightCurveInput =>
            curveInput;

        public RectTransform AircraftSpawnAnchor =>
            aircraftSpawnAnchor;

        public RectTransform CollisionCenterAnchor =>
            collisionCenterAnchor;

        public ItemView SelectedItem { get; private set; }

        public bool IsReady => isReady;
        public bool IsDebugAutoOperationRunning => debugAutoOperationRunning;
        public int DebugAutoOperationSuccessCount => debugAutoOperationSuccessCount;
        public string DebugAutoOperationStatus => debugAutoOperationStatus;
        public string DebugAutoOperationName => debugAutoOperationName;

        public int RollsPerPreparation =>
            Mathf.Max(1, rollsPerPreparation);

        public int RemainingRolls => remainingRolls;
        public bool HasDebugProgressionLevel => debugProgressionLevel >= ItemInstance.DefaultLevel;
        public int ActiveProgressionLevel => HasDebugProgressionLevel
            ? debugProgressionLevel : PlayerItemSystem.DefaultLevel;

        /// <summary>仅覆写当前对局背包实例，不写入玩家养成存档。</summary>
        public bool SetDebugProgressionLevel(int level)
        {
            if (!isReady || Backpack == null) return false;
            debugProgressionLevel = Mathf.Clamp(level, ItemInstance.DefaultLevel, ItemInstance.MaximumLevel);
            ApplyDebugProgressionLevel();
            return true;
        }

        public void ClearDebugProgressionLevel()
        {
            debugProgressionLevel = -1;
            foreach (ItemInstance item in Items)
                item?.SetProgressionLevel(PlayerItemSystem.Instance?.GetLevel(item.Data) ?? PlayerItemSystem.DefaultLevel);
        }

        public bool TryGetDebugProgressionLevel(out int level)
        {
            level = debugProgressionLevel;
            return HasDebugProgressionLevel;
        }

        private void ApplyDebugProgressionLevel()
        {
            if (!HasDebugProgressionLevel) return;
            foreach (ItemInstance item in Items) item?.SetProgressionLevel(debugProgressionLevel);
        }

        public IReadOnlyList<ItemData> ActiveDeckItems =>
            runtimeDeckItems ??
            playerItemSystem?.GetDeckItems() ??
            Array.Empty<ItemData>();

        public bool CanRollShop =>
            isReady &&
            BattleFlowController.CurrentPhase ==
            BattlePhase.Preparation &&
            remainingRolls > 0;

        private void Awake()
        {
            combatController =
                GetComponent<BackpackCombatController>();
            fighterSpawner =
                GetComponent<BackpackFighterSpawner>();
            playerItemSystem = PlayerItemSystem.Instance;
            if (playerItemSystem != null)
            {
                playerItemSystem.Changed += HandlePlayerItemsChanged;
            }

            ResolveBattleAnchors();
            HideAnchorGraphics();

            isReady = ValidateConfiguration();

            if (gridView != null)
            {
                dragDebugOverlay =
                    PlayerBackpackDragDebugOverlay.Create(gridView);
            }

            if (Backpack != null)
            {
                Backpack.ItemAdded += HandleModelItemAdded;
                Backpack.ItemMoved += HandleModelItemMoved;
                Backpack.ItemRemoved += HandleModelItemRemoved;
            }

            BindCurveInput(curveInput);
        }

        private void OnEnable()
        {
            BattleFlowController.PhaseChanged +=
                HandlePhaseChanged;
        }

        private void Start()
        {
            if (!isReady)
            {
                return;
            }

            RestoreDeckLayout();
            InitializeShopContainer();
            CacheShopItemScale();
            RebuildBackpackViews();
            InitializeShopForPreparation();

            HandlePhaseChanged(
                BattleFlowController.CurrentPhase);
        }

        public bool LoadLayout(
            IReadOnlyList<BackpackLayoutItem> layout)
        {
            if (!isReady ||
                BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation ||
                layout == null)
            {
                return false;
            }

            BackpackController validation =
                new BackpackController(
                    Backpack.Width,
                    Backpack.Height);

            for (int index = 0;
                 index < layout.Count;
                 index++)
            {
                BackpackLayoutItem placement =
                    layout[index];

                if (placement.Data == null)
                {
                    Debug.LogError(
                        $"布局条目 {index} 缺少物品或UI Prefab配置。",
                        this);
                    return false;
                }

                ItemInstance candidate =
                    new ItemInstance(
                        $"layout-validation-{index}",
                        placement.Data,
                        placement.AnchorCell,
                        placement.Level);

                if (!validation.PlaceItem(
                        candidate,
                        placement.AnchorCell))
                {
                    Debug.LogError(
                        $"布局条目 {index} 无法放置在 " +
                        $"{placement.AnchorCell}。",
                        this);
                    return false;
                }
            }

            isLoadingLayout = true;

            try
            {
                combatController.Clear();

                foreach (BackpackLayoutItem placement
                         in layout)
                {
                    combatController.AddItem(
                        placement.Data,
                        placement.AnchorCell,
                        placement.Level);
                }
            }
            finally
            {
                isLoadingLayout = false;
            }

            if (Application.isPlaying)
            {
                RebuildBackpackViews();
            }

            SetSelectedItem(null);
            return true;
        }

        public void RestoreDefaultLayout()
        {
            if (!isReady ||
                BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation)
            {
                return;
            }

            RestoreDeckLayout();
        }

        public bool ApplyDeckPreset(DeckPreset preset)
        {
            if (!isReady ||
                BattleFlowController.CurrentPhase != BattlePhase.Preparation ||
                preset == null ||
                !preset.IsValid(out _))
            {
                return false;
            }

            PlayerItemSystem items = PlayerItemSystem.Instance;
            if (items == null ||
                items.TryApplyDeck(preset.Slots) != PlayerDeckResult.Success)
            {
                return false;
            }

            runtimeDeckItems = null;
            RestoreDeckLayout();
            return true;
        }

        public bool CanApplyRuntimeDeck(
            IReadOnlyList<ItemData> deck)
        {
            return isReady &&
                   Backpack != null &&
                   DeckPreset.IsValidSlots(deck, out _) &&
                   DeckLayoutBuilder.TryBuild(
                       deck,
                       Backpack.Width,
                       Backpack.Height,
                       null,
                       out _);
        }

        /// <summary>调试用：复用敌方初始摆放规划器安排当前玩家 Deck。</summary>
        public bool RandomizeInitialPlacementWithEnemyAI()
        {
            if (!isReady || Backpack == null ||
                BattleFlowController.CurrentPhase != BattlePhase.Preparation ||
                !EnemyBackpackLayoutPlanner.TryBuild(
                    ActiveDeckItems, Backpack.Width, Backpack.Height,
                    out List<BackpackLayoutItem> layout))
            {
                return false;
            }

            return LoadLayout(layout);
        }

        /// <summary>调试用：先做一次初始布局，再以真实商店/背包操作执行最多 15 次 AI 决策。</summary>
        public bool StartDebugAutoOperations(int operationCount = 15, float interval = .3f)
        {
            if (debugAutoOperationRunning || !isReady || Backpack == null ||
                BattleFlowController.CurrentPhase != BattlePhase.Preparation ||
                !RandomizeInitialPlacementWithEnemyAI()) return false;
            debugAutoOperationSuccessCount = 0;
            debugAutoOperationStatus = "正在规划操作";
            debugAutoOperationName = "初始布局";
            // StartCoroutine 要到下一帧才开始；这里先上锁，避免同一帧进入战斗。
            debugAutoOperationRunning = true;
            IsAnyDebugAutoOperationRunning = true;
            debugAutoOperationRoutine = StartCoroutine(RunDebugAutoOperations(Mathf.Max(1, operationCount), Mathf.Max(.05f, interval)));
            return true;
        }

        public void CancelDebugAutoOperations(string reason = "已取消")
        {
            if (debugAutoOperationRoutine != null) StopCoroutine(debugAutoOperationRoutine);
            debugAutoOperationRoutine = null;
            debugAutoOperationRunning = false;
            IsAnyDebugAutoOperationRunning = false;
            debugAutoOperationStatus = reason;
            debugAutoOperationName = null;
        }

        private IEnumerator RunDebugAutoOperations(int targetCount, float interval)
        {
            while (debugAutoOperationSuccessCount < targetCount &&
                   BattleFlowController.CurrentPhase == BattlePhase.Preparation)
            {
                if (!BackpackOperationPlanner.TrySelectBest(Backpack, GetShopItemData(), remainingRolls, out BackpackOperation operation))
                {
                    debugAutoOperationStatus = "没有可执行的增益操作";
                    break;
                }
                debugAutoOperationName = DescribeDebugOperation(operation);
                if (TryExecuteDebugOperation(operation))
                {
                    debugAutoOperationSuccessCount++;
                    debugAutoOperationStatus = $"已完成 {debugAutoOperationSuccessCount} / {targetCount}";
                }
                else
                {
                    // 执行前后模型状态若被其他正式逻辑改变，不能把失败操作
                    // 误计数，也不应无限尝试同一个已失效候选。
                    debugAutoOperationStatus = "操作执行失败";
                    break;
                }
                yield return new WaitForSecondsRealtime(interval);
            }
            if (debugAutoOperationSuccessCount >= targetCount) debugAutoOperationStatus = $"已完成 {targetCount} 次操作";
            // 给最后一批重叠 Tween 留出收束时间后再允许进入战斗。
            yield return new WaitForSecondsRealtime(.65f);
            CancelDebugAutoOperations(debugAutoOperationStatus ?? "已结束");
        }

        private List<ItemData> GetShopItemData()
        {
            List<ItemData> result = new();
            foreach (ItemView view in shopItems) if (view?.Instance?.Data != null) result.Add(view.Instance.Data);
            return result;
        }

        private static string DescribeDebugOperation(BackpackOperation operation)
        {
            return operation?.Kind switch
            {
                BackpackOperationKind.RollShop => "刷新商店",
                BackpackOperationKind.MoveItem => "移动物品",
                BackpackOperationKind.AddShopItem => "从商店放置",
                BackpackOperationKind.RemoveItem => "移除物品",
                BackpackOperationKind.MergeItems => "合成物品",
                BackpackOperationKind.ReplaceItem => "替换物品",
                _ => "规划中",
            };
        }

        private bool TryExecuteDebugOperation(BackpackOperation operation)
        {
            switch (operation.Kind)
            {
                case BackpackOperationKind.RollShop: return TryRefreshShop();
                case BackpackOperationKind.MoveItem: return combatController.MoveItem(operation.Item, operation.Destination);
                case BackpackOperationKind.AddShopItem: return TryAutoPlaceShopItem(operation.ShopItem, operation.Destination);
                case BackpackOperationKind.RemoveItem: return combatController.RemoveItem(operation.Item);
                case BackpackOperationKind.MergeItems:
                    if (!Backpack.TryMerge(operation.Item, operation.SecondaryItem)) return false;
                    FindView(operation.SecondaryItem)?.PlayMergeFeedback(); return true;
                case BackpackOperationKind.ReplaceItem:
                    return combatController.RemoveItem(operation.Item) && TryAutoPlaceShopItem(operation.ShopItem, operation.Destination);
                default: return false;
            }
        }

        private bool TryAutoPlaceShopItem(ItemData data, Vector2Int cell)
        {
            ItemView view = shopItems.Find(candidate => candidate?.Instance?.Data == data);
            return view != null && combatController.PlaceItem(view.Instance, cell);
        }

        public bool TryApplyRuntimeDeck(
            IReadOnlyList<ItemData> deck)
        {
            if (BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation ||
                !CanApplyRuntimeDeck(deck) ||
                !DeckLayoutBuilder.TryBuild(
                    deck,
                    Backpack.Width,
                    Backpack.Height,
                    null,
                    out List<BackpackLayoutItem> layout))
            {
                return false;
            }

            if (!LoadLayout(layout))
            {
                return false;
            }

            runtimeDeckItems = new List<ItemData>(deck);
            RefreshShopInternal();
            return true;
        }

        private void RestoreDeckLayout()
        {
            IReadOnlyList<ItemData> deck = ActiveDeckItems;
            if (deck.Count == 0)
            {
                combatController.RestoreDefaultLayout();
                RebuildBackpackViews();
                SetSelectedItem(null);
                return;
            }

            if (!DeckLayoutBuilder.TryBuild(
                    deck,
                    Backpack.Width,
                    Backpack.Height,
                    null,
                    out List<BackpackLayoutItem> layout))
            {
                Debug.LogError("当前 Deck 无法完整放入背包。", this);
                return;
            }

            if (!LoadLayout(layout))
            {
                combatController.RestoreDefaultLayout();
                RebuildBackpackViews();
                SetSelectedItem(null);
            }
        }

        private static bool TryFindAvailableCell(
            BackpackController validation,
            ItemData item,
            ref int placementId,
            out Vector2Int cell)
        {
            for (int y = 0; y < validation.Height; y++)
            {
                for (int x = 0; x < validation.Width; x++)
                {
                    cell = new Vector2Int(x, y);
                    var candidate = new ItemInstance(
                        $"deck-layout-{++placementId}",
                        item,
                        cell);
                    if (validation.PlaceItem(candidate, cell))
                    {
                        return true;
                    }
                }
            }

            cell = default;
            Debug.LogWarning(
                $"Player Deck item '{item.ItemName}' does not fit in the default backpack layout.");
            return false;
        }

        /// <summary>
        /// 尝试消耗一次 Roll 刷新商店。仅准备阶段且尚有次数时有效。
        /// </summary>
        public bool TryRefreshShop()
        {
            if (!CanRollShop || !RefreshShopInternal())
            {
                return false;
            }

            remainingRolls--;
            NotifyRollStateChanged();
            return true;
        }

        // 保留给 Unity Button 事件和现有调用点使用。
        public void RefreshShop()
        {
            TryRefreshShop();
        }

        /// <summary>恢复本准备阶段的 Roll 次数，但不刷新商店。</summary>
        public bool ResetRolls()
        {
            if (!isReady)
            {
                return false;
            }

            remainingRolls = RollsPerPreparation;
            NotifyRollStateChanged();
            return true;
        }

        private bool RefreshShopInternal()
        {
            InitializeShopContainer();
            if (shopContainer == null)
            {
                return false;
            }

            List<ItemData> deckCatalog = new();
            foreach (ItemData item in ActiveDeckItems)
                if (item != null) deckCatalog.Add(item);

            if (deckCatalog.Count == 0)
            {
                Debug.LogWarning(
                    "Player Deck is empty; shop roll skipped.",
                    this);
                ClearShopViews();
                SetSelectedItem(null);
                return false;
            }

            List<ItemData> previousShopItems = GetCurrentShopItemData();
            List<ItemData> nextShopItems = BuildShopRoll(
                deckCatalog,
                previousShopItems,
                aircraftAppearancesThisPreparation);

            ClearShopViews();
            foreach (ItemData item in nextShopItems)
            {
                CreateShopItem(item);
            }

            RecordAircraftAppearances(
                nextShopItems,
                aircraftAppearancesThisPreparation);
            SetSelectedItem(null);
            return true;
        }

        private List<ItemData> GetCurrentShopItemData()
        {
            List<ItemData> currentItems = new();
            foreach (ItemView view in shopItems)
            {
                ItemData data = view?.Instance?.Data;
                if (data != null)
                {
                    currentItems.Add(data);
                }
            }

            return currentItems;
        }

        /// <summary>
        /// 生成一轮商店候选：60% 概率包含两架飞机和一件装备；
        /// 上一轮出现过的物品保留为候选，但其权重降至 10%。
        /// </summary>
        private static List<ItemData> BuildShopRoll(
            IReadOnlyList<ItemData> deckCatalog,
            IReadOnlyCollection<ItemData> previousShopItems,
            IReadOnlyDictionary<string, int>
                aircraftAppearancesThisPreparation)
        {
            List<ItemData> selected = new(ShopRollItemCount);
            List<ItemData> aircraft = new();
            List<ItemData> equipment = new();

            foreach (ItemData item in deckCatalog)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.ItemType == ItemType.Aircraft)
                {
                    aircraft.Add(item);
                }
                else if (item.ItemType == ItemType.Equipment)
                {
                    equipment.Add(item);
                }
            }

            bool usePreferredComposition =
                aircraft.Count > 0 && equipment.Count > 0 &&
                UnityEngine.Random.value <
                PreferredShopCompositionChance;
            if (usePreferredComposition)
            {
                AddWeightedItem(
                    aircraft,
                    previousShopItems,
                    aircraftAppearancesThisPreparation,
                    selected);
                AddWeightedItem(
                    aircraft,
                    previousShopItems,
                    aircraftAppearancesThisPreparation,
                    selected);
                AddWeightedItem(
                    equipment,
                    previousShopItems,
                    aircraftAppearancesThisPreparation,
                    selected);
            }

            while (selected.Count < ShopRollItemCount)
            {
                if (!AddWeightedItem(
                        deckCatalog,
                        previousShopItems,
                        aircraftAppearancesThisPreparation,
                        selected))
                {
                    break;
                }
            }

            return selected;
        }

        private static bool AddWeightedItem(
            IReadOnlyList<ItemData> candidates,
            IReadOnlyCollection<ItemData> previousShopItems,
            IReadOnlyDictionary<string, int>
                aircraftAppearancesThisPreparation,
            List<ItemData> selected)
        {
            float totalWeight = 0f;
            foreach (ItemData candidate in candidates)
            {
                if (CanAddShopItem(
                        candidate,
                        selected,
                        aircraftAppearancesThisPreparation))
                {
                    totalWeight += WasInPreviousShop(
                        candidate,
                        previousShopItems)
                        ? PreviousShopItemWeightMultiplier
                        : 1f;
                }
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            float roll = UnityEngine.Random.value * totalWeight;
            foreach (ItemData candidate in candidates)
            {
                if (!CanAddShopItem(
                        candidate,
                        selected,
                        aircraftAppearancesThisPreparation))
                {
                    continue;
                }

                roll -= WasInPreviousShop(
                    candidate,
                    previousShopItems)
                    ? PreviousShopItemWeightMultiplier
                    : 1f;
                if (roll <= 0f)
                {
                    selected.Add(candidate);
                    return true;
                }
            }

            return false;
        }

        private static bool WasInPreviousShop(
            ItemData candidate,
            IReadOnlyCollection<ItemData> previousShopItems)
        {
            foreach (ItemData previousItem in previousShopItems)
            {
                if (previousItem == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanAddShopItem(
            ItemData candidate,
            IReadOnlyList<ItemData> selected,
            IReadOnlyDictionary<string, int>
                aircraftAppearancesThisPreparation)
        {
            if (candidate == null)
            {
                return false;
            }

            if (candidate.ItemType == ItemType.Aircraft &&
                GetAircraftAppearanceCount(
                    candidate,
                    selected,
                    aircraftAppearancesThisPreparation) >=
                MaximumAircraftAppearancesPerPreparation)
            {
                return false;
            }

            int duplicateCount = 0;
            foreach (ItemData item in selected)
            {
                if (item == candidate && ++duplicateCount >= 2)
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetAircraftAppearanceCount(
            ItemData candidate,
            IReadOnlyList<ItemData> selected,
            IReadOnlyDictionary<string, int>
                aircraftAppearancesThisPreparation)
        {
            string itemId = candidate.ItemId;
            int count = 0;
            if (!string.IsNullOrEmpty(itemId) &&
                aircraftAppearancesThisPreparation != null)
            {
                aircraftAppearancesThisPreparation.TryGetValue(
                    itemId,
                    out count);
            }

            foreach (ItemData item in selected)
            {
                if (item != null &&
                    item.ItemType == ItemType.Aircraft &&
                    item.ItemId == itemId)
                {
                    count++;
                }
            }

            return count;
        }

        private static void RecordAircraftAppearances(
            IReadOnlyList<ItemData> items,
            IDictionary<string, int> aircraftAppearances)
        {
            if (items == null || aircraftAppearances == null)
            {
                return;
            }

            foreach (ItemData item in items)
            {
                if (item == null ||
                    item.ItemType != ItemType.Aircraft ||
                    string.IsNullOrEmpty(item.ItemId))
                {
                    continue;
                }

                aircraftAppearances.TryGetValue(
                    item.ItemId,
                    out int count);
                aircraftAppearances[item.ItemId] = count + 1;
            }
        }

        public void EnterCombat()
        {
            LevelFlowController.EnsureInstance()
                ?.RequestStartRound();
        }

        public bool CompleteCombatTransitionAfterMotion()
        {
            if (BattleFlowController.CurrentPhase !=
                BattlePhase.CombatTransition)
            {
                return false;
            }

            if (!SynchronizeBattleWorldAnchors())
            {
                Debug.LogError(
                    "背包FEEL动画已经结束，但UI锚点无法换算到" +
                    "战斗世界，战斗保持在过渡阶段。",
                    this);
                return false;
            }

            return BattleFlowController.EnsureInstance()
                .CompleteCombatTransition();
        }

        public bool SynchronizeBattleWorldAnchors()
        {
            ResolveBattleAnchors();

            Camera worldCamera = Camera.main;
            EnemyBackpackSystem enemySystem =
                FindEnemyBackpackSystem();
            BackpackCombatController enemy =
                enemySystem != null
                    ? enemySystem.CombatController
                    : null;
            if (aircraftSpawnAnchor == null ||
                collisionCenterAnchor == null ||
                worldCamera == null ||
                combatController == null ||
                fighterSpawner == null ||
                enemy == null ||
                enemy.FighterSpawner == null ||
                enemySystem.AircraftSpawnAnchor == null ||
                enemySystem.CollisionCenterAnchor == null ||
                adjustmentCurve == null)
            {
                return false;
            }

            Canvas.ForceUpdateCanvases();

            Camera uiCamera =
                GetCanvasCamera(aircraftSpawnAnchor);
            Vector2 playerSpawnScreen =
                RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    aircraftSpawnAnchor.position);
            Vector2 playerCenterScreen =
                RectTransformUtility.WorldToScreenPoint(
                    GetCanvasCamera(collisionCenterAnchor),
                    collisionCenterAnchor.position);

            Vector2 enemySpawnScreen =
                RectTransformUtility.WorldToScreenPoint(
                    GetCanvasCamera(
                        enemySystem.AircraftSpawnAnchor),
                    enemySystem.AircraftSpawnAnchor.position);
            Vector2 enemyCenterScreen =
                RectTransformUtility.WorldToScreenPoint(
                    GetCanvasCamera(
                        enemySystem.CollisionCenterAnchor),
                    enemySystem.CollisionCenterAnchor.position);

            if (!TryScreenPointToWorldOnPlane(
                    worldCamera,
                    playerSpawnScreen,
                    battleWorldPlaneZ,
                    out Vector3 playerSpawnWorld) ||
                !TryScreenPointToWorldOnPlane(
                    worldCamera,
                    playerCenterScreen,
                    battleWorldPlaneZ,
                    out Vector3 playerCenterWorld) ||
                !TryScreenPointToWorldOnPlane(
                    worldCamera,
                    enemySpawnScreen,
                    battleWorldPlaneZ,
                    out Vector3 enemySpawnWorld) ||
                !TryScreenPointToWorldOnPlane(
                    worldCamera,
                    enemyCenterScreen,
                    battleWorldPlaneZ,
                    out Vector3 enemyCenterWorld))
            {
                return false;
            }

            combatController.transform.position =
                playerCenterWorld;
            enemy.transform.position = enemyCenterWorld;

            if (!fighterSpawner.SetSpawnPointWorldPosition(
                    playerSpawnWorld) ||
                !enemy.FighterSpawner
                    .SetSpawnPointWorldPosition(
                        enemySpawnWorld))
            {
                return false;
            }

            fighterSpawner.SetMaximumBendDistance(
                adjustmentCurve.MaxBendDistance);
            enemy.FighterSpawner.SetMaximumBendDistance(
                adjustmentCurve.MaxBendDistance);

            adjustmentCurve.SetBattleWorldEndpoints(
                fighterSpawner.SpawnPoint,
                enemy.FighterSpawner.SpawnPoint);

            return true;
        }

        public static Vector2 MirrorScreenPointVertically(
            Vector2 screenPoint,
            float screenHeight)
        {
            return new Vector2(
                screenPoint.x,
                Mathf.Max(0f, screenHeight) -
                screenPoint.y);
        }

        public static bool TryScreenPointToWorldOnPlane(
            Camera camera,
            Vector2 screenPoint,
            float planeZ,
            out Vector3 worldPoint)
        {
            worldPoint = default;

            if (camera == null)
            {
                return false;
            }

            Plane plane =
                new Plane(
                    Vector3.forward,
                    new Vector3(0f, 0f, planeZ));
            Ray ray = camera.ScreenPointToRay(screenPoint);

            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            worldPoint = ray.GetPoint(distance);
            worldPoint.z = planeZ;
            return true;
        }

        public void BindCurveInput(
            HorizontalSwipeCurveInput input)
        {
            if (curveInput != null)
            {
                curveInput.ValueChanged -=
                    SetFlightCurveValue;
            }

            curveInput = input;

            if (curveInput != null)
            {
                curveInput.ValueChanged +=
                    SetFlightCurveValue;
                SetFlightCurveValue(
                    curveInput.CurrentValue);
            }
            else
            {
                SetFlightCurveValue(0f);
            }

            UpdateCurveInputForPhase(
                BattleFlowController.CurrentPhase);
        }

        public void SetFlightCurveValue(float value)
        {
            float clampedValue = Mathf.Clamp(value, -1f, 1f);
            fighterSpawner?.SetCurveValue(clampedValue);
            adjustmentCurve?.SetCurveValue(clampedValue);
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase != BattlePhase.Preparation && debugAutoOperationRunning)
            {
                CancelDebugAutoOperations("已离开准备阶段");
            }

            if (phase == BattlePhase.Preparation)
            {
                InitializeShopForPreparation();
            }
            else
            {
                shopInitializedForPreparation = false;
            }

            bool interactionEnabled =
                phase == BattlePhase.Preparation;

            foreach (ItemView view in backpackViews)
            {
                view?.SetInteractionEnabled(
                    interactionEnabled);
            }

            foreach (ItemView view in shopItems)
            {
                view?.SetInteractionEnabled(
                    interactionEnabled);
            }

            UpdateCurveInputForPhase(phase);
        }

        private void InitializeShopForPreparation()
        {
            if (!isReady ||
                BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation ||
                shopInitializedForPreparation)
            {
                return;
            }

            shopInitializedForPreparation = true;
            aircraftAppearancesThisPreparation.Clear();
            remainingRolls = RollsPerPreparation;
            RefreshShopInternal();
            NotifyRollStateChanged();
        }

        private void NotifyRollStateChanged()
        {
            RollStateChanged?.Invoke();
        }

        private BackpackCombatController
            FindEnemyCombatController()
        {
            foreach (BackpackCombatController candidate
                     in BackpackCombatController
                         .ActiveControllers)
            {
                if (candidate != null &&
                    candidate != combatController &&
                    candidate.Faction !=
                    combatController.Faction)
                {
                    return candidate;
                }
            }

            return null;
        }

        private EnemyBackpackSystem
            FindEnemyBackpackSystem()
        {
            foreach (EnemyBackpackSystem candidate in
                     FindObjectsByType<EnemyBackpackSystem>(
                         FindObjectsInactive.Include))
            {
                if (candidate != null &&
                    candidate.CombatController != null &&
                    candidate.CombatController.Faction !=
                    combatController.Faction)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void ResolveBattleAnchors()
        {
            if (aircraftSpawnAnchor != null &&
                collisionCenterAnchor != null)
            {
                return;
            }

            foreach (RectTransform rectTransform in
                     GetComponentsInChildren<
                         RectTransform>(true))
            {
                if (rectTransform.name ==
                    AircraftAnchorName)
                {
                    aircraftSpawnAnchor = rectTransform;
                }
                else if (rectTransform.name ==
                         CollisionAnchorName)
                {
                    collisionCenterAnchor =
                        rectTransform;
                }
            }
        }

        private void HideAnchorGraphics()
        {
            SetAnchorGraphicVisible(
                aircraftSpawnAnchor,
                false);
            SetAnchorGraphicVisible(
                collisionCenterAnchor,
                false);
        }

        private static void SetAnchorGraphicVisible(
            RectTransform anchor,
            bool visible)
        {
            if (anchor != null &&
                anchor.TryGetComponent(
                    out Graphic graphic))
            {
                graphic.enabled = visible;
            }
        }

        private static Camera GetCanvasCamera(
            RectTransform anchor)
        {
            Canvas canvas =
                anchor != null
                    ? anchor.GetComponentInParent<
                        Canvas>()
                    : null;

            return canvas != null &&
                   canvas.renderMode !=
                   RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        }

        private void UpdateCurveInputForPhase(
            BattlePhase phase)
        {
            bool combat =
                phase == BattlePhase.Combat;

            if (curveInput != null)
            {
                curveInput.SetInputEnabled(combat);
                return;
            }

            SetFlightCurveValue(0f);

            if (combat &&
                !missingCurveWarningReported)
            {
                missingCurveWarningReported = true;
                Debug.LogWarning(
                    "PlayerBackpackSystem没有绑定曲线输入，" +
                    "飞机将使用0（垂直）曲线。",
                    this);
            }
        }

        private void RebuildBackpackViews()
        {
            ClearBackpackViews();

            if (Backpack == null)
            {
                return;
            }

            foreach (ItemInstance item in Backpack.Items)
            {
                CreateBackpackView(item);
            }
        }

        private ItemView CreateBackpackView(
            ItemInstance item)
        {
            if (item == null ||
                FindView(item) != null)
            {
                return FindView(item);
            }

            ItemData data = item.Data;

            if (data == null)
            {
                Debug.LogError(
                    $"找不到 {item.Data?.name} 的UI Prefab。",
                    this);
                return null;
            }

            ItemView view =
                CreateView(data, itemLayer, item);

            if (view == null)
            {
                return null;
            }

            view.SetBackpackPosition(item.AnchorCell);
            backpackViews.Add(view);
            return view;
        }

        private void CreateShopItem(ItemData data)
        {
            ItemView view =
                CreateView(data, shopContainer);

            if (view == null)
            {
                return;
            }

            shopItems.Add(view);
            ReflowShopItems();
        }

        private Vector3 GetShopItemScale(
            RectTransform shopSlot)
        {
            if (!shopItemScaleCached)
            {
                CacheShopItemScale(shopSlot);
            }

            return shopItemLocalScale;
        }

        private void CacheShopItemScale(
            RectTransform shopSlot = null)
        {
            if (shopItemScaleCached)
            {
                return;
            }

            shopSlot ??= FindFirstShopSlot();

            if (itemLayer == null || shopSlot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            Vector3 backpackScale = itemLayer.lossyScale;
            Vector3 shopScale = shopSlot.lossyScale;

            shopItemLocalScale = new Vector3(
                DivideScale(backpackScale.x, shopScale.x),
                DivideScale(backpackScale.y, shopScale.y),
                1f);
            shopItemScaleCached = true;
        }

        private void InitializeShopContainer()
        {
            if (shopContainer != null)
            {
                return;
            }

            shopDropZone = FindFirstShopSlot()?.parent as RectTransform;
            if (shopDropZone == null)
            {
                return;
            }

            HorizontalLayoutGroup layout =
                shopDropZone.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
            }

            foreach (RectTransform slot in shopSlots)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(false);
                }
            }

            var containerObject = new GameObject(
                "ShopItemContainer",
                typeof(RectTransform));
            shopContainer =
                containerObject.GetComponent<RectTransform>();
            shopContainer.SetParent(shopDropZone, false);
            shopContainer.anchorMin = Vector2.zero;
            shopContainer.anchorMax = Vector2.one;
            shopContainer.offsetMin = Vector2.zero;
            shopContainer.offsetMax = Vector2.zero;
            shopContainer.pivot = new Vector2(.5f, .5f);

            foreach (ItemView view in backpackViews)
            {
                view?.SetShopDropZone(shopDropZone);
            }

            foreach (ItemView view in shopItems)
            {
                view?.SetShopDropZone(shopDropZone);
            }
        }

        private void ReflowShopItems(
            ItemView tweeningView = null,
            Vector3? tweenStartWorldPosition = null)
        {
            if (shopContainer == null || shopItems.Count == 0)
            {
                return;
            }

            float availableWidth = Mathf.Max(
                0f,
                shopContainer.rect.width - ShopHorizontalPadding * 2f);
            float widestItem = 0f;
            foreach (ItemView view in shopItems)
            {
                if (view != null)
                {
                    widestItem = Mathf.Max(
                        widestItem,
                        view.RectTransform.rect.width);
                }
            }

            Vector3 itemScale = shopItemLocalScale;
            float scaledWidth = widestItem;
            bool compactLayout =
                shopItems.Count <= CompactShopItemCount;
            float gap = shopItems.Count > 1
                ? compactLayout
                    ? CompactShopItemSpacing
                    : (availableWidth - scaledWidth * shopItems.Count) /
                      (shopItems.Count - 1)
                : 0f;
            float contentWidth = scaledWidth * shopItems.Count +
                gap * Mathf.Max(0, shopItems.Count - 1);
            float firstX = -contentWidth * .5f + scaledWidth * .5f;

            for (int index = 0; index < shopItems.Count; index++)
            {
                ItemView view = shopItems[index];
                if (view == null)
                {
                    continue;
                }

                Vector2 position = new(
                    firstX + index * (scaledWidth + gap),
                    0f);
                if (view == tweeningView)
                {
                    view.TweenToShopPosition(
                        shopContainer,
                        position,
                        itemScale,
                        tweenStartWorldPosition);
                }
                else
                {
                    view.SetShopPosition(
                        shopContainer,
                        position,
                        itemScale);
                }
            }
        }

        private RectTransform FindFirstShopSlot()
        {
            foreach (RectTransform shopSlot in shopSlots)
            {
                if (shopSlot != null)
                {
                    return shopSlot;
                }
            }

            return null;
        }

        private static float DivideScale(
            float desiredWorldScale,
            float parentWorldScale)
        {
            return Mathf.Abs(parentWorldScale) > Mathf.Epsilon
                ? desiredWorldScale / parentWorldScale
                : 1f;
        }

        private ItemView CreateView(
            ItemData data,
            Transform parent,
            ItemInstance existingInstance = null)
        {
            ItemView prefab = ResolveItemViewPrefab(data);
            if (data == null ||
                prefab == null ||
                parent == null)
            {
                return null;
            }

            ItemView view =
                Instantiate(prefab, parent, false);

            ItemInstance instance =
                existingInstance ??
                new ItemInstance(
                    $"shop-item-{++nextItemId}",
                    data,
                    Vector2Int.zero);

            view.Bind(
                instance,
                Backpack,
                gridView,
                itemLayer,
                dragLayer,
                trashZone,
                gridView.CellSize,
                gridView.Spacing,
                combatController);

            view.SelectionRequested +=
                HandleSelectionRequested;
            view.MergedSuccessfully +=
                HandleItemMerged;
            view.DragStateChanged +=
                HandleItemDragStateChanged;
            view.DragPreviewChanged +=
                HandleItemDragPreviewChanged;
            view.ShopDropRequested +=
                HandleShopDropRequested;
            view.SqueezeRequested +=
                HandleSqueezeRequested;
            view.PlacedSuccessfully +=
                HandleItemPlaced;
            view.DeletedSuccessfully +=
                HandleItemDeleted;
            view.SetInteractionEnabled(
                !BattleFlowController.IsCombatPhase);
            view.SetShopDropZone(shopDropZone);

            return view;
        }

        private void HandlePlayerItemsChanged()
        {
            if (BattleFlowController.CurrentPhase == BattlePhase.Preparation)
            {
                RefreshShopInternal();
            }
        }

        private ItemView ResolveItemViewPrefab(ItemData data) =>
            ItemViewPrefabSelector.Select(data, itemViewPrefab, itemView1x2Prefab, itemView2x1Prefab,
                itemViewLMissingBottomLeftPrefab, itemViewLMissingBottomRightPrefab,
                itemViewLMissingTopLeftPrefab, itemViewLMissingTopRightPrefab);

        private void HandleItemMerged(
            ItemView source,
            ItemInstance target)
        {
            ItemView targetView = FindView(target);
            if (targetView == null)
            {
                return;
            }

            targetView.PlayMergeFeedback();
        }

        private void HandleItemDragStateChanged(
            ItemView source,
            bool isDragging)
        {
            if (isDragging)
            {
                draggingItem = source;
            }
            else if (draggingItem == source)
            {
                draggingItem = null;
            }

            foreach (ItemView view in backpackViews)
            {
                if (view == null || view == source)
                {
                    continue;
                }

                bool canMerge = isDragging &&
                    Backpack != null &&
                    Backpack.CanMerge(source.Instance, view.Instance);
                view.SetMergeHighlight(canMerge);
            }

            if (!isDragging && SelectedItem == source)
            {
                SetSelectedItem(null);
            }

            RefreshDragCellVisualization();
        }

        private void HandleItemDragPreviewChanged(ItemView source)
        {
            if (source == draggingItem)
            {
                RefreshDragCellVisualization();
            }
        }

        private bool HandleShopDropRequested(ItemView view)
        {
            if (view == null || view.Instance == null)
            {
                return false;
            }

            if (!view.IsPlacedInBackpack)
            {
                ReflowShopItems();
                return true;
            }

            pendingShopTransfers.Add(view.Instance);
            pendingShopTransferStartPositions[view.Instance] =
                view.RectTransform.position;
            bool removed = CombatController != null
                ? CombatController.RemoveItem(view.Instance)
                : Backpack != null && Backpack.RemoveItem(view.Instance);
            if (!removed)
            {
                pendingShopTransfers.Remove(view.Instance);
                pendingShopTransferStartPositions.Remove(view.Instance);
            }

            return removed;
        }

        private bool HandleSqueezeRequested(
            ItemView source,
            Vector2Int anchorCell)
        {
            if (source?.Instance == null || Backpack == null)
            {
                return false;
            }

            ItemInstance displaced = null;
            foreach (Vector2Int cell in
                     Backpack.GetOccupiedCells(source.Instance, anchorCell))
            {
                if (!Backpack.IsInside(cell))
                {
                    return false;
                }

                ItemInstance occupant = Backpack.GetItemAt(cell);
                if (occupant == null || occupant == source.Instance)
                {
                    continue;
                }

                if (displaced != null && displaced != occupant)
                {
                    return false;
                }

                displaced = occupant;
            }

            if (displaced == null ||
                !Backpack.CanPlaceIgnoring(
                    source.Instance,
                    anchorCell,
                    source.Instance,
                    displaced))
            {
                return false;
            }

            pendingShopTransfers.Add(displaced);
            ItemView displacedView = FindView(displaced);
            if (displacedView != null)
            {
                pendingShopTransferStartPositions[displaced] =
                    displacedView.RectTransform.position;
            }
            bool removed = CombatController != null
                ? CombatController.RemoveItem(displaced)
                : Backpack.RemoveItem(displaced);
            if (!removed)
            {
                pendingShopTransfers.Remove(displaced);
                pendingShopTransferStartPositions.Remove(displaced);
            }

            return removed;
        }

        /// <summary>Refreshes the optional runtime drag-cell diagnostic overlay.</summary>
        public void RefreshDragCellVisualization()
        {
            bool enabled =
                PlayerBackpackDebugBridge.Active != null &&
                PlayerBackpackDebugBridge.Active.Target == this &&
                PlayerBackpackDebugBridge.Active
                    .DragCellVisualizationEnabled;

            dragDebugOverlay?.Refresh(
                Backpack,
                draggingItem,
                enabled);
        }

        private void LateUpdate()
        {
            // The item view follows the pointer with a tween, so update after it
            // to keep blue/green markers locked to its rendered cells.
            RefreshDragCellVisualization();
        }

        private void HandleModelItemAdded(
            ItemInstance item)
        {
            if (isLoadingLayout)
            {
                return;
            }

            ItemView existing = FindView(item);

            if (existing == null)
            {
                CreateBackpackView(item);
                return;
            }

            if (shopItems.Remove(existing))
            {
                ReflowShopItems();
            }

            if (!backpackViews.Contains(existing))
            {
                backpackViews.Add(existing);
            }

            if (debugAutoOperationRunning)
            {
                // 保留商店 ItemView，直接播放其进入背包的移动反馈。
                existing.AnimateToBackpackPosition(item.AnchorCell);
            }
            else
            {
                existing.SetBackpackPosition(item.AnchorCell);
            }
        }

        private void HandleModelItemMoved(
            ItemInstance item)
        {
            if (isLoadingLayout)
            {
                return;
            }

            ItemView view = FindView(item);
            if (debugAutoOperationRunning)
            {
                view?.AnimateToBackpackPosition(item.AnchorCell);
            }
            else
            {
                view?.SetBackpackPosition(item.AnchorCell);
            }
        }

        private void HandleModelItemRemoved(
            ItemInstance item)
        {
            if (isLoadingLayout)
            {
                return;
            }

            ItemView view = FindView(item);

            if (view == null)
            {
                return;
            }

            if (pendingShopTransfers.Remove(item))
            {
                Vector3 startWorldPosition =
                    view.RectTransform.position;
                if (pendingShopTransferStartPositions.TryGetValue(
                        item,
                        out Vector3 capturedStartWorldPosition))
                {
                    startWorldPosition = capturedStartWorldPosition;
                }
                pendingShopTransferStartPositions.Remove(item);
                backpackViews.Remove(view);
                if (!shopItems.Contains(view))
                {
                    shopItems.Add(view);
                }

                ReflowShopItems(view, startWorldPosition);
                return;
            }

            if (shopItems.Remove(view))
            {
                ReflowShopItems();
            }
            backpackViews.Remove(view);

            if (SelectedItem == view)
            {
                SetSelectedItem(null);
            }

            if (debugAutoOperationRunning)
            {
                view.AnimateRemoval(() =>
                {
                    if (view != null)
                    {
                        view.gameObject.SetActive(false);
                        Destroy(view.gameObject);
                    }
                });
            }
            else
            {
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }
        }

        private void HandleSelectionRequested(
            ItemView view)
        {
            SetSelectedItem(view);
        }

        private void HandleItemPlaced(ItemView view)
        {
            if (shopItems.Remove(view))
            {
                ReflowShopItems();
            }

            if (!backpackViews.Contains(view))
            {
                backpackViews.Add(view);
            }

        }

        private void HandleItemDeleted(ItemView view)
        {
            if (shopItems.Remove(view))
            {
                ReflowShopItems();
            }
            backpackViews.Remove(view);

            if (SelectedItem == view)
            {
                SetSelectedItem(null);
            }
        }

        private ItemView FindView(ItemInstance item)
        {
            foreach (ItemView view in backpackViews)
            {
                if (view != null &&
                    view.Instance == item)
                {
                    return view;
                }
            }

            foreach (ItemView view in shopItems)
            {
                if (view != null &&
                    view.Instance == item)
                {
                    return view;
                }
            }

            return null;
        }

        private void SetSelectedItem(ItemView view)
        {
            if (SelectedItem == view)
            {
                return;
            }

            SelectedItem = view;
            SelectedItemChanged?.Invoke(SelectedItem);
        }

        private void ClearShopViews()
        {
            if (SelectedItem != null &&
                shopItems.Contains(SelectedItem))
            {
                SetSelectedItem(null);
            }

            foreach (ItemView view in shopItems)
            {
                if (view == null)
                {
                    continue;
                }

                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }

            shopItems.Clear();
        }

        private void ClearBackpackViews()
        {
            if (SelectedItem != null &&
                backpackViews.Contains(SelectedItem))
            {
                SetSelectedItem(null);
            }

            foreach (ItemView view in backpackViews)
            {
                if (view == null)
                {
                    continue;
                }

                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }

            backpackViews.Clear();
        }

        private bool ValidateConfiguration()
        {
            bool valid =
                combatController != null &&
                fighterSpawner != null &&
                gridView != null &&
                itemLayer != null &&
                dragLayer != null &&
                trashZone != null &&
                shopSlots.Count > 0 &&
                itemViewPrefab != null;

            if (!valid)
            {
                Debug.LogError(
                    "PlayerBackpackSystem配置不完整。",
                    this);
            }

            return valid;
        }

        private void OnDisable()
        {
            CancelDebugAutoOperations("运行时已禁用");
            draggingItem = null;
            dragDebugOverlay?.Clear();
            BattleFlowController.PhaseChanged -=
                HandlePhaseChanged;

            if (curveInput != null)
            {
                curveInput.SetInputEnabled(false);
            }
        }

        private void OnDestroy()
        {
            CancelDebugAutoOperations("运行时已销毁");
            if (playerItemSystem != null)
            {
                playerItemSystem.Changed -= HandlePlayerItemsChanged;
            }

            if (Backpack != null)
            {
                Backpack.ItemAdded -= HandleModelItemAdded;
                Backpack.ItemMoved -= HandleModelItemMoved;
                Backpack.ItemRemoved -= HandleModelItemRemoved;
            }

            if (curveInput != null)
            {
                curveInput.ValueChanged -=
                    SetFlightCurveValue;
            }
        }
    }
}
