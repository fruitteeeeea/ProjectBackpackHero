using System;
using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackHero.Debugging;
using UnityEngine;

namespace BackpackPrototype
{
    [Serializable]
    public sealed class BackpackDefaultPlacement
    {
        [SerializeField]
        private ItemData data;

        [SerializeField]
        private Vector2Int anchorCell;

        public ItemData Data => data;
        public Vector2Int AnchorCell => anchorCell;
    }

    /// <summary>
    /// 一个正式战斗背包的运行时入口。
    /// 持有占格数据、推进当前风格指定的物品冷却，并生成飞机。
    /// </summary>
    [RequireComponent(typeof(FactionMember))]
    [DisallowMultipleComponent]
    public sealed class BackpackCombatController : MonoBehaviour
    {
        [SerializeField, Min(1)]
        private int width = 7;

        [SerializeField, Min(1)]
        private int height = 4;

        [SerializeField]
        private List<BackpackDefaultPlacement>
            defaultPlacements = new();

        private static readonly List<BackpackCombatController>
            activeControllers = new();

        private FactionMember factionMember;
        private BackpackFighterSpawner fighterSpawner;
        private BackpackCooldownLinkEffect cooldownLinkEffect;
        private BackpackController backpack;
        private int nextItemId;

        public static IReadOnlyList<BackpackCombatController>
            ActiveControllers => activeControllers;

        public BackpackController Backpack => backpack;
        public BattleFaction Faction =>
            factionMember != null
                ? factionMember.Faction
                : BattleFaction.Player;
        public BackpackFighterSpawner FighterSpawner =>
            fighterSpawner != null
                ? fighterSpawner
                : fighterSpawner =
                    GetComponent<BackpackFighterSpawner>();
        public IReadOnlyList<ItemInstance> Items =>
            backpack != null
                ? backpack.Items
                : Array.Empty<ItemInstance>();

        public event Action<ItemInstance> CooldownCompleted;

        private void Awake()
        {
            factionMember = GetComponent<FactionMember>();
            fighterSpawner =
                GetComponent<BackpackFighterSpawner>();
            cooldownLinkEffect =
                GetComponent<BackpackCooldownLinkEffect>();

            if (cooldownLinkEffect == null)
            {
                cooldownLinkEffect =
                    gameObject.AddComponent<BackpackCooldownLinkEffect>();
            }
            EnsureBackpack();
        }

        private void OnEnable()
        {
            if (!activeControllers.Contains(this))
            {
                activeControllers.Add(this);
            }

            BattleFlowController.PhaseChanged +=
                HandlePhaseChanged;
            StyleTendencyDebugRuntime.CooldownItemTypeChanged +=
                HandleCooldownItemTypeChanged;
        }

        private void Start()
        {
            if (backpack.Items.Count == 0)
            {
                RestoreDefaultLayout();
            }

            HandlePhaseChanged(
                BattleFlowController.CurrentPhase);
        }

        private void Update()
        {
            if (!BattleFlowController.IsCombatPhase ||
                backpack == null)
            {
                return;
            }

            for (int index = 0;
                 index < backpack.Items.Count;
                 index++)
            {
                ItemInstance item =
                    backpack.Items[index];

                if (item == null ||
                    !CanCooldown(item) ||
                    !item.TickCooldown(Time.deltaTime *
                        (LevelFlowController.Instance?.IsOvertime == true
                            ? LevelFlowController.OvertimeCooldownSpeedMultiplier
                            : 1f)))
                {
                    continue;
                }

                CooldownCompleted?.Invoke(item);

                HandleCooldownCompleted(item);

                if (backpack.Contains(item) &&
                    BattleFlowController.IsCombatPhase &&
                    CanCooldown(item))
                {
                    item.BeginCooldown();
                }
            }
        }

        private void HandleCooldownCompleted(ItemInstance item)
        {
            if (item.Data.ItemType == ItemType.Aircraft)
            {
                RequestAircraftSpawn(item);
                return;
            }

            IReadOnlyList<ItemInstance> adjacentAircraft =
                backpack.GetAdjacentAircraftItems(item);
            if (cooldownLinkEffect != null)
            {
                cooldownLinkEffect.Play(
                    item,
                    adjacentAircraft,
                    aircraft => RequestAircraftSpawn(aircraft, item));
                return;
            }

            foreach (ItemInstance aircraft in adjacentAircraft)
            {
                RequestAircraftSpawn(aircraft, item);
            }
        }

        private void RequestAircraftSpawn(
            ItemInstance aircraft,
            ItemInstance triggeringEquipment = null)
        {
            if (!BattleFlowController.IsCombatPhase ||
                aircraft == null ||
                aircraft.Data == null ||
                !backpack.Contains(aircraft) ||
                (triggeringEquipment == null &&
                 StyleTendencyDebugRuntime.GetCooldownItemType() !=
                 CooldownItemType.Aircraft) ||
                (triggeringEquipment != null &&
                 (StyleTendencyDebugRuntime.GetCooldownItemType() !=
                  CooldownItemType.Equipment ||
                  !backpack.Contains(triggeringEquipment) ||
                  triggeringEquipment.Data == null ||
                  triggeringEquipment.Data.ItemType !=
                  ItemType.Equipment)))
            {
                return;
            }

            int spawnCount = aircraft.Data.SpawnCount;
            for (int spawnIndex = 0;
                 spawnIndex < spawnCount;
                 spawnIndex++)
            {
                FighterSpawner?.RequestSpawn(
                    aircraft,
                    triggeringEquipment);
            }
        }

        public void ConfigureSize(
            int newWidth,
            int newHeight)
        {
            if (backpack != null &&
                backpack.Items.Count > 0)
            {
                Debug.LogWarning(
                    "背包已有物品，不能在运行时修改尺寸。",
                    this);
                return;
            }

            width = Mathf.Max(1, newWidth);
            height = Mathf.Max(1, newHeight);
            backpack =
                new BackpackController(width, height);
        }

        public ItemInstance AddItem(
            ItemData data,
            Vector2Int cell,
            int level = ItemInstance.DefaultLevel)
        {
            EnsureBackpack();

            if (data == null)
            {
                return null;
            }

            ItemInstance item =
                new ItemInstance(
                    $"{Faction.ToString().ToLowerInvariant()}-" +
                    $"item-{++nextItemId}",
                    data,
                    cell,
                    level);

            if (!backpack.PlaceItem(item, cell))
            {
                return null;
            }

            if (BattleFlowController.IsCombatPhase &&
                CanCooldown(item))
            {
                item.BeginCooldown();
            }

            return item;
        }

        public bool PlaceItem(
            ItemInstance item,
            Vector2Int cell)
        {
            EnsureBackpack();

            bool placed =
                backpack.PlaceItem(item, cell);

            if (placed &&
                BattleFlowController.IsCombatPhase &&
                CanCooldown(item))
            {
                item.BeginCooldown();
            }

            return placed;
        }

        public bool MoveItem(
            ItemInstance item,
            Vector2Int cell)
        {
            return backpack != null &&
                   backpack.MoveItem(item, cell);
        }

        public bool RemoveItem(ItemInstance item)
        {
            return backpack != null &&
                   backpack.RemoveItem(item);
        }

        public void Clear()
        {
            backpack?.Clear();
            FighterSpawner?.ClearPendingSpawns();
        }

        public void RestoreDefaultLayout()
        {
            EnsureBackpack();
            Clear();

            foreach (BackpackDefaultPlacement placement
                     in defaultPlacements)
            {
                if (placement?.Data != null)
                {
                    AddItem(
                        placement.Data,
                        placement.AnchorCell);
                }
            }
        }

        public void BeginAllCooldowns()
        {
            if (backpack == null)
            {
                return;
            }

            ResetAllCooldowns();
            foreach (ItemInstance item in backpack.Items)
            {
                if (CanCooldown(item))
                {
                    item.BeginCooldown();
                }
            }
        }

        public void ResetAllCooldowns()
        {
            if (backpack == null)
            {
                return;
            }

            foreach (ItemInstance item in backpack.Items)
            {
                item?.ResetCooldown();
            }
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.Combat)
            {
                BeginAllCooldowns();
            }
            else
            {
                ResetAllCooldowns();
                FighterSpawner?.ClearPendingSpawns();
            }
        }

        private void HandleCooldownItemTypeChanged(
            CooldownItemType _)
        {
            if (BattleFlowController.IsCombatPhase)
            {
                FighterSpawner?.ClearPendingSpawns();
                BeginAllCooldowns();
            }
        }

        private static bool CanCooldown(ItemInstance item) =>
            item?.Data != null &&
            item.Data.ItemType ==
            (StyleTendencyDebugRuntime.GetCooldownItemType() ==
             CooldownItemType.Aircraft
                ? ItemType.Aircraft
                : ItemType.Equipment) &&
            item.Data.CanEnterCooldown;

        private void EnsureBackpack()
        {
            if (backpack == null)
            {
                backpack =
                    new BackpackController(
                        Mathf.Max(1, width),
                        Mathf.Max(1, height));
            }
        }

        private void OnDisable()
        {
            activeControllers.Remove(this);
            BattleFlowController.PhaseChanged -=
                HandlePhaseChanged;
            StyleTendencyDebugRuntime.CooldownItemTypeChanged -=
                HandleCooldownItemTypeChanged;
            fighterSpawner?.ClearPendingSpawns();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
        }
#endif
    }
}
