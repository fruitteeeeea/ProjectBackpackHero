using PlanetWar.ReusableMainMenu;
using UnityEngine;

namespace BackpackHero.UI
{
    /// <summary>Host bridge from the reusable menu's central Battle button to the migrated original match UI.</summary>
    public sealed class MainMenuBattleSceneLoader : MonoBehaviour
    {
        [SerializeField] private UIBattle matchPrefab;
        [SerializeField] private MatchParticipant player = new MatchParticipant("Commander", "Bronze I", "0");
        [SerializeField] private MatchParticipant opponent = new MatchParticipant("Opponent", "Bronze I", "0");

        private MainMenuActionRelay actionRelay;
        private UIBattle matchView;

        private void Awake()
        {
            actionRelay = FindFirstObjectByType<MainMenuActionRelay>();
            if (actionRelay == null)
            {
                Debug.LogError("Main menu action relay was not found.", this);
                enabled = false;
                return;
            }

            actionRelay.ActionInvoked += HandleMenuAction;
            if (matchPrefab == null)
                matchPrefab = Resources.Load<UIBattle>("PlanetWar/UIBattle");

            if (matchPrefab == null)
            {
                Debug.LogError("The migrated UIBattle prefab was not found at Resources/PlanetWar/UIBattle.", this);
                enabled = false;
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            matchView = Instantiate(matchPrefab, canvas.transform);
            matchView.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (actionRelay != null)
                actionRelay.ActionInvoked -= HandleMenuAction;
        }

        private void HandleMenuAction(MainMenuAction action)
        {
            // BottomBattle remains a page-selection action. Only the original central Battle button starts matching.
            if (action == MainMenuAction.Start && matchView != null && !matchView.IsRunning)
                matchView.BeginMatch(player, opponent);
        }
    }
}
