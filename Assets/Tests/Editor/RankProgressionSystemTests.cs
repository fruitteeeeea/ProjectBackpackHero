using BackpackHero.Progression;
using NUnit.Framework;
using UnityEngine;

public sealed class RankProgressionSystemTests
{
    private GameObject root;

    [SetUp]
    public void SetUp()
    {
        if (RankProgressionSystem.Instance != null)
            Object.DestroyImmediate(RankProgressionSystem.Instance.gameObject);
        PlayerPrefs.DeleteKey(RankProgressionSystem.SaveKey);
        PlayerPrefs.Save();
        root = new GameObject("RankProgressionSystemTests");
    }

    [TearDown]
    public void TearDown()
    {
        if (RankProgressionSystem.Instance != null)
            Object.DestroyImmediate(RankProgressionSystem.Instance.gameObject);
        PlayerPrefs.DeleteKey(RankProgressionSystem.SaveKey);
        PlayerPrefs.Save();
        if (root != null) Object.DestroyImmediate(root);
    }

    [Test]
    public void DebugRestores_UseInitialAndMaximumRankStates()
    {
        RankProgressionSystem system = root.AddComponent<RankProgressionSystem>();

        system.RestoreDefaultProgression();

        Assert.That(system.Points, Is.EqualTo(RankProgressionSystem.MaximumPoints));
        Assert.That(system.CurrentNodeId,
            Is.EqualTo(system.Catalog.Nodes[system.Catalog.Nodes.Count - 1].id));
        Assert.That(system.DefeatRecovery.DowngradePending, Is.False);

        system.RestoreInitialProgression();

        Assert.That(system.Points, Is.EqualTo(RankProgressionSystem.InitialPoints));
        Assert.That(system.CurrentNodeId, Is.EqualTo(system.Catalog.Nodes[0].id));
        Assert.That(system.Wins, Is.Zero);
        Assert.That(system.DefeatRecovery.DowngradePending, Is.False);
    }
}
