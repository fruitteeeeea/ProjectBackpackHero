using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class ItemGrabOffsetCalculatorTests
{
    [TestCase(0, 0)]
    [TestCase(1, 0)]
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    public void Calculate_ReturnsCornerCell_ForAllLOrientations(
        int expectedX,
        int expectedY)
    {
        Vector2Int corner = new Vector2Int(expectedX, expectedY);
        Vector2Int horizontal = corner +
            (expectedX == 0 ? Vector2Int.right : Vector2Int.left);
        Vector2Int vertical = corner +
            (expectedY == 0 ? Vector2Int.down : Vector2Int.up);

        Assert.That(
            ItemGrabOffsetCalculator.Calculate(
                new[] { corner, horizontal, vertical }),
            Is.EqualTo(corner));
    }

    [TestCaseSource(nameof(NonLShapes))]
    public void Calculate_ReturnsBottomRightOccupiedCell_ForNonLShapes(
        Vector2Int[] shapeOffsets,
        Vector2Int expected)
    {
        Assert.That(
            ItemGrabOffsetCalculator.Calculate(shapeOffsets),
            Is.EqualTo(expected));
    }

    [Test]
    public void Calculate_ReturnsZero_ForNullOrEmptyShape()
    {
        Assert.That(
            ItemGrabOffsetCalculator.Calculate(null),
            Is.EqualTo(Vector2Int.zero));
        Assert.That(
            ItemGrabOffsetCalculator.Calculate(System.Array.Empty<Vector2Int>()),
            Is.EqualTo(Vector2Int.zero));
    }

    [Test]
    public void Calculate_PreservesLShapeOffsetUsedByPlacementAnchor()
    {
        Vector2Int grabOffset = ItemGrabOffsetCalculator.Calculate(
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
            });

        Assert.That(grabOffset, Is.EqualTo(new Vector2Int(1, 0)));
        Assert.That(
            BackpackGridView.CalculateAnchorCell(
                new Vector2Int(6, 3),
                grabOffset),
            Is.EqualTo(new Vector2Int(5, 3)));
    }

    [Test]
    public void Calculate_UsesBottomRightOffsetForPlacementAnchor()
    {
        Vector2Int grabOffset = ItemGrabOffsetCalculator.Calculate(
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
            });

        Assert.That(grabOffset, Is.EqualTo(new Vector2Int(2, 0)));
        Assert.That(
            BackpackGridView.CalculateAnchorCell(
                new Vector2Int(6, 3),
                grabOffset),
            Is.EqualTo(new Vector2Int(4, 3)));
    }

    private static object[] NonLShapes =
    {
        new object[]
        {
            new[] { new Vector2Int(2, 3) },
            new Vector2Int(2, 3),
        },
        new object[]
        {
            new[]
            {
                new Vector2Int(2, 1),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1),
            },
            new Vector2Int(2, 1),
        },
        new object[]
        {
            new[]
            {
                new Vector2Int(1, 2),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
            },
            new Vector2Int(1, 2),
        },
        new object[]
        {
            new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(2, 1),
            },
            new Vector2Int(2, 1),
        },
    };
}
