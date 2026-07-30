using BackpackHero.Battle;
using UnityEngine;

namespace BackpackPrototype
{
    [CreateAssetMenu(
        fileName = "EquipmentEffect_Projectile",
        menuName = "Backpack Prototype/Equipment Effects/Projectile")]
    public sealed class ProjectileEquipmentEffectDefinition :
        EquipmentEffectDefinition
    {
        public const float MinimumCooldown = 0.1f;

        [SerializeField]
        private BattleAttack2D projectilePrefab;

        [SerializeField, Min(MinimumCooldown)]
        private float cooldown = 0.8f;

        public BattleAttack2D ProjectilePrefab => projectilePrefab;

        public float Cooldown =>
            Mathf.Max(MinimumCooldown, cooldown);

        public void InitializeForTests(
            BattleAttack2D testProjectilePrefab,
            float testCooldown)
        {
            projectilePrefab = testProjectilePrefab;
            cooldown = testCooldown;
            OnValidate();
        }

        private void OnValidate()
        {
            cooldown = Mathf.Max(MinimumCooldown, cooldown);
        }
    }
}
