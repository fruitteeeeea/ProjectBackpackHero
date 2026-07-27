using System;
using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackHero.Input;
using UnityEngine;
using UnityEngine.UI;

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
        private const string AircraftAnchorName =
            "AircraftSpawnAnchor";
        private const string CollisionAnchorName =
            "CollisionCenterAnchor";

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

        [Header("Battle World Anchors")]
        [SerializeField]
        private RectTransform aircraftSpawnAnchor;

        [SerializeField]
        private RectTransform collisionCenterAnchor;

        [SerializeField]
        private float battleWorldPlaneZ;

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

        public RectTransform AircraftSpawnAnchor =>
            aircraftSpawnAnchor;

        public RectTransform CollisionCenterAnchor =>
            collisionCenterAnchor;

        public ItemView SelectedItem { get; private set; }

        public bool IsReady => isReady;

        private void Awake()
        {
            combatController =
                GetComponent<BackpackCombatController>();
            fighterSpawner =
                GetComponent<BackpackFighterSpawner>();

            ResolveBattleAnchors();
            HideAnchorGraphics();

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
                BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation ||
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
                BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation)
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

        public bool CompleteCombatTransitionAfterMotion()
        {
            if (BattleFlowController.CurrentPhase !=
                BattlePhase.CombatTransition)
            {
                return false;
            }

            if (!SynchronizeBattleWorldAnchors())
            {
                Debug.LogError(
                    "背包FEEL动画已经结束，但UI锚点无法换算到" +
                    "战斗世界，战斗保持在过渡阶段。",
                    this);
                return false;
            }

            return BattleFlowController.EnsureInstance()
                .CompleteCombatTransition();
        }

        public bool SynchronizeBattleWorldAnchors()
        {
            ResolveBattleAnchors();

            Camera worldCamera = Camera.main;
            EnemyBackpackSystem enemySystem =
                FindEnemyBackpackSystem();
            BackpackCombatController enemy =
                enemySystem != null
                    ? enemySystem.CombatController
                    : null;
            CurvedConnectionRenderer curve =
                FindAnyObjectByType<
                    CurvedConnectionRenderer>(
                    FindObjectsInactive.Include);

            if (aircraftSpawnAnchor == null ||
                collisionCenterAnchor == null ||
                worldCamera == null ||
                combatController == null ||
                fighterSpawner == null ||
                enemy == null ||
                enemy.FighterSpawner == null ||
                enemySystem.AircraftSpawnAnchor == null ||
                enemySystem.CollisionCenterAnchor == null ||
                curve == null)
            {
                return false;
            }

            Canvas.ForceUpdateCanvases();

            Camera uiCamera =
                GetCanvasCamera(aircraftSpawnAnchor);
            Vector2 playerSpawnScreen =
                RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    aircraftSpawnAnchor.position);
            Vector2 playerCenterScreen =
                RectTransformUtility.WorldToScreenPoint(
                    GetCanvasCamera(collisionCenterAnchor),
                    collisionCenterAnchor.position);

            Vector2 enemySpawnScreen =
                RectTransformUtility.WorldToScreenPoint(
                    GetCanvasCamera(
                        enemySystem.AircraftSpawnAnchor),
                    enemySystem.AircraftSpawnAnchor.position);
            Vector2 enemyCenterScreen =
                RectTransformUtility.WorldToScreenPoint(
                    GetCanvasCamera(
                        enemySystem.CollisionCenterAnchor),
                    enemySystem.CollisionCenterAnchor.position);

            if (!TryScreenPointToWorldOnPlane(
                    worldCamera,
                    playerSpawnScreen,
                    battleWorldPlaneZ,
                    out Vector3 playerSpawnWorld) ||
                !TryScreenPointToWorldOnPlane(
                    worldCamera,
                    playerCenterScreen,
                    battleWorldPlaneZ,
                    out Vector3 playerCenterWorld) ||
                !TryScreenPointToWorldOnPlane(
                    worldCamera,
                    enemySpawnScreen,
                    battleWorldPlaneZ,
                    out Vector3 enemySpawnWorld) ||
                !TryScreenPointToWorldOnPlane(
                    worldCamera,
                    enemyCenterScreen,
                    battleWorldPlaneZ,
                    out Vector3 enemyCenterWorld))
            {
                return false;
            }

            combatController.transform.position =
                playerCenterWorld;
            enemy.transform.position = enemyCenterWorld;

            if (!fighterSpawner.SetSpawnPointWorldPosition(
                    playerSpawnWorld) ||
                !enemy.FighterSpawner
                    .SetSpawnPointWorldPosition(
                        enemySpawnWorld))
            {
                return false;
            }

            fighterSpawner.SetMaximumBendDistance(
                curve.MaxBendDistance);
            enemy.FighterSpawner.SetMaximumBendDistance(
                curve.MaxBendDistance);

            curve.SetEndpoints(
                fighterSpawner.SpawnPoint,
                enemy.FighterSpawner.SpawnPoint);

            return true;
        }

        public static Vector2 MirrorScreenPointVertically(
            Vector2 screenPoint,
            float screenHeight)
        {
            return new Vector2(
                screenPoint.x,
                Mathf.Max(0f, screenHeight) -
                screenPoint.y);
        }

        public static bool TryScreenPointToWorldOnPlane(
            Camera camera,
            Vector2 screenPoint,
            float planeZ,
            out Vector3 worldPoint)
        {
            worldPoint = default;

            if (camera == null)
            {
                return false;
            }

            Plane plane =
                new Plane(
                    Vector3.forward,
                    new Vector3(0f, 0f, planeZ));
            Ray ray = camera.ScreenPointToRay(screenPoint);

            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            worldPoint = ray.GetPoint(distance);
            worldPoint.z = planeZ;
            return true;
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

        private BackpackCombatController
            FindEnemyCombatController()
        {
            foreach (BackpackCombatController candidate
                     in BackpackCombatController
                         .ActiveControllers)
            {
                if (candidate != null &&
                    candidate != combatController &&
                    candidate.Faction !=
                    combatController.Faction)
                {
                    return candidate;
                }
            }

            return null;
        }

        private EnemyBackpackSystem
            FindEnemyBackpackSystem()
        {
            foreach (EnemyBackpackSystem candidate in
                     FindObjectsByType<EnemyBackpackSystem>(
                         FindObjectsInactive.Include))
            {
                if (candidate != null &&
                    candidate.CombatController != null &&
                    candidate.CombatController.Faction !=
                    combatController.Faction)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void ResolveBattleAnchors()
        {
            if (aircraftSpawnAnchor != null &&
                collisionCenterAnchor != null)
            {
                return;
            }

            foreach (RectTransform rectTransform in
                     GetComponentsInChildren<
                         RectTransform>(true))
            {
                if (rectTransform.name ==
                    AircraftAnchorName)
                {
                    aircraftSpawnAnchor = rectTransform;
                }
                else if (rectTransform.name ==
                         CollisionAnchorName)
                {
                    collisionCenterAnchor =
                        rectTransform;
                }
            }
        }

        private void HideAnchorGraphics()
        {
            SetAnchorGraphicVisible(
                aircraftSpawnAnchor,
                false);
            SetAnchorGraphicVisible(
                collisionCenterAnchor,
                false);
        }

        private static void SetAnchorGraphicVisible(
            RectTransform anchor,
            bool visible)
        {
            if (anchor != null &&
                anchor.TryGetComponent(
                    out Graphic graphic))
            {
                graphic.enabled = visible;
            }
        }

        private static Camera GetCanvasCamera(
            RectTransform anchor)
        {
            Canvas canvas =
                anchor != null
                    ? anchor.GetComponentInParent<
                        Canvas>()
                    : null;

            return canvas != null &&
                   canvas.renderMode !=
                   RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
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
