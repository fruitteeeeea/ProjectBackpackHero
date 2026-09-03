using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BackpackPrototype
{
    [Serializable] public sealed class PlayerItemState { public string ItemId; public bool Unlocked; public int Level; public int FragmentCount; }
    [Serializable] public sealed class PlayerItemSaveData { public int Gold = 100; public int Diamond; public int CurrencyDefaultsVersion; public int ProgressionVersion; public List<PlayerItemState> Items = new(); public List<string> DeckItemIds; }
    public enum PlayerItemUpgradeResult { Success, NotFound, Locked, MaxLevel, GoldNotEnough, FragmentsNotEnough }
    public enum PlayerDeckResult { Success, InvalidSlot, InvalidItem, Locked, WrongType, Duplicate, Empty }

    public sealed class PlayerItemSystem : MonoBehaviour
    {
        public const string SaveKey = "PlayerItemModel";
        private const int CurrencyDefaultsVersion = 2;
        private const int CurrentProgressionVersion = 2;
        private const int InitialGold = 100;
        private const int InitialDiamond = 0;
        private static readonly string[] InitialItemIds =
        {
            "aircraft_charge", "aircraft_first", "aircraft_shield",
            "equipment_1x2", "equipment_arc_coil"
        };
        // Kept separate from ItemInstance.MaximumLevel: this is persistent,
        // out-of-match progression, while ItemInstance owns the current-run level.
        public const int DefaultLevel = 1;
        public const int MaximumLevel = 10;
        public const int AircraftDeckSlotCount = 3;
        public const int EquipmentDeckSlotCount = 2;
        public const int DeckSlotCount = AircraftDeckSlotCount + EquipmentDeckSlotCount;
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
        public int GetLevel(ItemData item) => Mathf.Clamp(GetState(item)?.Level ?? DefaultLevel, DefaultLevel, MaximumLevel);
        public bool IsUnlocked(ItemData item) => GetState(item)?.Unlocked ?? false;
        public int GetFragments(ItemData item) => GetState(item)?.FragmentCount ?? 0;
        public int GetUpgradeFragmentCost(ItemData item) =>
            item == null || GetLevel(item) >= MaximumLevel
                ? 0
                : item.GetUpgradeFragmentCost(GetLevel(item));
        public PlayerItemState GetState(ItemData item) => item == null ? null : data?.Items.FirstOrDefault(x => x.ItemId == item.ItemId);
        public IReadOnlyList<ItemData> GetDeckItems()
        {
            EnsureDeck();
            return data.DeckItemIds.Select(FindItemById).ToArray();
        }
        public ItemData GetDeckItem(int slot)
        {
            EnsureDeck();
            return IsValidDeckSlot(slot) ? FindItemById(data.DeckItemIds[slot]) : null;
        }
        public bool IsEquipped(ItemData item) => item != null && data != null && data.DeckItemIds != null && data.DeckItemIds.Contains(item.ItemId);
        public bool IsDeckSlotFor(ItemData item, int slot) => IsDeckSlotType(item, slot);
        public static bool IsDeckSlotType(ItemData item, int slot) => item != null && slot >= 0 && slot < DeckSlotCount &&
            (slot < AircraftDeckSlotCount ? item.ItemType == ItemType.Aircraft : item.ItemType == ItemType.Equipment);
        public PlayerDeckResult TryEquipDeckSlot(int slot, ItemData item)
        {
            EnsureDeck();
            if (!IsValidDeckSlot(slot)) return PlayerDeckResult.InvalidSlot;
            if (item == null || FindItemById(item.ItemId) != item) return PlayerDeckResult.InvalidItem;
            if (!IsUnlocked(item)) return PlayerDeckResult.Locked;
            if (!IsDeckSlotFor(item, slot)) return PlayerDeckResult.WrongType;
            int existing = data.DeckItemIds.IndexOf(item.ItemId);
            if (existing >= 0 && existing != slot) return PlayerDeckResult.Duplicate;
            data.DeckItemIds[slot] = item.ItemId;
            SaveAndNotify();
            return PlayerDeckResult.Success;
        }
        public PlayerDeckResult ClearDeckSlot(int slot)
        {
            EnsureDeck();
            if (!IsValidDeckSlot(slot)) return PlayerDeckResult.InvalidSlot;
            if (string.IsNullOrEmpty(data.DeckItemIds[slot])) return PlayerDeckResult.Empty;
            data.DeckItemIds[slot] = null;
            SaveAndNotify();
            return PlayerDeckResult.Success;
        }
        public PlayerDeckResult TryApplyDeck(IReadOnlyList<ItemData> deck)
        {
            EnsureDeck();
            if (deck == null || deck.Count != DeckSlotCount) return PlayerDeckResult.InvalidSlot;
            var ids = new List<string>(DeckSlotCount);
            var used = new HashSet<string>();
            for (int slot = 0; slot < DeckSlotCount; slot++)
            {
                ItemData item = deck[slot];
                if (item == null) { ids.Add(null); continue; }
                if (FindItemById(item.ItemId) != item) return PlayerDeckResult.InvalidItem;
                if (!IsUnlocked(item)) return PlayerDeckResult.Locked;
                if (!IsDeckSlotFor(item, slot)) return PlayerDeckResult.WrongType;
                if (!used.Add(item.ItemId)) return PlayerDeckResult.Duplicate;
                ids.Add(item.ItemId);
            }
            data.DeckItemIds = ids;
            SaveAndNotify();
            return PlayerDeckResult.Success;
        }

        public void AddCurrency(int gold, int diamond = 0) { data.Gold = Mathf.Max(0, data.Gold + gold); data.Diamond = Mathf.Max(0, data.Diamond + diamond); SaveAndNotify(); }
        public void AddFragments(ItemData item, int count) { var state = GetState(item); if (state == null) return; state.FragmentCount = Mathf.Max(0, state.FragmentCount + count); SaveAndNotify(); }
        public void Unlock(ItemData item) { var state = GetState(item); if (state == null || state.Unlocked) return; state.Unlocked = true; SaveAndNotify(); }

        /// <summary>Debug helper: resets only unlocked catalog items, keeping all other progression unchanged.</summary>
        public void ResetAllProgression()
        {
            if (data == null) return;
            foreach (ItemData item in GetAllItems())
            {
                PlayerItemState state = GetState(item);
                if (state == null || !state.Unlocked) continue;
                state.Level = DefaultLevel;
                state.FragmentCount = 0;
            }
            SaveAndNotify();
        }

        /// <summary>Debug helper: restores the release player's five-card starter collection and deck.</summary>
        public void RestoreInitialProgression()
        {
            if (data == null || catalog == null) return;
            ApplyFormalInitialCollection();
            EnsureDeck();
            BackpackHero.Progression.RankProgressionSystem.Instance?.RestoreInitialProgression();
            SaveAndNotify();
        }

        /// <summary>Debug helper: adds the same number of item-specific fragments to every aircraft.</summary>
        public void AddFragmentsToAllAircraft(int count)
        {
            if (data == null || count <= 0) return;
            foreach (ItemData item in GetAllItems())
            {
                if (item == null || item.ItemType != ItemType.Aircraft) continue;
                PlayerItemState state = GetState(item);
                if (state != null) state.FragmentCount = Mathf.Max(0, state.FragmentCount + count);
            }
            SaveAndNotify();
        }

        /// <summary>Debug helper: restores a fully unlocked, max-level collection.</summary>
        public void RestoreDefaultProgression()
        {
            if (data == null) return;
            foreach (ItemData item in GetAllItems())
            {
                PlayerItemState state = GetState(item);
                if (state == null) continue;
                state.Unlocked = true;
                state.Level = MaximumLevel;
                state.FragmentCount = 0;
            }
            BackpackHero.Progression.RankProgressionSystem.Instance?.RestoreDefaultProgression();
            SaveAndNotify();
        }

        public PlayerItemUpgradeResult TryUpgrade(ItemData item)
        {
            var state = GetState(item);
            if (state == null) return PlayerItemUpgradeResult.NotFound;
            if (!state.Unlocked) return PlayerItemUpgradeResult.Locked;
            if (state.Level >= MaximumLevel) return PlayerItemUpgradeResult.MaxLevel;
            int fragments = item.GetUpgradeFragmentCost(state.Level);
            if (state.FragmentCount < fragments) return PlayerItemUpgradeResult.FragmentsNotEnough;
            state.FragmentCount -= fragments;
            state.Level++;
            SaveAndNotify();
            return PlayerItemUpgradeResult.Success;
        }

        private void Load()
        {
            data = string.IsNullOrEmpty(PlayerPrefs.GetString(SaveKey)) ? new PlayerItemSaveData() : JsonUtility.FromJson<PlayerItemSaveData>(PlayerPrefs.GetString(SaveKey)) ?? new PlayerItemSaveData();
            data.Items ??= new List<PlayerItemState>();
            string catalogError = null;
            if (catalog != null && catalog.IsValid(out catalogError))
            {
                if (data.ProgressionVersion < CurrentProgressionVersion)
                {
                    // Pre-release saves were deliberately created with every card at Lv10.
                    // Move them to the real release starting collection exactly once.
                    ApplyFormalInitialCollection();
                }
                else
                {
                    foreach (ItemData item in catalog.Items)
                    {
                        PlayerItemState state = GetState(item);
                        if (state == null)
                        {
                            data.Items.Add(new PlayerItemState { ItemId = item.ItemId, Unlocked = false, Level = DefaultLevel, FragmentCount = 0 });
                        }
                        else state.Level = Mathf.Clamp(state.Level, DefaultLevel, MaximumLevel);
                    }
                }
            }
            else if (catalog != null) Debug.LogError(catalogError, catalog);
            EnsureDeck();
            Save();
        }

        private void ApplyFormalInitialCollection()
        {
            data.Items.Clear();
            var initialIds = new HashSet<string>(InitialItemIds);
            foreach (ItemData item in catalog.Items)
            {
                if (item == null) continue;
                data.Items.Add(new PlayerItemState
                {
                    ItemId = item.ItemId,
                    Unlocked = initialIds.Contains(item.ItemId),
                    Level = DefaultLevel,
                    FragmentCount = 0
                });
            }
            data.DeckItemIds = null;
            data.Gold = InitialGold;
            data.Diamond = InitialDiamond;
            data.CurrencyDefaultsVersion = CurrencyDefaultsVersion;
            data.ProgressionVersion = CurrentProgressionVersion;
        }
        private void EnsureDeck()
        {
            if (data == null) return;
            bool needsInitialDeck = data.DeckItemIds == null;
            data.DeckItemIds ??= new List<string>();
            while (data.DeckItemIds.Count < DeckSlotCount) data.DeckItemIds.Add(null);
            if (data.DeckItemIds.Count > DeckSlotCount) data.DeckItemIds.RemoveRange(DeckSlotCount, data.DeckItemIds.Count - DeckSlotCount);
            var used = new HashSet<string>();
            for (int slot = 0; slot < DeckSlotCount; slot++)
            {
                ItemData item = FindItemById(data.DeckItemIds[slot]);
                if (item == null || !IsUnlocked(item) || !IsDeckSlotFor(item, slot) || !used.Add(item.ItemId)) data.DeckItemIds[slot] = null;
            }
            if (!needsInitialDeck || catalog == null) return;
            FillInitialDeck(new[] { "aircraft_first", "aircraft_charge", "aircraft_shield", "equipment_rapid_cannon", "equipment_1x2" }, used);
            FillInitialDeck(ItemType.Aircraft, 0, AircraftDeckSlotCount, used);
            FillInitialDeck(ItemType.Equipment, AircraftDeckSlotCount, EquipmentDeckSlotCount, used);
        }
        private void FillInitialDeck(IEnumerable<string> ids, HashSet<string> used)
        {
            int slot = 0;
            foreach (string id in ids)
            {
                while (slot < DeckSlotCount && !string.IsNullOrEmpty(data.DeckItemIds[slot])) slot++;
                if (slot >= DeckSlotCount) return;
                ItemData item = FindItemById(id);
                if (item != null && IsDeckSlotFor(item, slot) && IsUnlocked(item) && used.Add(item.ItemId)) data.DeckItemIds[slot] = item.ItemId;
            }
        }
        private void FillInitialDeck(ItemType type, int firstSlot, int count, HashSet<string> used)
        {
            int slot = firstSlot;
            foreach (ItemData item in GetAllItems())
            {
                if (slot >= firstSlot + count) break;
                if (item == null || item.ItemType != type || !IsUnlocked(item) || !used.Add(item.ItemId)) continue;
                data.DeckItemIds[slot++] = item.ItemId;
            }
        }
        private bool IsValidDeckSlot(int slot) => slot >= 0 && slot < DeckSlotCount;
        private ItemData FindItemById(string id) => string.IsNullOrEmpty(id) || catalog == null ? null : catalog.Items.FirstOrDefault(item => item != null && item.ItemId == id);
        private void Save() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
        private void SaveAndNotify() { Save(); Changed?.Invoke(); }
    }
}
