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
            EnsureBindings();
            float value = 0f;
            if (next == null || previous == null)
            {
                if (point <= entry.score && entry.id != 1001) value = 0f;
                else value = (float)point / entry.score;
            }
            else
            {
                if (point <= previous.score)
                {
                    value = 0f;
                }
                else
                {
                    float sum = entry.score - previous.score;
                    float cur = point - previous.score;
                    value = cur / sum;
                    if (entry.type == 1) value *= 0.5f;
                }
            }

            bool isActive = point == entry.score || (value > 0f && value < 1f);
            if (sliderProgress != null) sliderProgress.value = value;
            if (objSlideArea != null) objSlideArea.SetActive(isActive);
            if (txtProgress != null) txtProgress.text = point.ToString();
            return isActive;
        }

        public void SetValue(float value, int point)
        {
            EnsureBindings();
            if (sliderProgress != null) sliderProgress.value = value;
            if (objSlideArea != null) objSlideArea.SetActive(value > 0f && value < 1f);
            if (txtProgress != null) txtProgress.text = point.ToString();
        }

        private void EnsureBindings()
        {
            if (sliderProgress == null) sliderProgress = GetComponentInChildren<Slider>(true);
            if (txtProgress == null && sliderProgress != null && sliderProgress.handleRect != null)
                txtProgress = sliderProgress.handleRect.GetComponentInChildren<TextMeshProUGUI>(true);
            if (objSlideArea == null)
            {
                Transform area = FindChild(transform, "Handle Slide Area");
                // The migrated reward template keeps its handle area as a sibling of
                // Slider. The original prefab serializes that container directly;
                // derive the same reference from Slider.handleRect for existing
                // baked menus where the serialized reference is missing.
                if (area == null && sliderProgress != null && sliderProgress.handleRect != null)
                    area = sliderProgress.handleRect.parent;
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
