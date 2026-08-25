using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    public sealed class RankInfoRewardItemView : MonoBehaviour
    {
        public Image icon;
        public Image resIcon;
        public Image packIcon;
        public TextMeshProUGUI countText;
        public GameObject claimedMark;

        public void Bind(RankInfoReward reward, bool locked, bool claimed = false)
        {
            if (reward == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (countText != null)
            {
                countText.text = reward.count.ToString();
                countText.color = locked ? Color.gray : Color.white;
            }
            if (claimedMark != null) claimedMark.SetActive(claimed);

            if (resIcon != null)
            {
                resIcon.gameObject.SetActive(!reward.isPack);
                if (reward.icon != null) resIcon.sprite = reward.icon;
                resIcon.color = locked ? Color.gray : Color.white;
            }
            if (packIcon != null)
            {
                packIcon.gameObject.SetActive(reward.isPack);
                if (reward.icon != null) packIcon.sprite = reward.icon;
                packIcon.color = locked ? Color.gray : Color.white;
            }
            if (icon != null)
            {
                if (reward.icon != null) icon.sprite = reward.icon;
                icon.color = locked ? Color.gray : Color.white;
            }

            var rootImage = GetComponent<Image>();
            if (rootImage != null) rootImage.color = locked ? Color.gray : Color.white;
            gameObject.SetActive(true);
        }
    }
}
