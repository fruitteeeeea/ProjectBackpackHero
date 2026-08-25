using System;
using System.Collections.Generic;
using System.Linq;
using BackpackHero.Battle;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Progression
{
    [Serializable] public sealed class RankLeaderboardEntry { public string name; public int score; public int countryIndex; public bool self; }
    [Serializable] public sealed class RankProgressionSaveData { public int version; public int points; public int currentNodeId = 1001; public int wins; public List<int> claimedRewardNodeIds = new(); public List<RankLeaderboardEntry> leaderboard = new(); }
    public readonly struct RankSettlement
    {
        public readonly int ScoreBefore, ScoreDelta, Gold, Diamond; public readonly bool Victory; public readonly IReadOnlyList<BattleResultReward> Rewards;
        public RankSettlement(bool victory, int before, int delta, int gold, int diamond, IReadOnlyList<BattleResultReward> rewards) { Victory = victory; ScoreBefore = before; ScoreDelta = delta; Gold = gold; Diamond = diamond; Rewards = rewards; }
    }

    public sealed class RankProgressionSystem : MonoBehaviour
    {
        public const string SaveKey = "RankProgressionModel";
        public const int MaximumPoints = 50000;
        private const int SaveVersion = 2;
        private const string CatalogPath = "RankProgressionCatalog";
        private RankProgressionSaveData data;
        private RankProgressionCatalog catalog;
        private bool settlementAppliedForMatch;
        public static RankProgressionSystem Instance { get; private set; }
        public event Action Changed;
        public int Points => data?.points ?? 0; public int Wins => data?.wins ?? 0;
        public int CurrentNodeId => data?.currentNodeId ?? 1001;
        public RankNodeDefinition CurrentNode => catalog?.Find(CurrentNodeId);
        public RankProgressionCatalog Catalog => catalog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] private static void EnsureInstance() { if (Instance != null) return; var go = new GameObject(nameof(RankProgressionSystem)); DontDestroyOnLoad(go); go.AddComponent<RankProgressionSystem>(); }
        private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; DontDestroyOnLoad(gameObject); catalog = Resources.Load<RankProgressionCatalog>(CatalogPath) ?? RankProgressionCatalog.CreateBuiltIn(); if (!catalog.MatchesPlanetWarMissionLayout(out string reason)) Debug.LogError($"Rank progression catalog is not aligned with PlanetWar tbmission: {reason}", this); Load(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void Load()
        {
            data = JsonUtility.FromJson<RankProgressionSaveData>(PlayerPrefs.GetString(SaveKey)) ?? new RankProgressionSaveData();
            if (data.version < SaveVersion) MigrateToMaximumProgression();
            Normalize();
            if (data.leaderboard.Count != 50) RebuildLeaderboard(false);
            Save();
        }
        private void Save() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
        private void Notify() { Save(); Changed?.Invoke(); }
        private void Normalize() { if (catalog.Find(data.currentNodeId) == null) data.currentNodeId = catalog.Nodes.Count > 0 ? catalog.Nodes[0].id : 1001; data.claimedRewardNodeIds ??= new List<int>(); data.leaderboard ??= new List<RankLeaderboardEntry>(); data.points = Mathf.Clamp(data.points, 0, MaximumPoints); }
        private void MigrateToMaximumProgression()
        {
            data.points = MaximumPoints;
            data.currentNodeId = 1038;
            data.claimedRewardNodeIds ??= new List<int>();
            foreach (RankNodeDefinition node in catalog.Nodes)
                if (node.type == 1 && !data.claimedRewardNodeIds.Contains(node.id)) data.claimedRewardNodeIds.Add(node.id);
            data.version = SaveVersion;
            RebuildLeaderboard(false);
        }

        public RankSettlement SettleMatch(bool victory)
        {
            if (settlementAppliedForMatch) return new RankSettlement(victory, Points, 0, 0, 0, Array.Empty<BattleResultReward>());
            settlementAppliedForMatch = true;
            int before = Points; RankNodeDefinition node = CurrentNode;
            int delta = victory ? Mathf.Min(catalog.VictoryScore, MaximumPoints - before) : -(node?.loseScore ?? 0);
            data.points = Mathf.Clamp(data.points + delta, 0, MaximumPoints);
            if (!victory)
            {
                int index = IndexOf(CurrentNodeId); if (index > 0) data.points = Mathf.Max(data.points, catalog.Nodes[index - 1].score);
            }
            else { data.wins++; AdvanceNodes(); }
            int gold = victory ? catalog.VictoryGold : 20;
            int diamond = victory ? UnityEngine.Random.Range(catalog.VictoryDiamondMin, catalog.VictoryDiamondMaxExclusive) : 0;
            var rewards = new List<BattleResultReward> { new BattleResultReward("GOLD", gold) }; if (diamond > 0) rewards.Add(new BattleResultReward("DIAMOND", diamond));
            PlayerItemSystem.Instance?.AddCurrency(gold, diamond);
            if (victory && PackSystem.Instance != null)
            {
                PackId[] packs = { PackId.Green, PackId.Blue, PackId.Purple, PackId.Gold }; PackId pack = packs[UnityEngine.Random.Range(0, packs.Length)];
                if (PackSystem.Instance.TryAddPack(pack)) rewards.Add(new BattleResultReward(pack.ToString().ToUpperInvariant(), 1));
            }
            RebuildLeaderboard(false); Notify();
            return new RankSettlement(victory, before, data.points - before, gold, diamond, rewards);
        }

        public void BeginMatch() => settlementAppliedForMatch = false;
        public bool IsRewardClaimed(int nodeId) => data.claimedRewardNodeIds.Contains(nodeId);
        public bool CanClaimReward(int nodeId) { var node = catalog.Find(nodeId); return node != null && node.type == 1 && Points >= node.score && !IsRewardClaimed(nodeId); }
        public bool ClaimReward(int nodeId, out IReadOnlyList<BattleResultReward> displayRewards)
        {
            displayRewards = Array.Empty<BattleResultReward>(); if (!CanClaimReward(nodeId)) return false;
            var node = catalog.Find(nodeId); var display = new List<BattleResultReward>();
            foreach (var reward in node.rewards ?? Array.Empty<RankRewardDefinition>())
            {
                if (reward == null || reward.amount <= 0) continue;
                if (reward.type == RankRewardDefinition.RewardType.Gold) { PlayerItemSystem.Instance?.AddCurrency(reward.amount); display.Add(new BattleResultReward("GOLD", reward.amount)); }
                else if (reward.type == RankRewardDefinition.RewardType.Diamond) { PlayerItemSystem.Instance?.AddCurrency(0, reward.amount); display.Add(new BattleResultReward("DIAMOND", reward.amount)); }
                else if (reward.type == RankRewardDefinition.RewardType.Pack) { int granted = 0; for (int i = 0; i < reward.amount; i++) if (PackSystem.Instance != null && PackSystem.Instance.TryAddPack(reward.pack)) granted++; if (granted > 0) display.Add(new BattleResultReward(reward.pack.ToString().ToUpperInvariant(), granted)); }
            }
            data.claimedRewardNodeIds.Add(nodeId); displayRewards = display; Notify(); return true;
        }
        public IReadOnlyList<RankLeaderboardEntry> GetLeaderboard() => data.leaderboard;
        public string CurrentRankName => GetRankNameForScore(Points);
        public string GetRankNameForScore(int score)
        {
            // A promotion threshold starts a rank. Choose the highest threshold that
            // has been reached, rather than the next threshold above the score.
            RankNodeDefinition resolved = catalog.Nodes.Where(node => node.type == 0 && node.score <= score).LastOrDefault()
                ?? catalog.Nodes.FirstOrDefault(node => node.type == 0);
            return !string.IsNullOrEmpty(resolved?.rankName) ? resolved.rankName : "Conqueror Realm";
        }
        public MatchParticipant CreateOpponent(Sprite avatar)
        {
            var ai = data.leaderboard.Where(entry => !entry.self).OrderBy(_ => UnityEngine.Random.value).FirstOrDefault();
            int score = ai != null ? ai.score : MaximumPoints - 500;
            return new MatchParticipant(ai?.name ?? "Starweaver", GetRankNameForScore(score), score.ToString(), avatar);
        }
        private void AdvanceNodes() { while (true) { int index = IndexOf(CurrentNodeId); var node = CurrentNode; if (node == null || data.points < node.score || index < 0 || index + 1 >= catalog.Nodes.Count) break; if (node.type == 0) Unlock(node); data.currentNodeId = catalog.Nodes[index + 1].id; } }
        private void Unlock(RankNodeDefinition node) { var player = PlayerItemSystem.Instance; if (player == null) return; foreach (string id in node.unlockItemIds ?? Array.Empty<string>()) { var item = player.GetAllItems().FirstOrDefault(x => x != null && x.ItemId == id); if (item == null) Debug.LogWarning($"Rank unlock item '{id}' was not found.", this); else player.Unlock(item); } }
        private int IndexOf(int id) { for (int i = 0; i < catalog.Nodes.Count; i++) if (catalog.Nodes[i].id == id) return i; return -1; }
        private void RebuildLeaderboard(bool notify)
        {
            var names = new[] { "Starweaver", "Millet Plant Glow", "Daisy Sunwhisper", "Noah Sunbloom", "Ion Frostweaver", "Orange Glow", "Iron Glow", "Sequoia", "Nova Skydancer", "River Ember", "Luna Cloudsong", "Aster Moonfall", "Sage Brightstar", "Echo Wildfire", "Robin Mistwalker", "Sky Silvermoon", "Piper Sunray", "Rowan Starfall", "Aquamarine", "Celeste Dawnbringer", "Ember Wildroot", "Finn Starforge", "Hazel Skysong", "Iris Moonwhisper", "Jasper Nightbloom", "Kai Silverwind", "Liora Frostfall", "Milo Suncrest", "Nora Emberglow", "Opal Brightwood", "Quinn Stormcaller", "Rhea Cloudwalker", "Silas Moonstone", "Talia Starbloom", "Uma Ravenwood", "Vale Sunseeker", "Wren Firelight", "Xander Cloudfall", "Yara Mistbloom", "Zane Starwatch", "Ayla Nightfall", "Briar Sunweaver", "Cora Dawnfall", "Dorian Frostwind", "Elara Skydancer", "Flint Moonrider", "Gwen Starling", "Hugo Brightleaf" };
            data.leaderboard.Clear();
            for (int i = 0; i < names.Length; i++) data.leaderboard.Add(new RankLeaderboardEntry { name = names[i], score = UnityEngine.Random.Range(30000, 49501), countryIndex = i % 6 });
            data.leaderboard.Add(new RankLeaderboardEntry { name = BackpackHero.UI.MenuDisplayProfile.PlayerName, score = MaximumPoints, countryIndex = 1, self = true });
            data.leaderboard.Sort((a, b) => b.score.CompareTo(a.score)); if (notify) Notify();
        }
    }
}
