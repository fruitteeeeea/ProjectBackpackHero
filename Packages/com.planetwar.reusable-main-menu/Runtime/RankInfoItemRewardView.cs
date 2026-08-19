using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    public sealed class RankInfoItemRewardView : MonoBehaviour
    {
        public Transform rewardContainer;
        public GameObject itemReward;
        public RankInfoSlider sliderRank;
        public TextMeshProUGUI txtPoint;
        public Image bg;
        public GameObject redPoint;

        private readonly List<GameObject> rewardItems = new List<GameObject>();

        public bool SetItem(RankInfoEntry entry, RankInfoEntry previous, RankInfoEntry next, int point)
        {
            if (entry == null) return false;
            bool locked = point < entry.score;

            if (bg != null) bg.color = locked ? Color.gray : Color.white;
            if (txtPoint != null) txtPoint.text = entry.score.ToString();
            if (redPoint != null) redPoint.SetActive(false);

            if (itemReward != null) itemReward.SetActive(false);
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
                    view.Bind(entry.rewards[i], locked);
                }
            }

            return sliderRank != null && sliderRank.SetProgress(entry, previous, next, point);
        }
    }
}
