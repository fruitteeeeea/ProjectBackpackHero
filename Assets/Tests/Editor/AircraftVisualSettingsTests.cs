using BackpackHero.Debugging;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AircraftVisualSettingsTests
{
    [Test]
    public void Default_EnablesEveryAircraftVisualEffect()
    {
        AircraftVisualSettings settings = AircraftVisualSettings.Default;

        Assert.That(settings.AircraftVisualOverridesEnabled, Is.True);
        Assert.That(settings.AttackScaleTween, Is.True);
        Assert.That(settings.HitRotationShakeTween, Is.True);
        Assert.That(settings.HitParticles, Is.True);
        Assert.That(settings.HitWhiteFlash, Is.True);
        Assert.That(settings.DeathRotationTween, Is.True);
        Assert.That(settings.DeathScaleTween, Is.True);
        Assert.That(settings.DeathExplosionParticles, Is.True);
        Assert.That(settings.DeathFlashParticles, Is.True);
        Assert.That(settings.AircraftLifetimeEnabled, Is.True);
        Assert.That(settings.HighlightOvertimePenaltyProjectile, Is.False);
        Assert.That(settings.HitParticlesStartSpeed, Is.EqualTo(8.8f));
        Assert.That(settings.HitParticlesStartSize, Is.EqualTo(0.5f));
        Assert.That(settings.HitParticlesBurstCount, Is.EqualTo(3));
        Assert.That(settings.DeathExplosionParticlesStartSpeed,
            Is.EqualTo(8.8f));
        Assert.That(settings.DeathExplosionParticlesStartSize,
            Is.EqualTo(2.5f));
        Assert.That(settings.DeathExplosionParticlesBurstCount, Is.EqualTo(6));
        Assert.That(settings.DeathFlashParticlesStartSpeed, Is.Zero);
        Assert.That(settings.DeathFlashParticlesStartSize, Is.EqualTo(3f));
        Assert.That(settings.DeathFlashParticlesBurstCount, Is.EqualTo(1));
        Assert.That(settings.ExplosiveImpactRangeMultiplier, Is.EqualTo(1f));
    }

    [Test]
    public void SettingsAsset_RoundTripsAndClampsParticleParameters()
    {
        AircraftVisualDebugSettings asset =
            ScriptableObject.CreateInstance<AircraftVisualDebugSettings>();
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Art/Images/VFX/Particles/symbol_02.png");
        AircraftVisualSettings expected = new(
            false, false, true, false, false, true, false, true, false, false,
            true,
            -1f, texture, -1.5f, -2,
            3.5f, texture, 2.25f, 9,
            -2f, null, -3f, -4,
            -5f);
        try
        {
            asset.SetValues(expected);

            AircraftVisualSettings actual = asset.GetValues();
            Assert.That(actual.AircraftVisualOverridesEnabled, Is.False);
            Assert.That(actual.AttackScaleTween, Is.False);
            Assert.That(actual.HitWhiteFlash, Is.False);
            Assert.That(actual.AircraftLifetimeEnabled, Is.False);
            Assert.That(actual.HighlightOvertimePenaltyProjectile, Is.True);
            Assert.That(actual.DeathExplosionParticlesStartSpeed,
                Is.EqualTo(3.5f));
            Assert.That(actual.HitParticlesStartSpeed, Is.Zero);
            Assert.That(actual.HitParticlesStartSize, Is.Zero);
            Assert.That(actual.HitParticlesBurstCount, Is.Zero);
            Assert.That(actual.DeathExplosionParticlesStartSize,
                Is.EqualTo(2.25f));
            Assert.That(actual.DeathExplosionParticlesBurstCount,
                Is.EqualTo(9));
            Assert.That(actual.DeathFlashParticlesStartSpeed, Is.Zero);
            Assert.That(actual.DeathFlashParticlesStartSize, Is.Zero);
            Assert.That(actual.DeathFlashParticlesBurstCount, Is.Zero);
            Assert.That(actual.HitParticlesTexture, Is.EqualTo(texture));
            Assert.That(actual.DeathFlashParticlesTexture, Is.Null);
            Assert.That(actual.ExplosiveImpactRangeMultiplier,
                Is.EqualTo(
                    AircraftVisualSettings
                        .MinimumExplosiveImpactRangeMultiplier));
            Assert.That(
                AircraftVisualSettings.Default
                    .WithExplosiveImpactRangeMultiplier(5f)
                    .ExplosiveImpactRangeMultiplier,
                Is.EqualTo(
                    AircraftVisualSettings
                        .MaximumExplosiveImpactRangeMultiplier));
            Assert.That(
                AircraftVisualSettings.Default
                    .WithAircraftVisualOverridesEnabled(false)
                    .AircraftVisualOverridesEnabled,
                Is.False);
        }
        finally
        {
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void DefaultSettingsAndParticleAssets_ExistAtExposedPaths()
    {
        AircraftVisualDebugSettings settings =
            AssetDatabase.LoadAssetAtPath<AircraftVisualDebugSettings>(
                "Assets/Resources/AircraftVisual/AircraftVisualDebugSettings.asset");
        Assert.That(settings, Is.Not.Null);
        AircraftVisualSettings values = settings.GetValues();
        Assert.That(values.HitParticlesStartSpeed, Is.GreaterThanOrEqualTo(0f));
        Assert.That(values.HitParticlesStartSize, Is.GreaterThanOrEqualTo(0f));
        Assert.That(values.HitParticlesBurstCount, Is.GreaterThanOrEqualTo(0));
        Assert.That(values.DeathExplosionParticlesStartSpeed,
            Is.GreaterThanOrEqualTo(0f));
        Assert.That(values.DeathExplosionParticlesStartSize,
            Is.GreaterThanOrEqualTo(0f));
        Assert.That(values.DeathExplosionParticlesBurstCount,
            Is.GreaterThanOrEqualTo(0));
        Assert.That(values.DeathFlashParticlesStartSpeed,
            Is.GreaterThanOrEqualTo(0f));
        Assert.That(values.DeathFlashParticlesStartSize,
            Is.GreaterThanOrEqualTo(0f));
        Assert.That(values.DeathFlashParticlesBurstCount,
            Is.GreaterThanOrEqualTo(0));
        Assert.That(values.HighlightOvertimePenaltyProjectile, Is.False);
        Assert.That(values.HitParticlesTexture, Is.Not.Null);
        Assert.That(values.DeathExplosionParticlesTexture, Is.Not.Null);
        Assert.That(values.DeathFlashParticlesTexture, Is.Not.Null);
        Assert.That(values.ExplosiveImpactRangeMultiplier,
            Is.InRange(
                AircraftVisualSettings.MinimumExplosiveImpactRangeMultiplier,
                AircraftVisualSettings.MaximumExplosiveImpactRangeMultiplier));
        Assert.That(
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/VFX/Particles/VFX_Particles_AircraftExplosion.prefab"),
            Is.Not.Null);
        Assert.That(
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/VFX/Particles/VFX_Particles_AircraftExplosion 2.prefab"),
            Is.Not.Null);
        Assert.That(
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/VFX/Particles/VFX_Particles_DeathFlash.prefab"),
            Is.Not.Null);
    }

    [Test]
    public void FighterNeonShader_ExposesPerRendererFlashAmount()
    {
        Shader shader = Shader.Find(
            "BackpackHero/Graphics/Neon Sprite Outline");

        Assert.That(shader, Is.Not.Null);
        Material material = new Material(shader);
        try
        {
            Assert.That(material.HasProperty("_FlashAmount"), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(material);
        }
    }
}
