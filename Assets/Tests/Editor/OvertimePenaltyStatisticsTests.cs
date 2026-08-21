using BackpackHero.Battle;
using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class OvertimePenaltyStatisticsTests
{
    [Test]
    public void DamageSource_CanExcludeOvertimePenaltyFromDpsStatistics()
    {
        BattleDamageSource normal = new(null);
        BattleDamageSource overtimePenalty = new(null, false);

        Assert.That(normal.CountsForDamageStatistics, Is.True);
        Assert.That(overtimePenalty.CountsForDamageStatistics, Is.False);
    }

    [Test]
    public void ProjectileVisualSettings_UsesRedForOvertimePenalty()
    {
        ProjectileVisualSettings settings =
            ScriptableObject.CreateInstance<ProjectileVisualSettings>();

        try
        {
            Assert.That(settings.GetProjectileColor(
                BattleFaction.Player,
                ProjectileVisualSource.OvertimePenalty),
                Is.EqualTo(new Color(1f, 0.1f, 0.1f, 1f)));
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }
}
