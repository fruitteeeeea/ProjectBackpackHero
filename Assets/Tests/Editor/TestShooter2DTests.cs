using System.Text.RegularExpressions;
using System.Collections.Generic;
using BackpackHero.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class TestShooter2DTests
{
    private GameObject firstObject;
    private GameObject secondObject;
    private readonly List<ScriptableObject>
        createdAssets = new();

    [TearDown]
    public void TearDown()
    {
        if (firstObject != null)
        {
            TestShooter2D shooter =
                firstObject.GetComponent<TestShooter2D>();

            TestShooterDebugRuntimeBridge.Unregister(
                shooter);

            Object.DestroyImmediate(firstObject);
        }

        if (secondObject != null)
        {
            TestShooter2D shooter =
                secondObject.GetComponent<TestShooter2D>();

            TestShooterDebugRuntimeBridge.Unregister(
                shooter);

            Object.DestroyImmediate(secondObject);
        }

        foreach (ScriptableObject asset in createdAssets)
        {
            if (asset != null)
            {
                Object.DestroyImmediate(asset);
            }
        }

        createdAssets.Clear();
    }

    [Test]
    public void Setters_ClampProjectileValues()
    {
        TestShooter2D shooter = CreateShooter(
            "Clamp Shooter",
            ref firstObject);

        shooter.SetProjectileDamage(-1f);
        shooter.SetProjectileSpeed(-2f);
        shooter.SetProjectileLifetime(-4f);
        shooter.SetManualTriggerInterval(-5f);

        Assert.That(shooter.ProjectileDamage, Is.Zero);
        Assert.That(shooter.ProjectileSpeed, Is.Zero);
        Assert.That(shooter.ProjectileLifetime, Is.Zero);
        Assert.That(
            shooter.ManualTriggerInterval,
            Is.EqualTo(
                TestShooter2D
                    .MinimumManualTriggerInterval));
    }

    [Test]
    public void AimAtWorldPosition_ZeroDirectionKeepsPreviousAim()
    {
        TestShooter2D shooter = CreateShooter(
            "Aim Shooter",
            ref firstObject);

        shooter.transform.up = Vector2.right;

        bool changed =
            shooter.AimAtWorldPosition(
                shooter.transform.position);

        Assert.That(changed, Is.False);
        Assert.That(
            Vector2.Distance(
                shooter.AimDirection,
                Vector2.right),
            Is.LessThan(0.0001f));

        changed =
            shooter.AimAtWorldPosition(Vector2.up * 2f);

        Assert.That(changed, Is.True);
        Assert.That(
            Vector2.Distance(
                shooter.AimDirection,
                Vector2.up),
            Is.LessThan(0.0001f));
    }

    [Test]
    public void SpreadPattern_CreatesThreeEvenDirections()
    {
        SpreadProjectileFirePattern pattern =
            CreateAsset<SpreadProjectileFirePattern>();

        List<Vector2> directions = new();
        pattern.AppendDirections(
            Vector2.up,
            directions);

        Assert.That(directions.Count, Is.EqualTo(3));
        AssertAngle(directions[0], -15f);
        AssertAngle(directions[1], 0f);
        AssertAngle(directions[2], 15f);
    }

    [Test]
    public void FireModes_UseIndependentAutomaticCooldowns()
    {
        firstObject =
            new GameObject("Fire Mode Controller");

        ProjectileFireModeController2D controller =
            firstObject.AddComponent<
                ProjectileFireModeController2D>();

        ForwardProjectileFirePattern pattern =
            CreateAsset<ForwardProjectileFirePattern>();

        controller.AddMode(pattern, 0.1f);
        controller.AddMode(pattern, 0.2f);

        int shotCount = 0;
        controller.ShotRequested +=
            _ => shotCount++;

        controller.Tick(0f, Vector2.up);
        Assert.That(shotCount, Is.EqualTo(2));

        controller.Tick(0.1f, Vector2.up);
        Assert.That(shotCount, Is.EqualTo(3));

        controller.Tick(0.1f, Vector2.up);
        Assert.That(shotCount, Is.EqualTo(5));
    }

    [Test]
    public void ManualFireMode_OnlyFiresWhenActivelyTriggered()
    {
        firstObject =
            new GameObject("Manual Fire Mode");

        ProjectileFireModeController2D controller =
            firstObject.AddComponent<
                ProjectileFireModeController2D>();

        ForwardProjectileFirePattern pattern =
            CreateAsset<ForwardProjectileFirePattern>();

        controller.AddMode(
            pattern,
            ProjectileFireModeController2D
                .ManualInterval);

        int shotCount = 0;
        controller.ShotRequested +=
            _ => shotCount++;

        controller.Tick(10f, Vector2.up);
        Assert.That(shotCount, Is.Zero);

        int triggered =
            controller.TriggerAll(Vector2.up);

        Assert.That(triggered, Is.EqualTo(1));
        Assert.That(shotCount, Is.EqualTo(1));
    }

    [Test]
    public void ManualTriggerTimer_RepeatsWhileHeld()
    {
        TestShooterManualTriggerTimer timer = new();

        Assert.That(
            timer.Tick(
                0.01f,
                true,
                true,
                0.2f),
            Is.True);

        Assert.That(
            timer.Tick(
                0.1f,
                true,
                false,
                0.2f),
            Is.False);

        Assert.That(
            timer.Tick(
                0.1001f,
                true,
                false,
                0.2f),
            Is.True);

        timer.Tick(
            0f,
            false,
            false,
            0.2f);

        Assert.That(
            timer.Tick(
                0f,
                true,
                true,
                0.2f),
            Is.True);
    }

    [Test]
    public void PatternAssets_HaveExpectedDefaultData()
    {
        ForwardProjectileFirePattern forward =
            AssetDatabase.LoadAssetAtPath<
                ForwardProjectileFirePattern>(
                "Assets/Settings/Battle/" +
                "ProjectileFirePatterns/" +
                "ForwardSingle.asset");

        SpreadProjectileFirePattern spread =
            AssetDatabase.LoadAssetAtPath<
                SpreadProjectileFirePattern>(
                "Assets/Settings/Battle/" +
                "ProjectileFirePatterns/" +
                "Spread30Three.asset");

        Assert.That(forward, Is.Not.Null);
        Assert.That(forward.ProjectileCount, Is.EqualTo(1));
        Assert.That(spread, Is.Not.Null);
        Assert.That(spread.ProjectileCount, Is.EqualTo(3));
        Assert.That(spread.SpreadAngle, Is.EqualTo(30f));
    }

    [Test]
    public void FighterFireModes_UseDefinitionIntervalAndCanGrow()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Battle/Fighter.prefab");

        FighterDefinition definition =
            AssetDatabase.LoadAssetAtPath<
                FighterDefinition>(
                "Assets/Settings/Battle/Fighters/" +
                "Fighter_02_Charge.asset");

        SpreadProjectileFirePattern spread =
            AssetDatabase.LoadAssetAtPath<
                SpreadProjectileFirePattern>(
                "Assets/Settings/Battle/" +
                "ProjectileFirePatterns/" +
                "Spread30Three.asset");

        Assert.That(prefab, Is.Not.Null);
        Assert.That(definition, Is.Not.Null);

        firstObject = Object.Instantiate(prefab);

        FighterCombat2D combat =
            firstObject.GetComponent<FighterCombat2D>();

        ProjectileFireModeController2D controller =
            firstObject.GetComponent<
                ProjectileFireModeController2D>();

        combat.ConfigureDefaultFireMode(
            definition.AttackInterval);

        Assert.That(controller.ModeCount, Is.EqualTo(1));
        Assert.That(
            controller.FireModes[0].Pattern,
            Is.TypeOf<ForwardProjectileFirePattern>());
        Assert.That(
            controller.FireModes[0].Interval,
            Is.EqualTo(definition.AttackInterval));

        controller.AddMode(spread, 0.75f);

        Assert.That(controller.ModeCount, Is.EqualTo(2));
        Assert.That(
            controller.FireModes[1].Pattern,
            Is.SameAs(spread));
        Assert.That(
            controller.RemoveModeAt(1),
            Is.True);
        Assert.That(controller.ModeCount, Is.EqualTo(1));
    }

    [Test]
    public void RuntimeBridge_KeepsFirstTargetUntilItUnregisters()
    {
        TestShooter2D first = CreateShooter(
            "First Shooter",
            ref firstObject);

        TestShooter2D second = CreateShooter(
            "Second Shooter",
            ref secondObject);

        Assert.That(
            TestShooterDebugRuntimeBridge.Register(first),
            Is.True);

        LogAssert.Expect(
            LogType.Warning,
            new Regex(
                "场景中存在多个活动的TestShooter"));

        Assert.That(
            TestShooterDebugRuntimeBridge.Register(second),
            Is.False);

        Assert.That(
            TestShooterDebugRuntimeBridge.Current,
            Is.SameAs(first));

        TestShooterDebugRuntimeBridge.Unregister(second);

        Assert.That(
            TestShooterDebugRuntimeBridge.Current,
            Is.SameAs(first));

        TestShooterDebugRuntimeBridge.Unregister(first);

        Assert.That(
            TestShooterDebugRuntimeBridge.HasTarget,
            Is.False);
    }

    private static TestShooter2D CreateShooter(
        string objectName,
        ref GameObject owner)
    {
        owner = new GameObject(objectName);
        owner.SetActive(false);
        return owner.AddComponent<TestShooter2D>();
    }

    private T CreateAsset<T>()
        where T : ScriptableObject
    {
        T asset =
            ScriptableObject.CreateInstance<T>();

        createdAssets.Add(asset);
        return asset;
    }

    private static void AssertAngle(
        Vector2 direction,
        float expectedSignedAngle)
    {
        float angle =
            Vector2.SignedAngle(
                Vector2.up,
                direction);

        Assert.That(
            angle,
            Is.EqualTo(expectedSignedAngle)
                .Within(0.001f));
    }
}
