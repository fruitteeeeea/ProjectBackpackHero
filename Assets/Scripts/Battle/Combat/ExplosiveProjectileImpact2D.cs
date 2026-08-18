using UnityEngine;

namespace BackpackHero.Battle
{
    [RequireComponent(typeof(AreaDamageResolver2D))]
    [RequireComponent(typeof(CircleDamageArea2D))]
    public sealed class ExplosiveProjectileImpact2D : ProjectileImpactEffect2D
    {
        [SerializeField, Min(0f)] private float radius = 1.5f;
        [SerializeField, Range(0f, 1f)] private float areaDamageMultiplier = 1f;
        [Header("Visual")]
        [SerializeField] private ParticleSystem impactVfxPrefab;
        [SerializeField, Min(0.01f)] private float impactVfxScale = 1f;
        [SerializeField] private Color playerImpactVfxColor =
            new Color(0.55f, 0.85f, 1f, 1f);
        [SerializeField] private Color enemyImpactVfxColor =
            new Color(1f, 0.55f, 0.55f, 1f);
        [SerializeField] private AreaDamageResolver2D damageResolver;
        [SerializeField] private CircleDamageArea2D damageArea;

        public float Radius => radius;
        public float AreaDamageMultiplier => areaDamageMultiplier;

        private void Awake() => FindReferences();

        public override bool ResolveImpact(
            HurtBox2D initialTarget,
            Vector2 impactPosition,
            BattleFaction attackerFaction,
            float damage,
            BattleDamageSource damageSource = default)
        {
            FindReferences();
            if (damageResolver == null || damageArea == null)
            {
                return false;
            }

            damageArea.Configure(impactPosition, radius);
            damageResolver.Resolve(
                damageArea,
                attackerFaction,
                damage * areaDamageMultiplier,
                damageSource);

            if (impactVfxPrefab != null)
            {
                ParticleSystem impactVfx = Instantiate(
                    impactVfxPrefab,
                    impactPosition,
                    impactVfxPrefab.transform.rotation);

                impactVfx.transform.localScale =
                    Vector3.one * impactVfxScale;

                ApplyFactionColor(impactVfx, attackerFaction);
            }

            return true;
        }

        private void FindReferences()
        {
            damageResolver ??= GetComponent<AreaDamageResolver2D>();
            damageArea ??= GetComponent<CircleDamageArea2D>();
        }

        private void ApplyFactionColor(
            ParticleSystem impactVfx,
            BattleFaction attackerFaction)
        {
            Color factionColor =
                attackerFaction == BattleFaction.Player
                    ? playerImpactVfxColor
                    : enemyImpactVfxColor;

            foreach (ParticleSystem particleSystem in
                     impactVfx.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = factionColor;
            }
        }
    }
}
