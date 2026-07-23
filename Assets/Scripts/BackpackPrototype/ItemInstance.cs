using UnityEngine;

namespace BackpackPrototype
{
    public sealed class ItemInstance
    {
        public ItemInstance(
            string id,
            ItemData data,
            Vector2Int anchorCell)
        {
            Id = id;
            Data = data;
            AnchorCell = anchorCell;
        }

        public string Id { get; }
        public ItemData Data { get; }
        public Vector2Int AnchorCell { get; set; }
    }
}
