using BackpackHero.Battle;
using UnityEngine;

namespace BackpackPrototype
{
    [CreateAssetMenu(
        fileName = "EquipmentEffect_LaserLink",
        menuName = "Backpack Prototype/Equipment Effects/Laser Link")]
    public sealed class LaserLinkEquipmentEffectDefinition : EquipmentEffectDefinition
    {
        public const float MinimumCooldown = 0.1f;

        [SerializeField] private BattleAttack2D laserAttackPrefab;
        [SerializeField, Min(MinimumCooldown)] private float cooldown = 0.8f;

        public BattleAttack2D LaserAttackPrefab => laserAttackPrefab;
        public float Cooldown => Mathf.Max(MinimumCooldown, cooldown);

        public override bool TryGetHangarStats(out EquipmentHangarStats stats)
        {
            stats = new EquipmentHangarStats(0f, 0f, Cooldown);
            return true;
        }

        public void InitializeForTests(
            BattleAttack2D testLaserAttackPrefab,
            float testCooldown)
        {
            laserAttackPrefab = testLaserAttackPrefab;
            cooldown = testCooldown;
        }
    }
}
