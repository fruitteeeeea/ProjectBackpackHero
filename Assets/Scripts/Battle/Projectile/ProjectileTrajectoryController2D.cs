using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 子弹上的统一轨迹执行器。
    /// 使用Profile创建每颗子弹独立的Runtime并按路程推进。
    /// </summary>
    public sealed class ProjectileTrajectoryController2D :
        MonoBehaviour
    {
        [Header("Trajectory")]
        [SerializeField]
        private ProjectileTrajectoryProfile trajectoryProfile;

        [Header("Runtime Debug")]
        [SerializeField, Min(0f)]
        private float speed;

        [SerializeField, Min(0f)]
        private float travelledDistance;

        private ProjectileTrajectoryRuntime runtime;
        private bool hasReportedFallback;

        public ProjectileTrajectoryProfile TrajectoryProfile =>
            trajectoryProfile;

        public float Speed => speed;
        public float TravelledDistance => travelledDistance;
        public bool IsInitialized => runtime != null;

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        public void SetTrajectoryProfile(
            ProjectileTrajectoryProfile profile)
        {
            trajectoryProfile = profile;
        }

        public void Initialize(
            float projectileSpeed,
            ProjectileTrajectoryLaunchContext context)
        {
            speed = Mathf.Max(0f, projectileSpeed);
            travelledDistance = 0f;
            hasReportedFallback = false;

            runtime =
                trajectoryProfile != null
                    ? trajectoryProfile.CreateRuntime(context)
                    : null;

            if (runtime == null)
            {
                ReportFallback();
                runtime =
                    StraightProjectileTrajectoryProfile
                        .CreateFallbackRuntime(context);
            }

            ApplyPose(runtime.Evaluate(0f));
        }

        /// <summary>
        /// 公开推进入口，Update和自动化测试共用同一段逻辑。
        /// </summary>
        public void Advance(float deltaTime)
        {
            if (runtime == null)
            {
                return;
            }

            travelledDistance +=
                speed * Mathf.Max(0f, deltaTime);

            ApplyPose(
                runtime.Evaluate(travelledDistance));
        }

        private void ApplyPose(
            ProjectileTrajectoryPose pose)
        {
            transform.position = pose.Position;

            if (pose.Direction.sqrMagnitude >
                Mathf.Epsilon)
            {
                transform.up =
                    pose.Direction.normalized;
            }
        }

        private void ReportFallback()
        {
            if (hasReportedFallback)
            {
                return;
            }

            hasReportedFallback = true;

            string profileName =
                trajectoryProfile != null
                    ? trajectoryProfile.name
                    : "None";

            Debug.LogWarning(
                $"{name}的轨迹Profile（{profileName}）" +
                "无法创建有效轨迹，将回退为直线移动。",
                this);
        }
    }
}
