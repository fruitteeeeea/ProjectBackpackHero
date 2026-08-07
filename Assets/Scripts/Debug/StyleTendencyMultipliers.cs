using UnityEngine;
using BackpackPrototype;

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
            float aircraftAttackSpeed = 1f,
            CooldownItemType cooldownItemType =
                BackpackPrototype.CooldownItemType.Equipment)
        {
            ItemCooldownSpeed = Clamp(itemCooldownSpeed);
            AircraftTargetingArcAngle = Clamp(aircraftTargetingArcAngle);
            AircraftAttackRange = Clamp(aircraftAttackRange);
            AircraftAttackSpeed = ClampAircraftAttackSpeed(aircraftAttackSpeed);
            CooldownItemType = cooldownItemType;
        }

        public float ItemCooldownSpeed { get; }
        public float AircraftTargetingArcAngle { get; }
        public float AircraftAttackRange { get; }
        public float AircraftAttackSpeed { get; }
        public CooldownItemType CooldownItemType { get; }

        public static StyleTendencyMultipliers Default =>
            new(1f, 1f, 1f, 1f,
                BackpackPrototype.CooldownItemType.Equipment);

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
