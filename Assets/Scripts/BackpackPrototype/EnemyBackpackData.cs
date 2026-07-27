using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    [Serializable]
    public sealed class BackpackLayoutEntry
    {
        [SerializeField]
        private ItemData data;

        [SerializeField]
        private Vector2Int anchorCell;

        public BackpackLayoutEntry(
            ItemData data,
            Vector2Int anchorCell)
        {
            this.data = data;
            this.anchorCell = anchorCell;
        }

        public ItemData Data => data;
        public Vector2Int AnchorCell => anchorCell;
    }

    [CreateAssetMenu(
        fileName = "EnemyBackpackData",
        menuName = "Backpack Prototype/Enemy Backpack Data")]
    public sealed class EnemyBackpackData : ScriptableObject
    {
        [SerializeField]
        private List<BackpackLayoutEntry> placements = new();

        public IReadOnlyList<BackpackLayoutEntry> Placements =>
            placements;

        public void InitializeForTests(
            IEnumerable<BackpackLayoutEntry> entries)
        {
            placements =
                entries != null
                    ? new List<BackpackLayoutEntry>(entries)
                    : new List<BackpackLayoutEntry>();
        }
    }
}
