using BackpackHero.Debugging;
using BackpackHero.Battle;
using BackpackPrototype;
using NUnit.Framework;
using TMPro;
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
        Assert.That(settings.ItemIconScale,
            Is.EqualTo(BackpackVisualSettings.DefaultItemIconScale));
        Assert.That(settings.AircraftGlowEnabled, Is.True);
        Assert.That(settings.AircraftGlowColor, Is.EqualTo(Color.white));
        Assert.That(settings.AircraftGlowMinimumIntensity, Is.EqualTo(.12f));
        Assert.That(settings.AircraftGlowMaximumIntensity, Is.EqualTo(.45f));
        Assert.That(settings.AircraftGlowCycleDuration, Is.EqualTo(1.2f));
        Assert.That(settings.AircraftGlowEdgeWidth, Is.EqualTo(8f));
        Assert.That(settings.EquipmentBottomPlateGlowEnabled, Is.False);
        Assert.That(settings.ColorQualityModeEnabled, Is.True);
        Assert.That(settings.ItemQualityPalette, Is.Null);
        Assert.That(settings.QualityLevelBadgeFont, Is.Null);
        Assert.That(settings.QualityLevelBadgeFontSize, Is.EqualTo(19f));
        Assert.That(settings.QualityLevelBadgeOutlineWidth, Is.EqualTo(.36f));
        Assert.That(settings.AircraftQualityPulseEnabled, Is.True);
        Assert.That(settings.AircraftQualityPulseInterval, Is.EqualTo(.8f));
        Assert.That(settings.AircraftQualityPulseScaleMultiplier,
            Is.EqualTo(1.12f));
        Assert.That(settings.AircraftQualityPulseTweenDuration,
            Is.EqualTo(.45f));
    }

    [Test]
    public void SettingsAsset_RoundTripsAndClampsValues()
    {
        BackpackVisualDebugSettings asset =
            ScriptableObject.CreateInstance<BackpackVisualDebugSettings>();
        BackpackVisualSettings expected = new(false, -1f, Color.red,
            Color.blue, -1f, 2f, -2f, .5f, 13f, -9f,
            BackpackPlacementScaleEase.OutCubic, -3f, Color.green, -4f,
            true, Color.magenta, -1f, 2f, -3f, -4f, false, false, null,
            TMP_Settings.defaultFontAsset, -5f, 2f, false,
            -1f, .5f, -2f, 9f);
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
            Assert.That(actual.ItemIconScale,
                Is.EqualTo(BackpackVisualSettings.MaximumItemIconScale));
            Assert.That(actual.AircraftGlowEnabled, Is.True);
            Assert.That(actual.AircraftGlowColor, Is.EqualTo(Color.magenta));
            Assert.That(actual.AircraftGlowMinimumIntensity, Is.Zero);
            Assert.That(actual.AircraftGlowMaximumIntensity, Is.EqualTo(1f));
            Assert.That(actual.AircraftGlowCycleDuration,
                Is.EqualTo(BackpackVisualSettings.MinimumAircraftGlowCycleDuration));
            Assert.That(actual.AircraftGlowEdgeWidth,
                Is.EqualTo(BackpackVisualSettings.MinimumAircraftGlowEdgeWidth));
            Assert.That(actual.EquipmentBottomPlateGlowEnabled, Is.False);
            Assert.That(actual.ColorQualityModeEnabled, Is.False);
            Assert.That(actual.ItemQualityPalette, Is.Null);
            Assert.That(actual.QualityLevelBadgeFont,
                Is.SameAs(TMP_Settings.defaultFontAsset));
            Assert.That(actual.QualityLevelBadgeFontSize,
                Is.EqualTo(BackpackVisualSettings.MinimumQualityLevelBadgeFontSize));
            Assert.That(actual.QualityLevelBadgeOutlineWidth,
                Is.EqualTo(BackpackVisualSettings.MaximumQualityLevelBadgeOutlineWidth));
            Assert.That(actual.AircraftQualityPulseEnabled, Is.False);
            Assert.That(actual.AircraftQualityPulseInterval,
                Is.EqualTo(BackpackVisualSettings.MinimumAircraftQualityPulseInterval));
            Assert.That(actual.AircraftQualityPulseScaleMultiplier,
                Is.EqualTo(BackpackVisualSettings.MinimumAircraftQualityPulseScale));
            Assert.That(actual.AircraftQualityPulseTweenDuration,
                Is.EqualTo(BackpackVisualSettings.MinimumAircraftQualityPulseTweenDuration));
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
            true, Color.white, .12f, .45f, 1.2f, 8f, true, true, null,
            null, 19f, .36f, true,
            .8f, 1.12f, .45f);
        BackpackVisualSettings custom = new(true, .5f, Color.yellow,
            Color.magenta, .18f, .62f, .7f, 1.17f, 10f, -6f,
            BackpackPlacementScaleEase.OutElastic, .32f, Color.green, 16f,
            true, Color.white, .12f, .45f, 1.2f, 8f, true, true, null,
            null, 19f, .36f, true,
            .8f, 1.12f, .45f);

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
                "Assets/GameData/Visual/BackpackVisual.asset");

        Assert.That(asset, Is.Not.Null);
        BackpackVisualSettings values = asset.GetValues();
        Assert.That(values.LevelFontColor, Is.EqualTo(Color.white));
        Assert.That(values.LevelFontSize,
            Is.GreaterThanOrEqualTo(BackpackVisualSettings.MinimumLevelFontSize));
        Assert.That(values.ItemIconScale,
            Is.EqualTo(BackpackVisualSettings.DefaultItemIconScale));
        Assert.That(values.AircraftGlowColor, Is.EqualTo(Color.white));
        Assert.That(values.AircraftGlowEdgeWidth,
            Is.GreaterThanOrEqualTo(BackpackVisualSettings.MinimumAircraftGlowEdgeWidth));
        Assert.That(values.EquipmentBottomPlateGlowEnabled, Is.False);
        Assert.That(values.ColorQualityModeEnabled, Is.True);
        Assert.That(values.ItemQualityPalette, Is.Not.Null);
        Assert.That(values.QualityLevelBadgeFont,
            Is.SameAs(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Resources/Fonts/Milker SDF.asset")));
        Assert.That(values.QualityLevelBadgeFontSize, Is.EqualTo(19f));
        Assert.That(values.QualityLevelBadgeOutlineWidth, Is.EqualTo(.36f));
        Assert.That(values.AircraftQualityPulseEnabled, Is.True);
        Assert.That(values.AircraftQualityPulseInterval, Is.EqualTo(.8f));
        Assert.That(values.AircraftQualityPulseScaleMultiplier,
            Is.EqualTo(1.12f));
        Assert.That(values.AircraftQualityPulseTweenDuration,
            Is.EqualTo(.45f));
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
    public void AircraftQualityPulse_RequiresQualityModePlayerBackpackAircraft()
    {
        Assert.That(ItemView.IsAircraftQualityPulseEligible(true, true,
            ItemType.Aircraft, true, BattleFaction.Player,
            BattlePhase.Preparation, false), Is.True);
        Assert.That(ItemView.IsAircraftQualityPulseEligible(false, true,
            ItemType.Aircraft, true, BattleFaction.Player,
            BattlePhase.Preparation, false), Is.False);
        Assert.That(ItemView.IsAircraftQualityPulseEligible(true, false,
            ItemType.Aircraft, true, BattleFaction.Player,
            BattlePhase.Preparation, false), Is.False);
        Assert.That(ItemView.IsAircraftQualityPulseEligible(true, true,
            ItemType.Equipment, true, BattleFaction.Player,
            BattlePhase.Preparation, false), Is.False);
        Assert.That(ItemView.IsAircraftQualityPulseEligible(true, true,
            ItemType.Aircraft, false, BattleFaction.Player,
            BattlePhase.Preparation, false), Is.False);
        Assert.That(ItemView.IsAircraftQualityPulseEligible(true, true,
            ItemType.Aircraft, true, BattleFaction.Enemy,
            BattlePhase.Preparation, false), Is.False);
        Assert.That(ItemView.IsAircraftQualityPulseEligible(true, true,
            ItemType.Aircraft, true, BattleFaction.Player,
            BattlePhase.Combat, false), Is.False);
        Assert.That(ItemView.IsAircraftQualityPulseEligible(true, true,
            ItemType.Aircraft, true, BattleFaction.Player,
            BattlePhase.Preparation, true), Is.False);
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

    [Test]
    public void ColorQualityMode_UsesAuthoredSpriteColorInsteadOfCodeTint()
    {
        Color original = new(.2f, .4f, .6f, 1f);

        Assert.That(ItemView.GetBottomPlateTint(original, 2, true, true),
            Is.EqualTo(Color.white));
        Assert.That(ItemView.GetBottomPlateTint(original, 2, true, false),
            Is.EqualTo(ItemView.GetBottomPlateColor(original, 2, true)));
    }
}
