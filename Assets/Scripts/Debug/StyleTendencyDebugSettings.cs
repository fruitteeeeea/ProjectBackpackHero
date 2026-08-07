using UnityEngine;
using BackpackPrototype;

namespace BackpackHero.Debugging
{
    /// <summary>可持久化的全局风格倾向倍率配置。</summary>
    [CreateAssetMenu(
        fileName = "StyleTendencyDebugSettings",
        menuName = "Debug/Style Tendency Settings")]
    public sealed class StyleTendencyDebugSettings : ScriptableObject
    {
        [Header("Global Multipliers")]
        [SerializeField, Range(
            StyleTendencyMultipliers.MinimumMultiplier,
            StyleTendencyMultipliers.MaximumMultiplier)]
        private float itemCooldownSpeedMultiplier = 1f;

        [SerializeField, Range(
            StyleTendencyMultipliers.MinimumMultiplier,
            StyleTendencyMultipliers.MaximumMultiplier)]
        private float aircraftTargetingArcAngleMultiplier = 1f;

        [SerializeField, Range(
            StyleTendencyMultipliers.MinimumMultiplier,
            StyleTendencyMultipliers.MaximumMultiplier)]
        private float aircraftAttackRangeMultiplier = 1f;

        [SerializeField, Range(
            StyleTendencyMultipliers.MinimumAircraftAttackSpeedMultiplier,
            StyleTendencyMultipliers.MaximumAircraftAttackSpeedMultiplier)]
        private float aircraftAttackSpeedMultiplier = 1f;

        [SerializeField, Range(
            StyleTendencyMultipliers.MinimumAircraftFlightSpeedMultiplier,
            StyleTendencyMultipliers.MaximumAircraftFlightSpeedMultiplier)]
        private float aircraftFlightSpeedMultiplier = 1f;

        [SerializeField, Range(
            StyleTendencyMultipliers.MinimumAircraftLifetimeMultiplier,
            StyleTendencyMultipliers.MaximumAircraftLifetimeMultiplier)]
        private float aircraftLifetimeMultiplier = 1f;

        [SerializeField, Range(
            StyleTendencyMultipliers.MinimumProjectileSpeedMultiplier,
            StyleTendencyMultipliers.MaximumProjectileMultiplier)]
        private float projectileSpeedMultiplier = 1f;

        [SerializeField, Range(
            StyleTendencyMultipliers.MinimumProjectileLifetimeMultiplier,
            StyleTendencyMultipliers.MaximumProjectileMultiplier)]
        private float projectileLifetimeMultiplier = 1f;

        [Header("Cooldown")]
        [SerializeField]
        private CooldownItemType cooldownItemType =
            BackpackPrototype.CooldownItemType.Equipment;

        public float ItemCooldownSpeedMultiplier => itemCooldownSpeedMultiplier;
        public float AircraftTargetingArcAngleMultiplier =>
            aircraftTargetingArcAngleMultiplier;
        public float AircraftAttackRangeMultiplier =>
            aircraftAttackRangeMultiplier;
        public float AircraftAttackSpeedMultiplier =>
            aircraftAttackSpeedMultiplier;
        public float AircraftFlightSpeedMultiplier =>
            aircraftFlightSpeedMultiplier;
        public CooldownItemType CooldownItemType => cooldownItemType;
        public float AircraftLifetimeMultiplier => aircraftLifetimeMultiplier;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public float ProjectileLifetimeMultiplier => projectileLifetimeMultiplier;

        public void SetValues(StyleTendencyMultipliers values)
        {
            itemCooldownSpeedMultiplier = values.ItemCooldownSpeed;
            aircraftTargetingArcAngleMultiplier =
                values.AircraftTargetingArcAngle;
            aircraftAttackRangeMultiplier = values.AircraftAttackRange;
            aircraftAttackSpeedMultiplier = values.AircraftAttackSpeed;
            aircraftFlightSpeedMultiplier = values.AircraftFlightSpeed;
            cooldownItemType = values.CooldownItemType;
            aircraftLifetimeMultiplier = values.AircraftLifetime;
            projectileSpeedMultiplier = values.ProjectileSpeed;
            projectileLifetimeMultiplier = values.ProjectileLifetime;
        }

        public StyleTendencyMultipliers GetValues() => new(
            itemCooldownSpeedMultiplier,
            aircraftTargetingArcAngleMultiplier,
            aircraftAttackRangeMultiplier,
            aircraftAttackSpeedMultiplier,
            cooldownItemType,
            aircraftLifetimeMultiplier,
            projectileSpeedMultiplier,
            projectileLifetimeMultiplier,
            aircraftFlightSpeedMultiplier);

#if UNITY_EDITOR
        private void OnValidate() => SetValues(GetValues());
#endif
    }
}
