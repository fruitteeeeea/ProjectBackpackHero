using UnityEngine;
using BackpackHero.Debugging;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 控制一颗战斗子弹的运行时初始化。
    /// 实际移动由ProjectileTrajectoryController2D负责，
    /// 实际伤害由HitBox2D负责。
    /// </summary>
    [RequireComponent(
        typeof(ProjectileTrajectoryController2D))]
    [RequireComponent(typeof(HitBox2D))]
    [RequireComponent(typeof(BattleFadeOut2D))]
    public sealed class Projectile2D : BattleAttack2D
    {
        [Header("References")]
        [SerializeField]
        private ProjectileTrajectoryController2D
            trajectoryController;

        [SerializeField]
        private HitBox2D hitBox;

        [SerializeField]
        private ProjectileVisualController2D visualController;

        private Health subscribedTargetHealth;
        private bool isFading;

        public BattleFaction Faction =>
            hitBox != null
                ? hitBox.Faction
                : BattleFaction.Player;

        private void Awake()
        {
            FindReferences();
        }

        private void OnDestroy()
        {
            UnsubscribeFromTargetDeath();
        }

        public override void Initialize(
            BattleAttackLaunchContext context)
        {
            ProjectileTrajectoryLaunchContext
                trajectoryContext =
                    context.HasAimPoint
                        ? ProjectileTrajectoryLaunchContext
                            .WithTarget(
                                context.Origin,
                                context.FireDirection,
                                context.ShooterPosition,
                                context.ShooterForward,
                                context.AimPoint)
                        : ProjectileTrajectoryLaunchContext
                            .WithoutTarget(
                                context.Origin,
                                context.FireDirection,
                                context.ShooterPosition,
                                context.ShooterForward);

            Initialize(
                context.Faction,
                context.Damage,
                context.Speed,
                context.FireDirection,
                trajectoryContext,
                context.VisualSource,
                context.CanDamageBackpack,
                context.DamageSource,
                context.TargetHealth);

            LifetimeAndScreenBounds2D lifetime =
                GetComponent<LifetimeAndScreenBounds2D>();
            if (lifetime != null)
            {
                float baseLifetime = context.Lifetime >= 0f
                    ? context.Lifetime
                    : lifetime.ConfiguredLifetime;
                lifetime.SetLifetime(
                    baseLifetime *
                    StyleTendencyDebugRuntime
                        .GetProjectileLifetimeMultiplier());
            }
        }

        /// <summary>
        /// 子弹生成后立即调用。
        /// </summary>
        public void Initialize(
            BattleFaction faction,
            float damage,
            float speed,
            Vector2 direction)
        {
            Vector2 safeDirection =
                GetSafeDirection(direction);

            ProjectileTrajectoryLaunchContext context =
                ProjectileTrajectoryLaunchContext
                    .WithoutTarget(
                        transform.position,
                        safeDirection,
                        transform.position,
                        safeDirection);

            Initialize(
                faction,
                damage,
                speed,
                direction,
                context,
                ProjectileVisualSource.FighterDefault);
        }

        /// <summary>
        /// 子弹生成后立即调用，并提供发射瞬间的轨迹上下文。
        /// </summary>
        public void Initialize(
            BattleFaction faction,
            float damage,
            float speed,
            Vector2 direction,
            ProjectileTrajectoryLaunchContext context,
            ProjectileVisualSource visualSource =
                ProjectileVisualSource.FighterDefault,
            bool canDamageBackpack = true,
            BattleDamageSource damageSource = default,
            Health targetHealth = null)
        {
            FindReferences();

            if (trajectoryController == null ||
                hitBox == null)
            {
                Debug.LogError(
                    $"{name}无法初始化：缺少轨迹控制器或HitBox。",
                    this);

                return;
            }

            Vector2 safeDirection =
                GetSafeDirection(direction);

            ProjectileTrajectoryLaunchContext
                safeContext =
                    new ProjectileTrajectoryLaunchContext(
                        context.StartPosition,
                        safeDirection,
                        context.ShooterPosition,
                        context.ShooterForward,
                        context.HasTarget,
                        context.TargetPosition);

            hitBox.Initialize(
                faction,
                damage,
                canDamageBackpack,
                damageSource);

            visualController?.Apply(faction, visualSource);

            trajectoryController.Initialize(
                Mathf.Max(0f, speed) *
                StyleTendencyDebugRuntime
                    .GetProjectileSpeedMultiplier(),
                safeContext);

            SubscribeToTargetDeath(targetHealth);

            gameObject.name =
                $"{faction} Projectile";
        }

        private void SubscribeToTargetDeath(Health targetHealth)
        {
            UnsubscribeFromTargetDeath();
            isFading = false;

            if (targetHealth == null)
            {
                return;
            }

            subscribedTargetHealth = targetHealth;
            subscribedTargetHealth.Died += HandleTargetDied;

            if (subscribedTargetHealth.IsDead &&
                BattleFlowController
                    .IsProjectileTargetDeathFadeEnabled)
            {
                BeginFadeOut();
            }
        }

        private void UnsubscribeFromTargetDeath()
        {
            if (subscribedTargetHealth != null)
            {
                subscribedTargetHealth.Died -= HandleTargetDied;
            }

            subscribedTargetHealth = null;
        }

        private void HandleTargetDied()
        {
            if (BattleFlowController
                    .IsProjectileTargetDeathFadeEnabled)
            {
                BeginFadeOut();
            }
        }

        private void BeginFadeOut()
        {
            if (isFading)
            {
                return;
            }

            isFading = true;
            UnsubscribeFromTargetDeath();
            BattleFadeOut2D.Begin(gameObject);
        }

        private void FindReferences()
        {
            if (trajectoryController == null)
            {
                trajectoryController =
                    GetComponent<
                        ProjectileTrajectoryController2D>();
            }

            if (hitBox == null)
            {
                hitBox =
                    GetComponent<HitBox2D>();
            }

            if (visualController == null)
            {
                visualController =
                    GetComponent<ProjectileVisualController2D>();
            }
        }

        private Vector2 GetSafeDirection(
            Vector2 direction)
        {
            if (direction.sqrMagnitude >
                Mathf.Epsilon)
            {
                return direction.normalized;
            }

            if (transform.up.sqrMagnitude >
                Mathf.Epsilon)
            {
                return transform.up;
            }

            return Vector2.up;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            FindReferences();
        }

        private void OnValidate()
        {
            FindReferences();
        }
#endif
    }
}
