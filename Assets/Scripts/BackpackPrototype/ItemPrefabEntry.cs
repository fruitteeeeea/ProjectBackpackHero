using System;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>
    /// 将物品数据与其 UI 预制体配对，供正式玩家和敌人背包的目录使用。
    /// </summary>
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
}
