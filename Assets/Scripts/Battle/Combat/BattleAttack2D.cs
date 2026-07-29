using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 所有可由战斗发射模式生成的攻击实例的统一入口。
    /// 飞行子弹、激光和后续范围效果各自实现初始化行为。
    /// </summary>
    public abstract class BattleAttack2D : MonoBehaviour
    {
        public abstract void Initialize(
            BattleAttackLaunchContext context);
    }

    /// <summary>
    /// 发射瞬间保存的攻击上下文。
    /// AimPoint是瞄准参考点；范围攻击可以根据Pattern方向重新计算终点。
    /// Lifetime小于0表示保留攻击Prefab自身的默认寿命。
    /// </summary>
    public readonly struct BattleAttackLaunchContext
    {
        public BattleFaction Faction { get; }
        public float Damage { get; }
        public float Speed { get; }
        public float Lifetime { get; }
        public Vector2 Origin { get; }
        public Vector2 FireDirection { get; }
        public Vector2 ShooterPosition { get; }
        public Vector2 ShooterForward { get; }
        public bool HasAimPoint { get; }
        public Vector2 AimPoint { get; }

        public BattleAttackLaunchContext(
            BattleFaction faction,
            float damage,
            float speed,
            float lifetime,
            Vector2 origin,
            Vector2 fireDirection,
            Vector2 shooterPosition,
            Vector2 shooterForward,
            bool hasAimPoint,
            Vector2 aimPoint)
        {
            Faction = faction;
            Damage = Mathf.Max(0f, damage);
            Speed = Mathf.Max(0f, speed);
            Lifetime = lifetime;
            Origin = origin;
            FireDirection = GetSafeDirection(
                fireDirection,
                shooterForward);
            ShooterPosition = shooterPosition;
            ShooterForward = GetSafeDirection(
                shooterForward,
                FireDirection);
            HasAimPoint = hasAimPoint;
            AimPoint = aimPoint;
        }

        public static BattleAttackLaunchContext
            WithAimPoint(
                BattleFaction faction,
                float damage,
                float speed,
                float lifetime,
                Vector2 origin,
                Vector2 fireDirection,
                Vector2 shooterPosition,
                Vector2 shooterForward,
                Vector2 aimPoint)
        {
            return new BattleAttackLaunchContext(
                faction,
                damage,
                speed,
                lifetime,
                origin,
                fireDirection,
                shooterPosition,
                shooterForward,
                true,
                aimPoint);
        }

        public static BattleAttackLaunchContext
            WithoutAimPoint(
                BattleFaction faction,
                float damage,
                float speed,
                float lifetime,
                Vector2 origin,
                Vector2 fireDirection,
                Vector2 shooterPosition,
                Vector2 shooterForward)
        {
            return new BattleAttackLaunchContext(
                faction,
                damage,
                speed,
                lifetime,
                origin,
                fireDirection,
                shooterPosition,
                shooterForward,
                false,
                Vector2.zero);
        }

        private static Vector2 GetSafeDirection(
            Vector2 direction,
            Vector2 fallback)
        {
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                return direction.normalized;
            }

            if (fallback.sqrMagnitude > Mathf.Epsilon)
            {
                return fallback.normalized;
            }

            return Vector2.up;
        }
    }
}
