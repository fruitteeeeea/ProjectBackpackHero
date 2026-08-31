using System;
using UnityEngine;

namespace BackpackHero.Debugging
{
    public enum ItemQuality
    {
        Green = 1,
        Blue = 2,
        Purple = 3,
        Orange = 4
    }

    /// <summary>Maps an in-match item quality and base shape to its authored sprite.</summary>
    [CreateAssetMenu(fileName = "ItemQualityPalette",
        menuName = "Debug/Item Quality Palette")]
    public sealed class ItemQualityPalette : ScriptableObject
    {
        [Serializable]
        private struct QualitySprites
        {
            [SerializeField] private ItemQuality quality;
            [SerializeField] private Sprite verticalBar;
            [SerializeField] private Sprite horizontalBar;
            [SerializeField] private Sprite lMissingBottomLeft;
            [SerializeField] private Sprite lMissingBottomRight;
            [SerializeField] private Sprite lMissingTopLeft;
            [SerializeField] private Sprite lMissingTopRight;

            public ItemQuality Quality => quality;

            public Sprite GetSprite(ItemBaseShape shape) => shape switch
            {
                ItemBaseShape.VerticalBar => verticalBar,
                ItemBaseShape.HorizontalBar => horizontalBar,
                ItemBaseShape.LMissingBottomLeft => lMissingBottomLeft,
                ItemBaseShape.LMissingBottomRight => lMissingBottomRight,
                ItemBaseShape.LMissingTopLeft => lMissingTopLeft,
                ItemBaseShape.LMissingTopRight => lMissingTopRight,
                _ => null
            };
        }

        [SerializeField] private QualitySprites[] qualitySprites = Array.Empty<QualitySprites>();

        public ItemQuality GetQualityForLevel(int itemLevel) =>
            Mathf.Clamp(itemLevel, 1, 3) switch
            {
                1 => ItemQuality.Green,
                2 => ItemQuality.Blue,
                _ => ItemQuality.Purple
            };

        public Sprite GetSprite(ItemQuality quality, ItemBaseShape shape)
        {
            foreach (QualitySprites entry in qualitySprites)
            {
                if (entry.Quality == quality) return entry.GetSprite(shape);
            }

            return null;
        }

        public Sprite GetSpriteForLevel(int itemLevel, ItemBaseShape shape) =>
            GetSprite(GetQualityForLevel(itemLevel), shape);
    }
}
