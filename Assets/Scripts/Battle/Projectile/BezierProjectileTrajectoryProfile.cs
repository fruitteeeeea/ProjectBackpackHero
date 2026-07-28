using UnityEngine;

namespace BackpackHero.Battle
{
    public enum BezierProjectileSide
    {
        Left,
        Right,
        Random
    }

    [CreateAssetMenu(
        fileName = "Bezier Projectile Trajectory",
        menuName = "Battle/Projectile Trajectories/Bezier")]
    public sealed class BezierProjectileTrajectoryProfile :
        ProjectileTrajectoryProfile
    {
        [SerializeField, Min(0f)]
        private float backwardDistance = 0.5f;

        [SerializeField, Min(0f)]
        private float lateralDistance = 0.75f;

        [SerializeField]
        private BezierProjectileSide side =
            BezierProjectileSide.Random;

        [SerializeField, Range(8, 256)]
        private int sampleCount = 32;

        public float BackwardDistance => backwardDistance;
        public float LateralDistance => lateralDistance;
        public BezierProjectileSide Side => side;
        public int SampleCount => sampleCount;

        internal override ProjectileTrajectoryRuntime
            CreateRuntime(
                ProjectileTrajectoryLaunchContext context)
        {
            if (!context.HasTarget)
            {
                return null;
            }

            Vector2 start = context.StartPosition;
            Vector2 target = context.TargetPosition;

            if ((target - start).sqrMagnitude <=
                Mathf.Epsilon)
            {
                return null;
            }

            Vector2 shooterForward =
                ProjectileTrajectoryMath.GetSafeDirection(
                    context.ShooterForward,
                    context.LaunchDirection);

            Vector2 shooterRight =
                new Vector2(
                    shooterForward.y,
                    -shooterForward.x);

            float sideSign = ResolveSideSign();

            Vector2 controlPoint =
                context.ShooterPosition -
                shooterForward *
                Mathf.Max(0f, backwardDistance) +
                shooterRight *
                (
                    Mathf.Max(0f, lateralDistance) *
                    sideSign
                );

            return new BezierRuntime(
                start,
                controlPoint,
                target,
                Mathf.Clamp(sampleCount, 8, 256));
        }

        private float ResolveSideSign()
        {
            switch (side)
            {
                case BezierProjectileSide.Left:
                    return -1f;

                case BezierProjectileSide.Right:
                    return 1f;

                default:
                    return Random.value < 0.5f
                        ? -1f
                        : 1f;
            }
        }

        private sealed class BezierRuntime :
            ProjectileTrajectoryRuntime
        {
            private readonly Vector2 start;
            private readonly Vector2 control;
            private readonly Vector2 end;
            private readonly float[] cumulativeDistances;
            private readonly float curveLength;
            private readonly Vector2 finalDirection;

            public BezierRuntime(
                Vector2 start,
                Vector2 control,
                Vector2 end,
                int sampleCount)
            {
                this.start = start;
                this.control = control;
                this.end = end;

                cumulativeDistances =
                    new float[sampleCount + 1];

                Vector2 previousPoint =
                    EvaluatePoint(0f);

                for (int index = 1;
                     index <= sampleCount;
                     index++)
                {
                    float t =
                        (float)index / sampleCount;

                    Vector2 point =
                        EvaluatePoint(t);

                    cumulativeDistances[index] =
                        cumulativeDistances[index - 1] +
                        Vector2.Distance(
                            previousPoint,
                            point);

                    previousPoint = point;
                }

                curveLength =
                    cumulativeDistances[
                        cumulativeDistances.Length - 1];

                finalDirection =
                    ProjectileTrajectoryMath
                        .GetSafeDirection(
                            end - control,
                            end - start);
            }

            public override ProjectileTrajectoryPose Evaluate(
                float travelledDistance)
            {
                float safeDistance =
                    Mathf.Max(0f, travelledDistance);

                if (curveLength <= Mathf.Epsilon)
                {
                    return new ProjectileTrajectoryPose(
                        start +
                        finalDirection * safeDistance,
                        finalDirection);
                }

                if (safeDistance >= curveLength)
                {
                    float straightDistance =
                        safeDistance - curveLength;

                    return new ProjectileTrajectoryPose(
                        end +
                        finalDirection *
                        straightDistance,
                        finalDirection);
                }

                float t =
                    ProjectileTrajectoryMath
                        .FindParameterAtDistance(
                            safeDistance,
                            cumulativeDistances);

                Vector2 tangent =
                    2f * (1f - t) *
                    (control - start) +
                    2f * t *
                    (end - control);

                return new ProjectileTrajectoryPose(
                    EvaluatePoint(t),
                    ProjectileTrajectoryMath
                        .GetSafeDirection(
                            tangent,
                            finalDirection));
            }

            private Vector2 EvaluatePoint(float t)
            {
                float safeT = Mathf.Clamp01(t);
                float inverseT = 1f - safeT;

                return
                    inverseT * inverseT * start +
                    2f * inverseT * safeT * control +
                    safeT * safeT * end;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            backwardDistance =
                Mathf.Max(0f, backwardDistance);

            lateralDistance =
                Mathf.Max(0f, lateralDistance);

            sampleCount =
                Mathf.Clamp(
                    sampleCount,
                    8,
                    256);
        }
#endif
    }
}
