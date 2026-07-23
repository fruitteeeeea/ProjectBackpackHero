using System;
using System.Collections.Generic;
using UnityEngine;

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

        [Header("Item Catalog")]
        [SerializeField]
        private List<ItemPrefabEntry> itemCatalog = new();

        [Header("Default Backpack")]
        [SerializeField]
        private List<DefaultItemPlacement> defaultPlacements = new();

        private readonly List<ItemView> shopItems = new();
        private readonly List<ItemView> backpackViews = new();

        private BackpackController backpack;
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

        public BackpackController Backpack => backpack;

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
            backpack = new BackpackController(width, height);
            IsReady = ValidateReferences();
        }

        private void Start()
        {
            if (!IsReady)
            {
                return;
            }

            RestoreDefaultBackpack();
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
            if (!IsReady)
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
            return backpack != null
                ? backpack.Items
                : Array.Empty<ItemInstance>();
        }

        public void EnterAllBackpackItemsCooldown()
        {
            if (!IsReady)
            {
                return;
            }

            foreach (ItemView view in backpackViews)
            {
                if (view == null)
                {
                    continue;
                }

                view.EnterCooldown();
            }
        }
        
        private void CreateBackpackItem(
            ItemPrefabEntry entry,
            Vector2Int anchorCell)
        {
            ItemView view =
                CreateView(entry, itemLayer);

            if (view == null)
            {
                return;
            }

            if (!backpack.PlaceItem(
                    view.Instance,
                    anchorCell))
            {
                Debug.LogWarning(
                    $"默认物品 {view.DisplayName} " +
                    $"不能放在 {anchorCell}。",
                    this);

                Destroy(view.gameObject);
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
            Transform parent)
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
                new ItemInstance(
                    $"item-{++nextItemId}",
                    entry.Data,
                    Vector2Int.zero);

            view.Bind(
                instance,
                backpack,
                gridView,
                itemLayer,
                dragLayer,
                trashZone,
                gridView.CellSize,
                gridView.Spacing);

            view.SelectionRequested +=
                HandleSelectionRequested;

            view.PlacedSuccessfully +=
                HandleItemPlaced;

            view.DeletedSuccessfully +=
                HandleItemDeleted;

            return view;
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
                    backpack.RemoveItem(view.Instance);
                }

                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }

            backpackViews.Clear();
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
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
