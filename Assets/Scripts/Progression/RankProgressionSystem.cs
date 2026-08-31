using System;
using System.Collections.Generic;
using System.Linq;
using BackpackHero.Battle;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Progression
{
    [Serializable] public sealed class RankLeaderboardEntry { public string name; public int score; public int countryIndex; public bool self; }
    [Serializable]
    public sealed class RankDefeatRecoveryState
    {
        public int nodeId;
        public int consecutiveDefeats;
        public bool downgradePending;

        public bool DowngradePending => downgradePending;
        public void Reset() { nodeId = 0; consecutiveDefeats = 0; downgradePending = false; }
    }

    [Serializable] public sealed class RankProgressionSaveData { public int version; public int points; public int currentNodeId = 1001; public int wins; public RankDefeatRecoveryState defeatRecovery = new(); public List<int> claimedRewardNodeIds = new(); public List<RankLeaderboardEntry> leaderboard = new(); }
    public readonly struct RankSettlement
    {
        public readonly int ScoreBefore, ScoreDelta, Gold, Diamond, RecommendedFragmentsNeeded;
        public readonly bool Victory, DowngradeUnlocked;
        public readonly string RecommendedItemName;
        public readonly IReadOnlyList<BattleResultReward> Rewards;
        public RankSettlement(bool victory, int before, int delta, int gold, int diamond, IReadOnlyList<BattleResultReward> rewards, string recommendedItemName = null, int recommendedFragmentsNeeded = 0, bool downgradeUnlocked = false)
        { Victory = victory; ScoreBefore = before; ScoreDelta = delta; Gold = gold; Diamond = diamond; Rewards = rewards; RecommendedItemName = recommendedItemName; RecommendedFragmentsNeeded = recommendedFragmentsNeeded; DowngradeUnlocked = downgradeUnlocked; }
    }

    public sealed class RankProgressionSystem : MonoBehaviour
    {
        public const string SaveKey = "RankProgressionModel";
        public const int MaximumPoints = 50000;
        private const int SaveVersion = 3;
        private const string CatalogPath = "RankProgressionCatalog";
        private const string EnemyDifficultyCatalogPath = "RankEnemyDifficultyCatalog";
        private RankProgressionSaveData data;
        private RankProgressionCatalog catalog;
        private RankEnemyDifficultyCatalog enemyDifficultyCatalog;
        private bool settlementAppliedForMatch;
        public static RankProgressionSystem Instance { get; private set; }
        public event Action Changed;
        public int Points => data?.points ?? 0; public int Wins => data?.wins ?? 0;
        public int CurrentNodeId => data?.currentNodeId ?? 1001;
        public RankNodeDefinition CurrentNode => catalog?.Find(CurrentNodeId);
        public RankProgressionCatalog Catalog => catalog;
        public RankDefeatRecoveryState DefeatRecovery => data?.defeatRecovery;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] private static void EnsureInstance() { if (Instance != null) return; var go = new GameObject(nameof(RankProgressionSystem)); DontDestroyOnLoad(go); go.AddComponent<RankProgressionSystem>(); }
        private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; DontDestroyOnLoad(gameObject); catalog = Resources.Load<RankProgressionCatalog>(CatalogPath) ?? RankProgressionCatalog.CreateBuiltIn(); enemyDifficultyCatalog = Resources.Load<RankEnemyDifficultyCatalog>(EnemyDifficultyCatalogPath); if (!catalog.MatchesPlanetWarMissionLayout(out string reason)) Debug.LogError($"Rank progression catalog is not aligned with PlanetWar tbmission: {reason}", this); if (enemyDifficultyCatalog == null) Debug.LogError("Rank enemy difficulty catalog is missing.", this); else if (!enemyDifficultyCatalog.IsValid(out string difficultyError)) Debug.LogError($"Rank enemy difficulty catalog is invalid: {difficultyError}", this); Load(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void Load()
        {
            data = JsonUtility.FromJson<RankProgressionSaveData>(PlayerPrefs.GetString(SaveKey)) ?? new RankProgressionSaveData();
            if (data.version < SaveVersion) MigrateToFormalProgression();
            Normalize();
            if (data.leaderboard.Count != 50) RebuildLeaderboard(false);
            Save();
        }
        private void Save() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
        private void Notify() { Save(); Changed?.Invoke(); }
        private void Normalize() { if (catalog.Find(data.currentNodeId) == null) data.currentNodeId = catalog.Nodes.Count > 0 ? catalog.Nodes[0].id : 1001; data.claimedRewardNodeIds ??= new List<int>(); data.leaderboard ??= new List<RankLeaderboardEntry>(); data.defeatRecovery ??= new RankDefeatRecoveryState(); data.points = Mathf.Clamp(data.points, 0, MaximumPoints); }
        private void MigrateToFormalProgression()
        {
            // Pre-release builds started at max rank. The first formal version starts
            // everybody at the beginning so rank rewards and card unlocks remain meaningful.
            data.points = 0;
            data.currentNodeId = catalog.Nodes.Count > 0 ? catalog.Nodes[0].id : 1001;
            data.wins = 0;
            data.claimedRewardNodeIds = new List<int>();
            data.defeatRecovery = new RankDefeatRecoveryState();
            data.version = SaveVersion;
            RebuildLeaderboard(false);
        }

        public RankSettlement SettleMatch(bool victory)
        {
            if (settlementAppliedForMatch) return new RankSettlement(victory, Points, 0, 0, 0, Array.Empty<BattleResultReward>());
            settlementAppliedForMatch = true;
            int before = Points;
            int startingNodeId = CurrentNodeId;
            int delta = victory ? Mathf.Min(catalog.VictoryScore, MaximumPoints - before) : 0;
            data.points = Mathf.Clamp(data.points + delta, 0, MaximumPoints);
            if (victory) { data.wins++; AdvanceNodes(); data.defeatRecovery.Reset(); }
            int gold = victory ? catalog.VictoryGold : 20;
            int diamond = victory ? UnityEngine.Random.Range(catalog.VictoryDiamondMin, catalog.VictoryDiamondMaxExclusive) : 0;
            var rewards = new List<BattleResultReward> { new BattleResultReward("GOLD", gold) }; if (diamond > 0) rewards.Add(new BattleResultReward("DIAMOND", diamond));
            PlayerItemSystem.Instance?.AddCurrency(gold, diamond);
            string recommendation = null;
            int fragmentsNeeded = 0;
            bool downgradeUnlocked = false;
            if (!victory)
            {
                ItemData item = GetRecommendedUpgradeItem();
                if (item != null)
                {
                    PlayerItemSystem player = PlayerItemSystem.Instance;
                    player?.AddFragments(item, 2);
                    rewards.Add(new BattleResultReward($"{item.Name} FRAGMENT", 2));
                    recommendation = item.Name;
                    if (player != null)
                        fragmentsNeeded = Mathf.Max(0,
                            player.GetUpgradeFragmentCost(item) - player.GetFragments(item));
                }
                RankDefeatRecoveryState recovery = data.defeatRecovery;
                if (recovery.nodeId != startingNodeId) { recovery.Reset(); recovery.nodeId = startingNodeId; }
                recovery.consecutiveDefeats = Mathf.Min(2, recovery.consecutiveDefeats + 1);
                if (recovery.consecutiveDefeats >= 2 && !recovery.downgradePending)
                {
                    recovery.downgradePending = true;
                    downgradeUnlocked = true;
                }
            }
            if (victory && PackSystem.Instance != null)
            {
                PackId[] packs = { PackId.Green, PackId.Blue, PackId.Purple, PackId.Gold }; PackId pack = packs[UnityEngine.Random.Range(0, packs.Length)];
                if (PackSystem.Instance.TryAddPack(pack)) rewards.Add(new BattleResultReward(pack.ToString().ToUpperInvariant(), 1));
            }
            RebuildLeaderboard(false); Notify();
            return new RankSettlement(victory, before, data.points - before, gold, diamond, rewards, recommendation, fragmentsNeeded, downgradeUnlocked);
        }

        public void BeginMatch() => settlementAppliedForMatch = false;
        public bool TryResolveEnemyMatchProfile(out EnemyMatchProfile profile) =>
            RankEnemyDifficultyResolver.TryResolve(enemyDifficultyCatalog, Points,
                CurrentNodeId, DefeatRecovery, out profile);
        public int GetUnlockRankForItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || catalog == null) return 1;
            foreach (RankNodeDefinition node in catalog.Nodes)
                if (node != null && (node.unlockItemIds ?? Array.Empty<string>()).Contains(itemId)) return node.level;
            return 1;
        }
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
            // The displayed opponent belongs to the same score band that chose the
            // actual enemy deck and progression level; it is not a random fake score.
            int score = Points;
            return new MatchParticipant(ai?.name ?? "Starweaver", GetRankNameForScore(score), score.ToString(), avatar);
        }
        private void AdvanceNodes() { while (true) { int index = IndexOf(CurrentNodeId); var node = CurrentNode; if (node == null || data.points < node.score || index < 0 || index + 1 >= catalog.Nodes.Count) break; if (node.type == 0) Unlock(node); data.currentNodeId = catalog.Nodes[index + 1].id; } }
        private void Unlock(RankNodeDefinition node) { var player = PlayerItemSystem.Instance; if (player == null) return; foreach (string id in node.unlockItemIds ?? Array.Empty<string>()) { var item = player.GetAllItems().FirstOrDefault(x => x != null && x.ItemId == id); if (item == null) Debug.LogWarning($"Rank unlock item '{id}' was not found.", this); else player.Unlock(item); } }
        private ItemData GetRecommendedUpgradeItem()
        {
            PlayerItemSystem player = PlayerItemSystem.Instance;
            if (player == null) return null;
            return player.GetDeckItems().Where(item => item != null && player.IsUnlocked(item) && player.GetLevel(item) < PlayerItemSystem.MaximumLevel)
                .OrderBy(player.GetLevel).ThenBy(player.GetUpgradeFragmentCost).FirstOrDefault();
        }
        private int IndexOf(int id) { for (int i = 0; i < catalog.Nodes.Count; i++) if (catalog.Nodes[i].id == id) return i; return -1; }
        private void RebuildLeaderboard(bool notify)
        {
            var names = new[] { "Starweaver", "Millet Plant Glow", "Daisy Sunwhisper", "Noah Sunbloom", "Ion Frostweaver", "Orange Glow", "Iron Glow", "Sequoia", "Nova Skydancer", "River Ember", "Luna Cloudsong", "Aster Moonfall", "Sage Brightstar", "Echo Wildfire", "Robin Mistwalker", "Sky Silvermoon", "Piper Sunray", "Rowan Starfall", "Aquamarine", "Celeste Dawnbringer", "Ember Wildroot", "Finn Starforge", "Hazel Skysong", "Iris Moonwhisper", "Jasper Nightbloom", "Kai Silverwind", "Liora Frostfall", "Milo Suncrest", "Nora Emberglow", "Opal Brightwood", "Quinn Stormcaller", "Rhea Cloudwalker", "Silas Moonstone", "Talia Starbloom", "Uma Ravenwood", "Vale Sunseeker", "Wren Firelight", "Xander Cloudfall", "Yara Mistbloom", "Zane Starwatch", "Ayla Nightfall", "Briar Sunweaver", "Cora Dawnfall", "Dorian Frostwind", "Elara Skydancer", "Flint Moonrider", "Gwen Starling", "Hugo Brightleaf" };
            data.leaderboard.Clear();
            for (int i = 0; i < names.Length; i++) data.leaderboard.Add(new RankLeaderboardEntry { name = names[i], score = UnityEngine.Random.Range(30000, 49501), countryIndex = i % 6 });
            data.leaderboard.Add(new RankLeaderboardEntry { name = BackpackHero.UI.MenuDisplayProfile.PlayerName, score = Points, countryIndex = 0, self = true });
            data.leaderboard.Sort((a, b) => b.score.CompareTo(a.score)); if (notify) Notify();
        }
    }
}
