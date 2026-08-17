using System.Reflection;
using BackpackPrototype;
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
        private static readonly FieldInfo RankEntriesField = typeof(RanksView).GetField(
            "entries", BindingFlags.Instance | BindingFlags.NonPublic);

        [SerializeField] private Sprite playerAvatar;
        [SerializeField] private Sprite enemyAvatar;

        public Sprite PlayerAvatar => playerAvatar;
        public Sprite EnemyAvatar => enemyAvatar;

        private void Awake()
        {
            ApplyProfile();
        }

        private void LateUpdate()
        {
            // Keep every page synchronized after its own page-refresh logic has run.
            ApplyProfile();
        }

        public MatchParticipant CreatePlayer() =>
            new MatchParticipant(PlayerName, "Bronze I", "0", playerAvatar);

        public MatchParticipant CreateOpponent() =>
            new MatchParticipant(
                EnemyNamePrefix + Random.Range(0, 10000).ToString("D4"),
                "Bronze I",
                "0",
                enemyAvatar);

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
            if (main != null && system != null)
                main.ApplyProfile(PlayerName, system.Gold, system.Diamond);

            if (playerAvatar == null) return;

            foreach (Image image in menu.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "headImg")
                    image.sprite = playerAvatar;
            }
        }

        private static void ApplyRankProfile()
        {
            foreach (RanksView ranks in FindObjectsByType<RanksView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (RankEntriesField?.GetValue(ranks) is not RankEntry[] entries) continue;
                foreach (RankEntry entry in entries)
                    if (entry != null && entry.isCurrentPlayer) entry.displayName = PlayerName;
            }
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
