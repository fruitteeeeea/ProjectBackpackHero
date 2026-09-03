using System;
using BackpackHero.Progression;
using BackpackPrototype;
using NUnit.Framework;
using UnityEditor;

public sealed class RankEnemyDifficultyTests
{
    private const string CatalogPath =
        "Assets/Resources/RankEnemyDifficultyCatalog.asset";

    [TestCase(0, 1, 1, 1)]
    [TestCase(199, 1, 3, 2)]
    [TestCase(200, 2, 1, 2)]
    [TestCase(999, 2, 3, 3)]
    [TestCase(1000, 3, 1, 3)]
    [TestCase(49999, 7, 3, 9)]
    [TestCase(50000, 8, 1, 10)]
    public void ScoreBoundaries_ResolveToTheConfiguredEnemyProfile(
        int score, int rank, int stage, int progressionLevel)
    {
        RankEnemyDifficultyCatalog catalog = LoadCatalog();

        Assert.That(RankEnemyDifficultyResolver.TryResolve(catalog, score,
            1001, null, out EnemyMatchProfile profile), Is.True);
        Assert.That(profile.Rank, Is.EqualTo(rank));
        Assert.That(profile.RawStage, Is.EqualTo(stage));
        Assert.That(profile.EffectiveStage, Is.EqualTo(stage));
        Assert.That(profile.ProgressionLevel, Is.EqualTo(progressionLevel));
        Assert.That(profile.DeckPreset, Is.Not.Null);
    }

    [Test]
    public void TwoDefeatProtection_LowersExactlyOneStage()
    {
        RankEnemyDifficultyCatalog catalog = LoadCatalog();
        var recovery = new RankDefeatRecoveryState
        {
            nodeId = 1004,
            consecutiveDefeats = 2,
            downgradePending = true
        };

        Assert.That(RankEnemyDifficultyResolver.TryResolve(catalog, 999,
            1004, recovery, out EnemyMatchProfile profile), Is.True);
        Assert.That(profile.RawStage, Is.EqualTo(3));
        Assert.That(profile.EffectiveStage, Is.EqualTo(2));
        Assert.That(profile.ProgressionLevel, Is.EqualTo(3));
    }

    [Test]
    public void Catalog_HasTwentyTwoFullDeckStages_WithAOneStageFinalRank()
    {
        RankEnemyDifficultyCatalog catalog = LoadCatalog();
        Assert.That(catalog.IsValid(out string error), Is.True, error);
        Assert.That(catalog.Ranks, Has.Count.EqualTo(8));
        int[] expectedLevels =
        {
            1, 1, 2, 2, 3, 3, 3, 4, 4, 5, 5, 5,
            6, 6, 6, 7, 7, 8, 8, 8, 9, 10
        };
        int index = 0;
        foreach (RankEnemyDifficultyRank rank in catalog.Ranks)
        {
            int expectedStageCount = rank.rank == 8 ? 1 : 3;
            Assert.That(rank.stages, Has.Length.EqualTo(expectedStageCount));
            foreach (RankEnemyDifficultyStage stage in rank.stages)
            {
                Assert.That(stage.progressionLevel,
                    Is.EqualTo(expectedLevels[index++]));
                Assert.That(stage.progressionLevel, Is.InRange(1, 10));
                Assert.That(stage.deckPreset.Slots, Has.Count.EqualTo(5));
            }
        }
        Assert.That(index, Is.EqualTo(expectedLevels.Length));
    }

    [Test]
    public void RankOne_DoesNotUseItemsUnlockedInLaterRanks()
    {
        RankEnemyDifficultyRank rank = LoadCatalog().FindRank(1);
        string[] futureItems =
        {
            "aircraft_explosive", "equipment_rapid_cannon",
            "aircraft_sniper", "equipment_wave_emitter",
            "aircraft_laser", "equipment_laser_link",
            "aircraft_shotgun", "equipment_first", "aircraft_l"
        };

        foreach (RankEnemyDifficultyStage stage in rank.stages)
        foreach (ItemData item in stage.deckPreset.Slots)
        {
            Assert.That(Array.IndexOf(futureItems, item.ItemId), Is.EqualTo(-1));
        }
    }

    [Test]
    public void TwinFormation_FirstAppearsAtRankSix()
    {
        RankEnemyDifficultyCatalog catalog = LoadCatalog();
        for (int rankNumber = 1; rankNumber < 6; rankNumber++)
        foreach (RankEnemyDifficultyStage stage in catalog.FindRank(rankNumber).stages)
        foreach (ItemData item in stage.deckPreset.Slots)
        {
            Assert.That(item.ItemId, Is.Not.EqualTo("aircraft_l"));
        }

        bool rankSixHasTwinFormation = false;
        foreach (ItemData item in catalog.FindRank(6).stages[0].deckPreset.Slots)
            rankSixHasTwinFormation |= item.ItemId == "aircraft_l";
        Assert.That(rankSixHasTwinFormation, Is.True);
    }

    [Test]
    public void FinalRank_HasOneReachableLevelTenLinkFormation()
    {
        RankEnemyDifficultyRank finalRank = LoadCatalog().FindRank(8);

        Assert.That(finalRank.stages, Has.Length.EqualTo(1));
        Assert.That(finalRank.stages[0].progressionLevel, Is.EqualTo(10));
        bool hasLaserLink = false;
        foreach (ItemData item in finalRank.stages[0].deckPreset.Slots)
            hasLaserLink |= item.ItemId == "equipment_laser_link";
        Assert.That(hasLaserLink, Is.True);
    }

    private static RankEnemyDifficultyCatalog LoadCatalog()
    {
        RankEnemyDifficultyCatalog catalog =
            AssetDatabase.LoadAssetAtPath<RankEnemyDifficultyCatalog>(CatalogPath);
        Assert.That(catalog, Is.Not.Null);
        return catalog;
    }
}
