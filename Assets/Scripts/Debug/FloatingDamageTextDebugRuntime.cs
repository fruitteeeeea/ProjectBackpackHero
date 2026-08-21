using System;
using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>全局伤害飘字调试配置的运行时桥接层。</summary>
    [DefaultExecutionOrder(-9997)]
    public sealed class FloatingDamageTextDebugRuntime : MonoBehaviour
    {
        private const string DefaultSettingsResourceName =
            "FloatingDamageText/FloatingDamageTextDebugSettings";

        [SerializeField] private FloatingDamageTextDebugSettings defaultSettings;
        private FloatingDamageTextVisualSettings settings;

        public static FloatingDamageTextDebugRuntime Instance { get; private set; }
        public static event Action<FloatingDamageTextVisualSettings> SettingsChanged;
        public static event Action<FloatingDamageTextDebugRuntime> InstanceAvailable;
        public static event Action InstanceUnavailable;

        public FloatingDamageTextDebugSettings DefaultSettings => defaultSettings;
        public FloatingDamageTextVisualSettings Settings => settings;

        public static FloatingDamageTextVisualSettings CurrentSettings =>
            Instance != null ? Instance.settings : default;

        /// <summary>
        /// 没有默认配置资产，或字体被清空时回退至飘字原始样式资产。
        /// </summary>
        public static bool HasRuntimeSettings =>
            Instance != null && Instance.settings.Font != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntime()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject runtimeObject = new(nameof(FloatingDamageTextDebugRuntime));
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<FloatingDamageTextDebugRuntime>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            defaultSettings = Resources.Load<FloatingDamageTextDebugSettings>(
                DefaultSettingsResourceName);
            LoadSavedValues();
            InstanceAvailable?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            Instance = null;
            InstanceUnavailable?.Invoke();
        }

        public void SetSettings(FloatingDamageTextVisualSettings values)
        {
            settings = values;
            SettingsChanged?.Invoke(settings);
        }

        public void LoadSavedValues()
        {
            SetSettings(defaultSettings != null
                ? defaultSettings.GetValues()
                : default);
        }

        public void SetDefaultSettings(FloatingDamageTextDebugSettings target)
        {
            if (target == null || defaultSettings == target)
            {
                return;
            }

            defaultSettings = target;
            LoadSavedValues();
        }
    }
}
