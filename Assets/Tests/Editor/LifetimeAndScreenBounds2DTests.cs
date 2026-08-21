using System.Reflection;
using BackpackHero.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class LifetimeAndScreenBounds2DTests
{
    private static readonly MethodInfo UpdateLifetimeMethod =
        typeof(LifetimeAndScreenBounds2D).GetMethod(
            "UpdateLifetime",
            BindingFlags.Instance | BindingFlags.NonPublic);

    [Test]
    public void ExpiredLifetime_WithAutoDestroyDisabled_StaysExpiredAndNotifiesOnce()
    {
        GameObject owner = new("Lifetime Test");

        try
        {
            LifetimeAndScreenBounds2D lifetime =
                owner.AddComponent<LifetimeAndScreenBounds2D>();
            int eventCount = 0;
            lifetime.LifetimeExpired += () => eventCount++;
            lifetime.SetLifetime(1f);
            lifetime.SetDestroyWhenLifetimeExpires(false);

            Advance(lifetime, 1f);
            Advance(lifetime, 1f);

            Assert.That(lifetime.HasLifetimeExpired, Is.True);
            Assert.That(lifetime.RemainingLifetime, Is.Zero);
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(owner, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void FighterPrefab_AssignsBezierProjectileForOvertimePenalty()
    {
        GameObject fighterPrefab =
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Battle/Fighter.prefab");
        GameObject projectilePrefab =
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Battle/Projectiles/Projectile_Bezier.prefab");

        Assert.That(fighterPrefab, Is.Not.Null);
        Assert.That(projectilePrefab, Is.Not.Null);

        SerializedObject serializedFighter = new(
            fighterPrefab.GetComponent<Fighter2D>());
        SerializedProperty penaltyPrefab =
            serializedFighter.FindProperty(
                "overtimePenaltyProjectilePrefab");

        Assert.That(penaltyPrefab.objectReferenceValue, Is.SameAs(
            projectilePrefab.GetComponent<BattleAttack2D>()));
    }

    private static void Advance(
        LifetimeAndScreenBounds2D lifetime,
        float deltaTime)
    {
        Assert.That(UpdateLifetimeMethod, Is.Not.Null);
        UpdateLifetimeMethod.Invoke(lifetime, new object[] { deltaTime });
    }
}
