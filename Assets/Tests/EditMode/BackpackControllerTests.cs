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

    private static ItemInstance NewItem(
        string id,
        IReadOnlyList<Vector2Int> offsets)
    {
        ItemShapeData data =
            ScriptableObject.CreateInstance<
                ItemShapeData>();

        data.InitializeForTests(
            id,
            null,
            offsets);

        return new ItemInstance(
            id,
            data,
            Vector2Int.zero);
    }
}