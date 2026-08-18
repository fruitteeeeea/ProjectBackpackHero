using System;
using System.Collections.Generic;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Battle
{
    public enum DamageStatisticsItemCategory { Aircraft, Equipment }

    public readonly struct DamageStatisticsEntry
    {
        public DamageStatisticsEntry(ItemData data, int level, DamageStatisticsItemCategory category, int count, float raw, float actual, int kills)
        { Data = data; Level = level; Category = category; BackpackCount = count; RawDamage = raw; ActualDamage = actual; KillCount = kills; }
        public ItemData Data { get; } public int Level { get; } public DamageStatisticsItemCategory Category { get; }
        public int BackpackCount { get; } public float RawDamage { get; } public float ActualDamage { get; } public int KillCount { get; }
    }

    /// <summary>本场战斗的背包伤害归因统计。只接收携带ItemInstance的攻击。</summary>
    [DisallowMultipleComponent]
    public sealed class DamageStatisticsRuntime : MonoBehaviour
    {
        private sealed class MutableEntry { public ItemData Data; public int Level; public DamageStatisticsItemCategory Category; public int Count; public float Raw; public float Actual; public int Kills; }
        private readonly Dictionary<BattleFaction, Dictionary<string, MutableEntry>> entries = new();
        public static DamageStatisticsRuntime Instance { get; private set; }
        public static event Action Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureGlobal() { if (Instance == null) new GameObject("Damage Statistics Runtime").AddComponent<DamageStatisticsRuntime>(); }
        private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; DontDestroyOnLoad(gameObject); BattleFlowController.PhaseChanged += HandlePhaseChanged; }
        private void OnDestroy() { BattleFlowController.PhaseChanged -= HandlePhaseChanged; if (Instance == this) Instance = null; }
        private void HandlePhaseChanged(BattlePhase phase) { if (phase == BattlePhase.Combat) Clear(); }
        public void Clear() { entries.Clear(); Changed?.Invoke(); }
        public static void RecordHit(BattleFaction faction, BattleDamageSource source, float raw, float actual, bool killedFighter) => Instance?.Record(faction, source, raw, actual, killedFighter);
        private void Record(BattleFaction faction, BattleDamageSource source, float raw, float actual, bool killedFighter)
        {
            if (!source.HasBackpackItem || actual <= 0f) return;
            ItemInstance item = source.Item; ItemData data = item.Data;
            if (!entries.TryGetValue(faction, out Dictionary<string, MutableEntry> factionEntries)) entries[faction] = factionEntries = new();
            DamageStatisticsItemCategory category = data.ItemType == ItemType.Aircraft ? DamageStatisticsItemCategory.Aircraft : DamageStatisticsItemCategory.Equipment;
            string key = data.GetEntityId() + ":" + item.Level + ":" + category;
            if (!factionEntries.TryGetValue(key, out MutableEntry entry)) factionEntries[key] = entry = new MutableEntry { Data = data, Level = item.Level, Category = category, Count = CountItems(faction, data, item.Level) };
            entry.Raw += Mathf.Max(0f, raw); entry.Actual += Mathf.Max(0f, actual); if (killedFighter) entry.Kills++; Changed?.Invoke();
        }
        private static int CountItems(BattleFaction faction, ItemData data, int level)
        { int count = 0; foreach (BackpackCombatController controller in BackpackCombatController.ActiveControllers) if (controller != null && controller.Faction == faction) foreach (ItemInstance item in controller.Items) if (item?.Data == data && item.Level == level) count++; return count; }
        public IReadOnlyList<DamageStatisticsEntry> GetEntries(BattleFaction faction)
        { var result = new List<DamageStatisticsEntry>(); if (!entries.TryGetValue(faction, out Dictionary<string, MutableEntry> values)) return result; foreach (MutableEntry e in values.Values) result.Add(new DamageStatisticsEntry(e.Data,e.Level,e.Category,CountItems(faction,e.Data,e.Level),e.Raw,e.Actual,e.Kills)); return result; }
    }
}
