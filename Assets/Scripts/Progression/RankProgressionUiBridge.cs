using BackpackPrototype;
using PlanetWar.ReusableMainMenu;
using UnityEngine;

namespace BackpackHero.Progression
{
    /// <summary>Connects the reusable menu assembly to the game-specific saved progression state.</summary>
    public sealed class RankProgressionUiBridge : MonoBehaviour
    {
        private UIGetReward rewardView;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Ensure()
        {
            var go = new GameObject(nameof(RankProgressionUiBridge));
            DontDestroyOnLoad(go);
            go.AddComponent<RankProgressionUiBridge>();
        }

        private void Awake()
        {
            RankInfoItemRewardView.IsClaimed = id => RankProgressionSystem.Instance != null && RankProgressionSystem.Instance.IsRewardClaimed(id);
            RankInfoItemRewardView.CanClaim = id => RankProgressionSystem.Instance != null && RankProgressionSystem.Instance.CanClaimReward(id);
            RankInfoItemRewardView.Claim = Claim;
        }

        private void Start()
        {
            if (RankProgressionSystem.Instance != null)
                RankProgressionSystem.Instance.Changed += RefreshVisibleRankPages;
        }

        private bool Claim(int id)
        {
            if (RankProgressionSystem.Instance == null ||
                !RankProgressionSystem.Instance.ClaimReward(id, out _, out PackReward rewards)) return false;
            ShowRewards(rewards);
            RefreshVisibleRankPages();
            return true;
        }

        private void ShowRewards(PackReward rewards)
        {
            if (rewardView == null)
            {
                MainMenuView menu = FindFirstObjectByType<MainMenuView>(FindObjectsInactive.Include);
                if (menu == null) return;
                GameObject prefab = Resources.Load<GameObject>("PackUI/Source/Prefabs/UIGetReward");
                if (prefab == null) return;
                Transform canvas = menu.GetComponentInParent<Canvas>()?.transform ?? menu.transform;
                rewardView = Instantiate(prefab, canvas).GetComponent<UIGetReward>();
            }
            rewardView?.ShowRewards(rewards);
        }

        private static void RefreshVisibleRankPages()
        {
            foreach (RankInfoView view in FindObjectsByType<RankInfoView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (view.isActiveAndEnabled) view.Show();
            foreach (RanksView view in FindObjectsByType<RanksView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (view.isActiveAndEnabled) view.Show();
        }

        private void OnDestroy()
        {
            if (RankProgressionSystem.Instance != null) RankProgressionSystem.Instance.Changed -= RefreshVisibleRankPages;
            RankInfoItemRewardView.IsClaimed = null;
            RankInfoItemRewardView.CanClaim = null;
            RankInfoItemRewardView.Claim = null;
        }
    }
}
