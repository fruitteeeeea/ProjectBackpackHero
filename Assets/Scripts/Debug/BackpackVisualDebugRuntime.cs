using System;
using UnityEngine;

namespace BackpackHero.Debugging
{
    [DefaultExecutionOrder(-9998)]
    public sealed class BackpackVisualDebugRuntime : MonoBehaviour
    {
        private const string DefaultSettingsResourceName =
            "BackpackVisual/BackpackVisualDebugSettings";

        [SerializeField] private BackpackVisualDebugSettings defaultSettings;
        private BackpackVisualSettings settings = BackpackVisualSettings.Default;

        public static BackpackVisualDebugRuntime Instance { get; private set; }
        public static event Action<BackpackVisualSettings> SettingsChanged;
        public static event Action<BackpackVisualDebugRuntime> InstanceAvailable;
        public static event Action InstanceUnavailable;

        public BackpackVisualDebugSettings DefaultSettings => defaultSettings;
        public BackpackVisualSettings Settings => settings;
        public static BackpackVisualSettings CurrentSettings =>
            Instance != null ? Instance.settings : BackpackVisualSettings.Default;
        public static bool AreVisualOverridesEnabled =>
            CurrentSettings.OverridesEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntime()
        {
            if (Instance != null) return;
            GameObject runtimeObject = new(nameof(BackpackVisualDebugRuntime));
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<BackpackVisualDebugRuntime>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            defaultSettings = Resources.Load<BackpackVisualDebugSettings>(
                DefaultSettingsResourceName);
            LoadSavedValues();
            InstanceAvailable?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            InstanceUnavailable?.Invoke();
        }

        public void SetSettings(BackpackVisualSettings values)
        {
            settings = values;
            SettingsChanged?.Invoke(settings);
        }

        public void LoadSavedValues() => SetSettings(defaultSettings != null
            ? defaultSettings.GetValues()
            : BackpackVisualSettings.Default);

        public void SetDefaultSettings(BackpackVisualDebugSettings target)
        {
            if (target == null || defaultSettings == target) return;
            defaultSettings = target;
            LoadSavedValues();
        }
    }
}
