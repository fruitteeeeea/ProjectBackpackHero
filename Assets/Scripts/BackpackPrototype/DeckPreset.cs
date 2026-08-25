using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>可由玩家和敌人共用的五槽 Hanger Deck 配置。</summary>
    [CreateAssetMenu(fileName = "DeckPreset", menuName = "Backpack Prototype/Deck Preset")]
    public sealed class DeckPreset : ScriptableObject
    {
        [SerializeField] private List<ItemData> slots = new();
        public IReadOnlyList<ItemData> Slots => slots;

        public bool IsValid(out string error)
        {
            return IsValidSlots(slots, out error);
        }

        public static bool IsValidSlots(
            IReadOnlyList<ItemData> candidateSlots,
            out string error)
        {
            error = null;
            if (candidateSlots == null ||
                candidateSlots.Count != PlayerItemSystem.DeckSlotCount)
            {
                error = $"Deck 预设必须包含 {PlayerItemSystem.DeckSlotCount} 个槽位。";
                return false;
            }
            var used = new HashSet<string>();
            for (int index = 0; index < candidateSlots.Count; index++)
            {
                ItemData item = candidateSlots[index];
                if (item == null) continue;
                if (!PlayerItemSystem.IsDeckSlotType(item, index)) { error = $"槽位 {index + 1} 的物品类型不匹配。"; return false; }
                if (string.IsNullOrWhiteSpace(item.ItemId) || !used.Add(item.ItemId)) { error = $"槽位 {index + 1} 的物品无效或重复。"; return false; }
            }
            return true;
        }

        public void SetSlots(IEnumerable<ItemData> items)
        {
            slots = items != null ? new List<ItemData>(items) : new List<ItemData>();
            NormalizeSlotCount();
        }

        private void OnValidate() => NormalizeSlotCount();
        private void NormalizeSlotCount()
        {
            slots ??= new List<ItemData>();
            while (slots.Count < PlayerItemSystem.DeckSlotCount) slots.Add(null);
            if (slots.Count > PlayerItemSystem.DeckSlotCount) slots.RemoveRange(PlayerItemSystem.DeckSlotCount, slots.Count - PlayerItemSystem.DeckSlotCount);
        }
    }

    /// <summary>将无坐标 Deck 按稳定的首个可用格规则转为战斗布局。</summary>
    public static class DeckLayoutBuilder
    {
        public static bool TryBuild(IReadOnlyList<ItemData> deck, int width, int height, Func<ItemData, int> levelForItem, out List<BackpackLayoutItem> layout)
        {
            layout = new List<BackpackLayoutItem>();
            if (deck == null) return false;
            var validation = new BackpackController(width, height);
            int placementId = 0;
            foreach (ItemData item in deck)
            {
                if (item == null) continue;
                bool placed = false;
                for (int y = 0; y < validation.Height && !placed; y++)
                for (int x = 0; x < validation.Width; x++)
                {
                    Vector2Int cell = new(x, y);
                    var candidate = new ItemInstance($"deck-layout-{++placementId}", item, cell);
                    if (!validation.PlaceItem(candidate, cell)) continue;
                    layout.Add(new BackpackLayoutItem(item, cell, levelForItem?.Invoke(item) ?? ItemInstance.DefaultLevel));
                    placed = true;
                    break;
                }
                if (!placed) { layout = null; return false; }
            }
            return true;
        }
    }
}
