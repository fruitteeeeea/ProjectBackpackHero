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

        public void SetValues(ArtAssetDebugSettingsValue values) =>
            backpackShipStyle = ArtAssetDebugSettingsValue.Normalize(
                values.BackpackShipStyle);

        public ArtAssetDebugSettingsValue GetValues() => new(backpackShipStyle);

#if UNITY_EDITOR
        private void OnValidate() => SetValues(GetValues());
#endif
    }
}
