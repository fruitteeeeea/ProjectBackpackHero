using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BackpackPrototype
{
    public enum PackId { Green = 10001, Blue = 10002, Purple = 10003, Gold = 10004 }
    public enum PackState { Empty, Start, Locked, Opening, Opened }

    [Serializable] public sealed class PackSlotData { public PackId Id; public bool HasPack; public long OpenTimeUtcMs; }
    [Serializable] public sealed class PackSaveData { public List<PackSlotData> Slots = new(); }
    public readonly struct PackReward { public readonly int Gold, Diamond; public readonly IReadOnlyDictionary<ItemData, int> Fragments; public PackReward(int gold, int diamond, IReadOnlyDictionary<ItemData, int> fragments) { Gold = gold; Diamond = diamond; Fragments = fragments; } }

    /// <summary>Persistent four-slot pack queue. Reward settlement is intentionally separate from UI animation.</summary>
    public sealed class PackSystem : MonoBehaviour
    {
        public const string SaveKey = "PlayerPackModel";
        public const int SlotCount = 4;
        const string CatalogResourcePath = "PackUI/PackCatalog";
        static readonly IReadOnlyList<PackDefinition> EmptyDefinitions = Array.Empty<PackDefinition>();
        PackSaveData data;
        PackCatalog catalog;
        public static PackSystem Instance { get; private set; }
        public event Action Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void EnsureInstance() { if (Instance != null) return; var go = new GameObject(nameof(PackSystem)); DontDestroyOnLoad(go); go.AddComponent<PackSystem>(); }
        void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; DontDestroyOnLoad(gameObject); catalog = Resources.Load<PackCatalog>(CatalogResourcePath); if (catalog == null) Debug.LogError($"Pack catalog is missing from Resources/{CatalogResourcePath}.", this); Load(); if (GetComponent<PackMenuPresenter>() == null) gameObject.AddComponent<PackMenuPresenter>(); }
        void OnDestroy() { if (Instance == this) Instance = null; }
        IReadOnlyList<PackDefinition> Definitions => catalog != null ? catalog.Definitions : EmptyDefinitions;
        int SecondsPerDiamond => catalog != null ? catalog.SecondsPerDiamond : 60;
        public IReadOnlyList<PackSlotData> GetSlots() => data.Slots;
        public PackDefinition GetDefinition(PackId id) => catalog != null ? catalog.GetDefinition(id) : null;
        public bool TryAddPack(PackId id) { int slot = data.Slots.FindIndex(x => !x.HasPack); if (slot < 0 || GetDefinition(id) == null) return false; data.Slots[slot] = new PackSlotData { Id = id, HasPack = true }; SaveAndNotify(); return true; }
        public bool TryAddRandomPack() { int roll = UnityEngine.Random.Range(0, Definitions.Sum(x => x.Weight)); foreach (var d in Definitions) { if (roll < d.Weight) return TryAddPack(d.Id); roll -= d.Weight; } return false; }
        public PackState GetSlotState(int index) { var slot = Slot(index); if (slot == null || !slot.HasPack) return PackState.Empty; if (slot.OpenTimeUtcMs > 0) return GetRemainingSeconds(index) <= 0 ? PackState.Opened : PackState.Opening; for (int i = 0; i < data.Slots.Count; i++) { var other = data.Slots[i]; if (i != index && other.HasPack && other.OpenTimeUtcMs > 0 && GetRemainingSeconds(i) > 0) return PackState.Locked; } return PackState.Start; }
        public int GetRemainingSeconds(int index) { var slot = Slot(index); if (slot == null || !slot.HasPack || slot.OpenTimeUtcMs <= 0) return 0; var d = GetDefinition(slot.Id); if (d == null) return 0; return Mathf.Max(0, d.OpenSeconds - (int)Math.Floor((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - slot.OpenTimeUtcMs) / 1000d)); }
        public int GetSkipDiamondCost(int index) { var state = GetSlotState(index); if (state == PackState.Empty || state == PackState.Start || state == PackState.Opened) return 0; int seconds = state == PackState.Locked ? GetDefinition(Slot(index).Id).OpenSeconds : GetRemainingSeconds(index); return Mathf.CeilToInt(seconds / (float)SecondsPerDiamond); }
        public bool TryStartPack(int index) { if (GetSlotState(index) != PackState.Start) return false; Slot(index).OpenTimeUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); SaveAndNotify(); return true; }
        public bool TryOpenPack(int index) => GetSlotState(index) == PackState.Opened;
        public bool TrySkipAndOpenPack(int index) { var state = GetSlotState(index); if (state != PackState.Locked && state != PackState.Opening) return false; int cost = GetSkipDiamondCost(index); var player = PlayerItemSystem.Instance; if (player == null || player.Diamond < cost) return false; player.AddCurrency(0, -cost); var slot = Slot(index); slot.OpenTimeUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - GetDefinition(slot.Id).OpenSeconds * 1000L; SaveAndNotify(); return true; }
        public bool ReduceOpenTime(int index, int seconds) { var slot = Slot(index); if (slot == null || GetSlotState(index) != PackState.Opening || seconds <= 0) return false; slot.OpenTimeUtcMs -= seconds * 1000L; SaveAndNotify(); return true; }
        /// <summary>Clears all pack slots without touching player currencies or item progression.</summary>
        public void ClearAllPacks() { for (int i = 0; i < SlotCount; i++) data.Slots[i] = new PackSlotData(); SaveAndNotify(); }
        public bool TrySettleReward(int index, out PackReward reward) { reward = default; if (!TryOpenPack(index)) return false; var d = GetDefinition(Slot(index).Id); var player = PlayerItemSystem.Instance; if (d == null || player == null) return false; int gold = UnityEngine.Random.Range(d.GoldMin, d.GoldMax), diamond = UnityEngine.Random.Range(d.DiamondMin, d.DiamondMax), draws = UnityEngine.Random.Range(d.FragmentMin, d.FragmentMax); var pool = player.GetAllItems().Where(x => x != null && player.IsUnlocked(x)).ToArray(); var fragments = new Dictionary<ItemData, int>(); for (int i = 0; i < draws && pool.Length > 0; i++) { var item = pool[UnityEngine.Random.Range(0, pool.Length)]; fragments[item] = fragments.TryGetValue(item, out int value) ? value + 1 : 1; } player.AddCurrency(gold, diamond); foreach (var item in fragments) player.AddFragments(item.Key, item.Value); data.Slots[index] = new PackSlotData(); SaveAndNotify(); reward = new PackReward(gold, diamond, fragments); return true; }
        PackSlotData Slot(int index) => index >= 0 && index < SlotCount ? data?.Slots[index] : null;
        void Load() { try { data = JsonUtility.FromJson<PackSaveData>(PlayerPrefs.GetString(SaveKey)); } catch { data = null; } data ??= new PackSaveData(); data.Slots ??= new List<PackSlotData>(); while (data.Slots.Count < SlotCount) data.Slots.Add(new PackSlotData()); if (data.Slots.Count > SlotCount) data.Slots.RemoveRange(SlotCount, data.Slots.Count - SlotCount); Save(); }
        void Save() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
        void SaveAndNotify() { Save(); Changed?.Invoke(); }
    }
}
