using System;
using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackHero.Input;
using UnityEngine;

namespace BackpackPrototype
{
    [Serializable]
    public readonly struct BackpackLayoutItem
    {
        public BackpackLayoutItem(
            ItemData data,
            Vector2Int anchorCell)
        {
            Data = data;
            AnchorCell = anchorCell;
        }

        public ItemData Data { get; }
        public Vector2Int AnchorCell { get; }
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

        [Header("Item Catalog")]
        [SerializeField]
        private List<ItemPrefabEntry> itemCatalog = new();

        [Header("Optional Scene Input")]
        [SerializeField]
        private HorizontalSwipeCurveInput curveInput;

        private readonly List<ItemView> shopItems = new();
        private readonly List<ItemView> backpackViews = new();

        private BackpackCombatController combatController;
        private BackpackFighterSpawner fighterSpawner;
        private bool isReady;
        private bool isLoadingLayout;
        private bool missingCurveWarningReported;
        private int nextItemId;

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

        public ItemView SelectedItem { get; private set; }

        public bool IsReady => isReady;

        private void Awake()
        {
            combatController =
                GetComponent<BackpackCombatController>();
            fighterSpawner =
                GetComponent<BackpackFighterSpawner>();

            isReady = ValidateConfiguration();

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

            RebuildBackpackViews();

            if (BattleFlowController.CurrentPhase ==
                BattlePhase.Preparation)
            {
                RefreshShop();
            }

            HandlePhaseChanged(
                BattleFlowController.CurrentPhase);
        }

        public bool LoadLayout(
            IReadOnlyList<BackpackLayoutItem> layout)
        {
            if (!isReady ||
                BattleFlowController.IsCombatPhase ||
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

                if (placement.Data == null ||
                    FindCatalogEntry(placement.Data) == null)
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
                        placement.AnchorCell);

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
                        placement.AnchorCell);
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

            SelectedItem = null;
            return true;
        }

        public void RestoreDefaultLayout()
        {
            if (!isReady ||
                BattleFlowController.IsCombatPhase)
            {
                return;
            }

            combatController.RestoreDefaultLayout();
            RebuildBackpackViews();
            SelectedItem = null;
        }

        public void RefreshShop()
        {
            if (!isReady ||
                BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation)
            {
                return;
            }

            ClearShopViews();

            if (itemCatalog.Count == 0)
            {
                return;
            }

            foreach (RectTransform slot in shopSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                ItemPrefabEntry entry =
                    itemCatalog[
                        UnityEngine.Random.Range(
                            0,
                            itemCatalog.Count)];

                CreateShopItem(entry, slot);
            }

            SelectedItem = null;
        }

        public void EnterCombat()
        {
            BattleFlowController.EnsureInstance()
                ?.SetPhase(BattlePhase.Combat);
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
            fighterSpawner?.SetCurveValue(value);
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
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

            ItemPrefabEntry entry =
                FindCatalogEntry(item.Data);

            if (entry == null)
            {
                Debug.LogError(
                    $"找不到 {item.Data?.name} 的UI Prefab。",
                    this);
                return null;
            }

            ItemView view =
                CreateView(entry, itemLayer, item);

            if (view == null)
            {
                return null;
            }

            view.SetBackpackPosition(item.AnchorCell);
            backpackViews.Add(view);
            return view;
        }

        private void CreateShopItem(
            ItemPrefabEntry entry,
            RectTransform slot)
        {
            ItemView view =
                CreateView(entry, slot);

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
            if (entry?.Data == null ||
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

            shopItems.Remove(existing);

            if (!backpackViews.Contains(existing))
            {
                backpackViews.Add(existing);
            }

            existing.SetBackpackPosition(
                item.AnchorCell);
        }

        private void HandleModelItemMoved(
            ItemInstance item)
        {
            if (isLoadingLayout)
            {
                return;
            }

            FindView(item)?.SetBackpackPosition(
                item.AnchorCell);
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

            shopItems.Remove(view);
            backpackViews.Remove(view);

            if (SelectedItem == view)
            {
                SelectedItem = null;
            }

            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }

        private void HandleSelectionRequested(
            ItemView view)
        {
            SelectedItem = view;
        }

        private void HandleItemPlaced(ItemView view)
        {
            shopItems.Remove(view);

            if (!backpackViews.Contains(view))
            {
                backpackViews.Add(view);
            }

            PlacementEffectPlayer.Play(
                view.GetComponent<RectTransform>());
        }

        private void HandleItemDeleted(ItemView view)
        {
            shopItems.Remove(view);
            backpackViews.Remove(view);

            if (SelectedItem == view)
            {
                SelectedItem = null;
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

        private ItemPrefabEntry FindCatalogEntry(
            ItemData data)
        {
            foreach (ItemPrefabEntry entry in itemCatalog)
            {
                if (entry != null &&
                    entry.Data == data)
                {
                    return entry;
                }
            }

            return null;
        }

        private void ClearShopViews()
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

        private void ClearBackpackViews()
        {
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
                shopSlots.Count == 3 &&
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
                    "PlayerBackpackSystem配置不完整。",
                    this);
            }

            return valid;
        }

        private void OnDisable()
        {
            BattleFlowController.PhaseChanged -=
                HandlePhaseChanged;

            if (curveInput != null)
            {
                curveInput.SetInputEnabled(false);
            }
        }

        private void OnDestroy()
        {
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
