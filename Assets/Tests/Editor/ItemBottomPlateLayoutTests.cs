using System.Reflection;
using BackpackHero.Debugging;
using BackpackPrototype;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class ItemBottomPlateLayoutTests
{
    private const string PalettePath =
        "Assets/GameData/Visual/ItemQualityPalette.asset";

    [TestCase(168f, 80f, 2, 1, 290f, 162f, 190.3125f, 101.25f)]
    [TestCase(80f, 168f, 1, 2, 162f, 290f, 101.25f, 190.3125f)]
    [TestCase(168f, 168f, 2, 2, 290f, 290f, 190.3125f, 190.3125f)]
    public void BottomPlateLayout_PreservesLogicalCoreAndCentersGlow(
        float logicalWidth, float logicalHeight,
        int cellsWide, int cellsHigh,
        float spriteWidth, float spriteHeight,
        float expectedWidth, float expectedHeight)
    {
        Vector2 displayedSize = CalculateDisplaySize(
            new Vector2(logicalWidth, logicalHeight),
            new Vector2Int(cellsWide, cellsHigh),
            new Vector2(spriteWidth, spriteHeight));

        Assert.That(displayedSize.x, Is.EqualTo(expectedWidth).Within(.0001f));
        Assert.That(displayedSize.y, Is.EqualTo(expectedHeight).Within(.0001f));

        float coreWidth = 128f * cellsWide;
        float coreHeight = 128f * cellsHigh;
        Assert.That(coreWidth * displayedSize.x / spriteWidth,
            Is.EqualTo(logicalWidth).Within(.0001f));
        Assert.That(coreHeight * displayedSize.y / spriteHeight,
            Is.EqualTo(logicalHeight).Within(.0001f));
    }

    [Test]
    public void QualitySprites_KeepTheExpectedSeventeenPixelGlowBorder()
    {
        ItemQualityPalette palette =
            AssetDatabase.LoadAssetAtPath<ItemQualityPalette>(PalettePath);

        foreach (ItemQuality quality in new[]
                 {
                     ItemQuality.Green, ItemQuality.Blue,
                     ItemQuality.Purple, ItemQuality.Orange
                 })
        {
            AssertSpriteSize(palette.GetSprite(quality,
                    ItemBaseShape.VerticalBar), 162f, 290f);
            AssertSpriteSize(palette.GetSprite(quality,
                    ItemBaseShape.HorizontalBar), 290f, 162f);

            foreach (ItemBaseShape shape in new[]
                     {
                         ItemBaseShape.LMissingBottomLeft,
                         ItemBaseShape.LMissingBottomRight,
                         ItemBaseShape.LMissingTopLeft,
                         ItemBaseShape.LMissingTopRight
                     })
            {
                AssertSpriteSize(palette.GetSprite(quality, shape),
                    290f, 290f);
            }
        }
    }

    [TestCase("Item_1x2.prefab")]
    [TestCase("Item_2x1.prefab")]
    [TestCase("Item_L_MissingBottomLeft.prefab")]
    [TestCase("Item_L_MissingBottomRight.prefab")]
    [TestCase("Item_L_MissingTopLeft.prefab")]
    [TestCase("Item_L_MissingTopRight.prefab")]
    public void MultiCellPrefabs_UseASeparateBottomPlateVisual(string prefabName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/BackpackUI/Items/" + prefabName);
        ItemView view = prefab.GetComponent<ItemView>();
        Image rootImage = prefab.GetComponent<Image>();
        Image bottomPlate = prefab.transform.Find("BottomPlate")
            .GetComponent<Image>();
        SerializedObject serializedView = new(view);

        Assert.That(bottomPlate, Is.Not.Null);
        Assert.That(serializedView.FindProperty("background")
            .objectReferenceValue, Is.SameAs(bottomPlate));
        Assert.That(rootImage.raycastTarget, Is.True);
        Assert.That(rootImage.color.a, Is.Zero);
        Assert.That(bottomPlate.raycastTarget, Is.False);
    }

    [Test]
    public void LevelBadgeNumber_UsesDoubleThePreviousOutlineWidth()
    {
        GameObject root = new("Item", typeof(RectTransform),
            typeof(CanvasGroup), typeof(ItemView));
        GameObject iconObject = new("ItemIcon", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        try
        {
            iconObject.transform.SetParent(root.transform, false);
            ItemView view = root.GetComponent<ItemView>();
            SetField(view, "icon", iconObject.GetComponent<Image>());

            typeof(ItemView).GetMethod("EnsureLevelBadge",
                BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, null);

            TextMeshProUGUI number = root.transform.Find(
                "LevelBadge/LevelNumber").GetComponent<TextMeshProUGUI>();
            Assert.That(number.outlineWidth, Is.EqualTo(.36f));
            Assert.That(number.font,
                Is.SameAs(Resources.Load<TMP_FontAsset>("Fonts/Milker SDF")));
            Assert.That(number.font.sourceFontFile,
                Is.SameAs(Resources.Load<Font>("Fonts/Milker")));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ExistingLevelBadgeNumber_ReappliesMilkerFontAfterReload()
    {
        GameObject root = new("Item", typeof(RectTransform),
            typeof(CanvasGroup), typeof(ItemView));
        GameObject iconObject = new("ItemIcon", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        GameObject badgeObject = new("LevelBadge", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        GameObject numberObject = new("LevelNumber", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        try
        {
            iconObject.transform.SetParent(root.transform, false);
            badgeObject.transform.SetParent(root.transform, false);
            numberObject.transform.SetParent(badgeObject.transform, false);
            TextMeshProUGUI number = numberObject.GetComponent<TextMeshProUGUI>();
            number.font = TMP_Settings.defaultFontAsset;
            number.outlineWidth = .18f;

            ItemView view = root.GetComponent<ItemView>();
            SetField(view, "icon", iconObject.GetComponent<Image>());
            typeof(ItemView).GetMethod("EnsureLevelBadge",
                BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, null);

            Assert.That(number.font,
                Is.SameAs(Resources.Load<TMP_FontAsset>("Fonts/Milker SDF")));
            Assert.That(number.font.sourceFontFile,
                Is.SameAs(Resources.Load<Font>("Fonts/Milker")));
            Assert.That(number.outlineWidth, Is.EqualTo(.36f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void MilkerLevelBadgeFontAsset_ContainsEveryLevelDigit()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/Milker SDF");

        Assert.That(font, Is.Not.Null);
        foreach (char digit in "0123456789")
        {
            Assert.That(font.HasCharacter(digit), Is.True,
                $"Milker TMP font asset is missing '{digit}'.");
        }
    }

    private static Vector2 CalculateDisplaySize(
        Vector2 logicalSize,
        Vector2Int cellCount,
        Vector2 spriteSize)
    {
        MethodInfo method = typeof(ItemView).GetMethod(
            "CalculateBottomPlateDisplaySize",
            BindingFlags.Static | BindingFlags.NonPublic);

        return (Vector2)method.Invoke(null,
            new object[] { logicalSize, cellCount, spriteSize });
    }

    private static void AssertSpriteSize(
        Sprite sprite,
        float expectedWidth,
        float expectedHeight)
    {
        Assert.That(sprite, Is.Not.Null);
        Assert.That(sprite.rect.width, Is.EqualTo(expectedWidth));
        Assert.That(sprite.rect.height, Is.EqualTo(expectedHeight));
    }

    private static void SetField(object target, string fieldName, object value)
    {
        typeof(ItemView).GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(target, value);
    }
}
