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
    public void MainMenu_SpellPreviewFitsInsideExpandedDetailPanelAboveActions()
    {
        GameObject menu = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Packages/com.planetwar.reusable-main-menu/Prefabs/MainMenu.prefab");
        Transform spell = menu.transform.Find("UICardView/UICardSpell");
        RectTransform background = spell.Find("bg") as RectTransform;
        RectTransform preview = spell.Find("ShapePreviewScroll") as RectTransform;
        RectTransform actions = spell.Find("btns") as RectTransform;

        Assert.That(spell.Find("ShapePreviewBackground"), Is.Not.Null);
        Assert.That(background.rect.height, Is.GreaterThanOrEqualTo(1100f));
        Assert.That(preview.rect.yMax + preview.anchoredPosition.y,
            Is.LessThanOrEqualTo(background.rect.yMax + background.anchoredPosition.y));
        Assert.That(preview.rect.yMin + preview.anchoredPosition.y,
            Is.GreaterThanOrEqualTo(background.rect.yMin + background.anchoredPosition.y));
        Assert.That(actions.rect.yMax + actions.anchoredPosition.y,
            Is.LessThan(preview.rect.yMin + preview.anchoredPosition.y));
    }

    [Test]
    public void MainMenu_SpellUsesCompactCooldownRowAndRectMaskedPreview()
    {
        GameObject menu = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Packages/com.planetwar.reusable-main-menu/Prefabs/MainMenu.prefab");
        Transform spell = menu.transform.Find("UICardView/UICardSpell");
        RectTransform cooldownRow = spell.Find("Image") as RectTransform;
        Transform preview = spell.Find("ShapePreviewScroll");
        Transform background = spell.Find("ShapePreviewBackground");

        Assert.That(cooldownRow.sizeDelta, Is.EqualTo(new Vector2(590f, 100f)));
        Assert.That(cooldownRow.anchoredPosition, Is.EqualTo(new Vector2(0f, -8f)));
        Assert.That(preview.GetComponent<RectMask2D>(), Is.Not.Null);
        Assert.That(preview.GetComponent<Mask>(), Is.Null);
        Assert.That(preview.GetComponent<Image>(), Is.Null);
        Assert.That(background.GetSiblingIndex(), Is.LessThan(cooldownRow.GetSiblingIndex()));
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
