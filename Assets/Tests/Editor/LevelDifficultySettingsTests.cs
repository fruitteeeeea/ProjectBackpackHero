using BackpackHero.Battle;
using NUnit.Framework;
using UnityEngine;

public sealed class LevelDifficultySettingsTests
{
    [Test]
    public void DefaultSettings_UseRequestedFiveLevelStrengthTable()
    {
        LevelDifficultySettings settings = ScriptableObject.CreateInstance<LevelDifficultySettings>();
        try
        {
            Assert.That(settings.GetEnemyStrength(1, 1).Health, Is.EqualTo(.79f));
            Assert.That(settings.GetEnemyStrength(1, 2).Damage, Is.EqualTo(.81f));
            Assert.That(settings.GetEnemyStrength(3, 4).Health, Is.EqualTo(.89f));
            Assert.That(settings.GetEnemyStrength(5, 2).Damage, Is.EqualTo(.93f));
            Assert.That(settings.GetEnemyStrength(5, 5).Health, Is.EqualTo(.95f));
        }
        finally { Object.DestroyImmediate(settings); }
    }

    [Test]
    public void ThirdStage_IsUsedForAllRoundsFromThreeOnward()
    {
        LevelDifficultySettings settings = ScriptableObject.CreateInstance<LevelDifficultySettings>();
        try
        {
            EnemyStrengthMultipliers third = settings.GetEnemyStrength(2, 3);
            EnemyStrengthMultipliers fifth = settings.GetEnemyStrength(2, 5);
            Assert.That(fifth.Health, Is.EqualTo(third.Health));
            Assert.That(fifth.Damage, Is.EqualTo(third.Damage));
        }
        finally { Object.DestroyImmediate(settings); }
    }

    [Test]
    public void CopyFrom_PreservesPlayerAndEnemyMultipliers()
    {
        LevelDifficultySettings source = ScriptableObject.CreateInstance<LevelDifficultySettings>();
        LevelDifficultySettings copy = ScriptableObject.CreateInstance<LevelDifficultySettings>();
        try
        {
            source.SetPlayerMultipliers(1.25f, 1.5f);
            source.SetEnemyStrength(4, 1, .7f, .8f);
            copy.CopyFrom(source);
            Assert.That(copy.ContentEquals(source), Is.True);
            Assert.That(copy.PlayerAircraftHealthMultiplier, Is.EqualTo(1.25f));
            Assert.That(copy.GetEnemyStrength(4, 1).Damage, Is.EqualTo(.8f));
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(copy);
        }
    }
}
