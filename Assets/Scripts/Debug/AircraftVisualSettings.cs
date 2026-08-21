using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>飞机视觉运行时快照，默认值保持全部视觉开启。</summary>
    public readonly struct AircraftVisualSettings :
        System.IEquatable<AircraftVisualSettings>
    {
        public const float MinimumExplosiveImpactRangeMultiplier = 0.5f;
        public const float MaximumExplosiveImpactRangeMultiplier = 1.5f;

        public static AircraftVisualSettings Default => new(
            true, true, true, true, true, true, true, true, true, true,
            false,
            8.8f, null, 0.5f, 3,
            8.8f, null, 2.5f, 6,
            0f, null, 3f, 1,
            1f);

        public AircraftVisualSettings(
            bool aircraftVisualOverridesEnabled,
            bool attackScaleTween,
            bool hitRotationShakeTween,
            bool hitParticles,
            bool hitWhiteFlash,
            bool deathRotationTween,
            bool deathScaleTween,
            bool deathExplosionParticles,
            bool deathFlashParticles,
            bool aircraftLifetimeEnabled,
            bool highlightOvertimePenaltyProjectile,
            float hitParticlesStartSpeed,
            Texture2D hitParticlesTexture,
            float hitParticlesStartSize,
            int hitParticlesBurstCount,
            float deathExplosionParticlesStartSpeed,
            Texture2D deathExplosionParticlesTexture,
            float deathExplosionParticlesStartSize,
            int deathExplosionParticlesBurstCount,
            float deathFlashParticlesStartSpeed,
            Texture2D deathFlashParticlesTexture,
            float deathFlashParticlesStartSize,
            int deathFlashParticlesBurstCount,
            float explosiveImpactRangeMultiplier)
        {
            AircraftVisualOverridesEnabled = aircraftVisualOverridesEnabled;
            AttackScaleTween = attackScaleTween;
            HitRotationShakeTween = hitRotationShakeTween;
            HitParticles = hitParticles;
            HitWhiteFlash = hitWhiteFlash;
            DeathRotationTween = deathRotationTween;
            DeathScaleTween = deathScaleTween;
            DeathExplosionParticles = deathExplosionParticles;
            DeathFlashParticles = deathFlashParticles;
            AircraftLifetimeEnabled = aircraftLifetimeEnabled;
            HighlightOvertimePenaltyProjectile =
                highlightOvertimePenaltyProjectile;
            HitParticlesStartSpeed = hitParticlesStartSpeed;
            HitParticlesTexture = hitParticlesTexture;
            HitParticlesStartSize = Mathf.Max(0f, hitParticlesStartSize);
            HitParticlesBurstCount = Mathf.Max(0, hitParticlesBurstCount);
            DeathExplosionParticlesStartSpeed =
                deathExplosionParticlesStartSpeed;
            DeathExplosionParticlesTexture =
                deathExplosionParticlesTexture;
            DeathExplosionParticlesStartSize = Mathf.Max(0f,
                deathExplosionParticlesStartSize);
            DeathExplosionParticlesBurstCount = Mathf.Max(0,
                deathExplosionParticlesBurstCount);
            DeathFlashParticlesStartSpeed = deathFlashParticlesStartSpeed;
            DeathFlashParticlesTexture = deathFlashParticlesTexture;
            DeathFlashParticlesStartSize = Mathf.Max(0f,
                deathFlashParticlesStartSize);
            DeathFlashParticlesBurstCount = Mathf.Max(0,
                deathFlashParticlesBurstCount);
            ExplosiveImpactRangeMultiplier = Mathf.Clamp(
                explosiveImpactRangeMultiplier,
                MinimumExplosiveImpactRangeMultiplier,
                MaximumExplosiveImpactRangeMultiplier);
        }

        public bool AircraftVisualOverridesEnabled { get; }
        public bool AttackScaleTween { get; }
        public bool HitRotationShakeTween { get; }
        public bool HitParticles { get; }
        public bool HitWhiteFlash { get; }
        public bool DeathRotationTween { get; }
        public bool DeathScaleTween { get; }
        public bool DeathExplosionParticles { get; }
        public bool DeathFlashParticles { get; }
        public bool AircraftLifetimeEnabled { get; }
        public bool HighlightOvertimePenaltyProjectile { get; }
        public float HitParticlesStartSpeed { get; }
        public Texture2D HitParticlesTexture { get; }
        public float HitParticlesStartSize { get; }
        public int HitParticlesBurstCount { get; }
        public float DeathExplosionParticlesStartSpeed { get; }
        public Texture2D DeathExplosionParticlesTexture { get; }
        public float DeathExplosionParticlesStartSize { get; }
        public int DeathExplosionParticlesBurstCount { get; }
        public float DeathFlashParticlesStartSpeed { get; }
        public Texture2D DeathFlashParticlesTexture { get; }
        public float DeathFlashParticlesStartSize { get; }
        public int DeathFlashParticlesBurstCount { get; }
        public float ExplosiveImpactRangeMultiplier { get; }

        public AircraftVisualSettings WithExplosiveImpactRangeMultiplier(
            float value) => new(
            AircraftVisualOverridesEnabled,
            AttackScaleTween,
            HitRotationShakeTween,
            HitParticles,
            HitWhiteFlash,
            DeathRotationTween,
            DeathScaleTween,
            DeathExplosionParticles,
            DeathFlashParticles,
            AircraftLifetimeEnabled,
            HighlightOvertimePenaltyProjectile,
            HitParticlesStartSpeed,
            HitParticlesTexture,
            HitParticlesStartSize,
            HitParticlesBurstCount,
            DeathExplosionParticlesStartSpeed,
            DeathExplosionParticlesTexture,
            DeathExplosionParticlesStartSize,
            DeathExplosionParticlesBurstCount,
            DeathFlashParticlesStartSpeed,
            DeathFlashParticlesTexture,
            DeathFlashParticlesStartSize,
            DeathFlashParticlesBurstCount,
            value);

        public AircraftVisualSettings WithAircraftVisualOverridesEnabled(
            bool value) => new(
            value,
            AttackScaleTween,
            HitRotationShakeTween,
            HitParticles,
            HitWhiteFlash,
            DeathRotationTween,
            DeathScaleTween,
            DeathExplosionParticles,
            DeathFlashParticles,
            AircraftLifetimeEnabled,
            HighlightOvertimePenaltyProjectile,
            HitParticlesStartSpeed,
            HitParticlesTexture,
            HitParticlesStartSize,
            HitParticlesBurstCount,
            DeathExplosionParticlesStartSpeed,
            DeathExplosionParticlesTexture,
            DeathExplosionParticlesStartSize,
            DeathExplosionParticlesBurstCount,
            DeathFlashParticlesStartSpeed,
            DeathFlashParticlesTexture,
            DeathFlashParticlesStartSize,
            DeathFlashParticlesBurstCount,
            ExplosiveImpactRangeMultiplier);

        public bool Equals(AircraftVisualSettings other) =>
            AircraftVisualOverridesEnabled == other.AircraftVisualOverridesEnabled &&
            AttackScaleTween == other.AttackScaleTween &&
            HitRotationShakeTween == other.HitRotationShakeTween &&
            HitParticles == other.HitParticles &&
            HitWhiteFlash == other.HitWhiteFlash &&
            DeathRotationTween == other.DeathRotationTween &&
            DeathScaleTween == other.DeathScaleTween &&
            DeathExplosionParticles == other.DeathExplosionParticles &&
            DeathFlashParticles == other.DeathFlashParticles &&
            AircraftLifetimeEnabled == other.AircraftLifetimeEnabled &&
            HighlightOvertimePenaltyProjectile ==
                other.HighlightOvertimePenaltyProjectile &&
            Mathf.Approximately(HitParticlesStartSpeed,
                other.HitParticlesStartSpeed) &&
            HitParticlesTexture == other.HitParticlesTexture &&
            Mathf.Approximately(HitParticlesStartSize,
                other.HitParticlesStartSize) &&
            HitParticlesBurstCount == other.HitParticlesBurstCount &&
            Mathf.Approximately(DeathExplosionParticlesStartSpeed,
                other.DeathExplosionParticlesStartSpeed) &&
            DeathExplosionParticlesTexture ==
                other.DeathExplosionParticlesTexture &&
            Mathf.Approximately(DeathExplosionParticlesStartSize,
                other.DeathExplosionParticlesStartSize) &&
            DeathExplosionParticlesBurstCount ==
                other.DeathExplosionParticlesBurstCount &&
            Mathf.Approximately(DeathFlashParticlesStartSpeed,
                other.DeathFlashParticlesStartSpeed) &&
            DeathFlashParticlesTexture == other.DeathFlashParticlesTexture &&
            Mathf.Approximately(DeathFlashParticlesStartSize,
                other.DeathFlashParticlesStartSize) &&
            DeathFlashParticlesBurstCount == other.DeathFlashParticlesBurstCount &&
            Mathf.Approximately(ExplosiveImpactRangeMultiplier,
                other.ExplosiveImpactRangeMultiplier);

        public override bool Equals(object obj) =>
            obj is AircraftVisualSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = AircraftVisualOverridesEnabled ? 1 : 0;
                hash = (hash * 31) + (AttackScaleTween ? 1 : 0);
                hash = (hash * 31) + (HitRotationShakeTween ? 1 : 0);
                hash = (hash * 31) + (HitParticles ? 1 : 0);
                hash = (hash * 31) + (HitWhiteFlash ? 1 : 0);
                hash = (hash * 31) + (DeathRotationTween ? 1 : 0);
                hash = (hash * 31) + (DeathScaleTween ? 1 : 0);
                hash = (hash * 31) + (DeathExplosionParticles ? 1 : 0);
                hash = (hash * 31) + (DeathFlashParticles ? 1 : 0);
                hash = (hash * 31) + (AircraftLifetimeEnabled ? 1 : 0);
                hash = (hash * 31) +
                    (HighlightOvertimePenaltyProjectile ? 1 : 0);
                hash = (hash * 31) + HitParticlesStartSpeed.GetHashCode();
                hash = (hash * 31) + (HitParticlesTexture != null
                    ? HitParticlesTexture.GetHashCode() : 0);
                hash = (hash * 31) + HitParticlesStartSize.GetHashCode();
                hash = (hash * 31) + HitParticlesBurstCount;
                hash = (hash * 31) +
                    DeathExplosionParticlesStartSpeed.GetHashCode();
                hash = (hash * 31) + (DeathExplosionParticlesTexture != null
                    ? DeathExplosionParticlesTexture.GetHashCode() : 0);
                hash = (hash * 31) +
                    DeathExplosionParticlesStartSize.GetHashCode();
                hash = (hash * 31) + DeathExplosionParticlesBurstCount;
                hash = (hash * 31) +
                    DeathFlashParticlesStartSpeed.GetHashCode();
                hash = (hash * 31) + (DeathFlashParticlesTexture != null
                    ? DeathFlashParticlesTexture.GetHashCode() : 0);
                hash = (hash * 31) +
                    DeathFlashParticlesStartSize.GetHashCode();
                hash = (hash * 31) + DeathFlashParticlesBurstCount;
                return (hash * 31) +
                    ExplosiveImpactRangeMultiplier.GetHashCode();
            }
        }
    }
}
