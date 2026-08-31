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
    private readonly List<GameObject> slotObjects = new();
    private readonly List<Object> testAssets = new();

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
        List<BackpackSlotView> slots = CreateSlots(3, 2);
        gridView.Initialize(
            gridRect,
            new Vector2(80f, 80f),
            new Vector2(8f, 8f),
            slots);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject slotObject in slotObjects)
        {
            Object.DestroyImmediate(slotObject);
        }

        foreach (Object testAsset in testAssets)
        {
            Object.DestroyImmediate(testAsset);
        }

        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void RefreshSlotVisibility_HidesEveryCellOccupiedByMultiCellItem()
    {
        BackpackController backpack = new(3, 2);
        ItemInstance item = NewItem(
            "two-cell",
            new[] { Vector2Int.zero, Vector2Int.right });
        Assert.That(backpack.PlaceItem(item, new Vector2Int(1, 0)), Is.True);

        gridView.RefreshSlotVisibility(backpack);

        Assert.That(SlotImage(new Vector2Int(0, 0)).enabled, Is.True);
        Assert.That(SlotImage(new Vector2Int(1, 0)).enabled, Is.False);
        Assert.That(SlotImage(new Vector2Int(2, 0)).enabled, Is.False);
        Assert.That(SlotImage(new Vector2Int(1, 1)).enabled, Is.True);
    }

    [Test]
    public void RefreshSlotVisibility_TemporarilyExposesOnlyDraggedItemCells()
    {
        BackpackController backpack = new(3, 2);
        ItemInstance draggedItem = NewItem("dragged", new[] { Vector2Int.zero });
        ItemInstance otherItem = NewItem("other", new[] { Vector2Int.zero });
        Assert.That(backpack.PlaceItem(draggedItem, new Vector2Int(0, 0)), Is.True);
        Assert.That(backpack.PlaceItem(otherItem, new Vector2Int(2, 1)), Is.True);

        gridView.RefreshSlotVisibility(backpack, draggedItem);

        Assert.That(SlotImage(new Vector2Int(0, 0)).enabled, Is.True);
        Assert.That(SlotImage(new Vector2Int(2, 1)).enabled, Is.False);
    }

    [Test]
    public void ClearPlacementPreview_RestoresOccupiedSlotHiddenState()
    {
        BackpackController backpack = new(3, 2);
        ItemInstance item = NewItem("occupied", new[] { Vector2Int.zero });
        Assert.That(backpack.PlaceItem(item, new Vector2Int(1, 0)), Is.True);
        gridView.RefreshSlotVisibility(backpack);

        gridView.ShowPlacementPreview(backpack, item, new Vector2Int(1, 0), item);
        Assert.That(SlotImage(new Vector2Int(1, 0)).enabled, Is.True);

        gridView.ClearPlacementPreview();

        Assert.That(SlotImage(new Vector2Int(1, 0)).enabled, Is.False);
        Assert.That(SlotImage(new Vector2Int(0, 1)).enabled, Is.True);
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

    [TestCase(-32f, 40f, 0, 0)]
    [TestCase(640f, 40f, 6, 0)]
    [TestCase(40f, -32f, 0, 0)]
    [TestCase(40f, 376f, 0, 3)]
    public void TryGetCellAtScreenPosition_MapsExpandedDropAreaToEdgeCell(
        float fromLeft,
        float fromTop,
        int expectedX,
        int expectedY)
    {
        Assert.That(
            TryGetCell(fromLeft, fromTop, out Vector2Int cell),
            Is.True);
        Assert.That(
            cell,
            Is.EqualTo(new Vector2Int(expectedX, expectedY)));
    }

    [TestCase(-32.01f, 40f)]
    [TestCase(640.01f, 40f)]
    [TestCase(40f, -32.01f)]
    [TestCase(40f, 376.01f)]
    public void TryGetCellAtScreenPosition_RejectsBeyondExpandedDropArea(
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

    private List<BackpackSlotView> CreateSlots(int columns, int rows)
    {
        List<BackpackSlotView> slots = new();
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                GameObject slotObject = new(
                    $"Slot ({x}, {y})",
                    typeof(RectTransform),
                    typeof(UnityEngine.UI.Image),
                    typeof(BackpackSlotView));
                slotObject.transform.SetParent(gridObject.transform, false);
                BackpackSlotView slot =
                    slotObject.GetComponent<BackpackSlotView>();
                slot.Initialize(new Vector2Int(x, y));
                slotObjects.Add(slotObject);
                slots.Add(slot);
            }
        }

        return slots;
    }

    private UnityEngine.UI.Image SlotImage(Vector2Int cell)
    {
        foreach (GameObject slotObject in slotObjects)
        {
            BackpackSlotView slot =
                slotObject.GetComponent<BackpackSlotView>();
            if (slot.Cell == cell)
            {
                return slotObject.GetComponent<UnityEngine.UI.Image>();
            }
        }

        Assert.Fail($"No slot exists at {cell}.");
        return null;
    }

    private ItemInstance NewItem(
        string id,
        IReadOnlyList<Vector2Int> offsets)
    {
        ItemShapeData shape = ScriptableObject.CreateInstance<ItemShapeData>();
        testAssets.Add(shape);
        shape.InitializeForTests("test-shape", null, offsets);
        ItemData data = ScriptableObject.CreateInstance<ItemData>();
        testAssets.Add(data);
        data.InitializeForTests(id, ItemType.Equipment, 1f, shape);
        return new ItemInstance(id, data, Vector2Int.zero);
    }
}
