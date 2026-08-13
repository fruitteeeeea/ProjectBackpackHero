using TMPro;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// Data adapter for the migrated original ItemReward prefab.
    /// It intentionally only supplies data; the card hierarchy, typography,
    /// background and icon slots remain authored in the source prefab.
    /// </summary>
    public sealed class OriginalSettlementRewardItem : MonoBehaviour
    {
        [SerializeField] private GameObject resourceRoot;
        [SerializeField] private ImageLoader resourceLoader;
        [SerializeField] private GameObject itemIconRoot;
        [SerializeField] private TextMeshProUGUI countText;

        public void SetData(BattleResultReward reward)
        {
            if (countText != null) countText.text = $"x{reward.Amount}";
            // Gold and diamond use the original prefab's `res` ImageLoader;
            // its sibling Image slot is only for card rewards.
            if (itemIconRoot != null) itemIconRoot.SetActive(false);
            if (resourceRoot != null) resourceRoot.SetActive(true);
            if (resourceLoader != null)
                resourceLoader.Select(reward.Label == "DIAMOND" ? 1 : 0);
        }
    }
}
