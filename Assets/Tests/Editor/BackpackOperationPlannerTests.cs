using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class BackpackOperationPlannerTests
{
    [Test]
    public void ShopPlacement_WinsWhenItsStrengthGainExceedsShopMerge()
    {
        ItemShapeData shape = NewOneCellShape();
        ItemData aircraft = NewAircraft(shape);
        try
        {
            BackpackController backpack = new(2, 1);
            ItemInstance target = new("target", aircraft, Vector2Int.zero);
            Assert.That(backpack.PlaceItem(target, target.AnchorCell), Is.True);

            Assert.That(BackpackOperationPlanner.TrySelectBest(
                backpack, new[] { aircraft }, 0, out BackpackOperation operation),
                Is.True);
            Assert.That(operation.Kind,
                Is.EqualTo(BackpackOperationKind.AddShopItem));
        }
        finally
        {
            Object.DestroyImmediate(aircraft);
            Object.DestroyImmediate(shape);
        }
    }

    [Test]
    public void FullBackpack_ShopMergeWinsWithoutAPlacementCell()
    {
        ItemShapeData shape = NewOneCellShape();
        ItemData aircraft = NewAircraft(shape);
        try
        {
            BackpackController backpack = new(1, 1);
            ItemInstance target = new("target", aircraft, Vector2Int.zero);
            Assert.That(backpack.PlaceItem(target, target.AnchorCell), Is.True);

            Assert.That(BackpackOperationPlanner.TrySelectBest(
                backpack, new[] { aircraft }, 0, out BackpackOperation operation),
                Is.True);
            Assert.That(operation.Kind,
                Is.EqualTo(BackpackOperationKind.MergeShopItem));
            Assert.That(operation.SecondaryItem, Is.SameAs(target));
        }
        finally
        {
            Object.DestroyImmediate(aircraft);
            Object.DestroyImmediate(shape);
        }
    }

    private static ItemShapeData NewOneCellShape()
    {
        ItemShapeData shape = ScriptableObject.CreateInstance<ItemShapeData>();
        shape.InitializeForTests("one-cell", null, new[] { Vector2Int.zero });
        return shape;
    }

    private static ItemData NewAircraft(ItemShapeData shape)
    {
        ItemData data = ScriptableObject.CreateInstance<ItemData>();
        data.InitializeForTests("aircraft", ItemType.Aircraft, 1f, shape);
        return data;
    }
}
