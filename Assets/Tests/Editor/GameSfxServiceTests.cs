using BackpackHero.Audio;
using NUnit.Framework;

public sealed class GameSfxServiceTests
{
    [Test]
    public void ProjectileSfxPriority_IsLaserThenSpreadThenEquipmentThenDefault()
    {
        Assert.That(GameSfxService.ResolveProjectileSfxId(false, false, false), Is.EqualTo(GameSfxId.DefaultProjectile));
        Assert.That(GameSfxService.ResolveProjectileSfxId(true, false, false), Is.EqualTo(GameSfxId.EquipmentProjectile));
        Assert.That(GameSfxService.ResolveProjectileSfxId(true, true, false), Is.EqualTo(GameSfxId.SpreadProjectile));
        Assert.That(GameSfxService.ResolveProjectileSfxId(true, true, true), Is.EqualTo(GameSfxId.LaserProjectile));
    }

    [Test]
    public void RateLimiter_LimitsOnlyTheSameSfxType()
    {
        GameSfxRateLimiter limiter = new();

        Assert.That(limiter.CanPlay(GameSfxId.DefaultProjectile, 1f, 0.1f), Is.True);
        Assert.That(limiter.CanPlay(GameSfxId.DefaultProjectile, 1.05f, 0.1f), Is.False);
        Assert.That(limiter.CanPlay(GameSfxId.EquipmentProjectile, 1.05f, 0.1f), Is.True);
        Assert.That(limiter.CanPlay(GameSfxId.DefaultProjectile, 1.1f, 0.1f), Is.True);
    }
}
