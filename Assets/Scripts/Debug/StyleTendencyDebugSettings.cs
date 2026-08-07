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
        public CooldownItemType CooldownItemType => cooldownItemType;

        public void SetValues(StyleTendencyMultipliers values)
        {
            itemCooldownSpeedMultiplier = values.ItemCooldownSpeed;
            aircraftTargetingArcAngleMultiplier =
                values.AircraftTargetingArcAngle;
            aircraftAttackRangeMultiplier = values.AircraftAttackRange;
            aircraftAttackSpeedMultiplier = values.AircraftAttackSpeed;
            cooldownItemType = values.CooldownItemType;
        }

        public StyleTendencyMultipliers GetValues() => new(
            itemCooldownSpeedMultiplier,
            aircraftTargetingArcAngleMultiplier,
            aircraftAttackRangeMultiplier,
            aircraftAttackSpeedMultiplier,
            cooldownItemType);

#if UNITY_EDITOR
        private void OnValidate() => SetValues(GetValues());
#endif
    }
}
