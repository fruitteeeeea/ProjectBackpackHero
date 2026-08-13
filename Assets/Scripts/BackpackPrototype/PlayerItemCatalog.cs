using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    [CreateAssetMenu(fileName = "PlayerItemCatalog", menuName = "Backpack Prototype/Player Item Catalog")]
    public sealed class PlayerItemCatalog : ScriptableObject
    {
        [SerializeField] private List<ItemData> items = new();
        public IReadOnlyList<ItemData> Items => items;

        /// <summary>Supports editor tests and editor tooling that construct a catalog in memory.</summary>
        public void SetItemsForTests(IEnumerable<ItemData> value)
        {
            items = value != null ? new List<ItemData>(value) : new List<ItemData>();
        }

        public bool IsValid(out string error)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (ItemData item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    error = "Each catalog entry needs a non-empty Item Id.";
                    return false;
                }
                if (!ids.Add(item.ItemId))
                {
                    error = $"Duplicate Item Id: {item.ItemId}";
                    return false;
                }
            }
            error = null;
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsValid(out string error)) Debug.LogError(error, this);
        }
#endif
    }
}
