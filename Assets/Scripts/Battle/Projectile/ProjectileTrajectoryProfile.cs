using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 可复用的子弹轨迹配置。
    /// Profile只保存配置；每颗子弹的进度由独立Runtime保存。
    /// </summary>
    public abstract class ProjectileTrajectoryProfile :
        ScriptableObject
    {
        internal abstract ProjectileTrajectoryRuntime
            CreateRuntime(
                ProjectileTrajectoryLaunchContext context);
    }

    internal readonly struct ProjectileTrajectoryPose
    {
        public Vector2 Position { get; }
        public Vector2 Direction { get; }

        public ProjectileTrajectoryPose(
            Vector2 position,
            Vector2 direction)
        {
            Position = position;
            Direction = direction;
        }
    }

    internal abstract class ProjectileTrajectoryRuntime
    {
        public abstract ProjectileTrajectoryPose Evaluate(
            float travelledDistance);
    }

    internal static class ProjectileTrajectoryMath
    {
        public static Vector2 GetSafeDirection(
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

        public static float FindParameterAtDistance(
            float distance,
            float[] cumulativeDistances)
        {
            if (cumulativeDistances == null ||
                cumulativeDistances.Length < 2)
            {
                return 0f;
            }

            float totalDistance =
                cumulativeDistances[
                    cumulativeDistances.Length - 1];

            if (totalDistance <= Mathf.Epsilon)
            {
                return 0f;
            }

            float clampedDistance =
                Mathf.Clamp(distance, 0f, totalDistance);

            int low = 0;
            int high = cumulativeDistances.Length - 1;

            while (low + 1 < high)
            {
                int middle = (low + high) / 2;

                if (cumulativeDistances[middle] <
                    clampedDistance)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            float segmentStart =
                cumulativeDistances[low];

            float segmentEnd =
                cumulativeDistances[high];

            float segmentLength =
                segmentEnd - segmentStart;

            float segmentProgress =
                segmentLength > Mathf.Epsilon
                    ? (clampedDistance - segmentStart) /
                      segmentLength
                    : 0f;

            return
                (low + segmentProgress) /
                (cumulativeDistances.Length - 1);
        }
    }
}
