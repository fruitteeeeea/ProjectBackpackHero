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
        }

        public void TogglePhase()
        {
            BattleFlowController.EnsureInstance()
                ?.TogglePhase();
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
            BattleFlowController.EnsureInstance()
                ?.SetPhase(BattlePhase.Combat);
            RefreshSnapshot();
        }

        public bool RefreshShop()
        {
            if (!CanModifyPreparation())
            {
                return false;
            }

            playerBackpackSystem.RefreshShop();
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
                if (item.Data.EquipmentEffectPrefab !=
                    null)
                {
                    text.Append("\n   效果：")
                        .Append(
                            item.Data
                                .EquipmentEffectPrefab
                                .name);
                }

                return text.ToString();
            }

            text.Append("\n   冷却：");

            if (item.IsCoolingDown)
            {
                text.Append(
                        item.RemainingCooldown
                            .ToString("0.00"))
                    .Append("s  ")
                    .Append(
                        (item.CooldownProgress * 100f)
                        .ToString("0"))
                    .Append('%');
            }
            else
            {
                text.Append("未冷却");
            }

            List<ItemInstance> equipment =
                backpack != null
                    ? backpack
                        .GetAdjacentEquipmentItems(item)
                    : new List<ItemInstance>();

            text.Append("\n   临近增益：")
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

                GameObject effectPrefab =
                    source.Data.EquipmentEffectPrefab;

                if (effectPrefab == null)
                {
                    text.Append(
                        "：未配置效果 Prefab");
                    continue;
                }

                text.Append("：")
                    .Append(effectPrefab.name);

                List<string> buffTypes =
                    GetBuffTypeNames(effectPrefab);

                if (buffTypes.Count > 0)
                {
                    text.Append(" [")
                        .Append(
                            string.Join(
                                ", ",
                                buffTypes))
                        .Append(']');
                }
                else
                {
                    text.Append(" [表现效果]");
                }
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

        private static List<string> GetBuffTypeNames(
            GameObject effectPrefab)
        {
            var names = new List<string>();

            foreach (MonoBehaviour behaviour in
                     effectPrefab.GetComponentsInChildren<
                         MonoBehaviour>(true))
            {
                if (behaviour is
                    IAircraftEquipmentBuff &&
                    !names.Contains(
                        behaviour.GetType().Name))
                {
                    names.Add(
                        behaviour.GetType().Name);
                }
            }

            return names;
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

        private void OnDisable()
        {
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
