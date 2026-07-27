using System.Collections.Generic;
using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class BackpackGridViewTests
{
    private const float GridWidth = 608f;
    private const float GridHeight = 344f;

    private GameObject gridObject;
    private RectTransform gridRect;
    private BackpackGridView gridView;

    [SetUp]
    public void SetUp()
    {
        gridObject =
            new GameObject(
                "Grid",
                typeof(RectTransform),
                typeof(BackpackGridView));
        gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.sizeDelta = new Vector2(GridWidth, GridHeight);
        gridView = gridObject.GetComponent<BackpackGridView>();
        gridView.Initialize(
            gridRect,
            new Vector2(80f, 80f),
            new Vector2(8f, 8f),
            new List<BackpackSlotView>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gridObject);
    }

    [TestCase(40f, 40f, 0, 0)]
    [TestCase(128f, 40f, 1, 0)]
    [TestCase(568f, 304f, 6, 3)]
    [TestCase(0f, 0f, 0, 0)]
    [TestCase(608f, 344f, 6, 3)]
    public void TryGetCellAtScreenPosition_MapsGridInterior(
        float fromLeft,
        float fromTop,
        int expectedX,
        int expectedY)
    {
        Assert.That(
            TryGetCell(fromLeft, fromTop, out var cell),
            Is.True);
        Assert.That(
            cell,
            Is.EqualTo(new Vector2Int(expectedX, expectedY)));
    }

    [TestCase(83f, 40f, 0, 0)]
    [TestCase(84f, 40f, 1, 0)]
    [TestCase(85f, 40f, 1, 0)]
    [TestCase(40f, 83f, 0, 0)]
    [TestCase(40f, 84f, 0, 1)]
    [TestCase(40f, 85f, 0, 1)]
    [TestCase(83f, 83f, 0, 0)]
    [TestCase(84f, 84f, 1, 1)]
    public void TryGetCellAtScreenPosition_MapsGapToNearestCell(
        float fromLeft,
        float fromTop,
        int expectedX,
        int expectedY)
    {
        Assert.That(
            TryGetCell(fromLeft, fromTop, out var cell),
            Is.True);
        Assert.That(
            cell,
            Is.EqualTo(new Vector2Int(expectedX, expectedY)));
    }

    [TestCase(-0.01f, 40f)]
    [TestCase(608.01f, 40f)]
    [TestCase(40f, -0.01f)]
    [TestCase(40f, 344.01f)]
    public void TryGetCellAtScreenPosition_RejectsOutsideGrid(
        float fromLeft,
        float fromTop)
    {
        Assert.That(
            TryGetCell(fromLeft, fromTop, out _),
            Is.False);
    }

    private bool TryGetCell(
        float fromLeft,
        float fromTop,
        out Vector2Int cell)
    {
        var screenPosition =
            new Vector2(
                gridRect.rect.xMin + fromLeft,
                gridRect.rect.yMax - fromTop);

        return gridView.TryGetCellAtScreenPosition(
            screenPosition,
            null,
            out cell);
    }
}
