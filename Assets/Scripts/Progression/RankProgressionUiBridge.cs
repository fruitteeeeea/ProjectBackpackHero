using BackpackPrototype;
using PlanetWar.ReusableMainMenu;
using UnityEngine;

namespace BackpackHero.Progression
{
    /// <summary>Connects the reusable menu assembly to the game-specific saved progression state.</summary>
    public sealed class RankProgressionUiBridge : MonoBehaviour
    {
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
            if (RankProgressionSystem.Instance == null || !RankProgressionSystem.Instance.ClaimReward(id, out _)) return false;
            RefreshVisibleRankPages();
            return true;
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
