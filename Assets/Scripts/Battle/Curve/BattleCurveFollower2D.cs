using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 让飞机先按照一段Battle Curve飞行，
    /// 离开曲线后沿终点切线继续直线飞行。
    /// </summary>
    [RequireComponent(typeof(DirectionalMover2D))]
    public sealed class BattleCurveFollower2D :
        MonoBehaviour
    {
        [Header("Curve Sampling")]
        [Tooltip("曲线采样数。数值越高轨迹越平滑。")]
        [SerializeField, Range(8, 128)]
        private int sampleCount = 32;

        private DirectionalMover2D mover;

        private Vector3[] sampledPoints;
        private float[] cumulativeDistances;

        private float totalLength;
        private float travelledDistance;
        private int currentSegment;
        private bool isFollowingCurve;
        private bool isPaused;

        public bool IsFollowingCurve =>
            isFollowingCurve;
        
        public bool IsPaused =>
            isPaused;

        private void Awake()
        {
            mover = GetComponent<DirectionalMover2D>();
        }

        private void Update()
        {
            if (!isFollowingCurve ||
                isPaused)
            {
                return;
            }

            FollowCurve(Time.deltaTime);
        }

        /// <summary>
        /// 生成飞机后调用，截取一条固定曲线。
        /// 后续Curve Line继续变化不会改变已经生成的飞机。
        /// </summary>
        public void BeginCurve(
            Vector3 start,
            Vector3 end,
            float curveValue,
            float maximumBendDistance)
        {
            if (mover == null)
            {
                mover = GetComponent<DirectionalMover2D>();
            }

            BuildCurveSamples(
                start,
                end,
                curveValue,
                maximumBendDistance);

            travelledDistance = 0f;
            currentSegment = 0;
            isPaused = false;
            isFollowingCurve =
                totalLength > Mathf.Epsilon;
            transform.position = start;

            if (!isFollowingCurve)
            {
                Vector2 fallbackDirection =
                    end - start;

                if (fallbackDirection.sqrMagnitude <=
                    Mathf.Epsilon)
                {
                    fallbackDirection = Vector2.up;
                }

                mover.SetDirection(fallbackDirection);
                mover.enabled = true;
                enabled = false;
                return;
            }

            Vector2 initialDirection =
                sampledPoints[1] - sampledPoints[0];

            mover.SetDirection(initialDirection);

            // 跟随曲线期间由本组件移动，避免直线移动组件重复移动。
            mover.enabled = false;
            enabled = true;
        }

        /// <summary>
        /// 暂停或恢复曲线路径推进。
        /// 已经走过的距离会保留，恢复后继续原路线。
        /// </summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }
        
        private void BuildCurveSamples(
            Vector3 start,
            Vector3 end,
            float curveValue,
            float maximumBendDistance)
        {
            int safeSampleCount =
                Mathf.Clamp(sampleCount, 8, 128);

            sampledPoints =
                new Vector3[safeSampleCount + 1];

            cumulativeDistances =
                new float[safeSampleCount + 1];

            sampledPoints[0] = start;
            cumulativeDistances[0] = 0f;

            totalLength = 0f;

            for (int index = 1;
                 index <= safeSampleCount;
                 index++)
            {
                float t =
                    (float)index / safeSampleCount;

                sampledPoints[index] =
                    BattleCurve2D.Evaluate(
                        start,
                        end,
                        curveValue,
                        maximumBendDistance,
                        t);

                totalLength += Vector3.Distance(
                    sampledPoints[index - 1],
                    sampledPoints[index]);

                cumulativeDistances[index] =
                    totalLength;
            }
        }

        private void FollowCurve(float deltaTime)
        {
            travelledDistance +=
                mover.CurrentSpeed * deltaTime;

            if (travelledDistance >= totalLength)
            {
                FinishCurve();
                return;
            }

            FindCurrentSegment();

            float segmentStartDistance =
                cumulativeDistances[currentSegment];

            float segmentEndDistance =
                cumulativeDistances[currentSegment + 1];

            float segmentLength =
                segmentEndDistance -
                segmentStartDistance;

            float segmentProgress =
                segmentLength > Mathf.Epsilon
                    ? (travelledDistance -
                       segmentStartDistance) /
                      segmentLength
                    : 1f;

            Vector3 startPoint =
                sampledPoints[currentSegment];

            Vector3 endPoint =
                sampledPoints[currentSegment + 1];

            transform.position = Vector3.Lerp(
                startPoint,
                endPoint,
                segmentProgress);

            Vector2 flightDirection =
                endPoint - startPoint;

            mover.SetDirection(flightDirection);
        }

        private void FindCurrentSegment()
        {
            while (currentSegment <
                   cumulativeDistances.Length - 2 &&
                   travelledDistance >
                   cumulativeDistances[
                       currentSegment + 1])
            {
                currentSegment++;
            }
        }

        private void FinishCurve()
        {
            int lastIndex =
                sampledPoints.Length - 1;

            transform.position =
                sampledPoints[lastIndex];

            Vector2 finalDirection =
                sampledPoints[lastIndex] -
                sampledPoints[lastIndex - 1];

            mover.SetDirection(finalDirection);

            isFollowingCurve = false;

            // 曲线结束后恢复普通直线移动。
            mover.enabled = true;
            enabled = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            sampleCount =
                Mathf.Clamp(sampleCount, 8, 128);
        }
#endif
    }
}