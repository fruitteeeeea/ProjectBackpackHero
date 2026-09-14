using System;
using BackpackHero.Progression;
using BackpackPrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

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
    public void Catalog_HasTwentyTwoStagesWithAllFiveSharedDeckCandidates()
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
                Assert.That(stage.deckPresets, Has.Length.EqualTo(5));
                foreach (DeckPreset deckPreset in stage.deckPresets)
                    Assert.That(deckPreset.Slots, Has.Count.EqualTo(5));
            }
        }
        Assert.That(index, Is.EqualTo(expectedLevels.Length));
    }

    [Test]
    public void EveryStage_UsesAllFiveSharedDeckPresets()
    {
        DeckPreset[] expectedDecks =
        {
            LoadDeck("DeckPreset_01"), LoadDeck("DeckPreset_02"),
            LoadDeck("DeckPreset_03"), LoadDeck("DeckPreset_04"),
            LoadDeck("DeckPreset_05")
        };

        foreach (RankEnemyDifficultyRank rank in LoadCatalog().Ranks)
        foreach (RankEnemyDifficultyStage stage in rank.stages)
            CollectionAssert.AreEquivalent(expectedDecks, stage.deckPresets);
    }

    [Test]
    public void Resolver_RandomlySelectsOnlyCurrentStageCandidates()
    {
        RankEnemyDifficultyCatalog catalog = LoadCatalog();
        RankEnemyDifficultyStage stage = catalog.FindRank(1).stages[0];
        Random.State previousRandomState = Random.state;
        try
        {
            Random.InitState(20260903);
            for (int attempt = 0; attempt < 50; attempt++)
            {
                Assert.That(RankEnemyDifficultyResolver.TryResolve(catalog, 0,
                    1001, null, out EnemyMatchProfile profile), Is.True);
                CollectionAssert.Contains(stage.deckPresets, profile.DeckPreset);
                Assert.That(profile.DeckPreset.IsValid(out string error), Is.True, error);
            }
        }
        finally { Random.state = previousRandomState; }
    }

    [Test]
    public void FinalRank_HasOneReachableLevelTenStageWithAllDeckCandidates()
    {
        RankEnemyDifficultyRank finalRank = LoadCatalog().FindRank(8);

        Assert.That(finalRank.stages, Has.Length.EqualTo(1));
        Assert.That(finalRank.stages[0].progressionLevel, Is.EqualTo(10));
        Assert.That(finalRank.stages[0].deckPresets, Has.Length.EqualTo(5));
    }

    private static RankEnemyDifficultyCatalog LoadCatalog()
    {
        RankEnemyDifficultyCatalog catalog =
            AssetDatabase.LoadAssetAtPath<RankEnemyDifficultyCatalog>(CatalogPath);
        Assert.That(catalog, Is.Not.Null);
        return catalog;
    }

    private static DeckPreset LoadDeck(string deckName) =>
        AssetDatabase.LoadAssetAtPath<DeckPreset>(
            $"Assets/Data/Backpack/DeckPresets/{deckName}.asset");
}
