using TMPro;
using BackpackHero.Debugging;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 为 FEEL 对象池生成的 TMP 飘字应用独立的颜色、描边和排序。
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class FloatingTextTmpStyle : MonoBehaviour
    {
        [SerializeField] private FloatingDamageTextSettings settings;
        [SerializeField] private BattleFaction faction;

        private TextMeshPro text;
        private float spawnedAt;

        private void Awake()
        {
            text = GetComponent<TextMeshPro>();
            settings?.ApplyTextStyle(text, faction);
        }

        private void OnEnable()
        {
            spawnedAt = Time.time;

            if (text == null)
            {
                text = GetComponent<TextMeshPro>();
            }

            RefreshStyle();
            FloatingDamageTextDebugRuntime.SettingsChanged += OnSettingsChanged;
        }

        private void OnDisable()
        {
            FloatingDamageTextDebugRuntime.SettingsChanged -= OnSettingsChanged;
        }

        /// <summary>供飘字调试面板立即刷新当前可见对象。</summary>
        public void RefreshStyle()
        {
            settings?.ApplyTextStyle(text, faction);
        }

        private void OnSettingsChanged(
            FloatingDamageTextVisualSettings _)
        {
            RefreshStyle();
        }

        private void Update()
        {
            if (settings == null || text == null)
            {
                return;
            }

            float lifetime = Mathf.Max(
                0.01f,
                settings.MaximumLifetime);
            float normalizedLifetime =
                (Time.time - spawnedAt) / lifetime;

            text.alpha = settings.EvaluateOpacity(
                normalizedLifetime);
        }
    }
}
