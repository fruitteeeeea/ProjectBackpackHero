using System.Collections.Generic;
using NUnit.Framework;
using PlanetWar.ReusableMainMenu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class HangarShapePreviewTests
{
    private GameObject root;
    private Texture2D texture;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("ShapePreview", typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(560f, 400f);
        texture = new Texture2D(2, 2);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void Show_LShape_CreatesOnlyOccupiedCellsAndPreservesTopRowDirection()
    {
        HangarShapePreview preview = root.AddComponent<HangarShapePreview>();
        preview.Configure(Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * .5f));

        preview.Show(new[]
        {
            new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)
        });

        Assert.That(preview.CellCount, Is.EqualTo(3));
        Vector2 topLeft = ((RectTransform)root.transform.GetChild(0)).anchoredPosition;
        Vector2 bottomLeft = ((RectTransform)root.transform.GetChild(1)).anchoredPosition;
        Vector2 bottomRight = ((RectTransform)root.transform.GetChild(2)).anchoredPosition;
        Assert.That(topLeft.y, Is.GreaterThan(bottomLeft.y));
        Assert.That(bottomRight.y, Is.EqualTo(bottomLeft.y));
        Assert.That(bottomRight.x, Is.GreaterThan(bottomLeft.x));
    }

    [Test]
    public void Show_UsesBoundingBoxCenterForEveryShape()
    {
        HangarShapePreview preview = root.AddComponent<HangarShapePreview>();
        preview.Configure(Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * .5f));
        preview.Show(new[]
        {
            new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)
        });

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (RectTransform cell in root.GetComponentsInChildren<RectTransform>())
        {
            if (cell == root.transform) continue;
            minX = Mathf.Min(minX, cell.anchoredPosition.x);
            maxX = Mathf.Max(maxX, cell.anchoredPosition.x);
            minY = Mathf.Min(minY, cell.anchoredPosition.y);
            maxY = Mathf.Max(maxY, cell.anchoredPosition.y);
        }

        Assert.That((minX + maxX) * .5f, Is.EqualTo(0f).Within(.001f));
        Assert.That((minY + maxY) * .5f, Is.EqualTo(0f).Within(.001f));
    }

    [Test]
    public void Show_EmptyShape_HidesPreviewAndRemovesOldCells()
    {
        HangarShapePreview preview = root.AddComponent<HangarShapePreview>();
        preview.Configure(Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * .5f));
        preview.Show(new[] { Vector2Int.zero, Vector2Int.right });

        preview.Show(System.Array.Empty<Vector2Int>());

        Assert.That(root.activeSelf, Is.False);
        Assert.That(preview.CellCount, Is.EqualTo(0));
    }

    [Test]
    public void Snapshot_CopiesShapeOffsets()
    {
        var source = new List<Vector2Int> { Vector2Int.zero, Vector2Int.right };
        var snapshot = new HangarItemSnapshot(HangarItemKind.Equipment, "Item", "", null,
            null, true, 1, 0, 1, 0, 1f, 1, null, Color.white, "Unlocked",
            shapeOffsets: source);
        source[0] = Vector2Int.up;

        Assert.That(snapshot.ShapeOffsets, Is.EqualTo(new[]
        {
            Vector2Int.zero, Vector2Int.right
        }));
    }

    [Test]
    public void DetailLayout_ShowsSnapshotShapeInsideBoundPreview()
    {
        GameObject layoutRoot = new GameObject("Detail", typeof(RectTransform));
        GameObject cardRoot = new GameObject("Card", typeof(RectTransform));
        GameObject previewRoot = new GameObject("Preview", typeof(RectTransform));
        try
        {
            HangarDetailLayout layout = layoutRoot.AddComponent<HangarDetailLayout>();
            HangarCardItem card = cardRoot.AddComponent<HangarCardItem>();
            HangarShapePreview preview = previewRoot.AddComponent<HangarShapePreview>();
            preview.Configure(Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f),
                Vector2.one * .5f));
            layout.Configure(null, null, null, null, null, null, null, null,
                shape: preview);
            card.Configure(null, new HangarItemSnapshot(HangarItemKind.Equipment,
                "Wave Emitter", "", null, null, true, 1, 0, 1, 0, 3f, 1,
                null, Color.white, "Unlocked",
                shapeOffsets: new[] { Vector2Int.zero, Vector2Int.up }));

            layout.ShowPreview(card);

            Assert.That(preview.gameObject.activeSelf, Is.True);
            Assert.That(preview.CellCount, Is.EqualTo(2));
            foreach (Image cell in preview.GetComponentsInChildren<Image>())
                Assert.That(cell.color.a, Is.GreaterThan(0f));
        }
        finally
        {
            Object.DestroyImmediate(layoutRoot);
            Object.DestroyImmediate(cardRoot);
            Object.DestroyImmediate(previewRoot);
        }
    }

    [Test]
    public void MainMenu_BindsBlockPreviewForAircraftAndEquipmentDetails()
    {
        GameObject menu = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Packages/com.planetwar.reusable-main-menu/Prefabs/MainMenu.prefab");
        foreach (string detailName in new[] { "UICardInfo", "UICardSpell" })
        {
            Transform detail = menu.transform.Find("UICardView/" + detailName);
            HangarDetailLayout layout = detail.GetComponent<HangarDetailLayout>();
            HangarShapePreview preview = new SerializedObject(layout)
                .FindProperty("shapePreview").objectReferenceValue as HangarShapePreview;
            Assert.That(preview, Is.Not.Null, detailName + " should bind a shape preview.");
            Assert.That(new SerializedObject(preview).FindProperty("cellSprite")
                .objectReferenceValue, Is.Not.Null, detailName + " should bind block.png.");
        }

        Transform entityScroll = menu.transform.Find("UICardView/UICardInfo/SkillScroll");
        ScrollRect entityScrollRect = entityScroll.GetComponent<ScrollRect>();
        Assert.That(entityScrollRect.enabled, Is.False);
        Assert.That(entityScrollRect.content.gameObject.activeSelf, Is.False);
        Assert.That(menu.transform.Find("UICardView/UICardSpell/ShapePreviewScroll"), Is.Not.Null);
    }

    [Test]
    public void MainMenu_SpellUsesFourPartLayoutWithPreviewAboveActions()
    {
        GameObject menu = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Packages/com.planetwar.reusable-main-menu/Prefabs/MainMenu.prefab");
        Transform spell = menu.transform.Find("UICardView/UICardSpell");
        RectTransform background = spell.Find("bg") as RectTransform;
        Transform flow = spell.Find("SpellAutoLayout");
        RectTransform cooldown = flow.Find("Image") as RectTransform;
        RectTransform preview = flow.Find("PreviewSection/ShapePreviewScroll") as RectTransform;
        RectTransform actions = flow.Find("btns") as RectTransform;

        Assert.That(flow.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
        Assert.That(background.rect.height, Is.GreaterThanOrEqualTo(1100f));
        Assert.That(cooldown.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(100f));
        Assert.That(preview.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(400f));
        Assert.That(actions.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(144f));
    }

    [Test]
    public void MainMenu_SpellUsesCompactCooldownRowAndRectMaskedPreview()
    {
        GameObject menu = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Packages/com.planetwar.reusable-main-menu/Prefabs/MainMenu.prefab");
        Transform spell = menu.transform.Find("UICardView/UICardSpell");
        Transform flow = spell.Find("SpellAutoLayout");
        RectTransform cooldownRow = flow.Find("Image") as RectTransform;
        Transform preview = flow.Find("PreviewSection/ShapePreviewScroll");
        Transform background = flow.Find("PreviewSection/ShapePreviewBackground");

        Assert.That(cooldownRow.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(100f));
        Assert.That(preview.GetComponent<RectMask2D>(), Is.Not.Null);
        Assert.That(preview.GetComponent<Mask>(), Is.Null);
        Assert.That(preview.GetComponent<Image>(), Is.Null);
        Assert.That(background.parent, Is.EqualTo(preview.parent));
        Assert.That(preview.GetSiblingIndex(), Is.GreaterThan(background.GetSiblingIndex()));
    }

    [Test]
    public void Show_EquipmentShape_CreatesVisibleCellsInsideRectMask()
    {
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform),
            typeof(RectMask2D));
        try
        {
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.sizeDelta = new Vector2(560f, 400f);
            root.transform.SetParent(viewport.transform, false);
            HangarShapePreview preview = root.AddComponent<HangarShapePreview>();
            preview.Configure(Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f),
                Vector2.one * .5f));

            preview.Show(new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up });

            Assert.That(preview.CellCount, Is.EqualTo(3));
            foreach (Image cell in root.GetComponentsInChildren<Image>())
            {
                Assert.That(cell.gameObject.activeSelf, Is.True);
                Assert.That(cell.sprite, Is.Not.Null);
                Assert.That(cell.raycastTarget, Is.False);
            }
        }
        finally { Object.DestroyImmediate(viewport); }
    }
}
