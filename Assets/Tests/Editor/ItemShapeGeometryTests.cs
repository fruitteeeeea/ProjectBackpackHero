using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class ItemShapeGeometryTests
{
    [TestCase(0f, 0f)]
    public void CalculateCenter_OneByOne_ReturnsCellCenter(
        float expectedX,
        float expectedY)
    {
        Assert.That(
            ItemShapeGeometry.CalculateCenter(
                new[] { Vector2Int.zero }),
            Is.EqualTo(new Vector2(expectedX, expectedY)));
    }

    [TestCaseSource(nameof(RectangularShapes))]
    public void CalculateCenter_RectangularShape_ReturnsSeamCenter(
        Vector2Int[] shape,
        Vector2 expected)
    {
        Assert.That(
            ItemShapeGeometry.CalculateCenter(shape),
            Is.EqualTo(expected));
    }

    [TestCase(0, 0)]
    [TestCase(1, 0)]
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    public void CalculateCenter_LShape_ReturnsOccupiedCornerCenter(
        int cornerX,
        int cornerY)
    {
        Vector2Int corner = new(cornerX, cornerY);
        Vector2Int horizontal = corner +
            (cornerX == 0 ? Vector2Int.right : Vector2Int.left);
        Vector2Int vertical = corner +
            (cornerY == 0 ? Vector2Int.down : Vector2Int.up);

        Assert.That(
            ItemShapeGeometry.CalculateCenter(
                new[] { corner, horizontal, vertical }),
            Is.EqualTo((Vector2)corner));
    }

    private static object[] RectangularShapes =
    {
        new object[]
        {
            new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) },
            new Vector2(0f, .5f),
        },
        new object[]
        {
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
            new Vector2(.5f, 0f),
        },
    };
}
