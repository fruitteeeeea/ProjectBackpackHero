using BackpackHero.Progression;
using BackpackPrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RankProgressionCatalogTests
{
    [Test]
    public void ResourceCatalog_MatchesPlanetWarMilestonesRewardsAndCardUnlocks()
    {
        RankProgressionCatalog catalog = AssetDatabase.LoadAssetAtPath<RankProgressionCatalog>(
            "Assets/Resources/RankProgressionCatalog.asset");

        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.MatchesPlanetWarMissionLayout(out string reason), Is.True, reason);
        Assert.That(catalog.Nodes, Has.Count.EqualTo(38));
        RankNodeDefinition threeThousand = catalog.Find(1010);
        Assert.That(threeThousand.rewards, Has.Length.EqualTo(2));
        Assert.That(threeThousand.rewards[1].type,
            Is.EqualTo(RankRewardDefinition.RewardType.Pack));
        Assert.That(threeThousand.rewards[1].pack, Is.EqualTo(PackId.Green));
        Assert.That(threeThousand.rewards[1].amount, Is.EqualTo(20));
        Assert.That(catalog.Find(1003).unlockItemIds,
            Is.EqualTo(new[] { "aircraft_explosive", "equipment_rapid_cannon" }));
        Assert.That(catalog.Find(1024).unlockItemIds,
            Is.EqualTo(new[] { "aircraft_l" }));
    }

    [Test]
    public void ResourceCatalog_UsesTheFullPlanetWarRewardSchedule()
    {
        RankProgressionCatalog catalog = AssetDatabase.LoadAssetAtPath<RankProgressionCatalog>(
            "Assets/Resources/RankProgressionCatalog.asset");

        AssertRewards(catalog, 1002, Gold(100));
        AssertRewards(catalog, 1004, Pack(PackId.Green, 5));
        AssertRewards(catalog, 1005, Gold(200));

        AssertRewards(catalog, 1007, Gold(100), Pack(PackId.Green, 5));
        AssertRewards(catalog, 1008, Gold(150), Pack(PackId.Green, 5));
        AssertRewards(catalog, 1009, Gold(200), Pack(PackId.Green, 5));
        AssertRewards(catalog, 1010, Gold(300), Pack(PackId.Green, 20));

        int[] milestoneStarts = { 1012, 1018, 1025, 1032 };
        int[] goldAmounts = { 100, 150, 200, 300, 300, 300 };
        int[] greenPackAmounts = { 5, 5, 5, 5, 8, 8 };
        foreach (int start in milestoneStarts)
        {
            for (int i = 0; i < goldAmounts.Length; i++)
                AssertRewards(catalog, start + i, Gold(goldAmounts[i]),
                    Pack(PackId.Green, greenPackAmounts[i]), Pack(PackId.Blue, 1));
        }
    }

    [Test]
    public void ResourceCatalog_UsesThePlannedRankTwoToSixUnlocks()
    {
        RankProgressionCatalog catalog = AssetDatabase.LoadAssetAtPath<RankProgressionCatalog>(
            "Assets/Resources/RankProgressionCatalog.asset");

        Assert.That(catalog.Find(1003).unlockItemIds,
            Is.EqualTo(new[] { "aircraft_explosive", "equipment_rapid_cannon" }));
        Assert.That(catalog.Find(1006).unlockItemIds,
            Is.EqualTo(new[] { "aircraft_sniper", "equipment_wave_emitter" }));
        Assert.That(catalog.Find(1011).unlockItemIds,
            Is.EqualTo(new[] { "aircraft_laser", "equipment_laser_link" }));
        Assert.That(catalog.Find(1017).unlockItemIds,
            Is.EqualTo(new[] { "aircraft_shotgun", "equipment_first" }));
        Assert.That(catalog.Find(1024).unlockItemIds,
            Is.EqualTo(new[] { "aircraft_l" }));
        Assert.That(catalog.Find(1031).unlockItemIds, Is.Empty);
        Assert.That(catalog.Find(1038).unlockItemIds, Is.Empty);
    }

    [Test]
    public void RankInfoUnlockCardTemplate_UsesReducedIconScale()
    {
        GameObject menu = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Packages/com.planetwar.reusable-main-menu/Prefabs/MainMenu.prefab");

        Assert.That(menu, Is.Not.Null);
        Transform unlockIcon = menu.transform.Find("UIRankInfo/ItemRankMain/reward/item/icon");
        Transform rewardResourceIcon = menu.transform.Find("UIRankInfo/ItemRankReward/reward/item/res");

        Assert.That(unlockIcon, Is.Not.Null);
        Assert.That(unlockIcon.localScale, Is.EqualTo(new Vector3(.392f, .392f, 1f)));
        Assert.That(rewardResourceIcon, Is.Not.Null);
        Assert.That(rewardResourceIcon.localScale, Is.EqualTo(new Vector3(.56f, .56f, 1f)));
    }

    private static ExpectedReward Gold(int amount) =>
        new ExpectedReward(RankRewardDefinition.RewardType.Gold, amount, default);

    private static ExpectedReward Pack(PackId pack, int amount) =>
        new ExpectedReward(RankRewardDefinition.RewardType.Pack, amount, pack);

    private static void AssertRewards(RankProgressionCatalog catalog, int nodeId,
        params ExpectedReward[] expected)
    {
        RankNodeDefinition node = catalog.Find(nodeId);
        Assert.That(node, Is.Not.Null, $"Missing node {nodeId}.");
        Assert.That(node.rewards, Has.Length.EqualTo(expected.Length), $"Node {nodeId}.");
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.That(node.rewards[i].type, Is.EqualTo(expected[i].Type), $"Node {nodeId}, reward {i} type.");
            Assert.That(node.rewards[i].amount, Is.EqualTo(expected[i].Amount), $"Node {nodeId}, reward {i} amount.");
            Assert.That(node.rewards[i].pack, Is.EqualTo(expected[i].Pack), $"Node {nodeId}, reward {i} pack.");
        }
    }

    private readonly struct ExpectedReward
    {
        public readonly RankRewardDefinition.RewardType Type;
        public readonly int Amount;
        public readonly PackId Pack;

        public ExpectedReward(RankRewardDefinition.RewardType type, int amount, PackId pack)
        {
            Type = type;
            Amount = amount;
            Pack = pack;
        }
    }
}
