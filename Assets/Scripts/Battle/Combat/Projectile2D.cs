using UnityEngine;

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
    public sealed class Projectile2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private ProjectileTrajectoryController2D
            trajectoryController;

        [SerializeField]
        private HitBox2D hitBox;

        public BattleFaction Faction =>
            hitBox != null
                ? hitBox.Faction
                : BattleFaction.Player;

        private void Awake()
        {
            FindReferences();
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
                context);
        }

        /// <summary>
        /// 子弹生成后立即调用，并提供发射瞬间的轨迹上下文。
        /// </summary>
        public void Initialize(
            BattleFaction faction,
            float damage,
            float speed,
            Vector2 direction,
            ProjectileTrajectoryLaunchContext context)
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
                damage);

            trajectoryController.Initialize(
                Mathf.Max(0f, speed),
                safeContext);

            gameObject.name =
                $"{faction} Projectile";
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
