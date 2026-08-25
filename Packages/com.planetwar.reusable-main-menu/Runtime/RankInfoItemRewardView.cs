using System.Collections.Generic;
using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    public sealed class RankInfoItemRewardView : MonoBehaviour
    {
        public static Func<int, bool> IsClaimed;
        public static Func<int, bool> CanClaim;
        public static Func<int, bool> Claim;
        public Transform rewardContainer;
        public GameObject itemReward;
        public RankInfoSlider sliderRank;
        public TextMeshProUGUI txtPoint;
        public Image bg;
        public GameObject redPoint;

        private readonly List<GameObject> rewardItems = new List<GameObject>();
        private int entryId;

        public bool SetItem(RankInfoEntry entry, RankInfoEntry previous, RankInfoEntry next, int point)
        {
            if (entry == null) return false;
            entryId = entry.id;
            bool locked = point < entry.score;

            if (bg != null) bg.color = locked ? Color.gray : Color.white;
            if (txtPoint != null) txtPoint.text = entry.score.ToString();
            bool claimed = IsClaimed != null && IsClaimed(entry.id);
            if (redPoint != null) redPoint.SetActive(!claimed && CanClaim != null && CanClaim(entry.id));

            if (itemReward != null) itemReward.SetActive(false);
            // Remove preview cards stored on the prefab before adding the real
            // rewards for this milestone. Otherwise old 1,500/100 examples remain.
            HideAuthoredRewardChildren();
            foreach (var item in rewardItems)
            {
                if (item != null) item.SetActive(false);
            }

            if (entry.rewards != null && rewardContainer != null && itemReward != null)
            {
                for (int i = 0; i < entry.rewards.Length; i++)
                {
                    GameObject item = null;
                    if (i < rewardItems.Count) item = rewardItems[i];
                    else
                    {
                        item = Instantiate(itemReward, rewardContainer);
                        rewardItems.Add(item);
                    }
                    var view = item.GetComponent<RankInfoRewardItemView>();
                    if (view == null) view = item.AddComponent<RankInfoRewardItemView>();
                    view.Bind(entry.rewards[i], locked || claimed, claimed);
                    var button = item.GetComponent<Button>();
                    if (button == null) button = item.AddComponent<Button>();
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(ClaimCurrentEntry);
                }
            }

            return sliderRank != null && sliderRank.SetProgress(entry, previous, next, point);
        }

        private void ClaimCurrentEntry()
        {
            if (entryId > 0 && Claim != null) Claim(entryId);
        }

        private void HideAuthoredRewardChildren()
        {
            if (rewardContainer == null) return;
            for (int i = 0; i < rewardContainer.childCount; i++)
            {
                var child = rewardContainer.GetChild(i).gameObject;
                if (child != itemReward) child.SetActive(false);
            }
        }
    }
}
