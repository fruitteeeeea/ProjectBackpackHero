using UnityEngine;

namespace BackpackHero.Battle
{
    [CreateAssetMenu(
        fileName = "Straight Projectile Trajectory",
        menuName = "Battle/Projectile Trajectories/Straight")]
    public sealed class StraightProjectileTrajectoryProfile :
        ProjectileTrajectoryProfile
    {
        internal override ProjectileTrajectoryRuntime
            CreateRuntime(
                ProjectileTrajectoryLaunchContext context)
        {
            return CreateFallbackRuntime(context);
        }

        internal static ProjectileTrajectoryRuntime
            CreateFallbackRuntime(
                ProjectileTrajectoryLaunchContext context)
        {
            return new StraightRuntime(context);
        }

        private sealed class StraightRuntime :
            ProjectileTrajectoryRuntime
        {
            private readonly Vector2 start;
            private readonly Vector2 direction;

            public StraightRuntime(
                ProjectileTrajectoryLaunchContext context)
            {
                start = context.StartPosition;
                direction =
                    ProjectileTrajectoryMath.GetSafeDirection(
                        context.LaunchDirection,
                        context.ShooterForward);
            }

            public override ProjectileTrajectoryPose Evaluate(
                float travelledDistance)
            {
                float safeDistance =
                    Mathf.Max(0f, travelledDistance);

                return new ProjectileTrajectoryPose(
                    start + direction * safeDistance,
                    direction);
            }
        }
    }
}
