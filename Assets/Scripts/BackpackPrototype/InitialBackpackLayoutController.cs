using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>
    /// 新对局背包布局的唯一入口。玩家和敌人共享布局生成、预校验和应用流程，
    /// 以确保任何一方都不会在无效布局时先清空现有背包。
    /// </summary>
    public static class InitialBackpackLayoutController
    {
        public static bool TryBuild(
            IReadOnlyList<ItemData> items,
            int width,
            int height,
            Func<ItemData, int> levelForItem,
            out List<BackpackLayoutItem> layout)
        {
            layout = null;
            if (!EnemyBackpackLayoutPlanner.TryBuild(
                    items, width, height,
                    out List<BackpackLayoutItem> generated))
            {
                return false;
            }

            layout = new List<BackpackLayoutItem>(generated.Count);
            foreach (BackpackLayoutItem placement in generated)
            {
                layout.Add(new BackpackLayoutItem(
                    placement.Data,
                    placement.AnchorCell,
                    levelForItem?.Invoke(placement.Data) ?? placement.Level));
            }

            return true;
        }

        public static bool TryApply(
            BackpackCombatController combatController,
            IReadOnlyList<BackpackLayoutItem> layout,
            Action<ItemInstance> onItemPlaced,
            out string failureReason)
        {
            failureReason = null;
            BackpackController backpack = combatController?.Backpack;
            if (backpack == null || layout == null)
            {
                failureReason = "背包或布局不可用。";
                return false;
            }

            BackpackController validation = new(backpack.Width, backpack.Height);
            for (int index = 0; index < layout.Count; index++)
            {
                BackpackLayoutItem placement = layout[index];
                if (placement.Data == null || !validation.PlaceItem(
                        new ItemInstance(
                            $"initial-layout-validation-{index}",
                            placement.Data,
                            placement.AnchorCell,
                            placement.Level),
                        placement.AnchorCell))
                {
                    failureReason = $"布局条目 {index} 无法放置。";
                    return false;
                }
            }

            // 预校验和实际应用使用同一 BackpackController 放置规则；只有完全可放置
            // 时才清空旧背包，避免布局构建失败造成空背包。
            List<BackpackLayoutItem> previousLayout = new();
            foreach (ItemInstance item in backpack.Items)
            {
                if (item?.Data != null)
                {
                    previousLayout.Add(new BackpackLayoutItem(
                        item.Data, item.AnchorCell, item.Level));
                }
            }

            combatController.Clear();
            foreach (BackpackLayoutItem placement in layout)
            {
                ItemInstance item = combatController.AddItem(
                    placement.Data,
                    placement.AnchorCell,
                    placement.Level);
                if (item == null)
                {
                    failureReason = "预校验通过后应用布局失败。";
                    combatController.Clear();
                    foreach (BackpackLayoutItem previous in previousLayout)
                    {
                        ItemInstance restored = combatController.AddItem(
                            previous.Data, previous.AnchorCell, previous.Level);
                        onItemPlaced?.Invoke(restored);
                    }
                    Debug.LogError(failureReason, combatController);
                    return false;
                }

                onItemPlaced?.Invoke(item);
            }

            return true;
        }
    }
}
