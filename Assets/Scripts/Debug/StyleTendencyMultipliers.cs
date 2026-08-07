using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>运行时使用的一组不可变风格倾向倍率。</summary>
    public readonly struct StyleTendencyMultipliers
    {
        public const float MinimumMultiplier = 0.5f;
        public const float MaximumMultiplier = 3f;
        public const float MinimumAircraftAttackSpeedMultiplier = 0.2f;
        public const float MaximumAircraftAttackSpeedMultiplier = 2f;

        public StyleTendencyMultipliers(
            float itemCooldownSpeed,
            float aircraftTargetingArcAngle,
            float aircraftAttackRange,
            float aircraftAttackSpeed = 1f)
        {
            ItemCooldownSpeed = Clamp(itemCooldownSpeed);
            AircraftTargetingArcAngle = Clamp(aircraftTargetingArcAngle);
            AircraftAttackRange = Clamp(aircraftAttackRange);
            AircraftAttackSpeed = ClampAircraftAttackSpeed(aircraftAttackSpeed);
        }

        public float ItemCooldownSpeed { get; }
        public float AircraftTargetingArcAngle { get; }
        public float AircraftAttackRange { get; }
        public float AircraftAttackSpeed { get; }

        public static StyleTendencyMultipliers Default => new(1f, 1f, 1f, 1f);

        private static float Clamp(float value) => Mathf.Clamp(
            value,
            MinimumMultiplier,
            MaximumMultiplier);

        private static float ClampAircraftAttackSpeed(float value) =>
            Mathf.Clamp(
                value,
                MinimumAircraftAttackSpeedMultiplier,
                MaximumAircraftAttackSpeedMultiplier);
    }
}
