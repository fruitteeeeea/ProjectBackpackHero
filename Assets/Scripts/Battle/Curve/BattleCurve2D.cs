using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// Battle Curve使用的二次贝塞尔曲线数学工具。
    /// 不保存场景状态，因此显示和飞行系统都可以安全复用。
    /// </summary>
    public static class BattleCurve2D
    {
        public static Vector3 Evaluate(
            Vector3 start,
            Vector3 end,
            float curveValue,
            float maximumBendDistance,
            float normalizedTime)
        {
            Vector3 controlPoint = CalculateControlPoint(
                start,
                end,
                curveValue,
                maximumBendDistance);

            float t = Mathf.Clamp01(normalizedTime);
            float inverseT = 1f - t;

            return
                inverseT * inverseT * start +
                2f * inverseT * t * controlPoint +
                t * t * end;
        }

        public static Vector3 EvaluateTangent(
            Vector3 start,
            Vector3 end,
            float curveValue,
            float maximumBendDistance,
            float normalizedTime)
        {
            Vector3 controlPoint = CalculateControlPoint(
                start,
                end,
                curveValue,
                maximumBendDistance);

            float t = Mathf.Clamp01(normalizedTime);

            Vector3 tangent =
                2f * (1f - t) * (controlPoint - start) +
                2f * t * (end - controlPoint);

            return tangent.sqrMagnitude > Mathf.Epsilon
                ? tangent.normalized
                : (end - start).normalized;
        }

        public static Vector3 CalculateControlPoint(
            Vector3 start,
            Vector3 end,
            float curveValue,
            float maximumBendDistance)
        {
            float clampedCurveValue =
                Mathf.Clamp(curveValue, -1f, 1f);

            float bendDistance =
                Mathf.Max(0f, maximumBendDistance);

            Vector3 midpoint = (start + end) * 0.5f;

            Vector3 desiredCurveMidpoint =
                midpoint +
                Vector3.right *
                (clampedCurveValue * bendDistance);

            // 二次贝塞尔在t=0.5时只获得控制点偏移的一半，
            // 因此将目标弯曲偏移放大两倍。
            return midpoint +
                   (desiredCurveMidpoint - midpoint) * 2f;
        }

        /// <summary>
        /// Returns the normalized position of the closest point on an evenly
        /// sampled polyline. The polyline points must be ordered from t=0 to t=1.
        /// </summary>
        public static float FindClosestNormalizedTime(
            Vector2 target,
            Vector2[] sampledPoints)
        {
            if (sampledPoints == null || sampledPoints.Length < 2)
            {
                return 0f;
            }

            float closestDistanceSquared = float.PositiveInfinity;
            float closestTime = 0f;
            int segmentCount = sampledPoints.Length - 1;

            for (int index = 0; index < segmentCount; index++)
            {
                Vector2 start = sampledPoints[index];
                Vector2 end = sampledPoints[index + 1];
                Vector2 segment = end - start;
                float segmentLengthSquared = segment.sqrMagnitude;
                float segmentTime = segmentLengthSquared > Mathf.Epsilon
                    ? Mathf.Clamp01(Vector2.Dot(target - start, segment) /
                        segmentLengthSquared)
                    : 0f;
                Vector2 closestPoint = start + segment * segmentTime;
                float distanceSquared =
                    (target - closestPoint).sqrMagnitude;

                if (distanceSquared >= closestDistanceSquared)
                {
                    continue;
                }

                closestDistanceSquared = distanceSquared;
                closestTime =
                    (index + segmentTime) / segmentCount;
            }

            return Mathf.Clamp01(closestTime);
        }
    }
}
