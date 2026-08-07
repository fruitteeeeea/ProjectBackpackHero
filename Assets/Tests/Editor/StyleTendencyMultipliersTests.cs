using BackpackHero.Debugging;
using BackpackPrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class StyleTendencyMultipliersTests
{
    private GameObject styleRuntimeObject;
    private GameObject pacingRuntimeObject;

    [TearDown]
    public void TearDown()
    {
        if (styleRuntimeObject != null)
        {
            Object.DestroyImmediate(styleRuntimeObject);
        }

        if (pacingRuntimeObject != null)
        {
            Object.DestroyImmediate(pacingRuntimeObject);
        }
    }

    [Test]
    public void Default_UsesOneForEveryStyleMultiplier()
    {
        StyleTendencyMultipliers values = StyleTendencyMultipliers.Default;

        Assert.That(values.ItemCooldownSpeed, Is.EqualTo(1f));
        Assert.That(values.AircraftTargetingArcAngle, Is.EqualTo(1f));
        Assert.That(values.AircraftAttackRange, Is.EqualTo(1f));
        Assert.That(values.AircraftAttackSpeed, Is.EqualTo(1f));
    }

    [Test]
    public void Constructor_ClampsEveryMultiplierToStyleRange()
    {
        StyleTendencyMultipliers values = new(0f, 4f, 0.25f, 3f);

        Assert.That(values.ItemCooldownSpeed,
            Is.EqualTo(StyleTendencyMultipliers.MinimumMultiplier));
        Assert.That(values.AircraftTargetingArcAngle,
            Is.EqualTo(StyleTendencyMultipliers.MaximumMultiplier));
        Assert.That(values.AircraftAttackRange,
            Is.EqualTo(StyleTendencyMultipliers.MinimumMultiplier));
        Assert.That(values.AircraftAttackSpeed,
            Is.EqualTo(
                StyleTendencyMultipliers.MaximumAircraftAttackSpeedMultiplier));

        StyleTendencyMultipliers lowAttackSpeed = new(1f, 1f, 1f, 0f);
        Assert.That(lowAttackSpeed.AircraftAttackSpeed,
            Is.EqualTo(
                StyleTendencyMultipliers.MinimumAircraftAttackSpeedMultiplier));
    }

    [Test]
    public void Settings_RoundTripsValues()
    {
        StyleTendencyDebugSettings settings =
            ScriptableObject.CreateInstance<StyleTendencyDebugSettings>();
        try
        {
            StyleTendencyMultipliers expected = new(3f, 0.5f, 1.5f, 0.2f);
            settings.SetValues(expected);
            StyleTendencyMultipliers actual = settings.GetValues();

            Assert.That(actual.ItemCooldownSpeed,
                Is.EqualTo(expected.ItemCooldownSpeed));
            Assert.That(actual.AircraftTargetingArcAngle,
                Is.EqualTo(expected.AircraftTargetingArcAngle));
            Assert.That(actual.AircraftAttackRange,
                Is.EqualTo(expected.AircraftAttackRange));
            Assert.That(actual.AircraftAttackSpeed,
                Is.EqualTo(expected.AircraftAttackSpeed));
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    [Test]
    public void PresetAssets_StartWithOneForEveryMultiplier()
    {
        AssertPresetIsDefault("Assets/Resources/StyleTendency/Strategy.asset");
        AssertPresetIsDefault("Assets/Resources/StyleTendency/Arcade.asset");
    }

    [Test]
    public void Runtime_UsesStrategyAsDefaultAndExposesAppliedValues()
    {
        StyleTendencyDebugRuntime runtime = EnsureStyleRuntime();
        StyleTendencyDebugSettings strategy =
            AssetDatabase.LoadAssetAtPath<StyleTendencyDebugSettings>(
                "Assets/Resources/StyleTendency/Strategy.asset");

        Assert.That(runtime.DefaultSettings, Is.EqualTo(strategy));

        runtime.SetMultipliers(new StyleTendencyMultipliers(2f, 3f, 0.5f, 1.5f));

        Assert.That(StyleTendencyDebugRuntime
                .GetItemCooldownSpeedMultiplier(), Is.EqualTo(2f));
        Assert.That(StyleTendencyDebugRuntime
                .GetAircraftTargetingArcAngleMultiplier(), Is.EqualTo(3f));
        Assert.That(StyleTendencyDebugRuntime
                .GetAircraftAttackRangeMultiplier(), Is.EqualTo(0.5f));
        Assert.That(StyleTendencyDebugRuntime
                .GetAircraftAttackSpeedMultiplier(), Is.EqualTo(1.5f));
    }

    [Test]
    public void ItemCooldownSpeed_AppliesAfterPacingDurationAndOnlyWhenStarted()
    {
        StyleTendencyDebugRuntime styleRuntime = EnsureStyleRuntime();
        GamePacingDebugRuntime pacingRuntime = EnsurePacingRuntime();
        GamePacingMultipliers originalPacing = pacingRuntime.Multipliers;
        StyleTendencyMultipliers originalStyle = styleRuntime.Multipliers;
        ItemData data = ScriptableObject.CreateInstance<ItemData>();
        data.InitializeForTests("Cooldown Equipment", ItemType.Equipment, 2f, null);
        ItemInstance item = new("cooldown-equipment", data, Vector2Int.zero);

        try
        {
            pacingRuntime.SetMultipliers(new GamePacingMultipliers(
                1f, 1f, 1f, 1f, 1f, 1f, 1f, 1.5f));
            styleRuntime.SetMultipliers(new StyleTendencyMultipliers(3f, 1f, 1f));

            item.BeginCooldown();
            Assert.That(item.RemainingCooldown, Is.EqualTo(1f));

            styleRuntime.SetMultipliers(new StyleTendencyMultipliers(0.5f, 1f, 1f));
            item.TickCooldown(0.5f);
            Assert.That(item.RemainingCooldown, Is.EqualTo(0.5f));

            item.TickCooldown(0.5f);
            item.BeginCooldown();
            Assert.That(item.RemainingCooldown, Is.EqualTo(6f));
        }
        finally
        {
            pacingRuntime.SetMultipliers(originalPacing);
            styleRuntime.SetMultipliers(originalStyle);
            Object.DestroyImmediate(data);
        }
    }

    private StyleTendencyDebugRuntime EnsureStyleRuntime()
    {
        if (StyleTendencyDebugRuntime.Instance != null)
        {
            return StyleTendencyDebugRuntime.Instance;
        }

        styleRuntimeObject = new GameObject("Style Tendency Runtime Test");
        return styleRuntimeObject.AddComponent<StyleTendencyDebugRuntime>();
    }

    private GamePacingDebugRuntime EnsurePacingRuntime()
    {
        if (GamePacingDebugRuntime.Instance != null)
        {
            return GamePacingDebugRuntime.Instance;
        }

        pacingRuntimeObject = new GameObject("Game Pacing Runtime Test");
        return pacingRuntimeObject.AddComponent<GamePacingDebugRuntime>();
    }

    private static void AssertPresetIsDefault(string path)
    {
        StyleTendencyDebugSettings settings =
            AssetDatabase.LoadAssetAtPath<StyleTendencyDebugSettings>(path);
        Assert.That(settings, Is.Not.Null, path);
        StyleTendencyMultipliers values = settings.GetValues();
        Assert.That(values.ItemCooldownSpeed, Is.EqualTo(1f), path);
        Assert.That(values.AircraftTargetingArcAngle, Is.EqualTo(1f), path);
        Assert.That(values.AircraftAttackRange, Is.EqualTo(1f), path);
        Assert.That(values.AircraftAttackSpeed, Is.EqualTo(1f), path);
    }
}
