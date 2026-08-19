using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    public sealed class RankInfoItemMainView : MonoBehaviour
    {
        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtIconName;
        public TextMeshProUGUI txtPoint;
        public Image imgIcon;
        public Image imgBlack;
        public Transform rewardContainer;
        public GameObject itemReward;
        public RankInfoSlider sliderRank;

        private readonly List<GameObject> rewardItems = new List<GameObject>();

        public bool SetItem(RankInfoEntry entry, RankInfoEntry previous, RankInfoEntry next, int point, int currentMissionId)
        {
            if (entry == null) return false;
            bool locked = currentMissionId <= entry.id;

            if (txtName != null) txtName.text = $"Rank {entry.level}";
            if (txtIconName != null) txtIconName.text = entry.enName;
            if (txtPoint != null) txtPoint.text = entry.score.ToString();
            if (imgBlack != null) imgBlack.gameObject.SetActive(locked);
            if (imgIcon != null) imgIcon.sprite = entry.iconSprite;
            if (imgBlack != null) imgBlack.sprite = entry.lockedIconSprite;

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
