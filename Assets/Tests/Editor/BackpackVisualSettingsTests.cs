using BackpackHero.Debugging;
using BackpackHero.Battle;
using BackpackPrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BackpackVisualSettingsTests
{
    [Test]
    public void Default_MatchesExistingBackpackVisualValues()
    {
        BackpackVisualSettings settings = BackpackVisualSettings.Default;

        Assert.That(settings.OverridesEnabled, Is.True);
        Assert.That(settings.DragOpacity, Is.EqualTo(.5f));
        Assert.That(settings.LegalPreviewColor,
            Is.EqualTo(new Color(.95f, .78f, .18f, 1f)));
        Assert.That(settings.IllegalPreviewColor,
            Is.EqualTo(new Color(.86f, .22f, .18f, 1f)));
        Assert.That(settings.MergeFlashMinimum, Is.EqualTo(.18f));
        Assert.That(settings.MergeFlashMaximum, Is.EqualTo(.62f));
        Assert.That(settings.MergeFlashCycleDuration, Is.EqualTo(.7f));
        Assert.That(settings.PlacementScaleMultiplier, Is.EqualTo(1.17f));
        Assert.That(settings.PlacementPositiveRotationDegrees, Is.EqualTo(10f));
        Assert.That(settings.PlacementNegativeRotationDegrees, Is.EqualTo(-6f));
        Assert.That(settings.PlacementScaleEase,
            Is.EqualTo(BackpackPlacementScaleEase.OutElastic));
        Assert.That(settings.ShopFlightDuration, Is.EqualTo(.32f));
        Assert.That(settings.LevelFontColor, Is.EqualTo(Color.white));
        Assert.That(settings.LevelFontSize, Is.EqualTo(16f));
        Assert.That(settings.AircraftGlowEnabled, Is.True);
        Assert.That(settings.AircraftGlowColor, Is.EqualTo(Color.white));
        Assert.That(settings.AircraftGlowMinimumIntensity, Is.EqualTo(.12f));
        Assert.That(settings.AircraftGlowMaximumIntensity, Is.EqualTo(.45f));
        Assert.That(settings.AircraftGlowCycleDuration, Is.EqualTo(1.2f));
        Assert.That(settings.AircraftGlowEdgeWidth, Is.EqualTo(8f));
    }

    [Test]
    public void SettingsAsset_RoundTripsAndClampsValues()
    {
        BackpackVisualDebugSettings asset =
            ScriptableObject.CreateInstance<BackpackVisualDebugSettings>();
        BackpackVisualSettings expected = new(false, -1f, Color.red,
            Color.blue, -1f, 2f, -2f, .5f, 13f, -9f,
            BackpackPlacementScaleEase.OutCubic, -3f, Color.green, -4f,
            true, Color.magenta, -1f, 2f, -3f, -4f);
        try
        {
            asset.SetValues(expected);
            BackpackVisualSettings actual = asset.GetValues();

            Assert.That(actual.OverridesEnabled, Is.False);
            Assert.That(actual.DragOpacity, Is.Zero);
            Assert.That(actual.LegalPreviewColor, Is.EqualTo(Color.red));
            Assert.That(actual.IllegalPreviewColor, Is.EqualTo(Color.blue));
            Assert.That(actual.MergeFlashMinimum, Is.Zero);
            Assert.That(actual.MergeFlashMaximum, Is.EqualTo(1f));
            Assert.That(actual.MergeFlashCycleDuration,
                Is.EqualTo(BackpackVisualSettings.MinimumFlashDuration));
            Assert.That(actual.PlacementScaleMultiplier,
                Is.EqualTo(BackpackVisualSettings.MinimumPlacementScale));
            Assert.That(actual.PlacementPositiveRotationDegrees, Is.EqualTo(13f));
            Assert.That(actual.PlacementNegativeRotationDegrees, Is.EqualTo(-9f));
            Assert.That(actual.PlacementScaleEase,
                Is.EqualTo(BackpackPlacementScaleEase.OutCubic));
            Assert.That(actual.ShopFlightDuration,
                Is.EqualTo(BackpackVisualSettings.MinimumShopFlightDuration));
            Assert.That(actual.LevelFontColor, Is.EqualTo(Color.green));
            Assert.That(actual.LevelFontSize,
                Is.EqualTo(BackpackVisualSettings.MinimumLevelFontSize));
            Assert.That(actual.AircraftGlowEnabled, Is.True);
            Assert.That(actual.AircraftGlowColor, Is.EqualTo(Color.magenta));
            Assert.That(actual.AircraftGlowMinimumIntensity, Is.Zero);
            Assert.That(actual.AircraftGlowMaximumIntensity, Is.EqualTo(1f));
            Assert.That(actual.AircraftGlowCycleDuration,
                Is.EqualTo(BackpackVisualSettings.MinimumAircraftGlowCycleDuration));
            Assert.That(actual.AircraftGlowEdgeWidth,
                Is.EqualTo(BackpackVisualSettings.MinimumAircraftGlowEdgeWidth));
            Assert.That(actual.GetPlacementScaleDotweenEase(),
                Is.EqualTo(DG.Tweening.Ease.OutCubic));
            Assert.That(BackpackVisualSettings.Default
                .WithOverridesEnabled(false).OverridesEnabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void LevelColor_UsesFactionOnlyForWhiteOrDisabledOverrides()
    {
        BackpackVisualSettings white = new(true, .5f, Color.yellow,
            Color.magenta, .18f, .62f, .7f, 1.17f, 10f, -6f,
            BackpackPlacementScaleEase.OutElastic, .32f,
            new Color(1f, 1f, 1f, .2f), 16f,
            true, Color.white, .12f, .45f, 1.2f, 8f);
        BackpackVisualSettings custom = new(true, .5f, Color.yellow,
            Color.magenta, .18f, .62f, .7f, 1.17f, 10f, -6f,
            BackpackPlacementScaleEase.OutElastic, .32f, Color.green, 16f,
            true, Color.white, .12f, .45f, 1.2f, 8f);

        Assert.That(white.UsesFactionLevelColor, Is.True);
        Assert.That(custom.UsesFactionLevelColor, Is.False);
        Assert.That(custom.WithOverridesEnabled(false).UsesFactionLevelColor,
            Is.True);
    }

    [Test]
    public void DefaultSettingsAsset_ExistsAtRuntimeResourcePath()
    {
        BackpackVisualDebugSettings asset =
            AssetDatabase.LoadAssetAtPath<BackpackVisualDebugSettings>(
                "Assets/Resources/BackpackVisual/BackpackVisualDebugSettings.asset");

        Assert.That(asset, Is.Not.Null);
        BackpackVisualSettings values = asset.GetValues();
        Assert.That(values.LevelFontColor, Is.EqualTo(Color.white));
        Assert.That(values.LevelFontSize,
            Is.GreaterThanOrEqualTo(BackpackVisualSettings.MinimumLevelFontSize));
        Assert.That(values.AircraftGlowColor, Is.EqualTo(Color.white));
        Assert.That(values.AircraftGlowEdgeWidth,
            Is.GreaterThanOrEqualTo(BackpackVisualSettings.MinimumAircraftGlowEdgeWidth));
    }

    [Test]
    public void AircraftGlow_RequiresPlayerBackpackAircraftInPreparation()
    {
        Assert.That(ItemView.IsAircraftGlowEligible(true, true,
            ItemType.Aircraft, true, BattleFaction.Player,
            BattlePhase.Preparation), Is.True);
        Assert.That(ItemView.IsAircraftGlowEligible(true, true,
            ItemType.Aircraft, false, BattleFaction.Player,
            BattlePhase.Preparation), Is.False);
        Assert.That(ItemView.IsAircraftGlowEligible(true, true,
            ItemType.Aircraft, true, BattleFaction.Enemy,
            BattlePhase.Preparation), Is.False);
        Assert.That(ItemView.IsAircraftGlowEligible(true, true,
            ItemType.Equipment, true, BattleFaction.Player,
            BattlePhase.Preparation), Is.False);
        Assert.That(ItemView.IsAircraftGlowEligible(true, true,
            ItemType.Aircraft, true, BattleFaction.Player,
            BattlePhase.Combat), Is.False);
        Assert.That(ItemView.IsAircraftGlowEligible(false, true,
            ItemType.Aircraft, true, BattleFaction.Player,
            BattlePhase.Preparation), Is.False);
        Assert.That(ItemView.IsAircraftGlowEligible(true, false,
            ItemType.Aircraft, true, BattleFaction.Player,
            BattlePhase.Preparation), Is.False);
    }

    [Test]
    public void BottomPlateHighlight_UsesLevelColorsAndHidesOnlyEquipmentLabels()
    {
        Color original = Color.red;

        Assert.That(ItemView.GetBottomPlateColor(original, 1, false),
            Is.EqualTo(original));
        Assert.That(ItemView.GetBottomPlateColor(original, 1, true),
            Is.EqualTo(new Color(1f, .8f, .5019608f, 1f)));
        Assert.That(ItemView.GetBottomPlateColor(original, 2, true),
            Is.EqualTo(new Color(.5058824f, .7803922f, .5176471f, 1f)));
        Assert.That(ItemView.GetBottomPlateColor(original, 3, true),
            Is.EqualTo(new Color(.65882355f, .33333334f, .96862745f, 1f)));
        Assert.That(ItemView.GetBottomPlateColor(original, 99, true),
            Is.EqualTo(new Color(.65882355f, .33333334f, .96862745f, 1f)));
        Assert.That(ItemView.ShouldHideEquipmentLevelLabel(true,
            ItemType.Equipment), Is.True);
        Assert.That(ItemView.ShouldHideEquipmentLevelLabel(true,
            ItemType.Aircraft), Is.False);
        Assert.That(ItemView.ShouldHideEquipmentLevelLabel(false,
            ItemType.Equipment), Is.False);
    }
}
