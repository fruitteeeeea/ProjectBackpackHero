using System;
using System.Collections.Generic;
using BackpackHero.Battle;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>
    /// 敌人背包的只读运行时入口。
    /// 从EnemyBackpackData原子加载布局并维护对应的只读UI。
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(BackpackCombatController))]
    [RequireComponent(typeof(BackpackFighterSpawner))]
    [DisallowMultipleComponent]
    public sealed class EnemyBackpackSystem : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField]
        private EnemyBackpackData defaultData;

        [SerializeField]
        private List<ItemPrefabEntry> itemCatalog = new();

        [Header("Read Only Backpack UI")]
        [SerializeField]
        private BackpackGridView gridView;

        [SerializeField]
        private RectTransform itemLayer;

        [Header("Battle World Anchors")]
        [SerializeField]
        private RectTransform aircraftSpawnAnchor;

        [SerializeField]
        private RectTransform collisionCenterAnchor;

        private readonly List<ItemView> itemViews = new();

        private BackpackCombatController combatController;
        private BackpackFighterSpawner fighterSpawner;
        private bool isReady;
        private bool isApplyingData;
        private EnemyBackpackData currentData;

        public EnemyBackpackData DefaultData => defaultData;
        public EnemyBackpackData CurrentData => currentData;
        public bool IsReady => isReady;
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
        public RectTransform AircraftSpawnAnchor =>
            aircraftSpawnAnchor;
        public RectTransform CollisionCenterAnchor =>
            collisionCenterAnchor;

        private void Awake()
        {
            combatController =
                GetComponent<BackpackCombatController>();
            fighterSpawner =
                GetComponent<BackpackFighterSpawner>();
            isReady = ValidateConfiguration();

            if (Backpack != null)
            {
                Backpack.ItemAdded += HandleItemAdded;
                Backpack.ItemMoved += HandleItemMoved;
                Backpack.ItemRemoved += HandleItemRemoved;
                Backpack.Cleared += HandleCleared;
            }
        }

        private void Start()
        {
            if (isReady)
            {
                RestoreDefaultData();
            }
        }

        public bool ApplyData(EnemyBackpackData data)
        {
            if (data == null ||
                data.Placements.Count == 0)
            {
                return false;
            }

            var layout =
                new List<BackpackLayoutItem>(
                    data.Placements.Count);

            foreach (BackpackLayoutEntry placement
                     in data.Placements)
            {
                layout.Add(
                    new BackpackLayoutItem(
                        placement?.Data,
                        placement?.AnchorCell ??
                        Vector2Int.zero));
            }

            return ApplyLayoutInternal(
                layout,
                data);
        }

        /// <summary>
        /// 用另一个运行时背包的当前物品与格子位置覆盖敌人背包。
        /// 复制结果不依赖EnemyBackpackData资产。
        /// </summary>
        public bool CopyLayoutFrom(
            BackpackController source)
        {
            if (source == null)
            {
                return false;
            }

            var layout =
                new List<BackpackLayoutItem>(
                    source.Items.Count);

            foreach (ItemInstance item in source.Items)
            {
                if (item?.Data == null)
                {
                    return false;
                }

                layout.Add(
                    new BackpackLayoutItem(
                        item.Data,
                        item.AnchorCell));
            }

            return ApplyLayoutInternal(
                layout,
                null);
        }

        private bool ApplyLayoutInternal(
            IReadOnlyList<BackpackLayoutItem> layout,
            EnemyBackpackData sourceData)
        {
            if (!isReady ||
                BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation ||
                layout == null ||
                Backpack == null)
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

                if (placement.Data == null ||
                    FindCatalogEntry(placement.Data) == null)
                {
                    Debug.LogError(
                        $"敌人背包布局条目 " +
                        $"{index} 缺少物品或UI Prefab。",
                        this);
                    return false;
                }

                ItemInstance candidate =
                    new ItemInstance(
                        $"enemy-validation-{index}",
                        placement.Data,
                        placement.AnchorCell);

                if (!validation.PlaceItem(
                        candidate,
                        placement.AnchorCell))
                {
                    Debug.LogError(
                        $"敌人背包布局条目 " +
                        $"{index} 无法放置在 " +
                        $"{placement.AnchorCell}。",
                        this);
                    return false;
                }
            }

            isApplyingData = true;

            try
            {
                combatController.Clear();

                for (int index = 0;
                     index < layout.Count;
                     index++)
                {
                    BackpackLayoutItem placement =
                        layout[index];
                    ItemInstance item =
                        new ItemInstance(
                            $"enemy-item-{index + 1}",
                            placement.Data,
                            placement.AnchorCell);

                    if (!combatController.PlaceItem(
                            item,
                            placement.AnchorCell))
                    {
                        Debug.LogError(
                            "已验证的敌人背包布局提交失败。",
                            this);
                        return false;
                    }
                }
            }
            finally
            {
                isApplyingData = false;
            }

            currentData = sourceData;
            if (Application.isPlaying)
            {
                RebuildViews();
            }
            return true;
        }

        public bool RestoreDefaultData()
        {
            return ApplyData(defaultData);
        }

        private void RebuildViews()
        {
            ClearViews();

            foreach (ItemInstance item in Items)
            {
                CreateView(item);
            }
        }

        private ItemView CreateView(ItemInstance item)
        {
            if (item == null || FindView(item) != null)
            {
                return FindView(item);
            }

            ItemPrefabEntry entry =
                FindCatalogEntry(item.Data);

            if (entry?.Prefab == null)
            {
                return null;
            }

            ItemView view =
                Instantiate(
                    entry.Prefab,
                    itemLayer,
                    false);

            view.Bind(
                item,
                Backpack,
                gridView,
                itemLayer,
                null,
                null,
                gridView.CellSize,
                gridView.Spacing,
                combatController);
            view.SetBackpackPosition(item.AnchorCell);
            view.SetInteractionEnabled(false);
            itemViews.Add(view);
            return view;
        }

        private ItemPrefabEntry FindCatalogEntry(
            ItemData data)
        {
            foreach (ItemPrefabEntry entry in itemCatalog)
            {
                if (entry != null && entry.Data == data)
                {
                    return entry;
                }
            }

            return null;
        }

        private ItemView FindView(ItemInstance item)
        {
            foreach (ItemView view in itemViews)
            {
                if (view != null && view.Instance == item)
                {
                    return view;
                }
            }

            return null;
        }

        private void HandleItemAdded(ItemInstance item)
        {
            if (!isApplyingData)
            {
                CreateView(item);
            }
        }

        private void HandleItemMoved(ItemInstance item)
        {
            if (!isApplyingData)
            {
                FindView(item)?.SetBackpackPosition(
                    item.AnchorCell);
            }
        }

        private void HandleItemRemoved(ItemInstance item)
        {
            if (isApplyingData)
            {
                return;
            }

            ItemView view = FindView(item);
            if (view != null)
            {
                itemViews.Remove(view);
                DestroyView(view);
            }
        }

        private void HandleCleared()
        {
            if (!isApplyingData)
            {
                ClearViews();
            }
        }

        private void ClearViews()
        {
            foreach (ItemView view in itemViews)
            {
                DestroyView(view);
            }

            itemViews.Clear();
        }

        private static void DestroyView(ItemView view)
        {
            if (view == null)
            {
                return;
            }

            view.gameObject.SetActive(false);

            if (Application.isPlaying)
            {
                Destroy(view.gameObject);
            }
            else
            {
                DestroyImmediate(view.gameObject);
            }
        }

        private bool ValidateConfiguration()
        {
            bool valid =
                combatController != null &&
                fighterSpawner != null &&
                defaultData != null &&
                gridView != null &&
                itemLayer != null &&
                aircraftSpawnAnchor != null &&
                collisionCenterAnchor != null &&
                itemCatalog.Count > 0;

            foreach (ItemPrefabEntry entry in itemCatalog)
            {
                valid &=
                    entry != null &&
                    entry.Data != null &&
                    entry.Prefab != null;
            }

            if (!valid)
            {
                Debug.LogError(
                    "EnemyBackpackSystem配置不完整。",
                    this);
            }

            return valid;
        }

        private void OnDestroy()
        {
            if (Backpack != null)
            {
                Backpack.ItemAdded -= HandleItemAdded;
                Backpack.ItemMoved -= HandleItemMoved;
                Backpack.ItemRemoved -= HandleItemRemoved;
                Backpack.Cleared -= HandleCleared;
            }
        }
    }
}
