using BackpackHero.Battle;
using BackpackHero.Debugging;
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
    public void Settings_RoundTripsOverallStrengths()
    {
        GamePacingDebugSettings settings =
            ScriptableObject.CreateInstance<GamePacingDebugSettings>();
        GamePacingMultipliers expected = new(
            0.5f, 1.5f, 2f, 0.75f, 1.25f, 1.5f, 0.5f);

        settings.SetValues(expected);
        GamePacingMultipliers actual = settings.GetValues();

        Assert.That(
            actual.PlayerOverallStrength,
            Is.EqualTo(expected.PlayerOverallStrength));
        Assert.That(
            actual.EnemyOverallStrength,
            Is.EqualTo(expected.EnemyOverallStrength));

        Object.DestroyImmediate(settings);
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
}
