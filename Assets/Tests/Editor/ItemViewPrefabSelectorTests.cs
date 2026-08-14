using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class ItemViewPrefabSelectorTests
{
    [Test]
    public void Select_LShape_UsesItsMissingCornerTemplate()
    {
        ItemShapeData shape = ScriptableObject.CreateInstance<ItemShapeData>();
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        GameObject fallbackRoot = new GameObject("fallback");
        GameObject expectedRoot = new GameObject("missing-bottom-right");
        try
        {
            shape.InitializeForTests("L", null, new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1),
            });
            item.InitializeForTests("L Item", ItemType.Aircraft, 1f, shape);
            ItemView fallback = fallbackRoot.AddComponent<ItemView>();
            ItemView expected = expectedRoot.AddComponent<ItemView>();

            ItemView selected = ItemViewPrefabSelector.Select(
                item, fallback, null, null, null, expected, null, null);

            Assert.That(selected, Is.SameAs(expected));
        }
        finally
        {
            Object.DestroyImmediate(shape);
            Object.DestroyImmediate(item);
            Object.DestroyImmediate(fallbackRoot);
            Object.DestroyImmediate(expectedRoot);
        }
    }
}
