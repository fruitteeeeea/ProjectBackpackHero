using System.Text.RegularExpressions;
using BackpackHero.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ProjectileTrajectory2DTests
{
    private GameObject trajectoryObject;
    private ProjectileTrajectoryProfile createdProfile;

    [TearDown]
    public void TearDown()
    {
        if (trajectoryObject != null)
        {
            Object.DestroyImmediate(trajectoryObject);
        }

        if (createdProfile != null)
        {
            Object.DestroyImmediate(createdProfile);
        }
    }

    [Test]
    public void StraightTrajectory_MatchesCurrentLinearMovement()
    {
        StraightProjectileTrajectoryProfile profile =
            CreateProfile<
                StraightProjectileTrajectoryProfile>();

        ProjectileTrajectoryController2D controller =
            CreateController(
                profile,
                ProjectileTrajectoryLaunchContext
                    .WithoutTarget(
                        Vector2.zero,
                        Vector2.up,
                        Vector2.zero,
                        Vector2.up),
                8f);

        controller.Advance(0.5f);

        Assert.That(
            Vector2.Distance(
                controller.transform.position,
                Vector2.up * 4f),
            Is.LessThan(0.0001f));

        Assert.That(
            Vector2.Distance(
                controller.transform.up,
                Vector2.up),
            Is.LessThan(0.0001f));
    }

    [Test]
    public void SineTrajectory_AlternatesSidesAndKeepsPathSpeed()
    {
        SineProjectileTrajectoryProfile profile =
            CreateProfile<
                SineProjectileTrajectoryProfile>();

        ProjectileTrajectoryController2D controller =
            CreateController(
                profile,
                ProjectileTrajectoryLaunchContext
                    .WithoutTarget(
                        Vector2.zero,
                        Vector2.up,
                        Vector2.zero,
                        Vector2.up),
                1f);

        Vector2 previousPosition =
            controller.transform.position;

        float measuredDistance = 0f;
        float maximumRight = 0f;
        float maximumLeft = 0f;

        for (int index = 0; index < 160; index++)
        {
            controller.Advance(0.01f);

            Vector2 position =
                controller.transform.position;

            measuredDistance +=
                Vector2.Distance(
                    previousPosition,
                    position);

            maximumRight =
                Mathf.Max(
                    maximumRight,
                    position.x);

            maximumLeft =
                Mathf.Min(
                    maximumLeft,
                    position.x);

            previousPosition = position;
        }

        Assert.That(maximumRight, Is.GreaterThan(0.2f));
        Assert.That(maximumLeft, Is.LessThan(-0.2f));
        Assert.That(measuredDistance, Is.EqualTo(1.6f).Within(0.03f));
    }

    [TestCase(BezierProjectileSide.Left, -1)]
    [TestCase(BezierProjectileSide.Right, 1)]
    public void BezierTrajectory_UsesConfiguredShooterSide(
        BezierProjectileSide side,
        int expectedSign)
    {
        BezierProjectileTrajectoryProfile profile =
            CreateBezierProfile(side);

        ProjectileTrajectoryController2D controller =
            CreateController(
                profile,
                CreateBezierContext(),
                1f);

        controller.Advance(0.2f);

        Assert.That(
            Mathf.Sign(
                controller.transform.position.x),
            Is.EqualTo(expectedSign));
    }

    [Test]
    public void BezierTrajectory_ContinuesAlongFinalTangent()
    {
        BezierProjectileTrajectoryProfile profile =
            CreateBezierProfile(
                BezierProjectileSide.Right);

        ProjectileTrajectoryController2D controller =
            CreateController(
                profile,
                CreateBezierContext(),
                1f);

        controller.Advance(10f);

        Vector2 target = new(0f, 2f);
        Vector2 continuation =
            (Vector2)controller.transform.position -
            target;

        Assert.That(
            continuation.magnitude,
            Is.GreaterThan(1f));

        Assert.That(
            Vector2.Dot(
                continuation.normalized,
                controller.transform.up),
            Is.GreaterThan(0.999f));
    }

    [Test]
    public void BezierWithoutTarget_FallsBackToStraight()
    {
        BezierProjectileTrajectoryProfile profile =
            CreateBezierProfile(
                BezierProjectileSide.Random);

        ProjectileTrajectoryController2D controller =
            CreateEmptyController(profile);

        LogAssert.Expect(
            LogType.Warning,
            new Regex("回退为直线移动"));

        controller.Initialize(
            2f,
            ProjectileTrajectoryLaunchContext
                .WithoutTarget(
                    Vector2.zero,
                    Vector2.right,
                    Vector2.zero,
                    Vector2.up));

        controller.Advance(0.5f);

        Assert.That(
            Vector2.Distance(
                controller.transform.position,
                Vector2.right),
            Is.LessThan(0.0001f));
    }

    [Test]
    public void TrajectoryAssetsAndPrefabs_HaveExpectedDefaults()
    {
        StraightProjectileTrajectoryProfile straight =
            AssetDatabase.LoadAssetAtPath<
                StraightProjectileTrajectoryProfile>(
                "Assets/Settings/Battle/" +
                "ProjectileTrajectories/Straight.asset");

        SineProjectileTrajectoryProfile sine =
            AssetDatabase.LoadAssetAtPath<
                SineProjectileTrajectoryProfile>(
                "Assets/Settings/Battle/" +
                "ProjectileTrajectories/Sine.asset");

        BezierProjectileTrajectoryProfile bezier =
            AssetDatabase.LoadAssetAtPath<
                BezierProjectileTrajectoryProfile>(
                "Assets/Settings/Battle/" +
                "ProjectileTrajectories/" +
                "BezierRandom.asset");

        Assert.That(straight, Is.Not.Null);
        Assert.That(sine, Is.Not.Null);
        Assert.That(sine.Amplitude, Is.EqualTo(0.25f));
        Assert.That(sine.Wavelength, Is.EqualTo(1.5f));
        Assert.That(sine.SamplesPerCycle, Is.EqualTo(64));
        Assert.That(bezier, Is.Not.Null);
        Assert.That(bezier.BackwardDistance, Is.EqualTo(0.5f));
        Assert.That(bezier.LateralDistance, Is.EqualTo(0.75f));
        Assert.That(
            bezier.Side,
            Is.EqualTo(BezierProjectileSide.Random));
        Assert.That(bezier.SampleCount, Is.EqualTo(32));

        AssertPrefabProfile<
            StraightProjectileTrajectoryProfile>(
                "Assets/Prefabs/Battle/Projectile.prefab");

        AssertPrefabProfile<
            SineProjectileTrajectoryProfile>(
                "Assets/Prefabs/Battle/" +
                "Projectile_Sine.prefab");

        AssertPrefabProfile<
            BezierProjectileTrajectoryProfile>(
                "Assets/Prefabs/Battle/" +
                "Projectile_Bezier.prefab");
    }

    private T CreateProfile<T>()
        where T : ProjectileTrajectoryProfile
    {
        T profile =
            ScriptableObject.CreateInstance<T>();

        createdProfile = profile;
        return profile;
    }

    private BezierProjectileTrajectoryProfile
        CreateBezierProfile(
            BezierProjectileSide side)
    {
        BezierProjectileTrajectoryProfile profile =
            CreateProfile<
                BezierProjectileTrajectoryProfile>();

        SerializedObject serializedProfile =
            new(profile);

        serializedProfile
            .FindProperty("side")
            .enumValueIndex = (int)side;

        serializedProfile.ApplyModifiedPropertiesWithoutUndo();
        return profile;
    }

    private ProjectileTrajectoryController2D
        CreateController(
            ProjectileTrajectoryProfile profile,
            ProjectileTrajectoryLaunchContext context,
            float speed)
    {
        ProjectileTrajectoryController2D controller =
            CreateEmptyController(profile);

        controller.Initialize(speed, context);
        return controller;
    }

    private ProjectileTrajectoryController2D
        CreateEmptyController(
            ProjectileTrajectoryProfile profile)
    {
        trajectoryObject =
            new GameObject("Trajectory Test");

        ProjectileTrajectoryController2D controller =
            trajectoryObject.AddComponent<
                ProjectileTrajectoryController2D>();

        controller.SetTrajectoryProfile(profile);
        return controller;
    }

    private static ProjectileTrajectoryLaunchContext
        CreateBezierContext()
    {
        return ProjectileTrajectoryLaunchContext
            .WithTarget(
                Vector2.zero,
                Vector2.up,
                Vector2.zero,
                Vector2.up,
                new Vector2(0f, 2f));
    }

    private static void AssertPrefabProfile<T>(
        string path)
        where T : ProjectileTrajectoryProfile
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                path);

        Assert.That(prefab, Is.Not.Null, path);

        ProjectileTrajectoryController2D controller =
            prefab.GetComponent<
                ProjectileTrajectoryController2D>();

        Assert.That(controller, Is.Not.Null, path);
        Assert.That(
            controller.TrajectoryProfile,
            Is.TypeOf<T>(),
            path);
    }
}
