using DG.Tweening;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuPageController : MonoBehaviour
    {
        private const int LockedPlaceholderTabIndex = 0;
        private const int RankTabIndex = 1;
        private const int BattleTabIndex = 2;
        private const int HangarTabIndex = 3;

        [SerializeField] private GameObject mainPage;
        [SerializeField] private RanksView ranksPage;
        [SerializeField] private HangarView hangarPage;
        [SerializeField] private RankInfoView rankInfoPage;
        [SerializeField] private SettingsView settingsPage;
        [SerializeField] private GameObject settingsBackdrop;
        [SerializeField] private GameObject bottomBar;
        [SerializeField] private MainBottmChoose tabIndicator;
        [SerializeField] private Transform[] tabTargets;

        private void Awake()
        {
            var relay = GetComponent<MainMenuActionRelay>();
            if (relay != null) relay.ActionInvoked += OnAction;
            if (rankInfoPage != null) rankInfoPage.CloseRequested += OnRankInfoClosed;
            if (settingsPage != null) settingsPage.CloseRequested += HideSettings;
            HideSettings();
        }

        private void OnDestroy()
        {
            var relay = GetComponent<MainMenuActionRelay>();
            if (relay != null) relay.ActionInvoked -= OnAction;
            if (rankInfoPage != null) rankInfoPage.CloseRequested -= OnRankInfoClosed;
            if (settingsPage != null) settingsPage.CloseRequested -= HideSettings;
            HideSettings();
        }

        private void OnAction(MainMenuAction action)
        {
            if (action == MainMenuAction.Settings) ShowSettings();
            else if (action == MainMenuAction.Rank) ShowRankInfo();
            else if (action == MainMenuAction.BottomRank) ShowRanks();
            else if (action == MainMenuAction.BottomCollection) ShowHangar();
            else if (action == MainMenuAction.BottomHome || action == MainMenuAction.BottomBattle) ShowBattle(action == MainMenuAction.BottomBattle ? BattleTabIndex : LockedPlaceholderTabIndex);
            else if (action >= MainMenuAction.BottomCollection && action <= MainMenuAction.BottomMore) SelectTab((int)action - (int)MainMenuAction.BottomHome);
        }

        private void ShowRanks()
        {
            HideSettings();
            SetBottomBarActive(true);
            if (mainPage != null) mainPage.SetActive(false);
            if (hangarPage != null) hangarPage.gameObject.SetActive(false);
            if (rankInfoPage != null) rankInfoPage.Hide();
            if (ranksPage != null) ranksPage.Show();
            SelectTab(RankTabIndex);
        }

        private void ShowSettings()
        {
            if (settingsBackdrop != null)
            {
                settingsBackdrop.SetActive(true);
                settingsBackdrop.transform.SetAsLastSibling();
            }

            if (settingsPage != null)
            {
                settingsPage.gameObject.SetActive(true);
                settingsPage.transform.SetAsLastSibling();
            }
        }

        private void HideSettings()
        {
            if (settingsPage != null) settingsPage.gameObject.SetActive(false);
            if (settingsBackdrop != null) settingsBackdrop.SetActive(false);
        }

        private void ShowRankInfo()
        {
            HideSettings();
            SetBottomBarActive(false);
            if (mainPage != null) mainPage.SetActive(false);
            if (ranksPage != null) ranksPage.gameObject.SetActive(false);
            if (hangarPage != null) hangarPage.gameObject.SetActive(false);
            if (rankInfoPage != null) rankInfoPage.Show();
        }

        private void OnRankInfoClosed()
        {
            ShowBattle(BattleTabIndex);
        }

        private void ShowBattle(int tabIndex)
        {
            HideSettings();
            if (ranksPage != null) ranksPage.gameObject.SetActive(false);
            if (hangarPage != null) hangarPage.gameObject.SetActive(false);
            if (rankInfoPage != null) rankInfoPage.Hide();
            if (mainPage != null) mainPage.SetActive(true);
            SetBottomBarActive(true);
            SelectTab(tabIndex);
        }

        private void ShowHangar()
        {
            HideSettings();
            SetBottomBarActive(true);
            if (mainPage != null) mainPage.SetActive(false);
            if (ranksPage != null) ranksPage.gameObject.SetActive(false);
            if (rankInfoPage != null) rankInfoPage.Hide();
            if (hangarPage != null) hangarPage.Show();
            SelectTab(HangarTabIndex);
        }

        private void SetBottomBarActive(bool active)
        {
            if (bottomBar != null) bottomBar.SetActive(active);
        }

        private void SelectTab(int index)
        {
            if (tabIndicator != null && tabTargets != null && index >= 0 && index < tabTargets.Length)
                tabIndicator.OnChooseBottom(tabTargets[index], index);
        }
    }
}
