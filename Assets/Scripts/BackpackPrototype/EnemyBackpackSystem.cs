using System;
using System.Collections;
using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackHero.Debugging;
using BackpackHero.Progression;
using UnityEngine;

namespace BackpackPrototype
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(BackpackCombatController))]
    [RequireComponent(typeof(BackpackFighterSpawner))]
    [DisallowMultipleComponent]
    public sealed class EnemyBackpackSystem : MonoBehaviour
    {
        private const int ShopRollItemCount = 3;
        private const int ShopRollsPerPreparation = 1;
        private const int MaximumOperationsPerPreparation = 8;
        // 移动/加入的位移与放置反馈最长约 .52 秒；留出余量保证串行。
        private const float VisualOperationDuration = .65f;

        [Header("Data")]
        [SerializeField] private EnemyBackpackData defaultData;
        [SerializeField] private DeckPreset defaultDeckPreset;
        [SerializeField] private List<DeckPreset> presetPool = new();
        [Header("Enemy Automation")]
        [SerializeField, Min(.1f)] private float operationInterval = 1f;
        [Header("Item View")]
        [SerializeField] private ItemView itemViewPrefab;
        [SerializeField] private ItemView itemView1x2Prefab;
        [SerializeField] private ItemView itemView2x1Prefab;
        [SerializeField] private ItemView itemViewLMissingBottomLeftPrefab;
        [SerializeField] private ItemView itemViewLMissingBottomRightPrefab;
        [SerializeField] private ItemView itemViewLMissingTopLeftPrefab;
        [SerializeField] private ItemView itemViewLMissingTopRightPrefab;
        [Header("Read Only Backpack UI")]
        [SerializeField] private BackpackGridView gridView;
        [SerializeField] private RectTransform itemLayer;
        [Header("Battle World Anchors")]
        [SerializeField] private RectTransform aircraftSpawnAnchor;
        [SerializeField] private RectTransform collisionCenterAnchor;

        private readonly List<ItemView> itemViews = new();
        private readonly List<ItemData> shopItems = new();
        private BackpackCombatController combatController;
        private BackpackFighterSpawner fighterSpawner;
        private bool isReady, isApplyingData, hasInitialized, operationRunning;
        private bool initialLayoutPending = true;
        private int pendingInitialLayoutVersion = -1;
        private int appliedInitialLayoutVersion = -1;
        private int appliedPreparationInitializationVersion = -1;
        private Coroutine debugFastOperationRoutine;
        private bool debugFastOperationRunning;
        private int debugFastOperationSuccessCount;
        private string debugFastOperationStatus;
        private string debugFastOperationName;
        private readonly HashSet<string> debugFastOperationVisitedLayouts = new();
        private readonly HashSet<string> formalOperationVisitedLayouts = new();
        private int pendingCountedOperations, remainingShopRolls, remainingOperations;
        private float nextAutomaticOperationTime;
        private EnemyBackpackData currentData;
        private DeckPreset currentDeckPreset;
        private DeckPreset runtimeDeckPreset;
        private EnemyMatchProfile matchProfile;
        private bool hasMatchProfile;
        private int defaultProgressionLevel =
            PlayerItemSystem.DefaultLevel;
        private int activeProgressionLevel =
            PlayerItemSystem.DefaultLevel;
        private bool hasProgressionLevelOverride;

        public EnemyBackpackData DefaultData => defaultData;
        public DeckPreset DefaultDeckPreset => defaultDeckPreset;
        public EnemyBackpackData CurrentData => currentData;
        public DeckPreset CurrentDeckPreset => currentDeckPreset;
        public IReadOnlyList<DeckPreset> PresetPool => presetPool;
        public IReadOnlyList<ItemData> ShopItems => shopItems;
        public bool IsReady => isReady;
        public bool IsOperationRunning => operationRunning;
        public bool IsDebugFastOperationRunning => debugFastOperationRunning;
        public int DebugFastOperationSuccessCount => debugFastOperationSuccessCount;
        public string DebugFastOperationStatus => debugFastOperationStatus;
        public string DebugFastOperationName => debugFastOperationName;
        public int RemainingOperations => remainingOperations;
        public int MaximumOperations => MaximumOperationsPerPreparation;
        public int RemainingShopRolls => remainingShopRolls;
        public float OperationInterval => operationInterval;
        public BackpackController Backpack => combatController != null ? combatController.Backpack : null;
        public IReadOnlyList<ItemInstance> Items => Backpack != null ? Backpack.Items : Array.Empty<ItemInstance>();
        public BackpackCombatController CombatController => combatController;
        public BackpackFighterSpawner FighterSpawner => fighterSpawner;
        public RectTransform AircraftSpawnAnchor => aircraftSpawnAnchor;
        public RectTransform CollisionCenterAnchor => collisionCenterAnchor;
        public int DefaultProgressionLevel => defaultProgressionLevel;
        public int ActiveProgressionLevel => activeProgressionLevel;
        public bool HasProgressionLevelOverride =>
            hasProgressionLevelOverride;
        public bool HasMatchProfile => hasMatchProfile;
        public EnemyMatchProfile MatchProfile => matchProfile;

        private void Awake()
        {
            combatController = GetComponent<BackpackCombatController>();
            fighterSpawner = GetComponent<BackpackFighterSpawner>();
            isReady = ValidateConfiguration();
            SubscribeBackpack();
        }
        private void OnEnable()
        {
            BattleFlowController.PhaseChanged += HandlePhaseChanged;
            LevelFlowController.MatchInitialized += HandleMatchInitialized;
        }
        private void Start()
        {
            if (!isReady) return;
            if (!hasInitialized)
            {
                if (!ApplyRankMatchProfile())
                {
                    ResetProgressionLevelForNewMatch();
                    SelectRandomPreset();
                }
                hasInitialized = true;
            }
            RequestInitialLayoutForNewMatch();
            InitializePreparation();
        }
        private void Update()
        {
            if (!CanOperate() || operationRunning || debugFastOperationRunning) return;
            if (remainingOperations <= 0) return;
            if (pendingCountedOperations > 0) { pendingCountedOperations--; StartCoroutine(RunOperation()); return; }
            if (Time.unscaledTime >= nextAutomaticOperationTime) StartCoroutine(RunOperation());
        }

        public bool SelectRandomPreset()
        {
            List<DeckPreset> candidates = new();
            foreach (DeckPreset preset in presetPool) if (preset != null && preset.IsValid(out _)) candidates.Add(preset);
            if (candidates.Count == 0 && defaultDeckPreset != null && defaultDeckPreset.IsValid(out _)) candidates.Add(defaultDeckPreset);
            if (candidates.Count == 0) return false;
            currentDeckPreset = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            currentData = null;
            ResetPreparationState();
            return true;
        }
        public bool SetCurrentDeckPreset(DeckPreset preset)
        {
            if (preset == null || !preset.IsValid(out _)) return false;
            currentDeckPreset = preset; currentData = null;
            if (BattleFlowController.CurrentPhase == BattlePhase.Preparation) RefreshHiddenShop();
            ResetPreparationState();
            return true;
        }
        public bool RandomizeInitialPlacement()
        {
            if (!CanOperate() || currentDeckPreset == null ||
                !InitialBackpackLayoutController.TryBuild(
                    currentDeckPreset.Slots, Backpack.Width, Backpack.Height,
                    null,
                    out List<BackpackLayoutItem> layout)) return false;
            return ApplyLayoutInternal(layout, null);
        }
        public bool ApplyDeckPreset(DeckPreset preset) => SetCurrentDeckPreset(preset) && RandomizeInitialPlacement();
        /// <summary>Applies the score-resolved enemy deck and out-of-match card level for this match.</summary>
        public bool ApplyMatchProfile(EnemyMatchProfile profile)
        {
            if (profile.DeckPreset == null || !profile.DeckPreset.IsValid(out _))
            {
                return false;
            }

            currentDeckPreset = profile.DeckPreset;
            currentData = null;
            matchProfile = profile;
            hasMatchProfile = true;
            defaultProgressionLevel = Mathf.Clamp(profile.ProgressionLevel,
                PlayerItemSystem.DefaultLevel, PlayerItemSystem.MaximumLevel - 1);
            activeProgressionLevel = defaultProgressionLevel;
            hasProgressionLevelOverride = false;
            ResetPreparationState();
            return true;
        }
        public bool CanApplyRuntimeDeck(IReadOnlyList<ItemData> deck)
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

        public bool TryApplyRuntimeDeck(IReadOnlyList<ItemData> deck)
        {
            if (!CanOperate() || !CanApplyRuntimeDeck(deck)) return false;

            DeckPreset temporaryPreset = ScriptableObject.CreateInstance<DeckPreset>();
            temporaryPreset.hideFlags = HideFlags.DontSave;
            temporaryPreset.SetSlots(new List<ItemData>(deck));

            DeckPreset previousRuntimePreset = runtimeDeckPreset;
            if (!ApplyDeckPreset(temporaryPreset))
            {
                DestroyRuntimeDeckPreset(temporaryPreset);
                return false;
            }

            runtimeDeckPreset = temporaryPreset;
            if (previousRuntimePreset != null &&
                previousRuntimePreset != temporaryPreset)
            {
                DestroyRuntimeDeckPreset(previousRuntimePreset);
            }

            return true;
        }

        /// <summary>调试桥接用：在准备阶段恢复已验证的精确背包布局。</summary>
        public bool LoadLayout(IReadOnlyList<BackpackLayoutItem> layout)
        {
            return ApplyLayoutInternal(layout, null);
        }

        /// <summary>仅在新对局或调试重启后请求一次初始布局。</summary>
        public void RequestInitialLayoutForNewMatch()
        {
            int version = LevelFlowController.MatchInitializationVersion;
            if (appliedInitialLayoutVersion == version) return;
            initialLayoutPending = true;
            pendingInitialLayoutVersion = version;
            TryApplyInitialLayoutForMatch();
        }

        private void TryApplyInitialLayoutForMatch()
        {
            if (!initialLayoutPending || !CanOperate()) return;
            bool applied = InitialBackpackItemAutoPlacementDebug.IsEnabled
                ? (currentDeckPreset != null
                    ? RandomizeInitialPlacement()
                    : RestoreDefaultData())
                : LoadLayout(Array.Empty<BackpackLayoutItem>());
            if (applied)
            {
                initialLayoutPending = false;
                appliedInitialLayoutVersion = pendingInitialLayoutVersion;
            }
        }


        public bool ApplyData(EnemyBackpackData data)
        {
            if (data == null || data.Placements.Count == 0) return false;
            List<BackpackLayoutItem> layout = new(data.Placements.Count);
            foreach (BackpackLayoutEntry placement in data.Placements) layout.Add(new BackpackLayoutItem(placement?.Data, placement?.AnchorCell ?? Vector2Int.zero));
            return ApplyLayoutInternal(layout, data);
        }
        public bool RestoreDefaultData()
        {
            if (defaultDeckPreset != null && SetCurrentDeckPreset(defaultDeckPreset)) return RandomizeInitialPlacement();
            return ApplyData(defaultData);
        }
        public bool RequestOperation()
        {
            if (!CanOperate() || debugFastOperationRunning ||
                remainingOperations <= 0) return false;
            pendingCountedOperations++; return true;
        }

        /// <summary>调试用：从当前背包开始快速执行共享探索规则，不重置布局。</summary>
        public bool StartDebugFastOperations(int operationCount = 15, float interval = .3f)
        {
            if (!CanOperate() || operationRunning || debugFastOperationRunning)
            {
                return false;
            }

            pendingCountedOperations = 0;
            debugFastOperationSuccessCount = 0;
            debugFastOperationStatus = "正在规划操作";
            debugFastOperationName = "规划中";
            debugFastOperationVisitedLayouts.Clear();
            debugFastOperationVisitedLayouts.Add(
                BackpackOperationPlanner.GetLayoutFingerprint(Backpack));
            debugFastOperationRunning = true;
            debugFastOperationRoutine = StartCoroutine(RunDebugFastOperations(
                Mathf.Max(1, operationCount), Mathf.Max(.05f, interval)));
            return true;
        }

        public void CancelDebugFastOperations(string reason = "已取消")
        {
            if (debugFastOperationRoutine != null)
            {
                StopCoroutine(debugFastOperationRoutine);
            }

            debugFastOperationRoutine = null;
            debugFastOperationRunning = false;
            debugFastOperationStatus = reason;
            debugFastOperationName = null;
        }

        private IEnumerator RunDebugFastOperations(int targetCount, float interval)
        {
            while (debugFastOperationSuccessCount < targetCount && CanOperate())
            {
                if (!TrySelectNextOperation(
                        debugFastOperationVisitedLayouts,
                        out BackpackOperation operation))
                {
                    debugFastOperationStatus = "已达到当前可探索最优解";
                    break;
                }

                debugFastOperationName = BackpackOperationDebugLogger
                    .Describe(operation);
                float scoreBefore = BackpackStrengthCalculator.Calculate(Backpack)
                    .TotalScore;
                operationRunning = true;
                bool succeeded = TryExecutePlannedOperation(operation);
                operationRunning = false;
                if (!succeeded)
                {
                    debugFastOperationStatus = "操作执行失败";
                    break;
                }

                BackpackOperationDebugLogger.Log(BattleFaction.Enemy, operation,
                    scoreBefore, Backpack, shopItems, remainingShopRolls, this);
                debugFastOperationSuccessCount++;
                debugFastOperationVisitedLayouts.Add(
                    BackpackOperationPlanner.GetLayoutFingerprint(Backpack));
                debugFastOperationStatus = operation.IsExploration
                    ? $"探索操作 {debugFastOperationSuccessCount} / {targetCount}"
                    : $"增益操作 {debugFastOperationSuccessCount} / {targetCount}";
                yield return new WaitForSecondsRealtime(interval);
            }

            if (debugFastOperationSuccessCount >= targetCount)
            {
                debugFastOperationStatus = $"已完成 {targetCount} 次操作";
            }

            yield return new WaitForSecondsRealtime(VisualOperationDuration);
            CancelDebugFastOperations(debugFastOperationStatus ?? "已结束");
        }
        public void SetOperationInterval(float value) => operationInterval = Mathf.Max(.1f, value);
        public void ResetShopRollAllowance() => remainingShopRolls = ShopRollsPerPreparation;
        public void ResetOperationAllowance() => remainingOperations = MaximumOperationsPerPreparation;

        public bool SetDebugProgressionLevel(int level)
        {
            if (!CanOperate() || operationRunning)
            {
                return false;
            }

            activeProgressionLevel = Mathf.Clamp(
                level,
                PlayerItemSystem.DefaultLevel,
                PlayerItemSystem.MaximumLevel);
            hasProgressionLevelOverride = true;
            ApplyProgressionLevelToItems();
            return true;
        }

        public bool RestoreDefaultProgressionLevel()
        {
            if (!CanOperate() || operationRunning)
            {
                return false;
            }

            defaultProgressionLevel = CalculateDefaultProgressionLevel();
            activeProgressionLevel = defaultProgressionLevel;
            hasProgressionLevelOverride = false;
            ApplyProgressionLevelToItems();
            return true;
        }

        private void ResetPreparationState()
        {
            pendingCountedOperations = 0;
            remainingShopRolls = ShopRollsPerPreparation;
            remainingOperations = MaximumOperationsPerPreparation;
            formalOperationVisitedLayouts.Clear();
            if (Backpack != null)
            {
                formalOperationVisitedLayouts.Add(
                    BackpackOperationPlanner.GetLayoutFingerprint(Backpack));
            }
            nextAutomaticOperationTime = Time.unscaledTime + operationInterval;
        }
        public void SetPresetPoolForTests(IEnumerable<DeckPreset> presets) => presetPool = presets != null ? new List<DeckPreset>(presets) : new List<DeckPreset>();

        private IEnumerator RunOperation()
        {
            operationRunning = true;
            if (TryExecuteOperation())
            {
                remainingOperations = Mathf.Max(0, remainingOperations - 1);
            }
            yield return new WaitForSecondsRealtime(VisualOperationDuration);
            operationRunning = false;
            nextAutomaticOperationTime = Time.unscaledTime + operationInterval;
        }
        private bool TryExecuteOperation()
        {
            if (!CanOperate()) return false;
            if (!TrySelectNextOperation(
                    formalOperationVisitedLayouts,
                    out BackpackOperation operation))
            {
                return false;
            }

            float scoreBefore = BackpackStrengthCalculator.Calculate(Backpack)
                .TotalScore;
            if (!TryExecutePlannedOperation(operation))
            {
                return false;
            }

            formalOperationVisitedLayouts.Add(
                BackpackOperationPlanner.GetLayoutFingerprint(Backpack));
            BackpackOperationDebugLogger.Log(BattleFaction.Enemy, operation,
                scoreBefore, Backpack, shopItems, remainingShopRolls, this);
            return true;
        }

        /// <summary>正式与快速调试共用同一套探索型操作选择规则。</summary>
        private bool TrySelectNextOperation(
            ISet<string> visitedLayouts,
            out BackpackOperation operation)
        {
            return BackpackOperationPlanner.TrySelectBest(
                Backpack, shopItems, remainingShopRolls,
                BackpackOperationSelectionMode.ExploreNonDecreasing,
                visitedLayouts, out operation);
        }

        private bool TryExecutePlannedOperation(BackpackOperation operation)
        {
            if (operation == null) return false;
            switch (operation.Kind)
            {
                case BackpackOperationKind.RollShop:
                    return TryRollShop();
                case BackpackOperationKind.MoveItem:
                    return combatController.MoveItem(operation.Item, operation.Destination);
                case BackpackOperationKind.AddShopItem:
                    return TryAddShopItem(operation.ShopItem, operation.Destination);
                case BackpackOperationKind.RemoveItem:
                    return combatController.RemoveItem(operation.Item);
                case BackpackOperationKind.MergeItems:
                    return Backpack.TryMerge(operation.Item, operation.SecondaryItem);
                case BackpackOperationKind.MergeShopItem:
                    return TryMergeShopItem(
                        operation.ShopItem, operation.SecondaryItem);
                case BackpackOperationKind.ReplaceItem:
                    return TryReplaceItem(operation.Item, operation.ShopItem, operation.Destination);
                default:
                    return false;
            }
        }

        private bool TryAddShopItem(ItemData data, Vector2Int cell)
        {
            int index = shopItems.IndexOf(data);
            if (index < 0 || AddEnemyItem(data, cell) == null)
            {
                return false;
            }

            shopItems.RemoveAt(index);
            return true;
        }

        private bool TryMergeShopItem(ItemData data, ItemInstance target)
        {
            int index = shopItems.IndexOf(data);
            if (index < 0 || target == null ||
                !Backpack.TryMerge(
                    new ItemInstance("enemy-shop-merge", data, Vector2Int.zero),
                    target))
            {
                return false;
            }

            shopItems.RemoveAt(index);
            FindView(target)?.PlayMergeFeedback();
            return true;
        }

        private bool TryReplaceItem(
            ItemInstance oldItem,
            ItemData data,
            Vector2Int cell)
        {
            int index = shopItems.IndexOf(data);
            if (index < 0 ||
                !combatController.RemoveItem(oldItem) ||
                AddEnemyItem(data, cell) == null)
            {
                return false;
            }

            shopItems.RemoveAt(index);
            return true;
        }
        private bool TryRollShop()
        {
            if (remainingShopRolls <= 0)
            {
                return false;
            }

            RefreshHiddenShop();
            if (shopItems.Count == 0)
            {
                return false;
            }

            remainingShopRolls--;
            return true;
        }
        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.Preparation)
            {
                TryApplyInitialLayoutForMatch();
                InitializePreparation();
                if (CanOperate())
                {
                    appliedPreparationInitializationVersion =
                        LevelFlowController.MatchInitializationVersion;
                }
            }
            else
            {
                pendingCountedOperations = 0;
                if (debugFastOperationRunning)
                {
                    CancelDebugFastOperations("已离开准备阶段");
                }
            }
        }
        private void InitializePreparation()
        {
            if (!CanOperate()) return;
            ResetPreparationState();
            RefreshHiddenShop();
        }
        private void ResetProgressionLevelForNewMatch()
        {
            defaultProgressionLevel = CalculateDefaultProgressionLevel();
            activeProgressionLevel = defaultProgressionLevel;
            hasProgressionLevelOverride = false;
        }

        private static int CalculateDefaultProgressionLevel()
        {
            PlayerItemSystem playerItems = PlayerItemSystem.Instance;
            return EnemyBackpackProgression.CalculateDefaultLevel(
                playerItems?.GetDeckItems(),
                playerItems != null ? playerItems.GetLevel : null);
        }

        private void ApplyProgressionLevelToItems()
        {
            foreach (ItemInstance item in Items)
            {
                ApplyProgressionLevel(item);
            }
        }

        private void ApplyProgressionLevel(ItemInstance item)
        {
            item?.SetProgressionLevel(activeProgressionLevel);
        }

        private ItemInstance AddEnemyItem(
            ItemData data,
            Vector2Int cell)
        {
            ItemInstance item = combatController.AddItem(data, cell);
            ApplyProgressionLevel(item);
            return item;
        }

        private void RefreshHiddenShop()
        {
            shopItems.Clear(); if (currentDeckPreset == null) return;
            List<ItemData> source = new(); foreach (ItemData item in currentDeckPreset.Slots) if (item != null) source.Add(item);
            for (int index = 0; index < ShopRollItemCount && source.Count > 0; index++) shopItems.Add(source[UnityEngine.Random.Range(0, source.Count)]);
        }

        private bool ApplyLayoutInternal(IReadOnlyList<BackpackLayoutItem> layout, EnemyBackpackData sourceData)
        {
            if (!CanOperate() || layout == null) return false;
            isApplyingData = true;
            bool applied;
            try
            {
                applied = InitialBackpackLayoutController.TryApply(
                    combatController, layout, ApplyProgressionLevel,
                    out string failureReason);
                if (!applied)
                {
                    Debug.LogError($"无法应用敌人背包布局：{failureReason}", this);
                }
            }
            finally { isApplyingData = false; }
            if (!applied) return false;
            currentData = sourceData; if (Application.isPlaying) RebuildViews(); return true;
        }

        private void HandleMatchInitialized()
        {
            ApplyRankMatchProfile();
            RequestInitialLayoutForNewMatch();
            InitializePreparationForNewMatch();
        }

        private bool ApplyRankMatchProfile()
        {
            RankProgressionSystem progression = RankProgressionSystem.Instance;
            return progression != null && progression.TryResolveEnemyMatchProfile(
                out EnemyMatchProfile profile) && ApplyMatchProfile(profile);
        }

        /// <summary>
        /// 调试重启可在已经处于准备阶段时发生；此时不会触发 PhaseChanged，
        /// 仍需为新对局重置隐藏商店、Roll 与操作额度。
        /// </summary>
        private void InitializePreparationForNewMatch()
        {
            int version = LevelFlowController.MatchInitializationVersion;
            if (appliedPreparationInitializationVersion == version)
            {
                return;
            }

            InitializePreparation();
            if (CanOperate())
            {
                appliedPreparationInitializationVersion = version;
            }
        }
        private void SubscribeBackpack()
        {
            if (Backpack == null) return;
            Backpack.ItemAdded += HandleItemAdded; Backpack.ItemMoved += HandleItemMoved; Backpack.ItemRemoved += HandleItemRemoved; Backpack.Cleared += HandleCleared;
        }
        private void RebuildViews()
        {
            ClearViews();
            foreach (ItemInstance item in Items) CreateView(item);
            RefreshSlotVisibility();
        }

        private void RefreshSlotVisibility()
        {
            gridView?.RefreshSlotVisibility(Backpack);
        }
        private ItemView CreateView(ItemInstance item)
        {
            if (item == null || FindView(item) != null) return FindView(item);
            ItemView prefab = ResolveItemViewPrefab(item.Data); if (prefab == null) return null;
            ItemView view = Instantiate(prefab, itemLayer, false); view.Bind(item, Backpack, gridView, itemLayer, null, null, gridView.CellSize, gridView.Spacing, combatController); view.SetBackpackPosition(item.AnchorCell); view.SetInteractionEnabled(false); itemViews.Add(view); return view;
        }
        private ItemView FindView(ItemInstance item) { foreach (ItemView view in itemViews) if (view != null && view.Instance == item) return view; return null; }
        private void HandleItemAdded(ItemInstance item)
        {
            ApplyProgressionLevel(item);
            RefreshSlotVisibility();
            if (isApplyingData) return;
            ItemView view = CreateView(item);
            if (operationRunning) view?.AnimateSpawnInBackpack();
        }

        private void HandleItemMoved(ItemInstance item)
        {
            RefreshSlotVisibility();
            if (isApplyingData) return;
            ItemView view = FindView(item);
            if (operationRunning) view?.AnimateToBackpackPosition(item.AnchorCell);
            else view?.SetBackpackPosition(item.AnchorCell);
        }
        private void HandleItemRemoved(ItemInstance item)
        {
            RefreshSlotVisibility();
            if (isApplyingData) return; ItemView view = FindView(item); if (view == null) return; itemViews.Remove(view);
            if (operationRunning) view.AnimateRemoval(() => DestroyView(view)); else DestroyView(view);
        }
        private void HandleCleared()
        {
            RefreshSlotVisibility();
            if (!isApplyingData) ClearViews();
        }
        private void ClearViews() { foreach (ItemView view in itemViews) DestroyView(view); itemViews.Clear(); }
        private static void DestroyView(ItemView view) { if (view == null) return; view.gameObject.SetActive(false); if (Application.isPlaying) Destroy(view.gameObject); else DestroyImmediate(view.gameObject); }
        private ItemView ResolveItemViewPrefab(ItemData data) => ItemViewPrefabSelector.Select(data, itemViewPrefab, itemView1x2Prefab, itemView2x1Prefab, itemViewLMissingBottomLeftPrefab, itemViewLMissingBottomRightPrefab, itemViewLMissingTopLeftPrefab, itemViewLMissingTopRightPrefab);
        private bool CanOperate() => isReady && BattleFlowController.CurrentPhase == BattlePhase.Preparation && Backpack != null;
        private static void DestroyRuntimeDeckPreset(DeckPreset preset)
        {
            if (preset == null) return;
            if (Application.isPlaying) Destroy(preset);
            else DestroyImmediate(preset);
        }
        private bool ValidateConfiguration()
        {
            bool valid = combatController != null && fighterSpawner != null && (defaultData != null || defaultDeckPreset != null || presetPool.Count > 0) && gridView != null && itemLayer != null && aircraftSpawnAnchor != null && collisionCenterAnchor != null && itemViewPrefab != null;
            if (!valid) Debug.LogError("EnemyBackpackSystem配置不完整。", this); return valid;
        }
        private void OnDisable()
        {
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
            LevelFlowController.MatchInitialized -= HandleMatchInitialized;
            CancelDebugFastOperations("运行时已禁用");
        }
        private void OnDestroy()
        {
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
            LevelFlowController.MatchInitialized -= HandleMatchInitialized;
            CancelDebugFastOperations("运行时已销毁");
            DestroyRuntimeDeckPreset(runtimeDeckPreset);
            if (Backpack == null) return;
            Backpack.ItemAdded -= HandleItemAdded; Backpack.ItemMoved -= HandleItemMoved; Backpack.ItemRemoved -= HandleItemRemoved; Backpack.Cleared -= HandleCleared;
        }
    }
}
