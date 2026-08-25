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
        [Tooltip("The visible title/Scroe label in the authored rank banner.")]
        public TextMeshProUGUI bannerScoreText;
        public Image imgIcon;
        public Image imgBlack;
        public Transform rewardContainer;
        public GameObject itemReward;
        public RankInfoSlider sliderRank;

        private readonly List<GameObject> rewardItems = new List<GameObject>();

        public bool SetItem(RankInfoEntry entry, RankInfoEntry previous, RankInfoEntry next, int point, int currentMissionId)
        {
            if (entry == null) return false;
            bool locked = currentMissionId < entry.id;

            if (txtName != null) txtName.text = $"Rank {entry.level}";
            if (txtIconName != null) txtIconName.text = entry.enName;
            SetScoreLabels(entry.score);
            if (imgIcon != null)
            {
                imgIcon.sprite = entry.iconSprite;
                imgIcon.enabled = entry.iconSprite != null;
            }
            if (imgBlack != null)
            {
                imgBlack.sprite = entry.lockedIconSprite;
                imgBlack.enabled = entry.lockedIconSprite != null;
                imgBlack.gameObject.SetActive(locked && entry.lockedIconSprite != null);
            }

            bool hasRewards = entry.rewards != null && entry.rewards.Length > 0;
            if (rewardContainer != null) rewardContainer.gameObject.SetActive(hasRewards);
            if (itemReward != null) itemReward.SetActive(false);
            // The authored template contains preview reward cards. They are not part
            // of this promotion node and must never leak into a cloned row.
            HideAuthoredRewardChildren();
            foreach (var item in rewardItems)
            {
                if (item != null) item.SetActive(false);
            }

            if (hasRewards && rewardContainer != null && itemReward != null)
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

        private void HideAuthoredRewardChildren()
        {
            if (rewardContainer == null) return;
            for (int i = 0; i < rewardContainer.childCount; i++)
            {
                var child = rewardContainer.GetChild(i).gameObject;
                if (child != itemReward) child.SetActive(false);
            }
        }

        private void SetScoreLabels(int score)
        {
            string value = score.ToString();
            if (txtPoint != null) txtPoint.text = value;

            // Some imported ItemRankMain prefabs kept txtPoint bound to an invisible
            // preview label. The player-facing banner is title/Scroe, so always bind
            // it directly and do not leave its 100000 sample value on screen.
            if (bannerScoreText == null)
            {
                Transform scoreTransform = transform.Find("title/Scroe");
                if (scoreTransform != null) bannerScoreText = scoreTransform.GetComponent<TextMeshProUGUI>();
            }
            if (bannerScoreText != null) bannerScoreText.text = value;
        }
    }
}
