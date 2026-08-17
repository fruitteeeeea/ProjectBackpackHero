using System;
using System.Collections;
using System.Collections.Generic;
using BackpackHero.Battle;
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
        // 移动/加入的位移与放置反馈最长约 .52 秒；留出余量保证串行。
        private const float VisualOperationDuration = .65f;

        [Header("Data")]
        [SerializeField] private EnemyBackpackData defaultData;
        [SerializeField] private DeckPreset defaultDeckPreset;
        [SerializeField] private List<DeckPreset> presetPool = new();
        [Header("Enemy Automation")]
        [SerializeField, Min(.1f)] private float operationInterval = 1f;
        [SerializeField, Min(0)] private int maximumOperations = 4;
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
        private int pendingImmediateOperations, pendingCountedOperations, remainingOperations;
        private float nextAutomaticOperationTime;
        private EnemyBackpackData currentData;
        private DeckPreset currentDeckPreset;

        public EnemyBackpackData DefaultData => defaultData;
        public DeckPreset DefaultDeckPreset => defaultDeckPreset;
        public EnemyBackpackData CurrentData => currentData;
        public DeckPreset CurrentDeckPreset => currentDeckPreset;
        public IReadOnlyList<DeckPreset> PresetPool => presetPool;
        public IReadOnlyList<ItemData> ShopItems => shopItems;
        public bool IsReady => isReady;
        public bool IsOperationRunning => operationRunning;
        public int RemainingOperations => remainingOperations;
        public int MaximumOperations => maximumOperations;
        public float OperationInterval => operationInterval;
        public BackpackController Backpack => combatController != null ? combatController.Backpack : null;
        public IReadOnlyList<ItemInstance> Items => Backpack != null ? Backpack.Items : Array.Empty<ItemInstance>();
        public BackpackCombatController CombatController => combatController;
        public BackpackFighterSpawner FighterSpawner => fighterSpawner;
        public RectTransform AircraftSpawnAnchor => aircraftSpawnAnchor;
        public RectTransform CollisionCenterAnchor => collisionCenterAnchor;

        private void Awake()
        {
            combatController = GetComponent<BackpackCombatController>();
            fighterSpawner = GetComponent<BackpackFighterSpawner>();
            isReady = ValidateConfiguration();
            SubscribeBackpack();
        }
        private void OnEnable() => BattleFlowController.PhaseChanged += HandlePhaseChanged;
        private void Start()
        {
            if (!isReady) return;
            if (!hasInitialized)
            {
                SelectRandomPreset();
                if (currentDeckPreset != null) RandomizeInitialPlacement(); else RestoreDefaultData();
                hasInitialized = true;
            }
            InitializePreparation();
        }
        private void Update()
        {
            if (!CanOperate() || operationRunning) return;
            if (pendingImmediateOperations > 0) { pendingImmediateOperations--; StartCoroutine(RunOperation(false)); return; }
            if (pendingCountedOperations > 0) { pendingCountedOperations--; StartCoroutine(RunOperation(true)); return; }
            if (remainingOperations > 0 && Time.unscaledTime >= nextAutomaticOperationTime) StartCoroutine(RunOperation(true));
        }

        public bool SelectRandomPreset()
        {
            List<DeckPreset> candidates = new();
            foreach (DeckPreset preset in presetPool) if (preset != null && preset.IsValid(out _)) candidates.Add(preset);
            if (candidates.Count == 0 && defaultDeckPreset != null && defaultDeckPreset.IsValid(out _)) candidates.Add(defaultDeckPreset);
            if (candidates.Count == 0) return false;
            currentDeckPreset = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            currentData = null;
            ResetOperationAllowance();
            return true;
        }
        public bool SetCurrentDeckPreset(DeckPreset preset)
        {
            if (preset == null || !preset.IsValid(out _)) return false;
            currentDeckPreset = preset; currentData = null;
            if (BattleFlowController.CurrentPhase == BattlePhase.Preparation) RefreshHiddenShop();
            ResetOperationAllowance();
            return true;
        }
        public bool RandomizeInitialPlacement()
        {
            if (!CanOperate() || currentDeckPreset == null || !EnemyBackpackLayoutPlanner.TryBuild(currentDeckPreset.Slots, Backpack.Width, Backpack.Height, out List<BackpackLayoutItem> layout)) return false;
            return ApplyLayoutInternal(layout, null);
        }
        public bool ApplyDeckPreset(DeckPreset preset) => SetCurrentDeckPreset(preset) && RandomizeInitialPlacement();
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
            if (!CanOperate() || remainingOperations <= 0) return false;
            pendingCountedOperations++; return true;
        }
        public bool RequestImmediateReaction()
        {
            if (!CanOperate()) return false;
            pendingImmediateOperations++; return true;
        }
        public void SetOperationInterval(float value) => operationInterval = Mathf.Max(.1f, value);
        public void SetMaximumOperations(int value) { maximumOperations = Mathf.Max(0, value); remainingOperations = Mathf.Min(remainingOperations, maximumOperations); }
        public void ResetOperationAllowance()
        {
            remainingOperations = maximumOperations;
            pendingCountedOperations = 0;
            nextAutomaticOperationTime = Time.unscaledTime + operationInterval;
        }
        public void SetPresetPoolForTests(IEnumerable<DeckPreset> presets) => presetPool = presets != null ? new List<DeckPreset>(presets) : new List<DeckPreset>();

        private IEnumerator RunOperation(bool countsTowardsLimit)
        {
            operationRunning = true;
            bool changed = TryExecuteOperation();
            if (changed && countsTowardsLimit) remainingOperations = Mathf.Max(0, remainingOperations - 1);
            yield return new WaitForSecondsRealtime(VisualOperationDuration);
            operationRunning = false; nextAutomaticOperationTime = Time.unscaledTime + operationInterval;
        }
        private bool TryExecuteOperation()
        {
            if (!CanOperate()) return false;
            if (shopItems.Count == 0)
            {
                RefreshHiddenShop();
            }

            // 保持商店三件库存：买走物品后，敌人有很高概率先 Roll。
            if (shopItems.Count < ShopRollItemCount &&
                UnityEngine.Random.value < .8f &&
                TryRollShop())
            {
                return true;
            }

            bool canAdd = CanAddAnyShopItem();
            // 只要还能放入商店物品，绝大多数操作直接拿取。
            if (canAdd && UnityEngine.Random.value < .85f)
            {
                return TryAddShopItem();
            }

            // 空间受限后才开放合成和移除；两者仍明显低于移动、Roll 和替换。
            int[] actions = canAdd
                ? new[] { 1, 1, 1, 1, 1, 0, 0, 5, 5, 3 }
                : new[] { 0, 0, 0, 0, 0, 0, 5, 5, 3, 3, 4, 2 };
            Shuffle(actions);
            foreach (int action in actions)
            {
                bool changed = action switch
                {
                    0 => TryMoveItem(),
                    1 => TryAddShopItem(),
                    2 => TryRemoveItem(),
                    3 => TryReplaceItem(),
                    4 => TryMergeItems(),
                    _ => TryRollShop(),
                };
                if (changed) return true;
            }
            return false;
        }
        private bool TryMoveItem()
        {
            if (Items.Count == 0) return false;
            ItemInstance item = Items[UnityEngine.Random.Range(0, Items.Count)];
            return EnemyBackpackLayoutPlanner.TryFindPreferredCell(Backpack, item, item, out Vector2Int cell) && combatController.MoveItem(item, cell);
        }
        private bool TryAddShopItem()
        {
            if (shopItems.Count == 0) return false;
            int index = UnityEngine.Random.Range(0, shopItems.Count); ItemData data = shopItems[index];
            if (!EnemyBackpackLayoutPlanner.TryFindPreferredCell(Backpack, new ItemInstance("enemy-cell-check", data, Vector2Int.zero), null, out Vector2Int cell) || combatController.AddItem(data, cell) == null) return false;
            shopItems.RemoveAt(index); return true;
        }
        private bool TryRollShop()
        {
            int oldCount = shopItems.Count;
            RefreshHiddenShop();
            return shopItems.Count > 0 || oldCount > 0;
        }
        private bool TryRemoveItem() => IsBackpackFull() && Items.Count > 0 && combatController.RemoveItem(Items[UnityEngine.Random.Range(0, Items.Count)]);
        private bool TryMergeItems()
        {
            if (Backpack == null || CanAddAnyShopItem()) return false;
            List<ItemInstance> sources = new();
            foreach (ItemInstance source in Items)
            {
                if (source?.Data == null || source.Level != ItemInstance.DefaultLevel)
                {
                    continue;
                }

                foreach (ItemInstance target in Items)
                {
                    if (Backpack.CanMerge(source, target))
                    {
                        sources.Add(source);
                        break;
                    }
                }
            }

            if (sources.Count == 0) return false;
            ItemInstance chosenSource = sources[UnityEngine.Random.Range(0, sources.Count)];
            foreach (ItemInstance target in Items)
            {
                if (Backpack.CanMerge(chosenSource, target))
                {
                    return Backpack.TryMerge(chosenSource, target);
                }
            }

            return false;
        }
        private bool TryReplaceItem()
        {
            if (Items.Count == 0 || shopItems.Count == 0) return false;
            ItemInstance old = Items[UnityEngine.Random.Range(0, Items.Count)]; int index = UnityEngine.Random.Range(0, shopItems.Count); ItemData data = shopItems[index];
            if (!Backpack.CanPlace(new ItemInstance("enemy-replace-check", data, old.AnchorCell), old.AnchorCell, old)) return false;
            if (!combatController.RemoveItem(old) || combatController.AddItem(data, old.AnchorCell) == null) return false;
            shopItems.RemoveAt(index); return true;
        }
        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.Preparation) InitializePreparation();
            else { pendingImmediateOperations = 0; pendingCountedOperations = 0; }
        }
        private void InitializePreparation()
        {
            if (!CanOperate()) return;
            pendingImmediateOperations = 0;
            ResetOperationAllowance();
            RefreshHiddenShop();
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
            BackpackController validation = new(Backpack.Width, Backpack.Height);
            for (int index = 0; index < layout.Count; index++)
            {
                BackpackLayoutItem placement = layout[index];
                if (placement.Data == null || !validation.PlaceItem(new ItemInstance($"enemy-validation-{index}", placement.Data, placement.AnchorCell, placement.Level), placement.AnchorCell)) return false;
            }
            isApplyingData = true;
            try { combatController.Clear(); for (int index = 0; index < layout.Count; index++) { BackpackLayoutItem p = layout[index]; if (!combatController.PlaceItem(new ItemInstance($"enemy-item-{index + 1}", p.Data, p.AnchorCell, p.Level), p.AnchorCell)) return false; } }
            finally { isApplyingData = false; }
            currentData = sourceData; if (Application.isPlaying) RebuildViews(); return true;
        }
        private void SubscribeBackpack()
        {
            if (Backpack == null) return;
            Backpack.ItemAdded += HandleItemAdded; Backpack.ItemMoved += HandleItemMoved; Backpack.ItemRemoved += HandleItemRemoved; Backpack.Cleared += HandleCleared;
        }
        private void RebuildViews() { ClearViews(); foreach (ItemInstance item in Items) CreateView(item); }
        private ItemView CreateView(ItemInstance item)
        {
            if (item == null || FindView(item) != null) return FindView(item);
            ItemView prefab = ResolveItemViewPrefab(item.Data); if (prefab == null) return null;
            ItemView view = Instantiate(prefab, itemLayer, false); view.Bind(item, Backpack, gridView, itemLayer, null, null, gridView.CellSize, gridView.Spacing, combatController); view.SetBackpackPosition(item.AnchorCell); view.SetInteractionEnabled(false); itemViews.Add(view); return view;
        }
        private ItemView FindView(ItemInstance item) { foreach (ItemView view in itemViews) if (view != null && view.Instance == item) return view; return null; }
        private void HandleItemAdded(ItemInstance item) { if (isApplyingData) return; ItemView view = CreateView(item); if (operationRunning) view?.AnimateSpawnInBackpack(); }
        private void HandleItemMoved(ItemInstance item) { if (isApplyingData) return; ItemView view = FindView(item); if (operationRunning) view?.AnimateToBackpackPosition(item.AnchorCell); else view?.SetBackpackPosition(item.AnchorCell); }
        private void HandleItemRemoved(ItemInstance item)
        {
            if (isApplyingData) return; ItemView view = FindView(item); if (view == null) return; itemViews.Remove(view);
            if (operationRunning) view.AnimateRemoval(() => DestroyView(view)); else DestroyView(view);
        }
        private void HandleCleared() { if (!isApplyingData) ClearViews(); }
        private void ClearViews() { foreach (ItemView view in itemViews) DestroyView(view); itemViews.Clear(); }
        private static void DestroyView(ItemView view) { if (view == null) return; view.gameObject.SetActive(false); if (Application.isPlaying) Destroy(view.gameObject); else DestroyImmediate(view.gameObject); }
        private ItemView ResolveItemViewPrefab(ItemData data) => ItemViewPrefabSelector.Select(data, itemViewPrefab, itemView1x2Prefab, itemView2x1Prefab, itemViewLMissingBottomLeftPrefab, itemViewLMissingBottomRightPrefab, itemViewLMissingTopLeftPrefab, itemViewLMissingTopRightPrefab);
        private bool IsBackpackFull()
        {
            if (Backpack == null) return false;
            for (int y = 0; y < Backpack.Height; y++)
            for (int x = 0; x < Backpack.Width; x++)
                if (Backpack.GetItemAt(new Vector2Int(x, y)) == null) return false;
            return true;
        }
        private bool CanAddAnyShopItem()
        {
            if (Backpack == null || shopItems.Count == 0) return false;
            foreach (ItemData data in shopItems)
            {
                if (data == null) continue;
                if (EnemyBackpackLayoutPlanner.TryFindPreferredCell(
                        Backpack,
                        new ItemInstance("enemy-fit-check", data, Vector2Int.zero),
                        null,
                        out _))
                {
                    return true;
                }
            }

            return false;
        }
        private static void Shuffle(int[] values)
        {
            for (int index = values.Length - 1; index > 0; index--)
            {
                int swap = UnityEngine.Random.Range(0, index + 1);
                (values[index], values[swap]) = (values[swap], values[index]);
            }
        }
        private bool CanOperate() => isReady && BattleFlowController.CurrentPhase == BattlePhase.Preparation && Backpack != null;
        private bool ValidateConfiguration()
        {
            bool valid = combatController != null && fighterSpawner != null && (defaultData != null || defaultDeckPreset != null || presetPool.Count > 0) && gridView != null && itemLayer != null && aircraftSpawnAnchor != null && collisionCenterAnchor != null && itemViewPrefab != null;
            if (!valid) Debug.LogError("EnemyBackpackSystem配置不完整。", this); return valid;
        }
        private void OnDisable() => BattleFlowController.PhaseChanged -= HandlePhaseChanged;
        private void OnDestroy()
        {
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
            if (Backpack == null) return;
            Backpack.ItemAdded -= HandleItemAdded; Backpack.ItemMoved -= HandleItemMoved; Backpack.ItemRemoved -= HandleItemRemoved; Backpack.Cleared -= HandleCleared;
        }
    }
}
