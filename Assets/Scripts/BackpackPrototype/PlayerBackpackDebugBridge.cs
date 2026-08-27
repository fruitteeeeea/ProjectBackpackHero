using System;
using System.Collections.Generic;
using System.Text;
using BackpackHero.Battle;
using BackpackHero.Debugging;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BackpackPrototype
{
    /// <summary>
    /// 玩家背包的运行时调试桥。
    /// EditorWindow只通过这里读取快照和调用公开业务接口。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerBackpackDebugBridge :
        MonoBehaviour
    {
        [SerializeField]
        private PlayerBackpackSystem playerBackpackSystem;

        [SerializeField]
        private EnemyBackpackSystem enemyBackpackSystem;

        [SerializeField]
        private bool debugEnabled = true;

        [SerializeField]
        private bool dragCellVisualizationEnabled;

        private bool backpacksHealthLocked;

        [SerializeField, Min(0.05f)]
        private float refreshInterval = 0.1f;

        private float nextRefreshTime;
        private PlayerBackpackDebugSnapshot snapshot =
            PlayerBackpackDebugSnapshot.Unavailable(
                "调试桥尚未初始化。");

        private PlayerBackpackDebugSnapshot enemySnapshot =
            PlayerBackpackDebugSnapshot.Unavailable(
                "敌人背包调试桥尚未初始化。");

        public static PlayerBackpackDebugBridge Active
        {
            get;
            private set;
        }

        public PlayerBackpackSystem Target =>
            playerBackpackSystem;

        public EnemyBackpackSystem EnemyTarget =>
            enemyBackpackSystem;

        public bool DebugEnabled => debugEnabled;

        public bool DragCellVisualizationEnabled =>
            dragCellVisualizationEnabled;

        public bool BackpacksHealthLocked =>
            backpacksHealthLocked;

        public bool CanLockBackpackHealth =>
            BattleFlowController.IsCombatPhase &&
            TryGetBackpackTargets(
                out BattleBackpackTarget2D playerTarget,
                out BattleBackpackTarget2D enemyTarget) &&
            playerTarget.IsAlive &&
            enemyTarget.IsAlive;

        public bool CanSwapRuntimeDecks =>
            playerBackpackSystem != null &&
            playerBackpackSystem.IsReady &&
            enemyBackpackSystem != null &&
            enemyBackpackSystem.IsReady &&
            enemyBackpackSystem.CurrentDeckPreset != null &&
            DeckPreset.IsValidSlots(
                playerBackpackSystem.ActiveDeckItems,
                out _) &&
            enemyBackpackSystem.CurrentDeckPreset.IsValid(out _) &&
            playerBackpackSystem.CanApplyRuntimeDeck(
                enemyBackpackSystem.CurrentDeckPreset.Slots) &&
            enemyBackpackSystem.CanApplyRuntimeDeck(
                playerBackpackSystem.ActiveDeckItems);

        public PlayerBackpackDebugSnapshot Snapshot =>
            snapshot;

        public PlayerBackpackDebugSnapshot EnemySnapshot =>
            enemySnapshot;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Active = null;
        }

        private void Awake()
        {
            if (!TryGetComponent(
                    out PlayerRandomFlightCurveController _))
            {
                gameObject.AddComponent<
                    PlayerRandomFlightCurveController>();
            }

            ResolveTarget();
            RefreshSnapshot();
        }

        private void OnEnable()
        {
            Active = this;
            nextRefreshTime = 0f;
            BattleFlowController.PhaseChanged += HandlePhaseChanged;
        }

        private void Update()
        {
            if (Keyboard.current != null &&
                Keyboard.current.f1Key.wasPressedThisFrame)
            {
                SetDebugEnabled(!debugEnabled);
            }

            if (playerBackpackSystem == null)
            {
                ResolveTarget();
            }

            if (enemyBackpackSystem == null)
            {
                ResolveEnemyTarget();
            }

            if (backpacksHealthLocked && !CanLockBackpackHealth)
            {
                SetBackpacksHealthLocked(false);
            }

            if (Time.unscaledTime >= nextRefreshTime)
            {
                RefreshSnapshot();
                nextRefreshTime =
                    Time.unscaledTime +
                    Mathf.Max(0.05f, refreshInterval);
            }
        }

        public void Bind(PlayerBackpackSystem target)
        {
            playerBackpackSystem = target;
            RefreshSnapshot();
        }

        public void SetDebugEnabled(bool enabled)
        {
            debugEnabled = enabled;
            if (!enabled)
            {
                SetBackpacksHealthLocked(false);
            }
        }

        public bool SetBackpacksHealthLocked(bool locked)
        {
            if (!locked)
            {
                ApplyBackpackHealthLock(false);
                backpacksHealthLocked = false;
                RefreshSnapshot();
                return true;
            }

            if (!CanLockBackpackHealth)
            {
                ApplyBackpackHealthLock(false);
                backpacksHealthLocked = false;
                RefreshSnapshot();
                return false;
            }

            ApplyBackpackHealthLock(true);
            backpacksHealthLocked = true;
            RefreshSnapshot();
            return true;
        }

        public void SetDragCellVisualizationEnabled(bool enabled)
        {
            dragCellVisualizationEnabled = enabled;
            playerBackpackSystem?.RefreshDragCellVisualization();
        }

        public void TogglePhase()
        {
            if (BattleFlowController.CurrentPhase ==
                BattlePhase.Preparation)
            {
                EnterCombat();
            }
            else
            {
                EnterPreparation();
            }
            RefreshSnapshot();
        }

        public void EnterPreparation()
        {
            BattleFlowController.EnsureInstance()
                ?.SetPhase(BattlePhase.Preparation);
            RefreshSnapshot();
        }

        public void EnterCombat()
        {
            LevelFlowController.EnsureInstance()
                ?.RequestStartRound();
            RefreshSnapshot();
        }

        public bool RefreshShop()
        {
            if (!CanModifyPreparation())
            {
                return false;
            }

            if (!playerBackpackSystem.TryRefreshShop())
            {
                return false;
            }

            RefreshSnapshot();
            return true;
        }

        public bool ResetRolls()
        {
            if (!CanModifyPreparation() ||
                !playerBackpackSystem.ResetRolls())
            {
                return false;
            }

            RefreshSnapshot();
            return true;
        }

        public bool RestoreDefaultLayout()
        {
            if (!CanModifyPreparation())
            {
                return false;
            }

            playerBackpackSystem.RestoreDefaultLayout();
            RefreshSnapshot();
            return true;
        }

        public bool ApplyPlayerDeckPreset(DeckPreset preset)
        {
            if (!CanModifyPreparation() ||
                !playerBackpackSystem.ApplyDeckPreset(preset))
            {
                return false;
            }

            RefreshSnapshot();
            return true;
        }

        public bool ApplyEnemyDeckPreset(DeckPreset preset)
        {
            if (!CanModifyPreparation() ||
                enemyBackpackSystem == null ||
                !enemyBackpackSystem.ApplyDeckPreset(preset))
            {
                return false;
            }

            RefreshSnapshot();
            return true;
        }

        public bool TrySwapRuntimeDecksAndResetMatch()
        {
            if (!CanSwapRuntimeDecks)
            {
                return false;
            }

            List<ItemData> playerDeck = new(
                playerBackpackSystem.ActiveDeckItems);
            List<ItemData> enemyDeck = new(
                enemyBackpackSystem.CurrentDeckPreset.Slots);

            LevelFlowController.EnsureInstance()?.ResetForDebugMatch();

            if (!playerBackpackSystem.TryApplyRuntimeDeck(enemyDeck))
            {
                return false;
            }

            if (enemyBackpackSystem.TryApplyRuntimeDeck(playerDeck))
            {
                SetBackpacksHealthLocked(false);
                RefreshSnapshot();
                return true;
            }

            playerBackpackSystem.TryApplyRuntimeDeck(playerDeck);
            return false;
        }

        /// <summary>把当前双方背包及临时调试状态收集为编辑器测试方案。</summary>
        public bool TryCaptureBalanceTestPreset(
            BalanceAdjustmentTestPreset preset,
            out string error)
        {
            error = null;
            if (preset == null)
            {
                error = "测试方案不可用。";
                return false;
            }

            if (!playerBackpackSystem || !playerBackpackSystem.IsReady ||
                !enemyBackpackSystem || !enemyBackpackSystem.IsReady)
            {
                error = "等待双方背包运行时就绪。";
                return false;
            }

            if (!playerBackpackSystem.TryGetUniformProgressionLevel(
                    out int playerProgressionLevel))
            {
                error = "玩家当前物品养成等级不一致；请先在平衡调整中选择一个临时统一养成等级后再保存方案。";
                return false;
            }

            PlayerRandomFlightCurveController flight =
                GetComponent<PlayerRandomFlightCurveController>();
            preset.SetRuntimeState(
                GamePacingDebugRuntime.Instance?.GameSpeed ?? 1f,
                flight != null ? flight.Mode : RandomFlightCurveMode.Off,
                playerProgressionLevel,
                enemyBackpackSystem.ActiveProgressionLevel,
                enemyBackpackSystem.CurrentDeckPreset,
                playerBackpackSystem.ActiveDeckItems,
                enemyBackpackSystem.CurrentDeckPreset?.Slots,
                CaptureLayout(playerBackpackSystem.Backpack),
                CaptureLayout(enemyBackpackSystem.Backpack));
            return true;
        }

        /// <summary>在准备阶段校验后一次性应用测试方案；不读取或写入正式配置。</summary>
        public bool TryApplyBalanceTestPreset(
            BalanceAdjustmentTestPreset preset,
            out string error)
        {
            error = null;
            if (!CanModifyPreparation())
            {
                error = "仅可在双方背包就绪的准备阶段应用测试方案。";
                return false;
            }

            if (preset == null ||
                !ValidatePreset(preset, playerBackpackSystem.Backpack,
                    enemyBackpackSystem.Backpack, out error))
            {
                return false;
            }

            // 先完成所有不写状态的校验，随后才开始重置并修改当前对局。
            LevelFlowController.EnsureInstance()?.ResetForDebugMatch();
            if (!playerBackpackSystem.TryApplyRuntimeDeck(preset.PlayerDeck) ||
                !enemyBackpackSystem.TryApplyRuntimeDeck(preset.EnemyDeck) ||
                !playerBackpackSystem.LoadLayout(ToRuntimeLayout(preset.PlayerLayout)) ||
                !enemyBackpackSystem.LoadLayout(ToRuntimeLayout(preset.EnemyLayout)) ||
                !playerBackpackSystem.SetDebugProgressionLevel(preset.PlayerProgressionLevel) ||
                !enemyBackpackSystem.SetDebugProgressionLevel(preset.EnemyProgressionLevel))
            {
                error = "测试方案布局无法应用到当前背包。";
                return false;
            }

            GamePacingDebugRuntime.Instance?.SetGameSpeed(preset.GameSpeed);
            GetComponent<PlayerRandomFlightCurveController>()?.SetMode(
                preset.RandomFlightMode);
            SetBackpacksHealthLocked(false);
            RefreshSnapshot();
            return true;
        }

        private static List<BalanceAdjustmentTestPreset.LayoutEntry> CaptureLayout(
            BackpackController backpack)
        {
            List<BalanceAdjustmentTestPreset.LayoutEntry> result = new();
            if (backpack == null) return result;
            foreach (ItemInstance item in backpack.Items)
                if (item?.Data != null)
                    result.Add(new BalanceAdjustmentTestPreset.LayoutEntry(
                        item.Data, item.AnchorCell, item.Level));
            return result;
        }

        private static List<BackpackLayoutItem> ToRuntimeLayout(
            IReadOnlyList<BalanceAdjustmentTestPreset.LayoutEntry> source)
        {
            List<BackpackLayoutItem> result = new();
            if (source == null) return result;
            foreach (BalanceAdjustmentTestPreset.LayoutEntry entry in source)
                result.Add(new BackpackLayoutItem(
                    entry.Item, entry.AnchorCell, entry.Level));
            return result;
        }

        private static bool ValidatePreset(BalanceAdjustmentTestPreset preset,
            BackpackController playerBackpack, BackpackController enemyBackpack,
            out string error)
        {
            error = null;
            if (!DeckPreset.IsValidSlots(preset.PlayerDeck, out error) ||
                !DeckPreset.IsValidSlots(preset.EnemyDeck, out error))
            {
                return false;
            }

            return ValidateLayout(preset.PlayerLayout, playerBackpack, "玩家", out error) &&
                   ValidateLayout(preset.EnemyLayout, enemyBackpack, "敌人", out error);
        }

        private static bool ValidateLayout(
            IReadOnlyList<BalanceAdjustmentTestPreset.LayoutEntry> source,
            BackpackController backpack, string faction, out string error)
        {
            error = null;
            if (backpack == null || source == null || source.Count == 0)
            {
                error = faction + "背包布局为空。";
                return false;
            }

            BackpackController validation = new(backpack.Width, backpack.Height);
            for (int index = 0; index < source.Count; index++)
            {
                BalanceAdjustmentTestPreset.LayoutEntry entry = source[index];
                if (entry.Item == null || !validation.PlaceItem(
                        new ItemInstance("balance-validation-" + index,
                            entry.Item, entry.AnchorCell, entry.Level),
                        entry.AnchorCell))
                {
                    error = faction + $"布局第 {index + 1} 项无效或发生重叠。";
                    return false;
                }
            }
            return true;
        }

        public bool ApplyEnemyData(
            EnemyBackpackData data)
        {
            if (enemyBackpackSystem == null ||
                !enemyBackpackSystem.ApplyData(data))
            {
                return false;
            }

            RefreshSnapshot();
            return true;
        }

        public bool RestoreEnemyDefaultData()
        {
            if (enemyBackpackSystem == null ||
                !enemyBackpackSystem
                    .RestoreDefaultData())
            {
                return false;
            }

            RefreshSnapshot();
            return true;
        }

        public bool BeginAllCooldowns()
        {
            if (playerBackpackSystem?.CombatController ==
                    null ||
                !BattleFlowController.IsCombatPhase)
            {
                return false;
            }

            playerBackpackSystem.CombatController
                .BeginAllCooldowns();
            RefreshSnapshot();
            return true;
        }

        public bool RandomizeAllEquipmentCooldowns()
        {
            if (!BattleFlowController.IsCombatPhase)
            {
                return false;
            }

            bool randomized = RandomizeEquipmentCooldowns(
                playerBackpackSystem?.CombatController);
            randomized |= RandomizeEquipmentCooldowns(
                enemyBackpackSystem?.CombatController);

            if (randomized)
            {
                RefreshSnapshot();
            }

            return randomized;
        }

        private static bool RandomizeEquipmentCooldowns(
            BackpackCombatController controller)
        {
            if (controller?.Backpack == null)
            {
                return false;
            }

            bool hasEquipment = false;
            foreach (ItemInstance item in controller.Items)
            {
                if (item?.Data == null ||
                    item.Data.ItemType != ItemType.Equipment)
                {
                    continue;
                }

                item.SetRuntimeCooldownDuration(
                    item.Data.CooldownDuration +
                    UnityEngine.Random.Range(-0.5f, 0.5f));
                hasEquipment = true;
            }

            if (hasEquipment)
            {
                controller.BeginAllCooldowns();
            }

            return hasEquipment;
        }

        /// <summary>
        /// 按玩家背包最大生命值的指定比例扣血；不依赖当前调试页选中的目标。
        /// </summary>
        public bool DamagePlayerBackpackByMaximumHealthFraction(
            float fraction)
        {
            if (backpacksHealthLocked)
            {
                return false;
            }

            return TryDamageBackpackByMaximumHealthFraction(
                playerBackpackSystem,
                fraction);
        }

        /// <summary>
        /// 按敌人背包最大生命值的指定比例扣血；不依赖当前调试页选中的目标。
        /// </summary>
        public bool DamageEnemyBackpackByMaximumHealthFraction(
            float fraction)
        {
            if (backpacksHealthLocked)
            {
                return false;
            }

            return TryDamageBackpackByMaximumHealthFraction(
                enemyBackpackSystem,
                fraction);
        }

        public bool SetCurveValue(float value)
        {
            if (playerBackpackSystem?.FighterSpawner ==
                null)
            {
                return false;
            }

            playerBackpackSystem.SetFlightCurveValue(
                value);
            RefreshSnapshot();
            return true;
        }

        private static bool TryDamageBackpackByMaximumHealthFraction(
            Component backpackSystem,
            float fraction)
        {
            if (backpackSystem == null ||
                fraction <= 0f ||
                !backpackSystem.TryGetComponent(
                    out BattleBackpackTarget2D target) ||
                target.Health == null ||
                target.Health.IsDead)
            {
                return false;
            }

            target.Health.DecreaseHealth(
                target.Health.MaxHealth * fraction);
            return true;
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase != BattlePhase.Combat)
            {
                SetBackpacksHealthLocked(false);
            }
            else
            {
                RefreshSnapshot();
            }
        }

        private bool TryGetBackpackTargets(
            out BattleBackpackTarget2D playerTarget,
            out BattleBackpackTarget2D enemyTarget)
        {
            playerTarget = playerBackpackSystem != null
                ? playerBackpackSystem.GetComponent<BattleBackpackTarget2D>()
                : null;
            enemyTarget = enemyBackpackSystem != null
                ? enemyBackpackSystem.GetComponent<BattleBackpackTarget2D>()
                : null;

            return playerTarget != null && enemyTarget != null;
        }

        private void ApplyBackpackHealthLock(bool locked)
        {
            TryGetBackpackTargets(
                out BattleBackpackTarget2D playerTarget,
                out BattleBackpackTarget2D enemyTarget);
            playerTarget?.SetHealthLocked(locked);
            enemyTarget?.SetHealthLocked(locked);
        }

        public void RefreshSnapshot()
        {
            if (playerBackpackSystem == null)
            {
                snapshot =
                    PlayerBackpackDebugSnapshot.Unavailable(
                        "未找到PlayerBackpackSystem；" +
                        "场景可能正在切换或目标已销毁。");
                RefreshEnemySnapshot();
                return;
            }

            BackpackController backpack =
                playerBackpackSystem.Backpack;
            BackpackFighterSpawner spawner =
                playerBackpackSystem.FighterSpawner;

            if (backpack == null)
            {
                snapshot =
                    PlayerBackpackDebugSnapshot.Unavailable(
                        "PlayerBackpackSystem尚未完成初始化。");
                RefreshEnemySnapshot();
                return;
            }

            var items =
                new List<PlayerBackpackDebugItemSnapshot>(
                    playerBackpackSystem.Items.Count);
            int index = 0;

            foreach (ItemInstance item in
                     playerBackpackSystem.Items)
            {
                index++;
                items.Add(
                    new PlayerBackpackDebugItemSnapshot(
                        item?.Id ?? "<null>",
                        item?.Data?.ItemName ??
                        "<无效物品>",
                        item?.Data?.ItemType ??
                        ItemType.Equipment,
                        BuildItemDescription(
                            backpack,
                            item,
                            index)));
            }

            snapshot =
                PlayerBackpackDebugSnapshot.Available(
                    BattleFlowController.CurrentPhase,
                    playerBackpackSystem.IsReady,
                    backpack.Width,
                    backpack.Height,
                    spawner?.PendingCount ?? 0,
                    spawner?.CurrentCurveValue ?? 0f,
                    items);

            RefreshEnemySnapshot();
        }

        private void RefreshEnemySnapshot()
        {
            if (enemyBackpackSystem == null)
            {
                enemySnapshot =
                    PlayerBackpackDebugSnapshot.Unavailable(
                        "未找到EnemyBackpackSystem。");
                return;
            }

            BackpackController backpack =
                enemyBackpackSystem.Backpack;
            BackpackFighterSpawner spawner =
                enemyBackpackSystem.FighterSpawner;

            if (backpack == null)
            {
                enemySnapshot =
                    PlayerBackpackDebugSnapshot.Unavailable(
                        "EnemyBackpackSystem尚未初始化。");
                return;
            }

            var items =
                new List<PlayerBackpackDebugItemSnapshot>(
                    enemyBackpackSystem.Items.Count);
            int index = 0;

            foreach (ItemInstance item in
                     enemyBackpackSystem.Items)
            {
                index++;
                items.Add(
                    new PlayerBackpackDebugItemSnapshot(
                        item?.Id ?? "<null>",
                        item?.Data?.ItemName ??
                        "<无效物品>",
                        item?.Data?.ItemType ??
                        ItemType.Equipment,
                        BuildItemDescription(
                            backpack,
                            item,
                            index)));
            }

            enemySnapshot =
                PlayerBackpackDebugSnapshot.Available(
                    BattleFlowController.CurrentPhase,
                    enemyBackpackSystem.IsReady,
                    backpack.Width,
                    backpack.Height,
                    spawner?.PendingCount ?? 0,
                    spawner?.CurrentCurveValue ?? 0f,
                    items);
        }

        public static string BuildItemDescription(
            BackpackController backpack,
            ItemInstance item,
            int displayIndex = 1)
        {
            if (item?.Data == null)
            {
                return $"{displayIndex}. <无效物品>";
            }

            var text = new StringBuilder();
            text.Append(displayIndex)
                .Append(". ")
                .Append(item.Data.ItemName)
                .Append(" [")
                .Append(item.Data.ItemType)
                .Append("]  格子 ")
                .Append(item.AnchorCell);

            if (item.Data.ItemType !=
                ItemType.Aircraft)
            {
                text.Append("\n   冷却：");
                if (item.IsCoolingDown)
                {
                    text.Append(item.RemainingCooldown.ToString("0.00"))
                        .Append("s  ")
                        .Append((item.CooldownProgress * 100f).ToString("0"))
                        .Append('%');
                }
                else
                {
                    text.Append("未冷却");
                }

                text.Append("\n   标识颜色：#")
                    .Append(ColorUtility.ToHtmlStringRGB(
                        item.Data.EquipmentColor));

                return text.ToString();
            }

            text.Append("\n   冷却：无（由相邻装备触发）");

            List<ItemInstance> equipment =
                backpack != null
                    ? backpack
                        .GetAdjacentEquipmentItems(item)
                    : new List<ItemInstance>();

            text.Append("\n   临近装备颜色：")
                .Append(equipment.Count);

            if (equipment.Count == 0)
            {
                text.Append("（无）");
                return text.ToString();
            }

            foreach (ItemInstance source in equipment)
            {
                text.Append("\n   • ")
                    .Append(source.Data.ItemName)
                    .Append(" @ ")
                    .Append(source.AnchorCell);

                text.Append("：#")
                    .Append(ColorUtility.ToHtmlStringRGB(
                        source.Data.EquipmentColor));
            }

            return text.ToString();
        }

        private bool CanModifyPreparation()
        {
            return playerBackpackSystem != null &&
                   playerBackpackSystem.IsReady &&
                   BattleFlowController.CurrentPhase ==
                   BattlePhase.Preparation;
        }

        private void ResolveTarget()
        {
            playerBackpackSystem =
                playerBackpackSystem != null
                    ? playerBackpackSystem
                    : FindAnyObjectByType<
                        PlayerBackpackSystem>(
                        FindObjectsInactive.Include);
        }

        private void ResolveEnemyTarget()
        {
            enemyBackpackSystem =
                enemyBackpackSystem != null
                    ? enemyBackpackSystem
                    : FindAnyObjectByType<
                        EnemyBackpackSystem>(
                        FindObjectsInactive.Include);
        }

        /// <summary>同时启动双方的共享探索 AI；任一方无法启动时不留下单独序列。</summary>
        public bool StartSynchronizedDebugAutoOperations(
            int operationCount = 15, float interval = .3f)
        {
            if (!CanModifyPreparation() || enemyBackpackSystem == null ||
                !enemyBackpackSystem.IsReady ||
                playerBackpackSystem.IsDebugAutoOperationRunning ||
                enemyBackpackSystem.IsDebugFastOperationRunning ||
                enemyBackpackSystem.IsOperationRunning)
            {
                return false;
            }

            if (!playerBackpackSystem.StartDebugAutoOperations(
                    operationCount, interval))
            {
                return false;
            }

            if (enemyBackpackSystem.StartDebugFastOperations(
                    operationCount, interval))
            {
                return true;
            }

            playerBackpackSystem.CancelDebugAutoOperations("敌方 AI 无法启动");
            return false;
        }

        private void OnDisable()
        {
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
            SetBackpacksHealthLocked(false);

            if (Active == this)
            {
                Active = null;
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }
    }

    public sealed class PlayerBackpackDebugSnapshot
    {
        private PlayerBackpackDebugSnapshot(
            bool hasTarget,
            string statusMessage,
            BattlePhase phase,
            bool systemReady,
            int width,
            int height,
            int pendingSpawnCount,
            float curveValue,
            IReadOnlyList<
                PlayerBackpackDebugItemSnapshot> items)
        {
            HasTarget = hasTarget;
            StatusMessage = statusMessage;
            Phase = phase;
            SystemReady = systemReady;
            Width = width;
            Height = height;
            PendingSpawnCount = pendingSpawnCount;
            CurveValue = curveValue;
            Items = items;
        }

        public bool HasTarget { get; }
        public string StatusMessage { get; }
        public BattlePhase Phase { get; }
        public bool SystemReady { get; }
        public int Width { get; }
        public int Height { get; }
        public int PendingSpawnCount { get; }
        public float CurveValue { get; }
        public IReadOnlyList<
            PlayerBackpackDebugItemSnapshot> Items
        {
            get;
        }

        public static PlayerBackpackDebugSnapshot
            Unavailable(string message)
        {
            return new PlayerBackpackDebugSnapshot(
                false,
                message,
                BattlePhase.Preparation,
                false,
                0,
                0,
                0,
                0f,
                Array.Empty<
                    PlayerBackpackDebugItemSnapshot>());
        }

        public static PlayerBackpackDebugSnapshot
            Available(
                BattlePhase phase,
                bool systemReady,
                int width,
                int height,
                int pendingSpawnCount,
                float curveValue,
                IReadOnlyList<
                    PlayerBackpackDebugItemSnapshot> items)
        {
            return new PlayerBackpackDebugSnapshot(
                true,
                string.Empty,
                phase,
                systemReady,
                width,
                height,
                pendingSpawnCount,
                curveValue,
                items ??
                Array.Empty<
                    PlayerBackpackDebugItemSnapshot>());
        }
    }

    public sealed class PlayerBackpackDebugItemSnapshot
    {
        public PlayerBackpackDebugItemSnapshot(
            string id,
            string itemName,
            ItemType itemType,
            string description)
        {
            Id = id;
            ItemName = itemName;
            ItemType = itemType;
            Description = description;
        }

        public string Id { get; }
        public string ItemName { get; }
        public ItemType ItemType { get; }
        public string Description { get; }
    }
}
