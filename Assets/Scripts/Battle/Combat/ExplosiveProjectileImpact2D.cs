using UnityEngine;

namespace BackpackHero.Battle
{
    [RequireComponent(typeof(AreaDamageResolver2D))]
    [RequireComponent(typeof(CircleDamageArea2D))]
    public sealed class ExplosiveProjectileImpact2D : ProjectileImpactEffect2D
    {
        [SerializeField, Min(0f)] private float radius = 1.5f;
        [Header("Visual")]
        [SerializeField] private ParticleSystem impactVfxPrefab;
        [SerializeField, Min(0.01f)] private float impactVfxScale = 1f;
        [SerializeField] private AreaDamageResolver2D damageResolver;
        [SerializeField] private CircleDamageArea2D damageArea;

        public float Radius => radius;

        private void Awake() => FindReferences();

        public override bool ResolveImpact(
            HurtBox2D initialTarget,
            Vector2 impactPosition,
            BattleFaction attackerFaction,
            float damage)
        {
            FindReferences();
            if (damageResolver == null || damageArea == null)
            {
                return false;
            }

            damageArea.Configure(impactPosition, radius);
            damageResolver.Resolve(damageArea, attackerFaction, damage);

            if (impactVfxPrefab != null)
            {
                ParticleSystem impactVfx = Instantiate(
                    impactVfxPrefab,
                    impactPosition,
                    impactVfxPrefab.transform.rotation);

                impactVfx.transform.localScale =
                    Vector3.one * impactVfxScale;
            }

            return true;
        }

        private void FindReferences()
        {
            damageResolver ??= GetComponent<AreaDamageResolver2D>();
            damageArea ??= GetComponent<CircleDamageArea2D>();
        }
    }
}
