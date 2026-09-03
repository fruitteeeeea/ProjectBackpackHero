using BackpackHero.Battle;
using NUnit.Framework;
using UnityEngine;

public sealed class ProjectileTargetDeathTests
{
    [Test]
    public void TargetDeath_BeginsProjectileFadeOut()
    {
        GameObject target = new("Tracked Target");
        GameObject projectileObject = new("Tracked Projectile");

        try
        {
            Health targetHealth = target.AddComponent<Health>();
            projectileObject.AddComponent<BoxCollider2D>();
            projectileObject.AddComponent<FactionMember>();
            projectileObject.AddComponent<HitBox2D>();
            projectileObject.AddComponent<ProjectileTrajectoryController2D>();
            projectileObject.AddComponent<BattleFadeOut2D>();
            Projectile2D projectile =
                projectileObject.AddComponent<Projectile2D>();

            projectile.Initialize(
                BattleAttackLaunchContext.WithAimPoint(
                    BattleFaction.Player,
                    1f,
                    5f,
                    1f,
                    Vector2.zero,
                    Vector2.up,
                    Vector2.zero,
                    Vector2.up,
                    Vector2.up,
                    targetHealth: targetHealth));

            targetHealth.SetHealth(0f);

            Assert.That(
                projectileObject.GetComponent<BattleFadeOut2D>()
                    .IsFading,
                Is.True);
        }
        finally
        {
            Object.DestroyImmediate(projectileObject);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void TargetDeath_DoesNotFadeProjectile_WhenGlobalRuleIsDisabled()
    {
        BattleFlowController controller =
            BattleFlowController.EnsureInstance();
        bool wasEnabled =
            BattleFlowController.IsProjectileTargetDeathFadeEnabled;
        GameObject target = new("Tracked Target");
        GameObject projectileObject = new("Tracked Projectile");

        try
        {
            controller.SetProjectileTargetDeathFadeEnabled(false);

            Health targetHealth = target.AddComponent<Health>();
            projectileObject.AddComponent<BoxCollider2D>();
            projectileObject.AddComponent<FactionMember>();
            projectileObject.AddComponent<HitBox2D>();
            projectileObject.AddComponent<ProjectileTrajectoryController2D>();
            projectileObject.AddComponent<BattleFadeOut2D>();
            Projectile2D projectile =
                projectileObject.AddComponent<Projectile2D>();

            projectile.Initialize(
                BattleAttackLaunchContext.WithAimPoint(
                    BattleFaction.Player,
                    1f,
                    5f,
                    1f,
                    Vector2.zero,
                    Vector2.up,
                    Vector2.zero,
                    Vector2.up,
                    Vector2.up,
                    targetHealth: targetHealth));

            targetHealth.SetHealth(0f);

            Assert.That(
                projectileObject.GetComponent<BattleFadeOut2D>()
                    .IsFading,
                Is.False);
        }
        finally
        {
            controller.SetProjectileTargetDeathFadeEnabled(wasEnabled);
            Object.DestroyImmediate(projectileObject);
            Object.DestroyImmediate(target);
        }
    }
}
