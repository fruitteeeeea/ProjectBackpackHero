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

        private TextMeshProUGUI handleProgressText;

        public bool SetProgress(RankInfoEntry entry, RankInfoEntry previous, RankInfoEntry next, int point)
        {
            EnsureBindings();
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
            if (objSlideArea != null) objSlideArea.SetActive(isActive);
            return isActive;
        }

        public void SetValue(float value, int point)
        {
            EnsureBindings();
            if (sliderProgress != null) sliderProgress.value = Mathf.Clamp01(value);
            if (objSlideArea != null) objSlideArea.SetActive(value > 0f && value < 1f);
            if (txtProgress != null) txtProgress.text = point.ToString();
            if (handleProgressText != null && handleProgressText != txtProgress) handleProgressText.text = point.ToString();
        }

        private void EnsureBindings()
        {
            if (sliderProgress == null) sliderProgress = GetComponentInChildren<Slider>(true);
            if (sliderProgress != null && handleProgressText == null && sliderProgress.handleRect != null)
                handleProgressText = sliderProgress.handleRect.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txtProgress == null) txtProgress = handleProgressText;
            if (objSlideArea == null)
            {
                Transform area = FindChild(transform, "Handle Slide Area");
                if (area != null) objSlideArea = area.gameObject;
            }
        }

        private static Transform FindChild(Transform root, string childName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName) return child;
                Transform found = FindChild(child, childName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
