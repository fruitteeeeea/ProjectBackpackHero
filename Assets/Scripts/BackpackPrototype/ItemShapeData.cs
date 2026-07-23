using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    [CreateAssetMenu(menuName = "Backpack Prototype/Item Shape Data")]
    public sealed class ItemShapeData : ScriptableObject
    {
        [SerializeField] private string itemName = "Item";
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0.01f)]
        private float cooldownDuration = 2f;
        [SerializeField] private List<Vector2Int> shapeOffsets = new() { Vector2Int.zero };

        public string ItemName => itemName;
        public Sprite Icon => icon;
        public float CooldownDuration => cooldownDuration;
        public IReadOnlyList<Vector2Int> ShapeOffsets => shapeOffsets;
        
        public void InitializeForTests(
            string testName,
            Sprite testIcon,
            IReadOnlyList<Vector2Int> testShapeOffsets)
        {
            itemName = testName;
            icon = testIcon;
            shapeOffsets =
                testShapeOffsets != null
                    ? new List<Vector2Int>(testShapeOffsets)
                    : new List<Vector2Int>();

            if (shapeOffsets.Count == 0)
            {
                shapeOffsets.Add(Vector2Int.zero);
            }
        }
        
    }
}
