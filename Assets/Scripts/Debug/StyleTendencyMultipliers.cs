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
        public const float MinimumAircraftLifetimeMultiplier = 0.5f;
        public const float MaximumAircraftLifetimeMultiplier = 2f;
        public const float MinimumProjectileMultiplier = 0.5f;
        public const float MaximumProjectileMultiplier = 1f;

        public StyleTendencyMultipliers(
            float itemCooldownSpeed,
            float aircraftTargetingArcAngle,
            float aircraftAttackRange,
            float aircraftAttackSpeed = 1f,
            CooldownItemType cooldownItemType =
                BackpackPrototype.CooldownItemType.Equipment,
            float aircraftLifetime = 1f,
            float projectileSpeed = 1f,
            float projectileLifetime = 1f)
        {
            ItemCooldownSpeed = Clamp(itemCooldownSpeed);
            AircraftTargetingArcAngle = Clamp(aircraftTargetingArcAngle);
            AircraftAttackRange = Clamp(aircraftAttackRange);
            AircraftAttackSpeed = ClampAircraftAttackSpeed(aircraftAttackSpeed);
            CooldownItemType = cooldownItemType;
            AircraftLifetime = ClampAircraftLifetime(aircraftLifetime);
            ProjectileSpeed = ClampProjectile(projectileSpeed);
            ProjectileLifetime = ClampProjectile(projectileLifetime);
        }

        public float ItemCooldownSpeed { get; }
        public float AircraftTargetingArcAngle { get; }
        public float AircraftAttackRange { get; }
        public float AircraftAttackSpeed { get; }
        public CooldownItemType CooldownItemType { get; }
        public float AircraftLifetime { get; }
        public float ProjectileSpeed { get; }
        public float ProjectileLifetime { get; }

        public static StyleTendencyMultipliers Default =>
            new(1f, 1f, 1f, 1f,
                BackpackPrototype.CooldownItemType.Equipment,
                1f, 1f, 1f);

        private static float Clamp(float value) => Mathf.Clamp(
            value,
            MinimumMultiplier,
            MaximumMultiplier);

        private static float ClampAircraftAttackSpeed(float value) =>
            Mathf.Clamp(
                value,
                MinimumAircraftAttackSpeedMultiplier,
                MaximumAircraftAttackSpeedMultiplier);

        private static float ClampAircraftLifetime(float value) =>
            Mathf.Clamp(
                value,
                MinimumAircraftLifetimeMultiplier,
                MaximumAircraftLifetimeMultiplier);

        private static float ClampProjectile(float value) => Mathf.Clamp(
            value,
            MinimumProjectileMultiplier,
            MaximumProjectileMultiplier);
    }
}
