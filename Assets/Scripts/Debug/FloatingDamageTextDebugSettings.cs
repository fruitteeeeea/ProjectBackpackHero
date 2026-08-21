using TMPro;
using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>可持久化的伤害飘字字体与字号调试配置。</summary>
    [CreateAssetMenu(
        fileName = "FloatingDamageTextDebugSettings",
        menuName = "Debug/Floating Damage Text Settings")]
    public sealed class FloatingDamageTextDebugSettings : ScriptableObject
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField, Min(FloatingDamageTextVisualSettings.MinimumFontSize)]
        private float fontSize = 4f;

        public void SetValues(FloatingDamageTextVisualSettings values)
        {
            font = values.Font;
            fontSize = Mathf.Max(
                FloatingDamageTextVisualSettings.MinimumFontSize,
                values.FontSize);
        }

        public FloatingDamageTextVisualSettings GetValues() => new(
            font,
            Mathf.Max(FloatingDamageTextVisualSettings.MinimumFontSize,
                fontSize));

#if UNITY_EDITOR
        private void OnValidate() => SetValues(GetValues());
#endif
    }
}
