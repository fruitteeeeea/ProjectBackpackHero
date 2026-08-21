using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>可持久化的全局飞机视觉开关。</summary>
    [CreateAssetMenu(
        fileName = "AircraftVisualDebugSettings",
        menuName = "Debug/Aircraft Visual Settings")]
    public sealed class AircraftVisualDebugSettings : ScriptableObject
    {
        [Header("Global")]
        [SerializeField] private bool aircraftVisualOverridesEnabled = true;

        [Header("Attack")]
        [SerializeField] private bool attackScaleTween = true;

        [Header("Hit")]
        [SerializeField] private bool hitRotationShakeTween = true;
        [SerializeField] private bool hitParticles = true;
        [SerializeField] private bool hitWhiteFlash = true;
        [SerializeField, Min(0f)] private float hitParticlesStartSpeed = 8.8f;
        [SerializeField] private Texture2D hitParticlesTexture;
        [SerializeField, Min(0f)] private float hitParticlesStartSize = 0.5f;
        [SerializeField, Min(0)] private int hitParticlesBurstCount = 3;

        [Header("Death")]
        [SerializeField] private bool deathRotationTween = true;
        [SerializeField] private bool deathScaleTween = true;
        [SerializeField] private bool deathExplosionParticles = true;
        [SerializeField] private bool deathFlashParticles = true;

        [Header("Flight")]
        [SerializeField] private bool aircraftLifetimeEnabled = true;
        [SerializeField]
        private bool highlightOvertimePenaltyProjectile;

        [Header("Death Particles")]
        [SerializeField, Min(0f)]
        private float deathExplosionParticlesStartSpeed = 8.8f;
        [SerializeField] private Texture2D deathExplosionParticlesTexture;
        [SerializeField, Min(0f)]
        private float deathExplosionParticlesStartSize = 2.5f;
        [SerializeField, Min(0)] private int deathExplosionParticlesBurstCount = 6;
        [SerializeField, Min(0f)]
        private float deathFlashParticlesStartSpeed;
        [SerializeField] private Texture2D deathFlashParticlesTexture;
        [SerializeField, Min(0f)]
        private float deathFlashParticlesStartSize = 3f;
        [SerializeField, Min(0)] private int deathFlashParticlesBurstCount = 1;

        [Header("Explosive Projectile")]
        [SerializeField, Range(
            AircraftVisualSettings.MinimumExplosiveImpactRangeMultiplier,
            AircraftVisualSettings.MaximumExplosiveImpactRangeMultiplier)]
        private float explosiveImpactRangeMultiplier = 1f;

        public void SetValues(AircraftVisualSettings values)
        {
            aircraftVisualOverridesEnabled =
                values.AircraftVisualOverridesEnabled;
            attackScaleTween = values.AttackScaleTween;
            hitRotationShakeTween = values.HitRotationShakeTween;
            hitParticles = values.HitParticles;
            hitWhiteFlash = values.HitWhiteFlash;
            deathRotationTween = values.DeathRotationTween;
            deathScaleTween = values.DeathScaleTween;
            deathExplosionParticles = values.DeathExplosionParticles;
            deathFlashParticles = values.DeathFlashParticles;
            aircraftLifetimeEnabled = values.AircraftLifetimeEnabled;
            highlightOvertimePenaltyProjectile =
                values.HighlightOvertimePenaltyProjectile;
            hitParticlesStartSpeed = Mathf.Max(0f,
                values.HitParticlesStartSpeed);
            hitParticlesTexture = values.HitParticlesTexture;
            hitParticlesStartSize = Mathf.Max(0f,
                values.HitParticlesStartSize);
            hitParticlesBurstCount = Mathf.Max(0,
                values.HitParticlesBurstCount);
            deathExplosionParticlesStartSpeed = Mathf.Max(0f,
                values.DeathExplosionParticlesStartSpeed);
            deathExplosionParticlesTexture =
                values.DeathExplosionParticlesTexture;
            deathExplosionParticlesStartSize = Mathf.Max(0f,
                values.DeathExplosionParticlesStartSize);
            deathExplosionParticlesBurstCount = Mathf.Max(0,
                values.DeathExplosionParticlesBurstCount);
            deathFlashParticlesStartSpeed = Mathf.Max(0f,
                values.DeathFlashParticlesStartSpeed);
            deathFlashParticlesTexture = values.DeathFlashParticlesTexture;
            deathFlashParticlesStartSize = Mathf.Max(0f,
                values.DeathFlashParticlesStartSize);
            deathFlashParticlesBurstCount = Mathf.Max(0,
                values.DeathFlashParticlesBurstCount);
            explosiveImpactRangeMultiplier = Mathf.Clamp(
                values.ExplosiveImpactRangeMultiplier,
                AircraftVisualSettings.MinimumExplosiveImpactRangeMultiplier,
                AircraftVisualSettings.MaximumExplosiveImpactRangeMultiplier);
        }

        public AircraftVisualSettings GetValues() => new(
            aircraftVisualOverridesEnabled,
            attackScaleTween,
            hitRotationShakeTween,
            hitParticles,
            hitWhiteFlash,
            deathRotationTween,
            deathScaleTween,
            deathExplosionParticles,
            deathFlashParticles,
            aircraftLifetimeEnabled,
            highlightOvertimePenaltyProjectile,
            Mathf.Max(0f, hitParticlesStartSpeed),
            hitParticlesTexture,
            Mathf.Max(0f, hitParticlesStartSize),
            Mathf.Max(0, hitParticlesBurstCount),
            Mathf.Max(0f, deathExplosionParticlesStartSpeed),
            deathExplosionParticlesTexture,
            Mathf.Max(0f, deathExplosionParticlesStartSize),
            Mathf.Max(0, deathExplosionParticlesBurstCount),
            Mathf.Max(0f, deathFlashParticlesStartSpeed),
            deathFlashParticlesTexture,
            Mathf.Max(0f, deathFlashParticlesStartSize),
            Mathf.Max(0, deathFlashParticlesBurstCount),
            Mathf.Clamp(
                explosiveImpactRangeMultiplier,
                AircraftVisualSettings.MinimumExplosiveImpactRangeMultiplier,
                AircraftVisualSettings.MaximumExplosiveImpactRangeMultiplier));

#if UNITY_EDITOR
        private void OnValidate() => SetValues(GetValues());
#endif
    }
}
