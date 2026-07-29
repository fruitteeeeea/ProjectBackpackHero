using System.Collections.Generic;
using BackpackHero.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LaserAttack2DTests
{
    private readonly List<GameObject>
        createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject createdObject
                 in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(
                    createdObject);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void SpreadRequests_KeepPrefabAndCreateThreeDirections()
    {
        GameObject controllerObject =
            CreateObject("Controller");

        ProjectileFireModeController2D controller =
            controllerObject.AddComponent<
                ProjectileFireModeController2D>();

        GameObject laserPrefabObject =
            AssetDatabase.LoadAssetAtPath<
                GameObject>(
                "Assets/Prefabs/Battle/" +
                "LaserBeamAttack.prefab");

        BattleAttack2D laserPrefab =
            laserPrefabObject.GetComponent<
                LaserBeamAttack2D>();

        SpreadProjectileFirePattern spread =
            AssetDatabase.LoadAssetAtPath<
                SpreadProjectileFirePattern>(
                "Assets/Settings/Battle/" +
                "ProjectileFirePatterns/" +
                "Spread30Three.asset");

        controller.AddMode(
            laserPrefab,
            spread,
            ProjectileFireModeController2D
                .ManualInterval);

        List<BattleShotRequest> requests = new();
        controller.ShotRequested += requests.Add;

        controller.TriggerAll(Vector2.up);

        Assert.That(requests.Count, Is.EqualTo(3));

        for (int index = 0;
             index < requests.Count;
             index++)
        {
            Assert.That(
                requests[index].AttackPrefab,
                Is.SameAs(laserPrefab));
            Assert.That(
                requests[index].ModeIndex,
                Is.Zero);
        }

        AssertAngle(requests[0].Direction, -15f);
        AssertAngle(requests[1].Direction, 0f);
        AssertAngle(requests[2].Direction, 15f);
    }

    [Test]
    public void LaserPrefab_HasExpectedVisualAndTimingDefaults()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Battle/" +
                "LaserBeamAttack.prefab");

        Assert.That(prefab, Is.Not.Null);
        Assert.That(
            prefab.GetComponent<
                LaserBeamAttack2D>(),
            Is.Not.Null);
        Assert.That(
            prefab.GetComponent<
                AreaDamageResolver2D>(),
            Is.Not.Null);
        Assert.That(
            prefab.GetComponent<
                SegmentBoxDamageArea2D>(),
            Is.Not.Null);

        SpriteRenderer renderer =
            prefab.GetComponentInChildren<
                SpriteRenderer>(true);

        Assert.That(renderer, Is.Not.Null);
        Assert.That(renderer.sprite, Is.Not.Null);
        Assert.That(
            renderer.sharedMaterial,
            Is.Not.Null);
        Assert.That(
            renderer.sharedMaterial.shader.name,
            Does.Contain("Sprite-Unlit"));

        SerializedObject serializedAttack =
            new SerializedObject(
                prefab.GetComponent<
                    LaserBeamAttack2D>());

        AssertSerializedFloat(
            serializedAttack,
            "fadeInDuration",
            0.1f);
        AssertSerializedFloat(
            serializedAttack,
            "holdDuration",
            0.2f);
        AssertSerializedFloat(
            serializedAttack,
            "fadeOutDuration",
            0.3f);
    }

    [Test]
    public void BattlePrototype_DefaultModeUsesLaserPrefab()
    {
        Scene scene =
            EditorSceneManager.OpenScene(
                "Assets/Scenes/" +
                "BattlePrototype.unity",
                OpenSceneMode.Additive);

        try
        {
            TestShooter2D shooter = null;

            foreach (GameObject root
                     in scene.GetRootGameObjects())
            {
                shooter =
                    root.GetComponentInChildren<
                        TestShooter2D>(true);

                if (shooter != null)
                {
                    break;
                }
            }

            Assert.That(shooter, Is.Not.Null);
            Assert.That(
                shooter.DefaultAttackPrefab,
                Is.TypeOf<LaserBeamAttack2D>());
            Assert.That(
                shooter.FireModeController
                    .FireModes[0]
                    .AttackPrefab,
                Is.TypeOf<LaserBeamAttack2D>());
        }
        finally
        {
            EditorSceneManager.CloseScene(
                scene,
                true);
        }
    }

    [Test]
    public void LaserEndpoints_UseAimDistanceAndPatternDirection()
    {
        LaserBeamAttack2D center =
            CreateLaserInstance("Center Laser");

        LaserBeamAttack2D side =
            CreateLaserInstance("Side Laser");

        Vector2 origin =
            new Vector2(1f, 2f);
        Vector2 aimPoint =
            new Vector2(1f, 7f);

        center.Initialize(
            CreateLaserContext(
                origin,
                Vector2.up,
                aimPoint));

        Vector2 sideDirection =
            Quaternion.Euler(
                0f,
                0f,
                15f) *
            Vector2.up;

        side.Initialize(
            CreateLaserContext(
                origin,
                sideDirection,
                aimPoint));

        Assert.That(
            Vector2.Distance(
                center.BeamEnd,
                aimPoint),
            Is.LessThan(0.0001f));

        Assert.That(
            center.BeamLength,
            Is.EqualTo(side.BeamLength)
                .Within(0.0001f));

        Assert.That(
            Vector2.Distance(
                center.BeamEnd,
                side.BeamEnd),
            Is.GreaterThan(0.1f));
    }

    [Test]
    public void SegmentArea_UsesPaddingWidthAndAngle()
    {
        GameObject areaObject =
            CreateObject("Segment Area");

        SegmentBoxDamageArea2D area =
            areaObject.AddComponent<
                SegmentBoxDamageArea2D>();

        area.Configure(
            Vector2.zero,
            Vector2.right * 4f,
            0.5f,
            0.25f,
            0.75f);

        Assert.That(
            area.WorldCenter.x,
            Is.EqualTo(2.25f)
                .Within(0.0001f));
        Assert.That(
            area.WorldSize.x,
            Is.EqualTo(5f)
                .Within(0.0001f));
        Assert.That(
            area.WorldSize.y,
            Is.EqualTo(0.5f)
                .Within(0.0001f));
        Assert.That(
            area.WorldAngle,
            Is.Zero.Within(0.0001f));
    }

    [Test]
    public void AreaDamage_HitsEveryEnemyOnceAndSkipsFriend()
    {
        AreaDamageResolver2D resolver =
            CreateObject("Resolver")
                .AddComponent<
                    AreaDamageResolver2D>();

        SegmentBoxDamageArea2D area =
            CreateObject("Area")
                .AddComponent<
                    SegmentBoxDamageArea2D>();

        area.Configure(
            Vector2.zero,
            Vector2.right * 6f,
            0.5f,
            0f,
            0f);

        Health firstEnemy =
            CreateTarget(
                "Enemy A",
                BattleFaction.Enemy,
                new Vector2(2f, 0f),
                2);

        Health secondEnemy =
            CreateTarget(
                "Enemy B",
                BattleFaction.Enemy,
                new Vector2(4f, 0f),
                1);

        Health outsideEnemy =
            CreateTarget(
                "Outside Enemy",
                BattleFaction.Enemy,
                new Vector2(3f, 2f),
                1);

        Health friendly =
            CreateTarget(
                "Friendly",
                BattleFaction.Player,
                new Vector2(3f, 0f),
                1);

        Physics2D.SyncTransforms();

        int damagedCount =
            resolver.Resolve(
                area,
                BattleFaction.Player,
                3f);

        Assert.That(damagedCount, Is.EqualTo(2));
        Assert.That(
            firstEnemy.CurrentHealth,
            Is.EqualTo(7f));
        Assert.That(
            secondEnemy.CurrentHealth,
            Is.EqualTo(7f));
        Assert.That(
            outsideEnemy.CurrentHealth,
            Is.EqualTo(10f));
        Assert.That(
            friendly.CurrentHealth,
            Is.EqualTo(10f));
    }

    [Test]
    public void HurtBox_RefreshesLayerFromSceneFaction()
    {
        GameObject owner =
            CreateObject("Scene Enemy");

        owner.AddComponent<Health>()
            .Initialize(10f);

        FactionMember factionMember =
            owner.AddComponent<FactionMember>();

        factionMember.SetFaction(
            BattleFaction.Enemy);

        GameObject hurtBoxObject =
            new GameObject("HurtBox");

        hurtBoxObject.transform.SetParent(
            owner.transform,
            false);

        createdObjects.Add(hurtBoxObject);

        hurtBoxObject.AddComponent<
            BoxCollider2D>();

        HurtBox2D hurtBox =
            hurtBoxObject.AddComponent<
                HurtBox2D>();

        hurtBox.RefreshOwnerConfiguration();

        Assert.That(
            hurtBoxObject.layer,
            Is.EqualTo(
                BattlePhysicsLayers
                    .GetHurtBoxLayer(
                        BattleFaction.Enemy)));
    }

    [Test]
    public void SeparateLaserAreas_CanDamageSameUnitTwice()
    {
        AreaDamageResolver2D resolver =
            CreateObject("Resolver")
                .AddComponent<
                    AreaDamageResolver2D>();

        SegmentBoxDamageArea2D firstArea =
            CreateObject("First Area")
                .AddComponent<
                    SegmentBoxDamageArea2D>();

        SegmentBoxDamageArea2D secondArea =
            CreateObject("Second Area")
                .AddComponent<
                    SegmentBoxDamageArea2D>();

        firstArea.Configure(
            Vector2.zero,
            Vector2.right * 4f,
            1f,
            0f,
            0f);

        secondArea.Configure(
            Vector2.zero,
            Vector2.right * 4f,
            1f,
            0f,
            0f);

        Health enemy =
            CreateTarget(
                "Enemy",
                BattleFaction.Enemy,
                new Vector2(2f, 0f),
                1);

        Physics2D.SyncTransforms();

        resolver.Resolve(
            firstArea,
            BattleFaction.Player,
            2f);

        resolver.Resolve(
            secondArea,
            BattleFaction.Player,
            2f);

        Assert.That(
            enemy.CurrentHealth,
            Is.EqualTo(6f));
    }

    private LaserBeamAttack2D CreateLaserInstance(
        string objectName)
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Battle/" +
                "LaserBeamAttack.prefab");

        Assert.That(prefab, Is.Not.Null);

        GameObject instance =
            Object.Instantiate(prefab);

        instance.name = objectName;
        createdObjects.Add(instance);

        return instance.GetComponent<
            LaserBeamAttack2D>();
    }

    private Health CreateTarget(
        string objectName,
        BattleFaction faction,
        Vector2 position,
        int hurtBoxCount)
    {
        GameObject owner =
            CreateObject(objectName);

        owner.SetActive(false);
        owner.transform.position = position;

        Health health =
            owner.AddComponent<Health>();
        health.Initialize(10f);

        FactionMember factionMember =
            owner.AddComponent<FactionMember>();
        factionMember.SetFaction(faction);

        for (int index = 0;
             index < hurtBoxCount;
             index++)
        {
            GameObject hurtBoxObject =
                new GameObject(
                    $"HurtBox {index + 1}");

            hurtBoxObject.transform.SetParent(
                owner.transform,
                false);

            BoxCollider2D collider =
                hurtBoxObject.AddComponent<
                    BoxCollider2D>();

            collider.isTrigger = true;

            HurtBox2D hurtBox =
                hurtBoxObject.AddComponent<
                    HurtBox2D>();

            hurtBox.ConfigureLayer(faction);
        }

        owner.SetActive(true);
        return health;
    }

    private GameObject CreateObject(
        string objectName)
    {
        GameObject createdObject =
            new GameObject(objectName);

        createdObjects.Add(createdObject);
        return createdObject;
    }

    private static BattleAttackLaunchContext
        CreateLaserContext(
            Vector2 origin,
            Vector2 direction,
            Vector2 aimPoint)
    {
        return BattleAttackLaunchContext
            .WithAimPoint(
                BattleFaction.Player,
                1f,
                0f,
                0f,
                origin,
                direction,
                origin,
                direction,
                aimPoint);
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

    private static void AssertSerializedFloat(
        SerializedObject serializedObject,
        string propertyName,
        float expected)
    {
        SerializedProperty property =
            serializedObject.FindProperty(
                propertyName);

        Assert.That(property, Is.Not.Null);
        Assert.That(
            property.floatValue,
            Is.EqualTo(expected)
                .Within(0.0001f));
    }

}
