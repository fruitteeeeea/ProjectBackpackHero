using BackpackHero.Debugging;
using BackpackPrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ArtAssetDebugSettingsTests
{
    [Test]
    public void Default_UsesBackpackShipStyle2()
    {
        Assert.That(ArtAssetDebugSettingsValue.Default.BackpackShipStyle,
            Is.EqualTo(BackpackShipStyle.Style2));
        Assert.That(ArtAssetDebugSettingsValue.Default.ItemBaseStyle,
            Is.EqualTo(ItemBaseStyle.Style2));
    }

    [Test]
    public void SettingsAsset_RoundTripsAndNormalizesInvalidStyle()
    {
        ArtAssetDebugSettings asset =
            ScriptableObject.CreateInstance<ArtAssetDebugSettings>();
        try
        {
            asset.SetValues(new ArtAssetDebugSettingsValue(
                BackpackShipStyle.Style1, ItemBaseStyle.Style1));
            Assert.That(asset.GetValues().BackpackShipStyle,
                Is.EqualTo(BackpackShipStyle.Style1));
            Assert.That(asset.GetValues().ItemBaseStyle,
                Is.EqualTo(ItemBaseStyle.Style1));

            asset.SetValues(new ArtAssetDebugSettingsValue(
                (BackpackShipStyle)999, (ItemBaseStyle)999));
            Assert.That(asset.GetValues().BackpackShipStyle,
                Is.EqualTo(BackpackShipStyle.Style2));
            Assert.That(asset.GetValues().ItemBaseStyle,
                Is.EqualTo(ItemBaseStyle.Style2));
        }
        finally
        {
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void DefaultArtAsset_IsRegisteredInGameDataCatalog()
    {
        GameDataCatalog catalog = Resources.Load<GameDataCatalog>(
            "GameDataCatalog");
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.ArtAsset, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(catalog.ArtAsset), Is.EqualTo(
            "Assets/GameData/Visual/ArtAsset.asset"));
        Assert.That(catalog.ArtAsset.GetValues().BackpackShipStyle,
            Is.EqualTo(BackpackShipStyle.Style2));
        Assert.That(catalog.ArtAsset.GetValues().ItemBaseStyle,
            Is.EqualTo(ItemBaseStyle.Style2));
        foreach (ItemBaseShape shape in System.Enum.GetValues(typeof(ItemBaseShape)))
        {
            Assert.That(catalog.ArtAsset.GetStyle2ItemBaseSprite(shape),
                Is.Not.Null, shape.ToString());
        }
    }

    [Test]
    public void Runtime_SetSettingsPublishesNormalizedCurrentStyle()
    {
        GameObject runtimeObject = new("ArtAssetDebugRuntimeTests");
        ArtAssetDebugRuntime runtime = runtimeObject.AddComponent<ArtAssetDebugRuntime>();
        ArtAssetDebugSettingsValue published = ArtAssetDebugSettingsValue.Default;
        void HandleChanged(ArtAssetDebugSettingsValue value) => published = value;
        ArtAssetDebugRuntime.SettingsChanged += HandleChanged;
        try
        {
            runtime.SetSettings(new ArtAssetDebugSettingsValue(
                BackpackShipStyle.Style1, ItemBaseStyle.Style1));

            Assert.That(ArtAssetDebugRuntime.CurrentSettings.BackpackShipStyle,
                Is.EqualTo(BackpackShipStyle.Style1));
            Assert.That(published.BackpackShipStyle,
                Is.EqualTo(BackpackShipStyle.Style1));
            Assert.That(published.ItemBaseStyle,
                Is.EqualTo(ItemBaseStyle.Style1));
        }
        finally
        {
            ArtAssetDebugRuntime.SettingsChanged -= HandleChanged;
            Object.DestroyImmediate(runtimeObject);
        }
    }

    [TestCase("Assets/Prefabs/BackpackUI/Items/Item_1x1.prefab", ItemBaseShape.Square)]
    [TestCase("Assets/Prefabs/BackpackUI/Items/Item_1x2.prefab", ItemBaseShape.VerticalBar)]
    [TestCase("Assets/Prefabs/BackpackUI/Items/Item_2x1.prefab", ItemBaseShape.HorizontalBar)]
    [TestCase("Assets/Prefabs/BackpackUI/Items/Item_L_MissingBottomLeft.prefab", ItemBaseShape.LMissingBottomLeft)]
    [TestCase("Assets/Prefabs/BackpackUI/Items/Item_L_MissingBottomRight.prefab", ItemBaseShape.LMissingBottomRight)]
    [TestCase("Assets/Prefabs/BackpackUI/Items/Item_L_MissingTopLeft.prefab", ItemBaseShape.LMissingTopLeft)]
    [TestCase("Assets/Prefabs/BackpackUI/Items/Item_L_MissingTopRight.prefab", ItemBaseShape.LMissingTopRight)]
    public void ItemViewPrefabs_DeclareTheirBaseShape(
        string path, ItemBaseShape expected)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<ItemView>().ItemBaseShape,
            Is.EqualTo(expected));
    }
}
