using System;
using System.Collections.Generic;
using BackpackHero.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    [Serializable]
    public sealed class ItemPrefabEntry
    {
        [SerializeField]
        private ItemData data;

        [SerializeField]
        private ItemView prefab;

        public ItemData Data => data;
        public ItemView Prefab => prefab;
    }

    [Serializable]
    public sealed class DefaultItemPlacement
    {
        [SerializeField]
        private ItemData data;

        [SerializeField]
        private Vector2Int anchorCell;

        public ItemData Data => data;
        public Vector2Int AnchorCell => anchorCell;
    }

    public sealed class BackpackDebugRuntime : MonoBehaviour
    {
        [Header("Backpack Size")]
        [SerializeField, Min(1)]
        private int width = 7;

        [SerializeField, Min(1)]
        private int height = 4;

        [Header("Scene References")]
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

        [Header("Battle Flow")]
        [SerializeField]
        private BackpackCombatController combatController;

        [SerializeField]
        private Text phaseLabel;

        [SerializeField]
        private GameObject shopRoot;

        [SerializeField]
        private Transform playerFighterSpawnPoint;

        [SerializeField]
        private GameObject fighterPrefab;

        [SerializeField]
        private Transform fighterContainer;

        [Header("Item Catalog")]
        [SerializeField]
        private List<ItemPrefabEntry> itemCatalog = new();

        [Header("Default Backpack")]
        [SerializeField]
        private List<DefaultItemPlacement> defaultPlacements = new();

        private readonly List<ItemView> shopItems = new();
        private readonly List<ItemView> backpackViews = new();
        private int nextItemId;

        public static BackpackDebugRuntime Instance
        {
            get;
            private set;
        }

        public bool IsReady
        {
            get;
            private set;
        }

        public ItemView SelectedItem
        {
            get;
            private set;
        }

        public BackpackController Backpack =>
            combatController != null
                ? combatController.Backpack
                : null;
        public BackpackCombatController CombatController =>
            combatController;
        public BattlePhase CurrentPhase =>
            BattleFlowController.CurrentPhase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError(
                    "场景中存在多个 BackpackDebugRuntime。",
                    this);

                enabled = false;
                return;
            }

            Instance = this;

            combatController =
                combatController != null
                    ? combatController
                    : GetComponent<
                        BackpackCombatController>();

            if (combatController == null)
            {
                combatController =
                    gameObject.AddComponent<
                        BackpackCombatController>();
            }

            combatController.ConfigureSize(
                width,
                height);

            BackpackFighterSpawner spawner =
                GetComponent<BackpackFighterSpawner>();

            if (spawner == null)
            {
                spawner =
                    gameObject.AddComponent<
                        BackpackFighterSpawner>();
            }

            spawner.Configure(
                fighterPrefab,
                playerFighterSpawnPoint,
                fighterContainer);

            IsReady = ValidateReferences();

            BattleFlowController.PhaseChanged +=
                HandlePhaseChanged;
        }

        private void Start()
        {
            if (!IsReady)
            {
                return;
            }

            RestoreDefaultBackpack();
            HandlePhaseChanged(
                BattleFlowController.CurrentPhase);
        }

        public void RestoreDefaultBackpack()
        {
            if (!IsReady)
            {
                return;
            }

            ClearBackpack();

            foreach (DefaultItemPlacement placement
                     in defaultPlacements)
            {
                if (placement == null ||
                    placement.Data == null)
                {
                    continue;
                }

                ItemPrefabEntry entry =
                    FindCatalogEntry(placement.Data);

                if (entry == null || entry.Prefab == null)
                {
                    Debug.LogWarning(
                        $"没有找到 {placement.Data.name} 对应的物品Prefab。",
                        this);

                    continue;
                }

                CreateBackpackItem(
                    entry,
                    placement.AnchorCell);
            }

            SelectedItem = null;
        }

        public void RefreshShop()
        {
            if (!IsReady ||
                BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation)
            {
                return;
            }

            ClearShop();

            for (int index = 0;
                 index < shopSlots.Count;
                 index++)
            {
                if (shopSlots[index] == null)
                {
                    continue;
                }

                ItemPrefabEntry entry =
                    itemCatalog[
                        UnityEngine.Random.Range(
                            0,
                            itemCatalog.Count)];

                CreateShopItem(
                    entry,
                    shopSlots[index]);
            }

            SelectedItem = null;
        }

        public IReadOnlyList<ItemInstance>
            GetBackpackItems()
        {
            return Backpack != null
                ? Backpack.Items
                : Array.Empty<ItemInstance>();
        }

        public void EnterAllBackpackItemsCooldown()
        {
            if (!IsReady ||
                !BattleFlowController.IsCombatPhase)
            {
                return;
            }

            combatController.BeginAllCooldowns();
        }
        
        private void CreateBackpackItem(
            ItemPrefabEntry entry,
            Vector2Int anchorCell)
        {
            ItemInstance instance =
                combatController.AddItem(
                    entry.Data,
                    anchorCell);

            if (instance == null)
            {
                Debug.LogWarning(
                    $"默认物品 {entry.Data.ItemName} " +
                    $"不能放在 {anchorCell}。",
                    this);
                return;
            }

            ItemView view =
                CreateView(
                    entry,
                    itemLayer,
                    instance);

            if (view == null)
            {
                combatController.RemoveItem(instance);
                return;
            }

            view.SetBackpackPosition(anchorCell);
            backpackViews.Add(view);
        }

        private void CreateShopItem(
            ItemPrefabEntry entry,
            RectTransform slot)
        {
            ItemView view = CreateView(entry, slot);

            if (view == null)
            {
                return;
            }

            RectTransform itemRect =
                view.GetComponent<RectTransform>();

            itemRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            itemRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            itemRect.pivot =
                new Vector2(0.5f, 0.5f);

            itemRect.anchoredPosition =
                Vector2.zero;

            shopItems.Add(view);
        }

        private ItemView CreateView(
            ItemPrefabEntry entry,
            Transform parent,
            ItemInstance existingInstance = null)
        {
            if (entry == null ||
                entry.Data == null ||
                entry.Prefab == null ||
                parent == null)
            {
                return null;
            }

            ItemView view =
                Instantiate(entry.Prefab, parent, false);

            ItemInstance instance =
                existingInstance ??
                new ItemInstance(
                    $"shop-item-{++nextItemId}",
                    entry.Data,
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

            view.PlacedSuccessfully +=
                HandleItemPlaced;

            view.DeletedSuccessfully +=
                HandleItemDeleted;

            view.SetInteractionEnabled(
                !BattleFlowController.IsCombatPhase);

            return view;
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            bool isPreparation =
                phase == BattlePhase.Preparation;

            if (phaseLabel != null)
            {
                phaseLabel.text =
                    $"当前阶段：{GetPhaseDisplayName(phase)}";
            }

            if (shopRoot != null)
            {
                shopRoot.SetActive(isPreparation);
            }

            foreach (ItemView view in backpackViews)
            {
                if (view == null)
                {
                    continue;
                }

                view.SetInteractionEnabled(isPreparation);
            }

            foreach (ItemView view in shopItems)
            {
                if (view != null)
                {
                    view.SetInteractionEnabled(isPreparation);
                }
            }
        }

        private static string GetPhaseDisplayName(
            BattlePhase phase)
        {
            return phase == BattlePhase.Combat
                ? "战斗阶段"
                : "准备阶段";
        }

        private void HandleSelectionRequested(
            ItemView itemView)
        {
            SelectedItem = itemView;
        }

        private void HandleItemPlaced(
            ItemView itemView)
        {
            shopItems.Remove(itemView);

            if (!backpackViews.Contains(itemView))
            {
                backpackViews.Add(itemView);
            }

            PlacementEffectPlayer.Play(
                itemView.GetComponent<RectTransform>());
        }

        private void HandleItemDeleted(
            ItemView itemView)
        {
            shopItems.Remove(itemView);
            backpackViews.Remove(itemView);

            if (SelectedItem == itemView)
            {
                SelectedItem = null;
            }
        }

        private ItemPrefabEntry FindCatalogEntry(
            ItemData data)
        {
            foreach (ItemPrefabEntry entry
                     in itemCatalog)
            {
                if (entry != null &&
                    entry.Data == data)
                {
                    return entry;
                }
            }

            return null;
        }

        private void ClearShop()
        {
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

        private void ClearBackpack()
        {
            foreach (ItemView view in backpackViews)
            {
                if (view == null)
                {
                    continue;
                }

                if (view.Instance != null)
                {
                    combatController.RemoveItem(
                        view.Instance);
                }

                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }

            backpackViews.Clear();
            combatController.Clear();
        }

        private bool ValidateReferences()
        {
            bool valid = true;

            if (gridView == null ||
                itemLayer == null ||
                dragLayer == null ||
                trashZone == null)
            {
                Debug.LogError(
                    "BackpackDebugRuntime缺少场景引用。",
                    this);

                valid = false;
            }

            if (shopSlots.Count != 3)
            {
                Debug.LogError(
                    "Shop Slots必须正好配置3个槽位。",
                    this);

                valid = false;
            }

            if (itemCatalog.Count == 0)
            {
                Debug.LogError(
                    "Item Catalog至少需要配置1个物品。",
                    this);

                valid = false;
            }

            foreach (ItemPrefabEntry entry
                     in itemCatalog)
            {
                if (entry == null ||
                    entry.Data == null ||
                    entry.Prefab == null)
                {
                    Debug.LogError(
                        "Item Catalog存在未配置条目。",
                        this);

                    valid = false;
                }
            }

            return valid;
        }

        private void OnDestroy()
        {
            BattleFlowController.PhaseChanged -=
                HandlePhaseChanged;

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
