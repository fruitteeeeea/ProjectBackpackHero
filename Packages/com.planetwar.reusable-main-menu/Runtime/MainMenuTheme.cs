using TMPro;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    [CreateAssetMenu(menuName = "PlanetWar/Main Menu Theme", fileName = "MainMenuTheme")]
    public sealed class MainMenuTheme : ScriptableObject
    {
        [Header("Optional visual overrides")]
        public Sprite background;
        public Sprite headerPanel;
        public Sprite primaryButton;
        public Sprite settingsButton;
        public Sprite profileButton;
        public Sprite[] bottomIcons = new Sprite[5];

        [Header("Palette")]
        public Color panelTint = Color.white;
        public Color textColor = Color.white;
        public Color accentColor = new Color(1f, .78f, .14f);

        [Header("Static display data")]
        public string playerName = "PLAYER";
        public string levelName = "BATTLE 1";
        public string gold = "1200";
        public string diamonds = "50";
        [Range(0f, 1f)] public float progress = .35f;
        public TMP_FontAsset font;
    }
}
