using BackpackHero.Debugging;
using NUnit.Framework;
using UnityEditor;

public sealed class ItemQualityPaletteTests
{
    private const string PalettePath =
        "Assets/GameData/Visual/ItemQualityPalette.asset";

    [Test]
    public void Palette_BindsEveryAuthoredQualityAndShape()
    {
        ItemQualityPalette palette =
            AssetDatabase.LoadAssetAtPath<ItemQualityPalette>(PalettePath);

        Assert.That(palette, Is.Not.Null);
        foreach (ItemQuality quality in new[]
                 {
                     ItemQuality.Green, ItemQuality.Blue,
                     ItemQuality.Purple, ItemQuality.Orange
                 })
        {
            foreach (ItemBaseShape shape in new[]
                     {
                         ItemBaseShape.VerticalBar,
                         ItemBaseShape.HorizontalBar,
                         ItemBaseShape.LMissingBottomLeft,
                         ItemBaseShape.LMissingBottomRight,
                         ItemBaseShape.LMissingTopLeft,
                         ItemBaseShape.LMissingTopRight
                     })
            {
                Assert.That(palette.GetSprite(quality, shape), Is.Not.Null,
                    $"{quality}/{shape}");
            }
        }

        Assert.That(palette.GetSprite(ItemQuality.Green,
            ItemBaseShape.Square), Is.Null);
    }

    [Test]
    public void Palette_BindsEachQualityAndShapeToItsExpectedAsset()
    {
        ItemQualityPalette palette =
            AssetDatabase.LoadAssetAtPath<ItemQualityPalette>(PalettePath);

        foreach ((ItemQuality quality, string colorFolder) in new[]
                 {
                     (ItemQuality.Green, "绿"),
                     (ItemQuality.Blue, "蓝"),
                     (ItemQuality.Purple, "紫"),
                     (ItemQuality.Orange, "橙")
                 })
        {
            AssertSpritePath(palette, quality, ItemBaseShape.VerticalBar,
                colorFolder, "item_1x2.png");
            AssertSpritePath(palette, quality, ItemBaseShape.HorizontalBar,
                colorFolder, "item_2x1.png");
            AssertSpritePath(palette, quality, ItemBaseShape.LMissingBottomLeft,
                colorFolder, "item_L_rot180.png");
            AssertSpritePath(palette, quality, ItemBaseShape.LMissingBottomRight,
                colorFolder, "item_L_rot090.png");
            AssertSpritePath(palette, quality, ItemBaseShape.LMissingTopLeft,
                colorFolder, "item_L_rot360.png");
            AssertSpritePath(palette, quality, ItemBaseShape.LMissingTopRight,
                colorFolder, "item_L_rot000.png");
        }
    }

    [TestCase(1, ItemQuality.Green)]
    [TestCase(2, ItemQuality.Blue)]
    [TestCase(3, ItemQuality.Purple)]
    [TestCase(99, ItemQuality.Purple)]
    public void Palette_MapsInMatchLevelsToConfiguredQuality(
        int level, ItemQuality expected)
    {
        ItemQualityPalette palette =
            AssetDatabase.LoadAssetAtPath<ItemQualityPalette>(PalettePath);

        Assert.That(palette.GetQualityForLevel(level), Is.EqualTo(expected));
    }

    [Test]
    public void Palette_MapsLShapesToTheirMatchingRotationAssets()
    {
        ItemQualityPalette palette =
            AssetDatabase.LoadAssetAtPath<ItemQualityPalette>(PalettePath);

        Assert.That(AssetDatabase.GetAssetPath(palette.GetSprite(
            ItemQuality.Green, ItemBaseShape.LMissingBottomLeft)),
            Does.EndWith("绿/item_L_rot180.png"));
        Assert.That(AssetDatabase.GetAssetPath(palette.GetSprite(
            ItemQuality.Green, ItemBaseShape.LMissingBottomRight)),
            Does.EndWith("绿/item_L_rot090.png"));
        Assert.That(AssetDatabase.GetAssetPath(palette.GetSprite(
            ItemQuality.Green, ItemBaseShape.LMissingTopLeft)),
            Does.EndWith("绿/item_L_rot360.png"));
        Assert.That(AssetDatabase.GetAssetPath(palette.GetSprite(
            ItemQuality.Green, ItemBaseShape.LMissingTopRight)),
            Does.EndWith("绿/item_L_rot000.png"));
    }

    private static void AssertSpritePath(
        ItemQualityPalette palette,
        ItemQuality quality,
        ItemBaseShape shape,
        string colorFolder,
        string expectedFileName)
    {
        Assert.That(AssetDatabase.GetAssetPath(palette.GetSprite(quality, shape)),
            Does.EndWith($"{colorFolder}/{expectedFileName}"),
            $"{quality}/{shape}");
    }
}
