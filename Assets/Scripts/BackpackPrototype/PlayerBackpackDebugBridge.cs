using System;
using System.Collections.Generic;
using System.Text;
using BackpackHero.Battle;
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
        private bool autoCopyPlayerLayoutToEnemy = true;

        [SerializeField]
        private bool dragCellVisualizationEnabled;

        [SerializeField, Min(0.05f)]
        private float refreshInterval = 0.1f;

        private float nextRefreshTime;
        private BackpackController subscribedPlayerBackpack;
        private bool pendingAutomaticCopy;
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

        public bool AutoCopyPlayerLayoutToEnemy =>
            autoCopyPlayerLayoutToEnemy;

        public bool DragCellVisualizationEnabled =>
            dragCellVisualizationEnabled;

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
            ResolveTarget();
            RefreshAutoCopySubscription();
            RefreshSnapshot();
        }

        private void OnEnable()
        {
            Active = this;
            nextRefreshTime = 0f;
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

            RefreshAutoCopySubscription();
            TryAutomaticCopy();

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
            RefreshAutoCopySubscription();
            RefreshSnapshot();
        }

        public void SetDebugEnabled(bool enabled)
        {
            debugEnabled = enabled;
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

        public void SetAutoCopyPlayerLayoutToEnemy(bool enabled)
        {
            autoCopyPlayerLayoutToEnemy = enabled;
            if (enabled)
            {
                pendingAutomaticCopy = true;
                TryAutomaticCopy();
            }
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

        public bool CopyPlayerLayoutToEnemy()
        {
            if (!CanModifyPreparation() ||
                enemyBackpackSystem == null ||
                !enemyBackpackSystem.IsReady ||
                !enemyBackpackSystem.CopyLayoutFrom(
                    playerBackpackSystem.Backpack))
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

        private void RefreshAutoCopySubscription()
        {
            BackpackController next =
                playerBackpackSystem != null
                    ? playerBackpackSystem.Backpack
                    : null;
            if (subscribedPlayerBackpack == next)
            {
                return;
            }

            if (subscribedPlayerBackpack != null)
            {
                subscribedPlayerBackpack.ItemAdded -=
                    HandlePlayerBackpackChanged;
                subscribedPlayerBackpack.ItemMoved -=
                    HandlePlayerBackpackChanged;
                subscribedPlayerBackpack.ItemRemoved -=
                    HandlePlayerBackpackChanged;
                subscribedPlayerBackpack.ItemLevelChanged -=
                    HandlePlayerBackpackChanged;
                subscribedPlayerBackpack.Cleared -=
                    HandlePlayerBackpackCleared;
            }

            subscribedPlayerBackpack = next;
            if (subscribedPlayerBackpack == null)
            {
                return;
            }

            subscribedPlayerBackpack.ItemAdded +=
                HandlePlayerBackpackChanged;
            subscribedPlayerBackpack.ItemMoved +=
                HandlePlayerBackpackChanged;
            subscribedPlayerBackpack.ItemRemoved +=
                HandlePlayerBackpackChanged;
            subscribedPlayerBackpack.ItemLevelChanged +=
                HandlePlayerBackpackChanged;
            subscribedPlayerBackpack.Cleared +=
                HandlePlayerBackpackCleared;
            pendingAutomaticCopy = autoCopyPlayerLayoutToEnemy;
        }

        private void HandlePlayerBackpackChanged(ItemInstance _)
        {
            pendingAutomaticCopy = true;
        }

        private void HandlePlayerBackpackCleared()
        {
            pendingAutomaticCopy = true;
        }

        private void TryAutomaticCopy()
        {
            if (!autoCopyPlayerLayoutToEnemy ||
                !pendingAutomaticCopy ||
                !CanModifyPreparation() ||
                enemyBackpackSystem == null ||
                !enemyBackpackSystem.IsReady ||
                playerBackpackSystem.Backpack == null)
            {
                return;
            }

            if (enemyBackpackSystem.CopyLayoutFrom(
                    playerBackpackSystem.Backpack))
            {
                pendingAutomaticCopy = false;
                RefreshSnapshot();
            }
        }

        private void OnDisable()
        {
            if (subscribedPlayerBackpack != null)
            {
                subscribedPlayerBackpack.ItemAdded -=
                    HandlePlayerBackpackChanged;
                subscribedPlayerBackpack.ItemMoved -=
                    HandlePlayerBackpackChanged;
                subscribedPlayerBackpack.ItemRemoved -=
                    HandlePlayerBackpackChanged;
                subscribedPlayerBackpack.ItemLevelChanged -=
                    HandlePlayerBackpackChanged;
                subscribedPlayerBackpack.Cleared -=
                    HandlePlayerBackpackCleared;
                subscribedPlayerBackpack = null;
            }

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
