using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>注册一个背包飞船样式对象，并由全局美术配置控制显隐。</summary>
    [DisallowMultipleComponent]
    public sealed class BackpackShipStyleTarget : MonoBehaviour
    {
        [SerializeField] private BackpackShipStyle style = BackpackShipStyle.Style2;

        private void Awake() => ArtAssetDebugRuntime.Instance?.Register(this);

        private void OnEnable() => ArtAssetDebugRuntime.Instance?.Register(this);

        private void OnDestroy() => ArtAssetDebugRuntime.Instance?.Unregister(this);

        internal void ApplyStyle(BackpackShipStyle selectedStyle)
        {
            bool shouldBeActive = style == selectedStyle;
            if (gameObject.activeSelf != shouldBeActive)
            {
                gameObject.SetActive(shouldBeActive);
            }
        }
    }
}
