using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>可持久化的美术资源选择。</summary>
    [CreateAssetMenu(fileName = "ArtAssetDebugSettings",
        menuName = "Debug/Art Asset Settings")]
    public sealed class ArtAssetDebugSettings : ScriptableObject
    {
        [SerializeField] private BackpackShipStyle backpackShipStyle =
            BackpackShipStyle.Style2;
        [SerializeField] private ItemBaseStyle itemBaseStyle =
            ItemBaseStyle.Style2;

        [Header("Item Base Style 2")]
        [SerializeField] private Sprite squareStyle2;
        [SerializeField] private Sprite verticalBarStyle2;
        [SerializeField] private Sprite horizontalBarStyle2;
        [SerializeField] private Sprite lMissingBottomLeftStyle2;
        [SerializeField] private Sprite lMissingBottomRightStyle2;
        [SerializeField] private Sprite lMissingTopLeftStyle2;
        [SerializeField] private Sprite lMissingTopRightStyle2;

        public void SetValues(ArtAssetDebugSettingsValue values)
        {
            backpackShipStyle = ArtAssetDebugSettingsValue.Normalize(
                values.BackpackShipStyle);
            itemBaseStyle = ArtAssetDebugSettingsValue.Normalize(
                values.ItemBaseStyle);
        }

        public ArtAssetDebugSettingsValue GetValues() => new(
            backpackShipStyle, itemBaseStyle);

        public Sprite GetStyle2ItemBaseSprite(ItemBaseShape shape) => shape switch
        {
            ItemBaseShape.Square => squareStyle2,
            ItemBaseShape.VerticalBar => verticalBarStyle2,
            ItemBaseShape.HorizontalBar => horizontalBarStyle2,
            ItemBaseShape.LMissingBottomLeft => lMissingBottomLeftStyle2,
            ItemBaseShape.LMissingBottomRight => lMissingBottomRightStyle2,
            ItemBaseShape.LMissingTopLeft => lMissingTopLeftStyle2,
            ItemBaseShape.LMissingTopRight => lMissingTopRightStyle2,
            _ => null
        };

#if UNITY_EDITOR
        private void OnValidate() => SetValues(GetValues());
#endif
    }
}
