using BackpackHero.Battle;
using NUnit.Framework;
using UnityEngine;

public sealed class LevelDifficultySettingsTests
{
    [Test]
    public void DefaultSettings_UseNeutralTenLevelStrengthTable()
    {
        LevelDifficultySettings settings = ScriptableObject.CreateInstance<LevelDifficultySettings>();
        try
        {
            for (int level = 1;
                 level <= LevelDifficultySettings.LevelCount;
                 level++)
            {
                for (int stage = 1;
                     stage <= LevelDifficultySettings.StageCount;
                     stage++)
                {
                    EnemyStrengthMultipliers strength =
                        settings.GetEnemyStrength(level, stage);
                    Assert.That(strength.Health, Is.EqualTo(1f));
                    Assert.That(strength.Damage, Is.EqualTo(1f));
                }
            }
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
    public void BackpackRoundHealthMultipliers_UseSixConfiguredValues()
    {
        LevelDifficultySettings settings =
            ScriptableObject.CreateInstance<
                LevelDifficultySettings>();
        try
        {
            float[] expected =
                { .75f, .8f, .85f, .9f, .95f, 1f };

            for (int round = 1;
                 round <= expected.Length;
                 round++)
            {
                Assert.That(
                    settings.GetBackpackRoundHealthMultiplier(
                        round),
                    Is.EqualTo(expected[round - 1]));
            }

            Assert.That(
                settings.GetBackpackRoundHealthMultiplier(0),
                Is.EqualTo(.75f));
            Assert.That(
                settings.GetBackpackRoundHealthMultiplier(99),
                Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    [Test]
    public void CopyFrom_PreservesPlayerAndEnemyMultipliers()
    {
        LevelDifficultySettings source = ScriptableObject.CreateInstance<LevelDifficultySettings>();
        LevelDifficultySettings copy = ScriptableObject.CreateInstance<LevelDifficultySettings>();
        try
        {
            source.SetPlayerMultipliers(1.25f, 1.5f);
            source.SetBackpackRoundHealthMultiplier(4, .72f);
            source.SetEnemyStrength(4, 1, .7f, .8f);
            copy.CopyFrom(source);
            Assert.That(copy.ContentEquals(source), Is.True);
            Assert.That(copy.PlayerAircraftHealthMultiplier, Is.EqualTo(1.25f));
            Assert.That(
                copy.GetBackpackRoundHealthMultiplier(4),
                Is.EqualTo(.72f));
            Assert.That(copy.GetEnemyStrength(4, 1).Damage, Is.EqualTo(.8f));
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(copy);
        }
    }
}
