using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>One pre-authored leaderboard row. It never creates child UI.</summary>
    public sealed class RankRowView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image countryFlag;
        [SerializeField] private Image medal;
        [SerializeField] private Sprite[] medalSprites;
        [SerializeField] private TMP_Text rankLabel;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text scoreLabel;

        public void Bind(int rank, RankEntry entry)
        {
            var isTopThree = rank <= 3;
            gameObject.SetActive(entry != null);
            if (entry == null) return;

            if (background != null) background.color = entry.isCurrentPlayer
                ? new Color(0.38f, 0.55f, 0.98f, 1f)
                : new Color(0.65f, 0.76f, 0.96f, 1f);
            if (countryFlag != null)
            {
                countryFlag.sprite = entry.countryFlag;
                countryFlag.gameObject.SetActive(entry.countryFlag != null);
            }
            if (medal != null)
            {
                medal.gameObject.SetActive(isTopThree);
                if (isTopThree && medalSprites != null && medalSprites.Length >= rank) medal.sprite = medalSprites[rank - 1];
            }
            if (rankLabel != null)
            {
                rankLabel.text = rank.ToString();
                rankLabel.gameObject.SetActive(!isTopThree);
            }
            if (nameLabel != null) nameLabel.text = entry.displayName;
            if (scoreLabel != null) scoreLabel.text = entry.score.ToString();
        }
    }
}
