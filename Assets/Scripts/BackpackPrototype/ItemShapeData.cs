using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    [CreateAssetMenu(
        menuName = "Backpack Prototype/Item Shape Data")]
    public sealed class ItemShapeData : ScriptableObject
    {
        [SerializeField]
        private List<Vector2Int> shapeOffsets =
            new() { Vector2Int.zero };

        public IReadOnlyList<Vector2Int> ShapeOffsets =>
            shapeOffsets;

        public void InitializeForTests(
            string unusedName,
            Sprite unusedIcon,
            IReadOnlyList<Vector2Int> testShapeOffsets)
        {
            shapeOffsets =
                testShapeOffsets != null
                    ? new List<Vector2Int>(
                        testShapeOffsets)
                    : new List<Vector2Int>();

            EnsureAtLeastOneCell();
        }

        private void OnValidate()
        {
            EnsureAtLeastOneCell();
        }

        private void EnsureAtLeastOneCell()
        {
            shapeOffsets ??= new List<Vector2Int>();

            if (shapeOffsets.Count == 0)
            {
                shapeOffsets.Add(Vector2Int.zero);
            }
        }
    }
}
