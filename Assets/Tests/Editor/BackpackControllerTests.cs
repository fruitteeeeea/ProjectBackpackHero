using System.Collections.Generic;
using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class BackpackControllerTests
{
    [Test]
    public void CanPlace_AllowsOneByOneAtLastCell()
    {
        BackpackController backpack =
            new BackpackController(7, 4);

        ItemInstance item =
            NewItem(
                "one",
                new[] { Vector2Int.zero });

        Assert.That(
            backpack.CanPlace(
                item,
                new Vector2Int(6, 3)),
            Is.True);
    }

    [Test]
    public void CanPlace_RejectsOneByTwoPastBottom()
    {
        BackpackController backpack =
            new BackpackController(7, 4);

        ItemInstance item =
            NewItem(
                "vertical-bar",
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1),
                });

        Assert.That(
            backpack.CanPlace(
                item,
                new Vector2Int(0, 3)),
            Is.False);
    }

    [Test]
    public void CanPlace_RejectsTwoByOnePastRight()
    {
        BackpackController backpack =
            new BackpackController(7, 4);

        ItemInstance item =
            NewItem(
                "horizontal-bar",
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                });

        Assert.That(
            backpack.CanPlace(
                item,
                new Vector2Int(6, 0)),
            Is.False);
    }

    [Test]
    public void CanPlace_AllowsAllFourLShapesInside()
    {
        BackpackController backpack =
            new BackpackController(7, 4);

        Vector2Int[][] shapes =
        {
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1),
            },
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
            },
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
            },
            new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1),
            },
        };

        for (int index = 0;
             index < shapes.Length;
             index++)
        {
            ItemInstance item =
                NewItem(
                    $"l-{index}",
                    shapes[index]);

            Assert.That(
                backpack.CanPlace(
                    item,
                    new Vector2Int(2, 1)),
                Is.True);
        }
    }

    [Test]
    public void CanPlace_RejectsAllFourLShapesAtBottomRight()
    {
        BackpackController backpack =
            new BackpackController(7, 4);

        Vector2Int[][] shapes =
        {
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1),
            },
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
            },
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
            },
            new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1),
            },
        };

        for (int index = 0;
             index < shapes.Length;
             index++)
        {
            ItemInstance item =
                NewItem(
                    $"l-{index}",
                    shapes[index]);

            Assert.That(
                backpack.CanPlace(
                    item,
                    new Vector2Int(6, 3)),
                Is.False);
        }
    }

    [Test]
    public void CanPlace_RejectsOverlapWithOtherItem()
    {
        BackpackController backpack =
            new BackpackController(7, 4);

        ItemInstance first =
            NewItem(
                "first",
                new[] { Vector2Int.zero });

        ItemInstance second =
            NewItem(
                "second",
                new[] { Vector2Int.zero });

        backpack.PlaceItem(
            first,
            new Vector2Int(4, 2));

        Assert.That(
            backpack.CanPlace(
                second,
                new Vector2Int(4, 2)),
            Is.False);
    }

    [Test]
    public void CanPlace_IgnoresOwnCellsWhenMoving()
    {
        BackpackController backpack =
            new BackpackController(7, 4);

        ItemInstance item =
            NewItem(
                "horizontal-bar",
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                });

        backpack.PlaceItem(
            item,
            new Vector2Int(3, 1));

        Assert.That(
            backpack.CanPlace(
                item,
                new Vector2Int(4, 1),
                item),
            Is.True);
    }

    [Test]
    public void RemoveItem_ClearsAllOccupiedCells()
    {
        BackpackController backpack =
            new BackpackController(7, 4);

        ItemInstance item =
            NewItem(
                "l-shape",
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                });

        backpack.PlaceItem(
            item,
            new Vector2Int(4, 1));

        Assert.That(
            backpack.RemoveItem(item),
            Is.True);

        Assert.That(
            backpack.GetItemAt(
                new Vector2Int(4, 1)),
            Is.Null);

        Assert.That(
            backpack.GetItemAt(
                new Vector2Int(4, 2)),
            Is.Null);

        Assert.That(
            backpack.GetItemAt(
                new Vector2Int(5, 2)),
            Is.Null);
    }

    [Test]
    public void CalculateAnchorCell_UsesGrabOffset()
    {
        Vector2Int pointerCell =
            new Vector2Int(6, 3);

        Assert.That(
            BackpackGridView.CalculateAnchorCell(
                pointerCell,
                new Vector2Int(0, 0)),
            Is.EqualTo(
                new Vector2Int(6, 3)));

        Assert.That(
            BackpackGridView.CalculateAnchorCell(
                pointerCell,
                new Vector2Int(1, 0)),
            Is.EqualTo(
                new Vector2Int(5, 3)));

        Assert.That(
            BackpackGridView.CalculateAnchorCell(
                pointerCell,
                new Vector2Int(0, 1)),
            Is.EqualTo(
                new Vector2Int(6, 2)));
    }

    [Test]
    public void BuildPlacementPreview_OutOfBoundsIsRed()
    {
        BackpackController backpack =
            new BackpackController(7, 4);

        ItemInstance item =
            NewItem(
                "horizontal-bar",
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                });

        PlacementPreview preview =
            BackpackGridView.BuildPlacementPreview(
                backpack,
                item,
                new Vector2Int(6, 2));

        Assert.That(
            preview.IsLegal,
            Is.False);

        Assert.That(
            preview.VisibleCells,
            Is.EqualTo(
                new[]
                {
                    new Vector2Int(6, 2),
                }));
    }

    [Test]
    public void GetAdjacentItems_FindsCardinalNeighbors()
    {
        BackpackController backpack =
            new BackpackController(5, 5);

        ItemInstance aircraft =
            NewItem(
                "aircraft",
                new[]
                {
                    Vector2Int.zero,
                    Vector2Int.right,
                },
                ItemType.Aircraft);

        ItemInstance above =
            NewItem("above", OneCell());
        ItemInstance below =
            NewItem("below", OneCell());
        ItemInstance left =
            NewItem("left", OneCell());
        ItemInstance right =
            NewItem("right", OneCell());

        backpack.PlaceItem(
            aircraft,
            new Vector2Int(1, 2));
        backpack.PlaceItem(
            above,
            new Vector2Int(1, 1));
        backpack.PlaceItem(
            below,
            new Vector2Int(2, 3));
        backpack.PlaceItem(
            left,
            new Vector2Int(0, 2));
        backpack.PlaceItem(
            right,
            new Vector2Int(3, 2));

        CollectionAssert.AreEquivalent(
            new[] { above, below, left, right },
            backpack.GetAdjacentItems(aircraft));
    }

    [Test]
    public void GetAdjacentItems_ExcludesDiagonalNeighbor()
    {
        BackpackController backpack =
            new BackpackController(4, 4);

        ItemInstance aircraft =
            NewItem(
                "aircraft",
                OneCell(),
                ItemType.Aircraft);

        ItemInstance diagonal =
            NewItem("diagonal", OneCell());

        backpack.PlaceItem(
            aircraft,
            new Vector2Int(1, 1));
        backpack.PlaceItem(
            diagonal,
            new Vector2Int(2, 2));

        Assert.That(
            backpack.GetAdjacentItems(aircraft),
            Is.Empty);
    }

    [Test]
    public void GetAdjacentItems_ReturnsMultiCellNeighborOnce()
    {
        BackpackController backpack =
            new BackpackController(4, 4);

        ItemInstance aircraft =
            NewItem(
                "aircraft",
                new[]
                {
                    Vector2Int.zero,
                    Vector2Int.right,
                },
                ItemType.Aircraft);

        ItemInstance equipment =
            NewItem(
                "equipment",
                new[]
                {
                    Vector2Int.zero,
                    Vector2Int.right,
                });

        backpack.PlaceItem(
            aircraft,
            new Vector2Int(1, 1));
        backpack.PlaceItem(
            equipment,
            new Vector2Int(1, 2));

        Assert.That(
            backpack.GetAdjacentItems(aircraft),
            Is.EqualTo(new[] { equipment }));
    }

    [Test]
    public void GetAdjacentEquipmentItems_FiltersOutAircraft()
    {
        BackpackController backpack =
            new BackpackController(4, 4);

        ItemInstance aircraft =
            NewItem(
                "aircraft",
                OneCell(),
                ItemType.Aircraft);

        ItemInstance equipment =
            NewItem("equipment", OneCell());

        ItemInstance otherAircraft =
            NewItem(
                "other-aircraft",
                OneCell(),
                ItemType.Aircraft);

        backpack.PlaceItem(
            aircraft,
            new Vector2Int(1, 1));
        backpack.PlaceItem(
            equipment,
            new Vector2Int(1, 0));
        backpack.PlaceItem(
            otherAircraft,
            new Vector2Int(2, 1));

        Assert.That(
            backpack.GetAdjacentEquipmentItems(aircraft),
            Is.EqualTo(new[] { equipment }));
    }

    [Test]
    public void GetAdjacentItems_ReflectsMoveAndRemoval()
    {
        BackpackController backpack =
            new BackpackController(4, 4);

        ItemInstance aircraft =
            NewItem(
                "aircraft",
                OneCell(),
                ItemType.Aircraft);

        ItemInstance equipment =
            NewItem("equipment", OneCell());

        backpack.PlaceItem(
            aircraft,
            new Vector2Int(1, 1));
        backpack.PlaceItem(
            equipment,
            new Vector2Int(3, 3));

        Assert.That(
            backpack.GetAdjacentItems(aircraft),
            Is.Empty);

        backpack.MoveItem(
            equipment,
            new Vector2Int(1, 2));

        Assert.That(
            backpack.GetAdjacentItems(aircraft),
            Is.EqualTo(new[] { equipment }));

        backpack.RemoveItem(equipment);

        Assert.That(
            backpack.GetAdjacentItems(aircraft),
            Is.Empty);
    }

    [Test]
    public void GetAdjacentEquipmentItems_KeepsSeparateItemsWithSameData()
    {
        BackpackController backpack =
            new BackpackController(4, 4);

        ItemShapeData oneCellShape =
            NewShape(OneCell());
        ItemData aircraftData =
            NewData(
                "aircraft",
                ItemType.Aircraft,
                2f,
                oneCellShape);
        ItemData sharedEquipmentData =
            NewData(
                "shared-equipment",
                ItemType.Equipment,
                -1f,
                oneCellShape);

        ItemInstance aircraft =
            new ItemInstance(
                "aircraft",
                aircraftData,
                new Vector2Int(1, 1));
        ItemInstance first =
            new ItemInstance(
                "first",
                sharedEquipmentData,
                new Vector2Int(1, 0));
        ItemInstance second =
            new ItemInstance(
                "second",
                sharedEquipmentData,
                new Vector2Int(2, 1));

        backpack.PlaceItem(
            aircraft,
            aircraft.AnchorCell);
        backpack.PlaceItem(
            first,
            first.AnchorCell);
        backpack.PlaceItem(
            second,
            second.AnchorCell);

        CollectionAssert.AreEquivalent(
            new[] { first, second },
            backpack.GetAdjacentEquipmentItems(
                aircraft));
    }

    [Test]
    public void ItemData_OnlyAircraftCanEnterCooldown()
    {
        ItemShapeData shape =
            NewShape(OneCell());

        ItemData aircraft =
            NewData(
                "aircraft",
                ItemType.Aircraft,
                2f,
                shape);

        ItemData equipment =
            NewData(
                "equipment",
                ItemType.Equipment,
                2f,
                shape);

        Assert.That(
            aircraft.CanEnterCooldown,
            Is.True);
        Assert.That(
            aircraft.CooldownDuration,
            Is.EqualTo(2f));
        Assert.That(
            equipment.CanEnterCooldown,
            Is.False);
        Assert.That(
            equipment.CooldownDuration,
            Is.EqualTo(-1f));
    }

    [Test]
    public void ItemData_DefaultBackgroundColorIsLightOrange()
    {
        ItemData data =
            ScriptableObject.CreateInstance<
                ItemData>();

        Color32 color =
            data.BackgroundColor;

        Assert.That(color.r, Is.EqualTo(255));
        Assert.That(color.g, Is.EqualTo(204));
        Assert.That(color.b, Is.EqualTo(128));
        Assert.That(color.a, Is.EqualTo(255));
    }

    [Test]
    public void ModelEventsAndContains_FollowItemLifecycle()
    {
        BackpackController backpack =
            new BackpackController(4, 4);

        ItemInstance item =
            NewItem("event-item", OneCell());

        int added = 0;
        int moved = 0;
        int removed = 0;

        backpack.ItemAdded += _ => added++;
        backpack.ItemMoved += _ => moved++;
        backpack.ItemRemoved += _ => removed++;

        Assert.That(
            backpack.PlaceItem(
                item,
                new Vector2Int(0, 0)),
            Is.True);
        Assert.That(backpack.Contains(item), Is.True);

        Assert.That(
            backpack.MoveItem(
                item,
                new Vector2Int(1, 1)),
            Is.True);

        Assert.That(
            backpack.RemoveItem(item),
            Is.True);
        Assert.That(backpack.Contains(item), Is.False);
        Assert.That(added, Is.EqualTo(1));
        Assert.That(moved, Is.EqualTo(1));
        Assert.That(removed, Is.EqualTo(1));
    }

    [Test]
    public void AircraftCooldown_IsStoredOnItemInstance()
    {
        ItemInstance aircraft =
            NewItem(
                "cooldown-aircraft",
                OneCell(),
                ItemType.Aircraft);

        aircraft.BeginCooldown();

        Assert.That(
            aircraft.IsCoolingDown,
            Is.True);
        Assert.That(
            aircraft.RemainingCooldown,
            Is.EqualTo(2f));
        Assert.That(
            aircraft.TickCooldown(1f),
            Is.False);
        Assert.That(
            aircraft.CooldownProgress,
            Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(
            aircraft.TickCooldown(1f),
            Is.True);
        Assert.That(
            aircraft.IsCoolingDown,
            Is.False);
        Assert.That(
            aircraft.CooldownProgress,
            Is.EqualTo(1f));
    }

    [Test]
    public void ItemInstance_StartsAtLevelOne_AndCanUpgradeOnlyOnce()
    {
        ItemInstance item = NewItem("level", OneCell());

        Assert.That(item.Level, Is.EqualTo(ItemInstance.DefaultLevel));
        Assert.That(item.CanUpgrade, Is.True);
        Assert.That(item.TryUpgrade(), Is.True);
        Assert.That(item.Level, Is.EqualTo(ItemInstance.MaximumLevel));
        Assert.That(item.CanUpgrade, Is.False);
        Assert.That(item.TryUpgrade(), Is.False);
    }

    [Test]
    public void ItemInstance_ConstructorPreservesRequestedLevel()
    {
        ItemInstance source = NewItem(
            "copy-source",
            OneCell());
        ItemInstance item = new ItemInstance(
            "copied-level-two",
            source.Data,
            Vector2Int.zero,
            ItemInstance.MaximumLevel);

        Assert.That(
            item.Level,
            Is.EqualTo(ItemInstance.MaximumLevel));
        Assert.That(item.IsCoolingDown, Is.False);
    }

    [Test]
    public void TryMerge_ConsumesSourceAndUpgradesMatchingTarget()
    {
        BackpackController backpack = new BackpackController(4, 4);
        ItemData data = NewData("matching", ItemType.Equipment, -1f, NewShape(OneCell()));
        ItemInstance source = new ItemInstance("source", data, Vector2Int.zero);
        ItemInstance target = new ItemInstance("target", data, new Vector2Int(1, 0));

        Assert.That(backpack.PlaceItem(source, Vector2Int.zero), Is.True);
        Assert.That(backpack.PlaceItem(target, new Vector2Int(1, 0)), Is.True);
        Assert.That(backpack.CanMergeAt(source, new Vector2Int(1, 0)), Is.True);
        Assert.That(backpack.TryMerge(source, target), Is.True);
        Assert.That(backpack.Contains(source), Is.False);
        Assert.That(backpack.Contains(target), Is.True);
        Assert.That(backpack.Items.Count, Is.EqualTo(1));
        Assert.That(target.Level, Is.EqualTo(ItemInstance.MaximumLevel));
        Assert.That(target.AnchorCell, Is.EqualTo(new Vector2Int(1, 0)));
    }

    [Test]
    public void TryMerge_AllowsAnUnplacedShopSource()
    {
        BackpackController backpack = new BackpackController(4, 4);
        ItemData data = NewData("matching", ItemType.Equipment, -1f, NewShape(OneCell()));
        ItemInstance shopSource = new ItemInstance("shop", data, Vector2Int.zero);
        ItemInstance target = new ItemInstance("target", data, new Vector2Int(2, 1));

        Assert.That(backpack.PlaceItem(target, new Vector2Int(2, 1)), Is.True);
        Assert.That(backpack.CanMergeAt(shopSource, new Vector2Int(2, 1)), Is.True);
        Assert.That(backpack.TryMerge(shopSource, target), Is.True);
        Assert.That(backpack.Items.Count, Is.EqualTo(1));
        Assert.That(target.Level, Is.EqualTo(ItemInstance.MaximumLevel));
    }

    [Test]
    public void CanMergeAt_RejectsPartialOrMultiItemOverlap()
    {
        BackpackController backpack = new BackpackController(4, 4);
        ItemData data = NewData(
            "bar",
            ItemType.Equipment,
            -1f,
            NewShape(new[] { Vector2Int.zero, Vector2Int.right }));
        ItemInstance source = new ItemInstance("source", data, Vector2Int.zero);
        ItemInstance first = new ItemInstance("first", data, Vector2Int.zero);
        ItemInstance second = new ItemInstance("second", data, new Vector2Int(1, 0));

        Assert.That(backpack.PlaceItem(first, Vector2Int.zero), Is.True);
        Assert.That(backpack.PlaceItem(second, new Vector2Int(2, 0)), Is.True);
        Assert.That(backpack.CanMergeAt(source, new Vector2Int(1, 0)), Is.False);
    }

    [Test]
    public void Merge_RejectsDifferentDataOrLevelTwoItems()
    {
        BackpackController backpack = new BackpackController(4, 4);
        ItemInstance source = NewItem("source", OneCell());
        ItemInstance different = NewItem("different", OneCell());

        Assert.That(backpack.PlaceItem(different, Vector2Int.zero), Is.True);
        Assert.That(backpack.CanMerge(source, different), Is.False);

        ItemData data = NewData("matching", ItemType.Equipment, -1f, NewShape(OneCell()));
        ItemInstance levelOne = new ItemInstance("one", data, Vector2Int.zero);
        ItemInstance levelTwo = new ItemInstance("two", data, new Vector2Int(1, 0));
        Assert.That(backpack.PlaceItem(levelTwo, new Vector2Int(1, 0)), Is.True);
        Assert.That(levelTwo.TryUpgrade(), Is.True);
        Assert.That(backpack.CanMerge(levelOne, levelTwo), Is.False);
    }

    private static ItemInstance NewItem(
        string id,
        IReadOnlyList<Vector2Int> offsets,
        ItemType itemType = ItemType.Equipment)
    {
        ItemShapeData shape =
            NewShape(offsets);

        ItemData data =
            NewData(
                id,
                itemType,
                itemType == ItemType.Aircraft
                    ? 2f
                    : -1f,
                shape);

        return new ItemInstance(
            id,
            data,
            Vector2Int.zero);
    }

    private static ItemShapeData NewShape(
        IReadOnlyList<Vector2Int> offsets)
    {
        ItemShapeData shape =
            ScriptableObject.CreateInstance<
                ItemShapeData>();

        shape.InitializeForTests(
            "test-shape",
            null,
            offsets);

        return shape;
    }

    private static ItemData NewData(
        string name,
        ItemType itemType,
        float cooldownDuration,
        ItemShapeData shape)
    {
        ItemData data =
            ScriptableObject.CreateInstance<
                ItemData>();

        data.InitializeForTests(
            name,
            itemType,
            cooldownDuration,
            shape);

        return data;
    }

    private static IReadOnlyList<Vector2Int> OneCell()
    {
        return new[] { Vector2Int.zero };
    }
}
