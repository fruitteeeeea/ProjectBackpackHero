using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BackpackPrototype
{
    [Serializable] public sealed class PlayerItemState { public string ItemId; public bool Unlocked; public int Level; public int FragmentCount; }
    [Serializable] public sealed class PlayerItemSaveData { public int Gold; public int Diamond; public List<PlayerItemState> Items = new(); }
    public enum PlayerItemUpgradeResult { Success, NotFound, Locked, MaxLevel, GoldNotEnough, FragmentsNotEnough }

    public sealed class PlayerItemSystem : MonoBehaviour
    {
        public const string SaveKey = "PlayerItemModel";
        public const int MaximumLevel = ItemInstance.MaximumLevel;
        [SerializeField] private PlayerItemCatalog catalog;
        private PlayerItemSaveData data;
        public static PlayerItemSystem Instance { get; private set; }
        public event Action Changed;
        public int Gold => data?.Gold ?? 0;
        public int Diamond => data?.Diamond ?? 0;
        public PlayerItemCatalog Catalog => catalog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance != null) return;
            var root = new GameObject(nameof(PlayerItemSystem));
            DontDestroyOnLoad(root);
            root.AddComponent<PlayerItemSystem>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (catalog == null) catalog = Resources.Load<PlayerItemCatalog>("PlayerItemCatalog");
            Load();
            if (FindFirstObjectByType<PlayerItemHangarPresenter>(FindObjectsInactive.Include) == null)
                gameObject.AddComponent<PlayerItemHangarPresenter>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetCatalog(PlayerItemCatalog value) { catalog = value; Load(); Changed?.Invoke(); }
        public IReadOnlyList<ItemData> GetAllItems() => catalog?.Items ?? Array.Empty<ItemData>();
        public int GetLevel(ItemData item) => Mathf.Clamp(GetState(item)?.Level ?? ItemInstance.DefaultLevel, ItemInstance.DefaultLevel, MaximumLevel);
        public bool IsUnlocked(ItemData item) => GetState(item)?.Unlocked ?? false;
        public int GetFragments(ItemData item) => GetState(item)?.FragmentCount ?? 0;
        public PlayerItemState GetState(ItemData item) => item == null ? null : data?.Items.FirstOrDefault(x => x.ItemId == item.ItemId);

        public void AddCurrency(int gold, int diamond = 0) { data.Gold = Mathf.Max(0, data.Gold + gold); data.Diamond = Mathf.Max(0, data.Diamond + diamond); SaveAndNotify(); }
        public void AddFragments(ItemData item, int count) { var state = GetState(item); if (state == null) return; state.FragmentCount = Mathf.Max(0, state.FragmentCount + count); SaveAndNotify(); }
        public void Unlock(ItemData item) { var state = GetState(item); if (state == null || state.Unlocked) return; state.Unlocked = true; SaveAndNotify(); }
        public PlayerItemUpgradeResult TryUpgrade(ItemData item)
        {
            var state = GetState(item);
            if (state == null) return PlayerItemUpgradeResult.NotFound;
            if (!state.Unlocked) return PlayerItemUpgradeResult.Locked;
            if (state.Level >= MaximumLevel) return PlayerItemUpgradeResult.MaxLevel;
            if (data.Gold < item.UpgradeGoldCost) return PlayerItemUpgradeResult.GoldNotEnough;
            if (state.FragmentCount < item.UpgradeFragmentCost) return PlayerItemUpgradeResult.FragmentsNotEnough;
            data.Gold -= item.UpgradeGoldCost; state.FragmentCount -= item.UpgradeFragmentCost; state.Level++; SaveAndNotify();
            return PlayerItemUpgradeResult.Success;
        }

        private void Load()
        {
            data = string.IsNullOrEmpty(PlayerPrefs.GetString(SaveKey)) ? new PlayerItemSaveData() : JsonUtility.FromJson<PlayerItemSaveData>(PlayerPrefs.GetString(SaveKey)) ?? new PlayerItemSaveData();
            data.Items ??= new List<PlayerItemState>();
            string catalogError = null;
            if (catalog != null && catalog.IsValid(out catalogError))
            {
                foreach (ItemData item in catalog.Items)
                    if (GetState(item) == null)
                    {
                        data.Items.Add(new PlayerItemState { ItemId = item.ItemId, Unlocked = true, Level = MaximumLevel, FragmentCount = 0 });
                    }
            }
            else if (catalog != null) Debug.LogError(catalogError, catalog);
            Save();
        }
        private void Save() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
        private void SaveAndNotify() { Save(); Changed?.Invoke(); }
    }
}
