using BackpackHero.Debugging;
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
    }

    [Test]
    public void SettingsAsset_RoundTripsAndNormalizesInvalidStyle()
    {
        ArtAssetDebugSettings asset =
            ScriptableObject.CreateInstance<ArtAssetDebugSettings>();
        try
        {
            asset.SetValues(new ArtAssetDebugSettingsValue(
                BackpackShipStyle.Style1));
            Assert.That(asset.GetValues().BackpackShipStyle,
                Is.EqualTo(BackpackShipStyle.Style1));

            asset.SetValues(new ArtAssetDebugSettingsValue(
                (BackpackShipStyle)999));
            Assert.That(asset.GetValues().BackpackShipStyle,
                Is.EqualTo(BackpackShipStyle.Style2));
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
                BackpackShipStyle.Style1));

            Assert.That(ArtAssetDebugRuntime.CurrentSettings.BackpackShipStyle,
                Is.EqualTo(BackpackShipStyle.Style1));
            Assert.That(published.BackpackShipStyle,
                Is.EqualTo(BackpackShipStyle.Style1));
        }
        finally
        {
            ArtAssetDebugRuntime.SettingsChanged -= HandleChanged;
            Object.DestroyImmediate(runtimeObject);
        }
    }
}
