using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 一颗子弹生成时截取的轨迹上下文。
    /// 所有坐标都是世界空间；目标位置不会在飞行中继续更新。
    /// </summary>
    public readonly struct ProjectileTrajectoryLaunchContext
    {
        public Vector2 StartPosition { get; }
        public Vector2 LaunchDirection { get; }
        public Vector2 ShooterPosition { get; }
        public Vector2 ShooterForward { get; }
        public bool HasTarget { get; }
        public Vector2 TargetPosition { get; }

        public ProjectileTrajectoryLaunchContext(
            Vector2 startPosition,
            Vector2 launchDirection,
            Vector2 shooterPosition,
            Vector2 shooterForward,
            bool hasTarget,
            Vector2 targetPosition)
        {
            StartPosition = startPosition;
            LaunchDirection = launchDirection;
            ShooterPosition = shooterPosition;
            ShooterForward = shooterForward;
            HasTarget = hasTarget;
            TargetPosition = targetPosition;
        }

        public static ProjectileTrajectoryLaunchContext
            WithoutTarget(
                Vector2 startPosition,
                Vector2 launchDirection,
                Vector2 shooterPosition,
                Vector2 shooterForward)
        {
            return new ProjectileTrajectoryLaunchContext(
                startPosition,
                launchDirection,
                shooterPosition,
                shooterForward,
                false,
                Vector2.zero);
        }

        public static ProjectileTrajectoryLaunchContext
            WithTarget(
                Vector2 startPosition,
                Vector2 launchDirection,
                Vector2 shooterPosition,
                Vector2 shooterForward,
                Vector2 targetPosition)
        {
            return new ProjectileTrajectoryLaunchContext(
                startPosition,
                launchDirection,
                shooterPosition,
                shooterForward,
                true,
                targetPosition);
        }
    }
}
