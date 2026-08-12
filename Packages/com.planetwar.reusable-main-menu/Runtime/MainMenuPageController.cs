using DG.Tweening;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>
    /// Package-local equivalent of the original UIMainBottom controller: one bottom tab owns one
    /// visible page, and choosing a new tab closes the previous page before opening its target.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuPageController : MonoBehaviour
    {
        [SerializeField] private GameObject mainPage;
        [SerializeField] private RanksView ranksPage;
        [SerializeField] private MainBottmChoose tabIndicator;
        [SerializeField] private Transform[] tabTargets;

        private void Awake()
        {
            var relay = GetComponent<MainMenuActionRelay>();
            if (relay != null) relay.ActionInvoked += OnAction;
        }

        private void OnDestroy()
        {
            var relay = GetComponent<MainMenuActionRelay>();
            if (relay != null) relay.ActionInvoked -= OnAction;
        }

        private void OnAction(MainMenuAction action)
        {
            if (action == MainMenuAction.BottomRank) ShowRanks();
            else if (action == MainMenuAction.BottomHome || action == MainMenuAction.BottomBattle) ShowBattle(action == MainMenuAction.BottomBattle ? 2 : 0);
            else if (action >= MainMenuAction.BottomCollection && action <= MainMenuAction.BottomMore) SelectTab((int)action - (int)MainMenuAction.BottomHome);
        }

        private void ShowRanks()
        {
            if (mainPage != null) mainPage.SetActive(false);
            if (ranksPage != null) ranksPage.Show();
            SelectTab(1);
        }

        private void ShowBattle(int tabIndex)
        {
            if (ranksPage != null) ranksPage.gameObject.SetActive(false);
            if (mainPage != null) mainPage.SetActive(true);
            SelectTab(tabIndex);
        }

        private void SelectTab(int index)
        {
            if (tabIndicator != null && tabTargets != null && index >= 0 && index < tabTargets.Length)
                tabIndicator.OnChooseBottom(tabTargets[index], index);
        }
    }
}
