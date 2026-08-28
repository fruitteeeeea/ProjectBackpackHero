using System;

namespace BackpackHero.Debugging
{
    public enum BackpackShipStyle
    {
        Style1 = 1,
        Style2 = 2
    }

    /// <summary>美术资源运行时快照。</summary>
    public readonly struct ArtAssetDebugSettingsValue :
        IEquatable<ArtAssetDebugSettingsValue>
    {
        public static ArtAssetDebugSettingsValue Default => new(
            BackpackShipStyle.Style2);

        public ArtAssetDebugSettingsValue(BackpackShipStyle backpackShipStyle)
        {
            BackpackShipStyle = Normalize(backpackShipStyle);
        }

        public BackpackShipStyle BackpackShipStyle { get; }

        public ArtAssetDebugSettingsValue WithBackpackShipStyle(
            BackpackShipStyle value) => new(value);

        public bool Equals(ArtAssetDebugSettingsValue other) =>
            BackpackShipStyle == other.BackpackShipStyle;

        public override bool Equals(object obj) => obj is
            ArtAssetDebugSettingsValue other && Equals(other);

        public override int GetHashCode() => (int)BackpackShipStyle;

        public static BackpackShipStyle Normalize(BackpackShipStyle value) =>
            value == BackpackShipStyle.Style1
                ? BackpackShipStyle.Style1
                : BackpackShipStyle.Style2;
    }
}
