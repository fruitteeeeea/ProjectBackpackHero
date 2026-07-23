using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 一个阵营的运行时背包。
    /// 管理物品和冷却，但不直接生成飞机。
    /// </summary>
    public sealed class BattleBackpack
    {
        private readonly List<BattleBackpackItem> items =
            new List<BattleBackpackItem>();

        /// <summary>
        /// 某个物品完成冷却时触发。
        /// </summary>
        public event Action<BattleBackpackItem>
            ItemCharged;

        public IReadOnlyList<BattleBackpackItem> Items =>
            items;

        public int ItemCount =>
            items.Count;

        /// <summary>
        /// 推进背包中所有物品的冷却。
        /// </summary>
        public void Tick(float deltaTime)
        {
            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);

            // 使用普通for循环，方便以后支持运行时删除物品。
            for (int index = 0;
                 index < items.Count;
                 index++)
            {
                BattleBackpackItem item =
                    items[index];

                if (item == null)
                {
                    continue;
                }

                if (item.Tick(safeDeltaTime))
                {
                    ItemCharged?.Invoke(item);
                }
            }
        }

        /// <summary>
        /// 向背包添加一个机体物品。
        /// </summary>
        public BattleBackpackItem AddFighter(
            FighterDefinition definition,
            float cooldown,
            float initialDelay = 0f)
        {
            if (definition == null)
            {
                Debug.LogWarning(
                    "无法添加背包物品：FighterDefinition为空。");

                return null;
            }

            float safeCooldown =
                Mathf.Max(0.1f, cooldown);

            BattleBackpackItem item =
                new BattleBackpackItem(
                    definition,
                    safeCooldown,
                    initialDelay);

            items.Add(item);
            return item;
        }

        /// <summary>
        /// 按照物品引用删除。
        /// 调试窗口推荐调用这个版本。
        /// </summary>
        public bool RemoveItem(
            BattleBackpackItem item)
        {
            if (item == null)
            {
                return false;
            }

            return items.Remove(item);
        }

        /// <summary>
        /// 按照索引删除。
        /// </summary>
        public bool RemoveAt(int index)
        {
            if (index < 0 ||
                index >= items.Count)
            {
                return false;
            }

            items.RemoveAt(index);
            return true;
        }

        public BattleBackpackItem GetItem(int index)
        {
            if (index < 0 ||
                index >= items.Count)
            {
                return null;
            }

            return items[index];
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}