using System;
using System.Collections.Generic;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Progression
{
    [Serializable]
    public sealed class RankRewardDefinition
    {
        public enum RewardType { Gold, Diamond, Pack }
        public RewardType type;
        public int amount;
        public PackId pack;
    }

    [Serializable]
    public sealed class RankNodeDefinition
    {
        public int id;
        public int level;
        public int score;
        [Tooltip("0 = a rank promotion, 1 = an in-rank reward milestone")]
        public int type;
        public int loseScore;
        public string rankName;
        public string[] unlockItemIds;
        public RankRewardDefinition[] rewards;
    }

    [CreateAssetMenu(fileName = "RankProgressionCatalog", menuName = "Backpack Hero/Rank Progression Catalog")]
    public sealed class RankProgressionCatalog : ScriptableObject
    {
        [SerializeField] private RankNodeDefinition[] nodes;
        [SerializeField] private int victoryScore = 100;
        [SerializeField] private int victoryGold = 50;
        [SerializeField] private int victoryDiamondMin = 1;
        [SerializeField] private int victoryDiamondMaxExclusive = 3;

        public IReadOnlyList<RankNodeDefinition> Nodes => nodes ?? Array.Empty<RankNodeDefinition>();
        public int VictoryScore => victoryScore;
        public int VictoryGold => victoryGold;
        public int VictoryDiamondMin => victoryDiamondMin;
        public int VictoryDiamondMaxExclusive => victoryDiamondMaxExclusive;

        public RankNodeDefinition Find(int id)
        {
            if (nodes == null) return null;
            for (int i = 0; i < nodes.Length; i++) if (nodes[i] != null && nodes[i].id == id) return nodes[i];
            return null;
        }

        /// <summary>Checks the target catalog against PlanetWar's tbmission rank layout.</summary>
        public bool MatchesPlanetWarMissionLayout(out string reason)
        {
            int[] scores = { 50, 120, 200, 400, 650, 1000, 1500, 2000, 2500, 3000, 3600, 4500, 5000, 5500, 6000, 6500, 7000, 8000, 9000, 10000, 12000, 14000, 16000, 18000, 20000, 22000, 24000, 26000, 28000, 30000, 32000, 34000, 36000, 38000, 40000, 42000, 44000, 50000 };
            int[] promotionIds = { 1001, 1003, 1006, 1011, 1017, 1024, 1031, 1038 };
            if (nodes == null || nodes.Length != scores.Length) { reason = $"Expected {scores.Length} rank nodes, found {nodes?.Length ?? 0}."; return false; }
            for (int i = 0; i < nodes.Length; i++)
            {
                RankNodeDefinition node = nodes[i];
                int expectedId = 1001 + i;
                bool promotion = Array.IndexOf(promotionIds, expectedId) >= 0;
                if (node == null || node.id != expectedId || node.score != scores[i] || node.type != (promotion ? 0 : 1))
                {
                    reason = $"Node {i} does not match tbmission: expected id={expectedId}, score={scores[i]}, type={(promotion ? 0 : 1)}.";
                    return false;
                }
            }
            reason = null;
            return true;
        }

        public static RankProgressionCatalog CreateBuiltIn()
        {
            var catalog = CreateInstance<RankProgressionCatalog>();
            catalog.nodes = BuildPlanetWarNodes();
            return catalog;
        }

        // The source project has 8 promotion nodes and 30 milestone nodes. Keeping the
        // tuning in one fallback prevents a missing .asset from turning progression into sample UI.
        private static RankNodeDefinition[] BuildPlanetWarNodes()
        {
            var entries = new List<RankNodeDefinition>();
            Add(entries, 1001, 1, 50, 0, 0, "Glow Belt");
            Add(entries, 1002, 1, 120, 1, 0, null, rewards: R(G(100)));
            Add(entries, 1003, 2, 200, 0, 0, "Pale Cluster", new[] { "aircraft_explosive", "equipment_rapid_cannon" });
            Add(entries, 1004, 2, 400, 1, 0, null, rewards: R(P(PackId.Green, 5)));
            Add(entries, 1005, 2, 650, 1, 0, null, rewards: R(G(200)));
            Add(entries, 1006, 3, 1000, 0, 0, "Swift Zone", new[] { "aircraft_sniper", "equipment_wave_emitter" });
            Add(entries, 1007, 3, 1500, 1, 0, null, rewards: R(G(100), P(PackId.Green, 5)));
            Add(entries, 1008, 3, 2000, 1, 0, null, rewards: R(G(150), P(PackId.Green, 5)));
            Add(entries, 1009, 3, 2500, 1, 0, null, rewards: R(G(200), P(PackId.Green, 5)));
            Add(entries, 1010, 3, 3000, 1, 0, null, rewards: R(G(300), P(PackId.Gold, 20)));
            Add(entries, 1011, 4, 3600, 0, 0, "Deep Outpost", new[] { "aircraft_laser", "equipment_laser_link" });
            Add(entries, 1012, 4, 4500, 1, 0, null, rewards: R(G(100), P(PackId.Green, 5), P(PackId.Blue, 1)));
            Add(entries, 1013, 4, 5000, 1, 0, null, rewards: R(G(150), P(PackId.Green, 5), P(PackId.Blue, 1)));
            Add(entries, 1014, 4, 5500, 1, 0, null, rewards: R(G(200), P(PackId.Green, 5), P(PackId.Blue, 1)));
            Add(entries, 1015, 4, 6000, 1, 0, null, rewards: R(G(300), P(PackId.Green, 5), P(PackId.Blue, 1)));
            Add(entries, 1016, 4, 6500, 1, 0, null, rewards: R(G(300), P(PackId.Green, 8), P(PackId.Blue, 1)));
            Add(entries, 1017, 5, 7000, 0, 0, "Hunt Thicket", new[] { "aircraft_shotgun", "equipment_first" });
            int[] fifth = { 8000, 9000, 10000, 12000, 14000, 16000 }; int[] sixth = { 20000, 22000, 24000, 26000, 28000, 30000 }; int[] seventh = { 34000, 36000, 38000, 40000, 42000, 44000 };
            AddMilestones(entries, 1018, 5, fifth); Add(entries, 1024, 6, 18000, 0, 0, "Dome Nebula", new[] { "aircraft_l" });
            AddMilestones(entries, 1025, 6, sixth); Add(entries, 1031, 7, 32000, 0, 0, "Raid Nest");
            AddMilestones(entries, 1032, 7, seventh); Add(entries, 1038, 8, 50000, 0, 20, "Conqueror Realm", Array.Empty<string>());
            return entries.ToArray();
        }

        private static void AddMilestones(List<RankNodeDefinition> list, int id, int level, int[] scores)
        {
            int[] gold = { 100, 150, 200, 300, 300, 300 };
            for (int i = 0; i < scores.Length; i++)
            {
                Add(list, id + i, level, scores[i], 1, 0, null, rewards: R(G(gold[i]), P(PackId.Green, i >= 4 ? 8 : 5), P(PackId.Blue, 1)));
            }
        }
        private static RankRewardDefinition G(int amount) => new RankRewardDefinition { type = RankRewardDefinition.RewardType.Gold, amount = amount };
        private static RankRewardDefinition P(PackId pack, int amount) => new RankRewardDefinition { type = RankRewardDefinition.RewardType.Pack, pack = pack, amount = amount };
        private static RankRewardDefinition[] R(params RankRewardDefinition[] value) => value;
        private static void Add(List<RankNodeDefinition> list, int id, int level, int score, int type, int lose, string name, string[] unlocks = null, RankRewardDefinition[] rewards = null) => list.Add(new RankNodeDefinition { id = id, level = level, score = score, type = type, loseScore = lose, rankName = name, unlockItemIds = unlocks ?? Array.Empty<string>(), rewards = rewards ?? Array.Empty<RankRewardDefinition>() });
    }
}
