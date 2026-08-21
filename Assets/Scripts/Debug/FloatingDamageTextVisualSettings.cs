using System;
using TMPro;
using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>伤害飘字在调试运行时使用的字体与字号快照。</summary>
    public readonly struct FloatingDamageTextVisualSettings :
        IEquatable<FloatingDamageTextVisualSettings>
    {
        public const float MinimumFontSize = 0.1f;

        public FloatingDamageTextVisualSettings(
            TMP_FontAsset font,
            float fontSize)
        {
            Font = font;
            FontSize = Mathf.Max(MinimumFontSize, fontSize);
        }

        public TMP_FontAsset Font { get; }
        public float FontSize { get; }

        public bool Equals(FloatingDamageTextVisualSettings other) =>
            Font == other.Font && Mathf.Approximately(FontSize, other.FontSize);

        public override bool Equals(object obj) =>
            obj is FloatingDamageTextVisualSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Font != null ? Font.GetHashCode() : 0) * 31) +
                    FontSize.GetHashCode();
            }
        }
    }
}
