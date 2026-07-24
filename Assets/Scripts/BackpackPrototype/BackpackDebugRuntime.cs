using System;
using System.Collections;
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
        private Text phaseLabel;

        [SerializeField]
        private GameObject shopRoot;

        [SerializeField]
        private Transform playerFighterSpawnPoint;

        [SerializeField]
        private GameObject fighterPrefab;

        [SerializeField]
        private Transform fighterContainer;

        [SerializeField]
        private Color playerFighterColor =
            new Color(0.45f, 0.85f, 1f, 1f);

        [SerializeField, Min(0.1f)]
        private float fighterSpawnInterval = 0.1f;

        [Header("Item Catalog")]
        [SerializeField]
        private List<ItemPrefabEntry> itemCatalog = new();

        [Header("Default Backpack")]
        [SerializeField]
        private List<DefaultItemPlacement> defaultPlacements = new();

        private readonly List<ItemView> shopItems = new();
        private readonly List<ItemView> backpackViews = new();
        private readonly Queue<ItemInstance>
            pendingFighterSpawns = new();

        private BackpackController backpack;
        private int nextItemId;
        private Coroutine fighterSpawnCoroutine;

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
            backpack = new BackpackController(width, height);
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
            return backpack != null
                ? backpack.Items
                : Array.Empty<ItemInstance>();
        }

        public void EnterAllBackpackItemsCooldown()
        {
            if (!IsReady ||
                !BattleFlowController.IsCombatPhase)
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

            view.CooldownCompleted +=
                HandleCooldownCompleted;

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

            if (isPreparation)
            {
                ClearPendingFighterSpawns();
            }

            foreach (ItemView view in backpackViews)
            {
                if (view == null)
                {
                    continue;
                }

                view.SetInteractionEnabled(isPreparation);

                if (isPreparation)
                {
                    view.StopCooldown();
                }
                else
                {
                    view.EnterCooldown();
                }
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

        private void HandleCooldownCompleted(ItemView itemView)
        {
            if (!BattleFlowController.IsCombatPhase ||
                itemView == null ||
                !itemView.IsPlacedInBackpack ||
                itemView.Instance == null ||
                itemView.Instance.Data == null ||
                itemView.Instance.Data.ItemType !=
                ItemType.Aircraft)
            {
                return;
            }

            QueueFighterSpawn(itemView.Instance);

            if (BattleFlowController.IsCombatPhase &&
                itemView != null)
            {
                itemView.EnterCooldown();
            }
        }

        private void QueueFighterSpawn(
            ItemInstance aircraftItem)
        {
            if (aircraftItem == null ||
                !BattleFlowController.IsCombatPhase)
            {
                return;
            }

            pendingFighterSpawns.Enqueue(aircraftItem);

            if (fighterSpawnCoroutine == null)
            {
                fighterSpawnCoroutine =
                    StartCoroutine(
                        ProcessFighterSpawnQueue());
            }
        }

        private IEnumerator ProcessFighterSpawnQueue()
        {
            float safeInterval =
                Mathf.Max(0.1f, fighterSpawnInterval);

            while (pendingFighterSpawns.Count > 0 &&
                   BattleFlowController.IsCombatPhase)
            {
                ItemInstance aircraftItem =
                    pendingFighterSpawns.Dequeue();

                SpawnFighter(aircraftItem);
                yield return new WaitForSeconds(
                    safeInterval);
            }

            pendingFighterSpawns.Clear();
            fighterSpawnCoroutine = null;
        }

        private void ClearPendingFighterSpawns()
        {
            pendingFighterSpawns.Clear();

            if (fighterSpawnCoroutine == null)
            {
                return;
            }

            StopCoroutine(fighterSpawnCoroutine);
            fighterSpawnCoroutine = null;
        }

        public GameObject SpawnFighter(ItemInstance aircraftItem)
        {
            if (aircraftItem == null ||
                aircraftItem.Data == null ||
                fighterPrefab == null ||
                playerFighterSpawnPoint == null)
            {
                return null;
            }

            FighterDefinition definition =
                aircraftItem.Data.FighterDefinition;

            if (definition == null)
            {
                Debug.LogWarning(
                    $"{aircraftItem.Data.ItemName} 没有配置飞机种类。",
                    this);
                return null;
            }

            GameObject fighterObject =
                Instantiate(
                    fighterPrefab,
                    playerFighterSpawnPoint.position,
                    Quaternion.identity,
                    fighterContainer);

            if (!fighterObject.TryGetComponent(
                    out Fighter2D fighter) ||
                !fighterObject.TryGetComponent(
                    out DirectionalMover2D mover))
            {
                Debug.LogError(
                    "Fighter Prefab缺少Fighter2D或DirectionalMover2D。",
                    fighterObject);
                Destroy(fighterObject);
                return null;
            }

            fighter.Initialize(
                definition,
                BattleFaction.Player,
                playerFighterColor);
            mover.Initialize(Vector2.up);
            fighterObject.name =
                $"Player - {definition.DisplayName}";

            if (fighterObject.TryGetComponent(
                    out FighterFlight2D flight))
            {
                flight.RestartBurst();
            }

            AttachAdjacentEquipmentEffects(
                fighterObject.transform,
                aircraftItem);

            return fighterObject;
        }

        private void AttachAdjacentEquipmentEffects(
            Transform fighter,
            ItemInstance aircraftItem)
        {
            HashSet<GameObject> attachedPrefabs =
                new HashSet<GameObject>();

            foreach (ItemInstance equipment in
                     backpack.GetAdjacentEquipmentItems(
                         aircraftItem))
            {
                GameObject effectPrefab =
                    equipment.Data.EquipmentEffectPrefab;

                if (effectPrefab == null ||
                    !attachedPrefabs.Add(effectPrefab))
                {
                    continue;
                }

                GameObject effect =
                    Instantiate(
                        effectPrefab,
                        fighter,
                        false);
                effect.name =
                    $"{effectPrefab.name} (Equipment)";
            }
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
            ClearPendingFighterSpawns();

            BattleFlowController.PhaseChanged -=
                HandlePhaseChanged;

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
