using System.Reflection;
using System.Collections.Generic;
using BackpackPrototype;
using BackpackHero.Debugging;
using BackpackHero.Progression;
using PlanetWar.ReusableMainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackHero.UI
{
    /// <summary>Single player identity profile for the reusable main-menu UI.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class MenuDisplayProfile : MonoBehaviour
    {
        public const string PlayerName = "fruittea";
        private const string EnemyNamePrefix = "random enemy ";

        private static readonly FieldInfo GoldTextField = typeof(HangarView).GetField(
            "goldText", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo DiamondTextField = typeof(HangarView).GetField(
            "diamondText", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo RankGoldTextField = typeof(RanksView).GetField(
            "goldText", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo RankDiamondTextField = typeof(RanksView).GetField(
            "diamondText", BindingFlags.Instance | BindingFlags.NonPublic);

        [SerializeField] private Sprite playerAvatar;
        [SerializeField] private Sprite enemyAvatar;

        public Sprite PlayerAvatar => playerAvatar;
        public Sprite EnemyAvatar => enemyAvatar;

        private void Awake()
        {
            ApplyProfile();
            if (RankProgressionSystem.Instance != null)
                RankProgressionSystem.Instance.Changed += OnProgressionChanged;
            FunctionBlockRuntime.StateChanged += OnFunctionBlockChanged;
        }

        private void OnDestroy()
        {
            if (RankProgressionSystem.Instance != null)
                RankProgressionSystem.Instance.Changed -= OnProgressionChanged;
            FunctionBlockRuntime.StateChanged -= OnFunctionBlockChanged;
        }

        private void LateUpdate()
        {
            // Keep every page synchronized after its own page-refresh logic has run.
            ApplyProfile();
        }

        private void OnProgressionChanged()
        {
            ApplyProfile();
            foreach (RanksView ranks in FindObjectsByType<RanksView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (ranks.isActiveAndEnabled) ranks.Show();
        }

        private void OnFunctionBlockChanged(bool _, bool __) => ApplyProfile();

        public MatchParticipant CreatePlayer() =>
            new MatchParticipant(PlayerName,
                RankProgressionSystem.Instance?.CurrentRankName ?? "Glow Belt",
                (RankProgressionSystem.Instance?.Points ?? 0).ToString(), playerAvatar);

        public MatchParticipant CreateOpponent() =>
            RankProgressionSystem.Instance != null
                ? RankProgressionSystem.Instance.CreateOpponent(enemyAvatar)
                : new MatchParticipant(EnemyNamePrefix + Random.Range(0, 10000).ToString("D4"), "Glow Belt", "0", enemyAvatar);

        private void ApplyProfile()
        {
            ApplyMainMenuProfile();
            ApplyRankProfile();
            ApplyCurrencyToSecondaryPages();
        }

        private void ApplyMainMenuProfile()
        {
            MainMenuView menu = FindFirstObjectByType<MainMenuView>(FindObjectsInactive.Include);
            if (menu == null) return;

            PlayerItemSystem system = PlayerItemSystem.Instance;
            UIMain main = menu.GetComponentInChildren<UIMain>(true);
            if (main != null)
            {
                if (system != null) main.ApplyProfile(PlayerName, system.Gold, system.Diamond);
                RankProgressionSystem progression = RankProgressionSystem.Instance;
                if (progression != null)
                {
                    if (FunctionBlockRuntime.IsMilestoneBlocked)
                        main.ApplyMainProgress(progression.Points, RankProgressionSystem.MaximumPoints,
                            CalculateCollectionLevel(system));
                    else
                        main.ApplyRankProgress(progression.Points, RankProgressionSystem.MaximumPoints,
                            progression.CurrentRankName);
                }
            }

            if (playerAvatar == null) return;

            foreach (Image image in menu.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "headImg")
                    image.sprite = playerAvatar;
            }
        }

        public static int CalculateCollectionLevel(PlayerItemSystem system)
        {
            if (system == null) return PlayerItemSystem.DefaultLevel;

            int totalLevel = 0;
            int itemCount = 0;
            foreach (ItemData item in system.GetAllItems())
            {
                if (item == null || !system.IsUnlocked(item)) continue;
                totalLevel += system.GetLevel(item);
                itemCount++;
            }

            return itemCount == 0
                ? PlayerItemSystem.DefaultLevel
                : totalLevel / itemCount;
        }

        private static void ApplyRankProfile()
        {
            RankProgressionSystem progression = RankProgressionSystem.Instance;
            foreach (RanksView ranks in FindObjectsByType<RanksView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (progression != null)
                {
                    var board = progression.GetLeaderboard();
                    var entries = new List<RankEntry>(board.Count);
                    foreach (var player in board)
                    {
                        if (player == null) continue;
                        entries.Add(new RankEntry {
                            displayName = player.self ? PlayerName : player.name,
                            score = player.self ? progression.Points : player.score,
                            countryFlag = ranks.GetCountryFlag(player.self ? 0 : player.countryIndex),
                            isCurrentPlayer = player.self
                        });
                    }
                    ranks.SetEntries(entries);
                }
            }

            foreach (RankInfoView info in FindObjectsByType<RankInfoView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (progression == null) continue;
                info.ApplyProgression(BuildRankInfoEntries(info.entries, progression), progression.CurrentNodeId, progression.Points);
            }
        }

        private static RankInfoEntry[] BuildRankInfoEntries(RankInfoEntry[] authoredEntries, RankProgressionSystem progression)
        {
            var nodes = progression.Catalog.Nodes;
            var result = new RankInfoEntry[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var artwork = authoredEntries != null && i < authoredEntries.Length ? authoredEntries[i] : null;
                Sprite rankIcon = LoadRankSprite(node.level, false) ?? artwork?.iconSprite;
                Sprite lockedRankIcon = LoadRankSprite(node.level, true) ?? artwork?.lockedIconSprite;
                result[i] = new RankInfoEntry {
                    id = node.id, level = node.level, score = node.score, type = node.type,
                    enName = node.rankName ?? string.Empty,
                    iconSprite = rankIcon, lockedIconSprite = lockedRankIcon,
                    rewards = BuildRankRewards(node, artwork?.rewards)
                };
            }
            return result;
        }

        private static Sprite LoadRankSprite(int level, bool locked)
        {
            if (level < 1 || level > 8) return null;
            string suffix = locked ? "_bai" : string.Empty;
            return Resources.Load<Sprite>($"PlanetWar/Rank/icon_huizhang_{level}{suffix}");
        }

        private static RankInfoReward[] BuildRankRewards(RankNodeDefinition node, RankInfoReward[] artwork)
        {
            if (node.rewards == null || node.rewards.Length == 0) return System.Array.Empty<RankInfoReward>();
            var result = new RankInfoReward[node.rewards.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var reward = node.rewards[i];
                var visual = artwork != null && i < artwork.Length ? artwork[i] : null;
                bool pack = reward.type == RankRewardDefinition.RewardType.Pack;
                result[i] = new RankInfoReward {
                    icon = visual?.icon, count = reward.amount, isPack = pack,
                    displayName = pack ? reward.pack.ToString() : reward.type.ToString()
                };
            }
            return result;
        }

        private static void ApplyCurrencyToSecondaryPages()
        {
            PlayerItemSystem system = PlayerItemSystem.Instance;
            if (system == null) return;

            foreach (HangarView hangar in FindObjectsByType<HangarView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SetText(GoldTextField?.GetValue(hangar) as TMP_Text, system.Gold);
                SetText(DiamondTextField?.GetValue(hangar) as TMP_Text, system.Diamond);
            }

            foreach (RanksView ranks in FindObjectsByType<RanksView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SetText(RankGoldTextField?.GetValue(ranks) as TMP_Text, system.Gold);
                SetText(RankDiamondTextField?.GetValue(ranks) as TMP_Text, system.Diamond);
            }
        }

        private static void SetText(TMP_Text text, int value)
        {
            if (text != null) text.text = value.ToString();
        }
    }
}
