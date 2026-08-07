using BackpackHero.Battle;
using BackpackHero.Debugging;
using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class GamePacingMultipliersTests
{
    private GameObject runtimeObject;

    [TearDown]
    public void TearDown()
    {
        if (runtimeObject != null)
        {
            Object.DestroyImmediate(runtimeObject);
        }
    }

    [Test]
    public void Default_UsesOneForBothOverallStrengths()
    {
        GamePacingMultipliers values =
            GamePacingMultipliers.Default;

        Assert.That(values.PlayerOverallStrength, Is.EqualTo(1f));
        Assert.That(values.EnemyOverallStrength, Is.EqualTo(1f));
        Assert.That(values.WhiteboardCooldown, Is.EqualTo(1f));
    }

    [Test]
    public void Constructor_ClampsOverallStrengths()
    {
        GamePacingMultipliers values = new(
            1f, 1f, 1f, 1f, 1f, -1f, 3f);

        Assert.That(
            values.PlayerOverallStrength,
            Is.EqualTo(GamePacingMultipliers.MinimumMultiplier));
        Assert.That(
            values.EnemyOverallStrength,
            Is.EqualTo(GamePacingMultipliers.MaximumMultiplier));
    }

    [Test]
    public void Constructor_ClampsWhiteboardCooldown()
    {
        GamePacingMultipliers values = new(
            1f, 1f, 1f, 1f, 1f, 1f, 1f, 3f);

        Assert.That(
            values.WhiteboardCooldown,
            Is.EqualTo(GamePacingMultipliers.MaximumMultiplier));
    }

    [Test]
    public void Settings_RoundTripsOverallStrengths()
    {
        GamePacingDebugSettings settings =
            ScriptableObject.CreateInstance<GamePacingDebugSettings>();
        GamePacingMultipliers expected = new(
            0.5f, 1.5f, 2f, 0.75f, 1.25f, 1.5f, 0.5f, 1.25f);

        settings.SetValues(expected);
        GamePacingMultipliers actual = settings.GetValues();

        Assert.That(
            actual.PlayerOverallStrength,
            Is.EqualTo(expected.PlayerOverallStrength));
        Assert.That(
            actual.EnemyOverallStrength,
            Is.EqualTo(expected.EnemyOverallStrength));
        Assert.That(
            actual.WhiteboardCooldown,
            Is.EqualTo(expected.WhiteboardCooldown));

        Object.DestroyImmediate(settings);
    }

    [Test]
    public void Settings_DamageFloatingTextMagicNumberDefaultsToSixteen()
    {
        GamePacingDebugSettings settings =
            ScriptableObject.CreateInstance<GamePacingDebugSettings>();
        try
        {
            Assert.That(
                settings.DamageFloatingTextMagicNumber,
                Is.EqualTo(16f));

            settings.SetDamageFloatingTextMagicNumber(24f);
            Assert.That(
                settings.DamageFloatingTextMagicNumber,
                Is.EqualTo(24f));
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    [Test]
    public void Runtime_CombinesGlobalAndFactionStrengths()
    {
        GamePacingDebugRuntime runtime =
            GamePacingDebugRuntime.Instance;
        if (runtime == null)
        {
            runtimeObject = new GameObject(
                "Game Pacing Runtime Test");
            runtime = runtimeObject.AddComponent<
                GamePacingDebugRuntime>();
            runtime.SendMessage("Awake");
        }

        GamePacingMultipliers original = runtime.Multipliers;
        try
        {
            runtime.SetMultipliers(new GamePacingMultipliers(
                0.5f, 1.5f, 2f, 0.75f, 1.25f, 2f, 0.5f));

            Assert.That(
                GamePacingDebugRuntime.GetAircraftSpeedMultiplier(
                    BattleFaction.Player),
                Is.EqualTo(1f));
            Assert.That(
                GamePacingDebugRuntime.GetProjectileDamageMultiplier(
                    BattleFaction.Enemy),
                Is.EqualTo(0.75f));
            Assert.That(
                GamePacingDebugRuntime.GetAircraftHealthMultiplier(
                    BattleFaction.Player),
                Is.EqualTo(4f));
            Assert.That(
                GamePacingDebugRuntime.GetBackpackHealthMultiplier(
                    BattleFaction.Player),
                Is.EqualTo(1.5f));
            Assert.That(
                GamePacingDebugRuntime.GetBackpackHealthMultiplier(
                    BattleFaction.Enemy),
                Is.EqualTo(0.625f));
        }
        finally
        {
            runtime.SetMultipliers(original);
        }
    }

    [Test]
    public void Runtime_DamageFloatingTextMagicNumberDoesNotChangeDamageMultipliers()
    {
        GamePacingDebugRuntime runtime =
            GamePacingDebugRuntime.Instance;
        if (runtime == null)
        {
            runtimeObject = new GameObject(
                "Game Pacing Display Runtime Test");
            runtime = runtimeObject.AddComponent<
                GamePacingDebugRuntime>();
            runtime.SendMessage("Awake");
        }

        float originalMagicNumber =
            runtime.DamageFloatingTextMagicNumber;
        GamePacingMultipliers original = runtime.Multipliers;
        try
        {
            runtime.SetDamageFloatingTextMagicNumber(16f);

            Assert.That(
                GamePacingDebugRuntime
                    .GetDamageFloatingTextMagicNumber(),
                Is.EqualTo(16f));
            Assert.That(runtime.Multipliers,
                Is.EqualTo(original));
        }
        finally
        {
            runtime.SetDamageFloatingTextMagicNumber(
                originalMagicNumber);
        }
    }

    [Test]
    public void WhiteboardCooldown_AffectsOnlyNewEquipmentCooldowns()
    {
        GamePacingDebugRuntime runtime = GamePacingDebugRuntime.Instance;
        if (runtime == null)
        {
            runtimeObject = new GameObject(
                "Game Pacing Cooldown Runtime Test");
            runtime = runtimeObject.AddComponent<GamePacingDebugRuntime>();
            runtime.SendMessage("Awake");
        }

        ItemData data = ScriptableObject.CreateInstance<ItemData>();
        data.InitializeForTests(
            "Cooldown Equipment", ItemType.Equipment, 2f, null);
        var item = new ItemInstance("cooldown-equipment", data, Vector2Int.zero);
        GamePacingMultipliers original = runtime.Multipliers;
        try
        {
            runtime.SetMultipliers(new GamePacingMultipliers(
                1f, 1f, 1f, 1f, 1f, 1f, 1f, 0.5f));
            item.BeginCooldown();
            Assert.That(item.RemainingCooldown, Is.EqualTo(1f));

            runtime.SetMultipliers(new GamePacingMultipliers(
                1f, 1f, 1f, 1f, 1f, 1f, 1f, 2f));
            item.TickCooldown(0.5f);
            Assert.That(item.RemainingCooldown, Is.EqualTo(0.5f));

            item.TickCooldown(0.5f);
            item.BeginCooldown();
            Assert.That(item.RemainingCooldown, Is.EqualTo(4f));
        }
        finally
        {
            runtime.SetMultipliers(original);
            Object.DestroyImmediate(data);
        }
    }
}
