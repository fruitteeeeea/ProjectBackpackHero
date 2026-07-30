using System.Collections.Generic;
using BackpackHero.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BattleRandomProjectilePoolTests
{
    private GameObject poolObject;

    [TearDown]
    public void TearDown()
    {
        if (poolObject != null)
        {
            Object.DestroyImmediate(poolObject);
        }
    }

    [Test]
    public void ProjectileDirectory_ContainsFiveAttackPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { BattleRandomProjectilePool.PrefabDirectory });

        List<BattleAttack2D> attacks = new();

        foreach (string guid in guids)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                AssetDatabase.GUIDToAssetPath(guid));
            BattleAttack2D attack =
                prefab != null
                    ? prefab.GetComponent<BattleAttack2D>()
                    : null;

            if (attack != null)
            {
                attacks.Add(attack);
            }
        }

        Assert.That(attacks, Has.Count.EqualTo(5));
    }

    [Test]
    public void DisabledOrEmptyPool_ReturnsNullForDefaultFallback()
    {
        BattleRandomProjectilePool pool = CreatePool();
        pool.SetProjectilePrefabs(
            System.Array.Empty<BattleAttack2D>());

        Assert.That(pool.SelectProjectilePrefab(), Is.Null);

        pool.SetRandomProjectilesEnabled(true);

        Assert.That(pool.SelectProjectilePrefab(), Is.Null);
    }

    [Test]
    public void EnabledPool_SelectsOnlyConfiguredAttackPrefabs()
    {
        BattleRandomProjectilePool pool = CreatePool();
        BattleAttack2D straight = LoadAttack("Projectile.prefab");
        BattleAttack2D sine = LoadAttack("Projectile_Sine.prefab");

        pool.SetProjectilePrefabs(new[] { straight, sine });
        pool.SetRandomProjectilesEnabled(true);

        for (int index = 0; index < 20; index++)
        {
            BattleAttack2D selected = pool.SelectProjectilePrefab();

            Assert.That(
                selected == straight || selected == sine,
                Is.True);
        }
    }

    private BattleRandomProjectilePool CreatePool()
    {
        poolObject = new GameObject("Random Projectile Pool");
        return poolObject.AddComponent<BattleRandomProjectilePool>();
    }

    private static BattleAttack2D LoadAttack(string fileName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            BattleRandomProjectilePool.PrefabDirectory + "/" + fileName);
        return prefab.GetComponent<BattleAttack2D>();
    }
}
