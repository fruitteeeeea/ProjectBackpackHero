using System.Collections.Generic;
using BackpackHero.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ProjectileImpactEffect2DTests
{
    private readonly List<GameObject> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void CircleArea_CollectsOnlyCollidersInsideRadius()
    {
        CircleDamageArea2D area =
            CreateObject("Circle Area")
                .AddComponent<CircleDamageArea2D>();

        area.Configure(Vector2.zero, 1.5f);

        Health inside = CreateTarget(
            "Inside", BattleFaction.Enemy, Vector2.right, 1);
        Health outside = CreateTarget(
            "Outside", BattleFaction.Enemy, Vector2.right * 3f, 1);

        Physics2D.SyncTransforms();

        List<Collider2D> results = new();
        area.CollectOverlaps(
            1 << BattlePhysicsLayers.GetHurtBoxLayer(
                BattleFaction.Enemy),
            results);

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(
            results[0].GetComponent<HurtBox2D>().TargetHealth,
            Is.SameAs(inside));
        Assert.That(outside.CurrentHealth, Is.EqualTo(10f));
    }

    [Test]
    public void Explosion_DamagesEveryEnemyOnceIncludingInitialTarget()
    {
        ExplosiveProjectileImpact2D effect =
            CreateImpactObject<ExplosiveProjectileImpact2D>(
                "Explosion");

        HurtBox2D initialTarget = CreateTarget(
            "Initial", BattleFaction.Enemy, Vector2.zero, 2)
            .GetComponentInChildren<HurtBox2D>();
        Health nearby = CreateTarget(
            "Nearby", BattleFaction.Enemy, Vector2.right, 1);
        Health friendly = CreateTarget(
            "Friendly", BattleFaction.Player, Vector2.up, 1);
        Health outside = CreateTarget(
            "Outside", BattleFaction.Enemy, Vector2.right * 3f, 1);

        Physics2D.SyncTransforms();

        Assert.That(effect.ResolveImpact(
            initialTarget,
            Vector2.zero,
            BattleFaction.Player,
            2f), Is.True);

        Assert.That(initialTarget.TargetHealth.CurrentHealth, Is.EqualTo(8f));
        Assert.That(nearby.CurrentHealth, Is.EqualTo(8f));
        Assert.That(friendly.CurrentHealth, Is.EqualTo(10f));
        Assert.That(outside.CurrentHealth, Is.EqualTo(10f));
    }

    [Test]
    public void ChainLightning_JumpsToNearestUnhitEnemies()
    {
        ChainLightningProjectileImpact2D effect =
            CreateImpactObject<ChainLightningProjectileImpact2D>(
                "Lightning");

        HurtBox2D initialTarget = CreateTarget(
            "Initial", BattleFaction.Enemy, Vector2.zero, 1)
            .GetComponentInChildren<HurtBox2D>();
        Health nearest = CreateTarget(
            "Nearest", BattleFaction.Enemy, new Vector2(1f, 0f), 1);
        Health next = CreateTarget(
            "Next", BattleFaction.Enemy, new Vector2(3f, 0f), 1);
        Health final = CreateTarget(
            "Final", BattleFaction.Enemy, new Vector2(5f, 0f), 1);
        Health friendly = CreateTarget(
            "Friendly", BattleFaction.Player, new Vector2(0.5f, 0f), 1);
        Health beyond = CreateTarget(
            "Beyond", BattleFaction.Enemy, new Vector2(8f, 0f), 1);

        Physics2D.SyncTransforms();

        Assert.That(effect.ResolveImpact(
            initialTarget,
            Vector2.zero,
            BattleFaction.Player,
            2f), Is.True);

        Assert.That(initialTarget.TargetHealth.CurrentHealth, Is.EqualTo(8f));
        Assert.That(nearest.CurrentHealth, Is.EqualTo(8f));
        Assert.That(next.CurrentHealth, Is.EqualTo(8f));
        Assert.That(final.CurrentHealth, Is.EqualTo(8f));
        Assert.That(friendly.CurrentHealth, Is.EqualTo(10f));
        Assert.That(beyond.CurrentHealth, Is.EqualTo(10f));
    }

    [Test]
    public void SpecialProjectilePrefabs_HaveRequiredImpactConfiguration()
    {
        GameObject explosive = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Battle/Projectile_Explosive.prefab");
        GameObject lightning = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Battle/Projectile_Lightning.prefab");

        Assert.That(explosive.GetComponent<Projectile2D>(), Is.Not.Null);
        Assert.That(explosive.GetComponent<HitBox2D>(), Is.Not.Null);
        Assert.That(explosive.GetComponent<ExplosiveProjectileImpact2D>(), Is.Not.Null);
        Assert.That(explosive.GetComponent<CircleDamageArea2D>().Radius,
            Is.EqualTo(1.5f));

        Assert.That(lightning.GetComponent<Projectile2D>(), Is.Not.Null);
        Assert.That(lightning.GetComponent<HitBox2D>(), Is.Not.Null);
        ChainLightningProjectileImpact2D lightningEffect =
            lightning.GetComponent<ChainLightningProjectileImpact2D>();
        Assert.That(lightningEffect, Is.Not.Null);
        Assert.That(lightningEffect.JumpRadius, Is.EqualTo(2.5f));
        Assert.That(lightningEffect.MaximumExtraTargets, Is.EqualTo(3));

        GameObject explosionVfx =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Battle/ExplosionImpactVfx.prefab");
        GameObject lightningVfx =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Battle/LightningLinkVfx.prefab");

        Assert.That(
            explosionVfx.GetComponent<ExplosionImpactVfx2D>(),
            Is.Not.Null);
        Assert.That(
            lightningVfx.GetComponent<LightningLinkVfx2D>(),
            Is.Not.Null);
        Assert.That(
            lightningVfx.GetComponent<LineRenderer>(),
            Is.Not.Null);
    }

    private T CreateImpactObject<T>(string name)
        where T : Component
    {
        GameObject target = CreateObject(name);
        target.AddComponent<AreaDamageResolver2D>();
        target.AddComponent<CircleDamageArea2D>();
        return target.AddComponent<T>();
    }

    private Health CreateTarget(
        string name,
        BattleFaction faction,
        Vector2 position,
        int hurtBoxCount)
    {
        GameObject owner = CreateObject(name);
        owner.SetActive(false);
        owner.transform.position = position;

        Health health = owner.AddComponent<Health>();
        health.Initialize(10f);

        FactionMember factionMember = owner.AddComponent<FactionMember>();
        factionMember.SetFaction(faction);

        for (int index = 0; index < hurtBoxCount; index++)
        {
            GameObject hurtBoxObject = new GameObject($"HurtBox {index}");
            createdObjects.Add(hurtBoxObject);
            hurtBoxObject.transform.SetParent(owner.transform, false);
            hurtBoxObject.AddComponent<BoxCollider2D>().isTrigger = true;
            HurtBox2D hurtBox = hurtBoxObject.AddComponent<HurtBox2D>();
            hurtBox.ConfigureLayer(faction);
        }

        owner.SetActive(true);
        return health;
    }

    private GameObject CreateObject(string name)
    {
        GameObject target = new GameObject(name);
        createdObjects.Add(target);
        return target;
    }
}
