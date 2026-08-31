using BackpackHero.Progression;
using NUnit.Framework;
using UnityEditor;

public sealed class RankEnemyDifficultyTests
{
    private const string CatalogPath =
        "Assets/Resources/RankEnemyDifficultyCatalog.asset";

    [TestCase(0, 1, 1, 1)]
    [TestCase(199, 1, 3, 1)]
    [TestCase(200, 2, 1, 1)]
    [TestCase(999, 2, 3, 2)]
    [TestCase(1000, 3, 1, 2)]
    [TestCase(49999, 7, 3, 7)]
    [TestCase(50000, 8, 3, 9)]
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
        Assert.That(profile.ProgressionLevel, Is.EqualTo(2));
    }

    [Test]
    public void Catalog_HasTwentyFourFullDeckStages_AndNeverUsesLevelTen()
    {
        RankEnemyDifficultyCatalog catalog = LoadCatalog();
        Assert.That(catalog.IsValid(out string error), Is.True, error);
        Assert.That(catalog.Ranks, Has.Count.EqualTo(8));
        foreach (RankEnemyDifficultyRank rank in catalog.Ranks)
        {
            Assert.That(rank.stages, Has.Length.EqualTo(3));
            foreach (RankEnemyDifficultyStage stage in rank.stages)
            {
                Assert.That(stage.progressionLevel, Is.InRange(1, 9));
                Assert.That(stage.deckPreset.Slots, Has.Count.EqualTo(5));
            }
        }
    }

    private static RankEnemyDifficultyCatalog LoadCatalog()
    {
        RankEnemyDifficultyCatalog catalog =
            AssetDatabase.LoadAssetAtPath<RankEnemyDifficultyCatalog>(CatalogPath);
        Assert.That(catalog, Is.Not.Null);
        return catalog;
    }
}
