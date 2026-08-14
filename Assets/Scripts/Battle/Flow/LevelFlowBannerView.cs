using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 回合横幅的场景视图与可视化配置。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelFlowBannerView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Image bannerBackground;

        [SerializeField]
        private TextMeshProUGUI bannerLabel;

        [Header("Timing")]
        [SerializeField, Min(0.3f)]
        private float displayDuration = 2f;

        [SerializeField, Min(0.01f)]
        private float flickerInterval = 0.05f;

        [Header("Colors")]
        [SerializeField]
        private Color roundColor = new Color32(
            0xFD, 0xD8, 0x35, 0xFF);

        [SerializeField]
        private Color winColor = new Color32(
            0x21, 0x96, 0xF3, 0xFF);

        [SerializeField]
        private Color loseColor = new Color32(
            0xE5, 0x39, 0x35, 0xFF);

        [Header("Layout")]
        [SerializeField, Min(1f)]
        private float bannerHeight = 128f;

        [SerializeField, Min(1f)]
        private float fontSize = 64f;

        [SerializeField]
        private Color textColor = Color.white;

        public float DisplayDuration => displayDuration;
        public float FlickerInterval => flickerInterval;
        public Color RoundColor => roundColor;
        public Color WinColor => winColor;
        public Color LoseColor => loseColor;

        private void Awake()
        {
            ApplyStyle();
            SetVisible(false);
        }

        public void SetContent(string text, Color color)
        {
            ApplyStyle();

            if (bannerLabel != null)
            {
                bannerLabel.text = text;
            }
        }

        public void SetVisible(bool visible)
        {
            if (bannerBackground != null)
            {
                bannerBackground.gameObject.SetActive(visible);
            }
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void ApplyStyle()
        {
            if (bannerBackground != null)
            {
                RectTransform rect = bannerBackground.rectTransform;
                float halfHeight = bannerHeight * 0.5f;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(0f, -halfHeight);
                rect.offsetMax = new Vector2(0f, halfHeight);
            }

            if (bannerLabel != null)
            {
                bannerLabel.fontSize = fontSize;
                bannerLabel.color = textColor;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            displayDuration = Mathf.Max(0.3f, displayDuration);
            flickerInterval = Mathf.Max(0.01f, flickerInterval);
            bannerHeight = Mathf.Max(1f, bannerHeight);
            fontSize = Mathf.Max(1f, fontSize);
            ApplyStyle();
        }
#endif
    }
}
