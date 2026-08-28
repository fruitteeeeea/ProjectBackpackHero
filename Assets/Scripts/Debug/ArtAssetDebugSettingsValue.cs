using System;

namespace BackpackHero.Debugging
{
    public enum BackpackShipStyle
    {
        Style1 = 1,
        Style2 = 2
    }

    public enum ItemBaseStyle
    {
        Style1 = 1,
        Style2 = 2
    }

    public enum ItemBaseShape
    {
        Square = 1,
        VerticalBar = 2,
        HorizontalBar = 3,
        LMissingBottomLeft = 4,
        LMissingBottomRight = 5,
        LMissingTopLeft = 6,
        LMissingTopRight = 7
    }

    /// <summary>美术资源运行时快照。</summary>
    public readonly struct ArtAssetDebugSettingsValue :
        IEquatable<ArtAssetDebugSettingsValue>
    {
        public static ArtAssetDebugSettingsValue Default => new(
            global::BackpackHero.Debugging.BackpackShipStyle.Style2,
            global::BackpackHero.Debugging.ItemBaseStyle.Style2);

        public ArtAssetDebugSettingsValue(
            BackpackShipStyle backpackShipStyle,
            ItemBaseStyle itemBaseStyle)
        {
            BackpackShipStyle = Normalize(backpackShipStyle);
            ItemBaseStyle = Normalize(itemBaseStyle);
        }

        public ArtAssetDebugSettingsValue(BackpackShipStyle backpackShipStyle)
            : this(backpackShipStyle,
                global::BackpackHero.Debugging.ItemBaseStyle.Style2)
        {
        }

        public BackpackShipStyle BackpackShipStyle { get; }
        public ItemBaseStyle ItemBaseStyle { get; }

        public ArtAssetDebugSettingsValue WithBackpackShipStyle(
            BackpackShipStyle value) => new(value, ItemBaseStyle);

        public ArtAssetDebugSettingsValue WithItemBaseStyle(
            ItemBaseStyle value) => new(BackpackShipStyle, value);

        public bool Equals(ArtAssetDebugSettingsValue other) =>
            BackpackShipStyle == other.BackpackShipStyle &&
            ItemBaseStyle == other.ItemBaseStyle;

        public override bool Equals(object obj) => obj is
            ArtAssetDebugSettingsValue other && Equals(other);

        public override int GetHashCode() =>
            ((int)BackpackShipStyle * 397) ^ (int)ItemBaseStyle;

        public static BackpackShipStyle Normalize(BackpackShipStyle value) =>
            value == global::BackpackHero.Debugging.BackpackShipStyle.Style1
                ? global::BackpackHero.Debugging.BackpackShipStyle.Style1
                : global::BackpackHero.Debugging.BackpackShipStyle.Style2;

        public static ItemBaseStyle Normalize(ItemBaseStyle value) =>
            value == global::BackpackHero.Debugging.ItemBaseStyle.Style1
                ? global::BackpackHero.Debugging.ItemBaseStyle.Style1
                : global::BackpackHero.Debugging.ItemBaseStyle.Style2;
    }
}
