using UnityEngine;

namespace BackpackHero.Battle
{
    [CreateAssetMenu(
        fileName = "Sine Projectile Trajectory",
        menuName = "Battle/Projectile Trajectories/Sine")]
    public sealed class SineProjectileTrajectoryProfile :
        ProjectileTrajectoryProfile
    {
        private const float MinimumWavelength = 0.01f;

        [SerializeField, Min(0f)]
        private float amplitude = 0.25f;

        [SerializeField, Min(MinimumWavelength)]
        private float wavelength = 1.5f;

        [SerializeField, Range(8, 256)]
        private int samplesPerCycle = 64;

        public float Amplitude => amplitude;
        public float Wavelength => wavelength;
        public int SamplesPerCycle => samplesPerCycle;

        internal override ProjectileTrajectoryRuntime
            CreateRuntime(
                ProjectileTrajectoryLaunchContext context)
        {
            float safeWavelength =
                Mathf.Max(MinimumWavelength, wavelength);

            int safeSampleCount =
                Mathf.Clamp(samplesPerCycle, 8, 256);

            return new SineRuntime(
                context,
                Mathf.Max(0f, amplitude),
                safeWavelength,
                safeSampleCount);
        }

        private sealed class SineRuntime :
            ProjectileTrajectoryRuntime
        {
            private readonly Vector2 start;
            private readonly Vector2 forward;
            private readonly Vector2 right;
            private readonly float amplitude;
            private readonly float wavelength;
            private readonly float angularFrequency;
            private readonly float[] cumulativeDistances;
            private readonly float cycleLength;

            public SineRuntime(
                ProjectileTrajectoryLaunchContext context,
                float amplitude,
                float wavelength,
                int sampleCount)
            {
                start = context.StartPosition;
                forward =
                    ProjectileTrajectoryMath.GetSafeDirection(
                        context.LaunchDirection,
                        context.ShooterForward);

                right =
                    new Vector2(
                        forward.y,
                        -forward.x);

                this.amplitude = amplitude;
                this.wavelength = wavelength;

                angularFrequency =
                    Mathf.PI * 2f / wavelength;

                cumulativeDistances =
                    new float[sampleCount + 1];

                Vector2 previousPoint =
                    EvaluateLocalPoint(0f);

                for (int index = 1;
                     index <= sampleCount;
                     index++)
                {
                    float x =
                        wavelength * index / sampleCount;

                    Vector2 point =
                        EvaluateLocalPoint(x);

                    cumulativeDistances[index] =
                        cumulativeDistances[index - 1] +
                        Vector2.Distance(
                            previousPoint,
                            point);

                    previousPoint = point;
                }

                cycleLength =
                    cumulativeDistances[
                        cumulativeDistances.Length - 1];
            }

            public override ProjectileTrajectoryPose Evaluate(
                float travelledDistance)
            {
                float safeDistance =
                    Mathf.Max(0f, travelledDistance);

                if (cycleLength <= Mathf.Epsilon)
                {
                    return new ProjectileTrajectoryPose(
                        start + forward * safeDistance,
                        forward);
                }

                int completedCycles =
                    Mathf.FloorToInt(
                        safeDistance / cycleLength);

                float distanceInCycle =
                    safeDistance -
                    completedCycles * cycleLength;

                float normalizedCycle =
                    ProjectileTrajectoryMath
                        .FindParameterAtDistance(
                            distanceInCycle,
                            cumulativeDistances);

                float x =
                    completedCycles * wavelength +
                    normalizedCycle * wavelength;

                Vector2 position =
                    start + EvaluateLocalPoint(x);

                float lateralDerivative =
                    amplitude *
                    angularFrequency *
                    Mathf.Cos(
                        angularFrequency * x);

                Vector2 tangent =
                    forward +
                    right * lateralDerivative;

                return new ProjectileTrajectoryPose(
                    position,
                    ProjectileTrajectoryMath
                        .GetSafeDirection(
                            tangent,
                            forward));
            }

            private Vector2 EvaluateLocalPoint(float x)
            {
                return
                    forward * x +
                    right *
                    (
                        amplitude *
                        Mathf.Sin(
                            angularFrequency * x)
                    );
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            amplitude = Mathf.Max(0f, amplitude);

            wavelength =
                Mathf.Max(
                    MinimumWavelength,
                    wavelength);

            samplesPerCycle =
                Mathf.Clamp(
                    samplesPerCycle,
                    8,
                    256);
        }
#endif
    }
}
