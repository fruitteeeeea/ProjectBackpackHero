using DG.Tweening;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuPageController : MonoBehaviour
    {
        [SerializeField] private GameObject mainPage;
        [SerializeField] private RanksView ranksPage;
        [SerializeField] private HangarView hangarPage;
        [SerializeField] private RankInfoView rankInfoPage;
        [SerializeField] private GameObject bottomBar;
        [SerializeField] private MainBottmChoose tabIndicator;
        [SerializeField] private Transform[] tabTargets;

        private void Awake()
        {
            var relay = GetComponent<MainMenuActionRelay>();
            if (relay != null) relay.ActionInvoked += OnAction;
            if (rankInfoPage != null) rankInfoPage.CloseRequested += OnRankInfoClosed;
        }

        private void OnDestroy()
        {
            var relay = GetComponent<MainMenuActionRelay>();
            if (relay != null) relay.ActionInvoked -= OnAction;
            if (rankInfoPage != null) rankInfoPage.CloseRequested -= OnRankInfoClosed;
        }

        private void OnAction(MainMenuAction action)
        {
            if (action == MainMenuAction.Rank) ShowRankInfo();
            else if (action == MainMenuAction.BottomRank) ShowRanks();
            else if (action == MainMenuAction.BottomCollection) ShowHangar();
            else if (action == MainMenuAction.BottomHome || action == MainMenuAction.BottomBattle) ShowBattle(action == MainMenuAction.BottomBattle ? 2 : 0);
            else if (action >= MainMenuAction.BottomCollection && action <= MainMenuAction.BottomMore) SelectTab((int)action - (int)MainMenuAction.BottomHome);
        }

        private void ShowRanks()
        {
            SetBottomBarActive(true);
            if (mainPage != null) mainPage.SetActive(false);
            if (hangarPage != null) hangarPage.gameObject.SetActive(false);
            if (rankInfoPage != null) rankInfoPage.Hide();
            if (ranksPage != null) ranksPage.Show();
            SelectTab(1);
        }

        private void ShowRankInfo()
        {
            SetBottomBarActive(false);
            if (mainPage != null) mainPage.SetActive(false);
            if (ranksPage != null) ranksPage.gameObject.SetActive(false);
            if (hangarPage != null) hangarPage.gameObject.SetActive(false);
            if (rankInfoPage != null) rankInfoPage.Show();
        }

        private void OnRankInfoClosed()
        {
            ShowBattle(0);
        }

        private void ShowBattle(int tabIndex)
        {
            if (ranksPage != null) ranksPage.gameObject.SetActive(false);
            if (hangarPage != null) hangarPage.gameObject.SetActive(false);
            if (rankInfoPage != null) rankInfoPage.Hide();
            if (mainPage != null) mainPage.SetActive(true);
            SetBottomBarActive(true);
            SelectTab(tabIndex);
        }

        private void ShowHangar()
        {
            SetBottomBarActive(true);
            if (mainPage != null) mainPage.SetActive(false);
            if (ranksPage != null) ranksPage.gameObject.SetActive(false);
            if (rankInfoPage != null) rankInfoPage.Hide();
            if (hangarPage != null) hangarPage.Show();
            SelectTab(3);
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
