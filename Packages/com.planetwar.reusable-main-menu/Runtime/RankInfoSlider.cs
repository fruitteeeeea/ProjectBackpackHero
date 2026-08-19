using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    public sealed class RankInfoSlider : MonoBehaviour
    {
        public Slider sliderProgress;
        public TextMeshProUGUI txtProgress;
        public GameObject objSlideArea;

        public bool SetProgress(RankInfoEntry entry, RankInfoEntry previous, RankInfoEntry next, int point)
        {
            float value = 0f;
            if (next == null || previous == null)
            {
                if (point <= entry.score && entry.id != 1001) value = 0f;
                else value = (float)point / Mathf.Max(1, entry.score);
            }
            else
            {
                if (point <= previous.score)
                {
                    value = 0f;
                }
                else
                {
                    float sum = Mathf.Max(1, entry.score - previous.score);
                    float cur = point - previous.score;
                    value = Mathf.Clamp01(cur / sum);
                    if (entry.type == 1) value *= 0.5f;
                }
            }

            bool isActive = point == entry.score || (value > 0f && value < 1f);
            SetValue(value, point);
            objSlideArea.SetActive(isActive);
            return isActive;
        }

        public void SetValue(float value, int point)
        {
            if (sliderProgress != null) sliderProgress.value = Mathf.Clamp01(value);
            if (objSlideArea != null) objSlideArea.SetActive(value > 0f && value < 1f);
            if (txtProgress != null) txtProgress.text = point.ToString();
        }
    }
}
