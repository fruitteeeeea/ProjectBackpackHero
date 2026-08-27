using BackpackHero.Battle;
using NUnit.Framework;

public sealed class BattleResultDataTests
{
    [Test]
    public void CreateDefault_VictoryPreview_PreservesProvidedTotalScore()
    {
        BattleResultData data = BattleResultData.CreateDefault(true, 1234);

        Assert.That(data.TotalScore, Is.EqualTo(1234));
        Assert.That(data.ScoreDelta, Is.EqualTo(30));
    }

    [Test]
    public void CreateDefault_DefeatPreview_PreservesProvidedTotalScore()
    {
        BattleResultData data = BattleResultData.CreateDefault(false, 5678);

        Assert.That(data.TotalScore, Is.EqualTo(5678));
        Assert.That(data.ScoreDelta, Is.EqualTo(-10));
    }
}
