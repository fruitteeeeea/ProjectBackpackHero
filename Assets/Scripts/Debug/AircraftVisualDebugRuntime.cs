using System;
using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>供飞机视觉和编辑器调试页共用的全局运行时配置。</summary>
    [DefaultExecutionOrder(-9998)]
    public sealed class AircraftVisualDebugRuntime : MonoBehaviour
    {
        private const string DefaultSettingsResourceName =
            "AircraftVisual/AircraftVisualDebugSettings";

        [SerializeField] private AircraftVisualDebugSettings defaultSettings;
        private AircraftVisualSettings settings = AircraftVisualSettings.Default;

        public static AircraftVisualDebugRuntime Instance { get; private set; }
        public static event Action<AircraftVisualSettings> SettingsChanged;
        public static event Action<AircraftVisualDebugRuntime> InstanceAvailable;
        public static event Action InstanceUnavailable;

        public AircraftVisualDebugSettings DefaultSettings => defaultSettings;
        public AircraftVisualSettings Settings => settings;

        public static AircraftVisualSettings CurrentSettings =>
            Instance != null ? Instance.settings : AircraftVisualSettings.Default;

        public static bool AreVisualOverridesEnabled =>
            CurrentSettings.AircraftVisualOverridesEnabled;

        public static float GetExplosiveImpactRangeMultiplier() =>
            AreVisualOverridesEnabled
                ? CurrentSettings.ExplosiveImpactRangeMultiplier
                : 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntime()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject runtimeObject = new(nameof(AircraftVisualDebugRuntime));
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<AircraftVisualDebugRuntime>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            defaultSettings = Resources.Load<AircraftVisualDebugSettings>(
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

        public void SetSettings(AircraftVisualSettings values)
        {
            settings = values;
            SettingsChanged?.Invoke(settings);
        }

        public void LoadSavedValues()
        {
            SetSettings(defaultSettings != null
                ? defaultSettings.GetValues()
                : AircraftVisualSettings.Default);
        }

        public void SetDefaultSettings(AircraftVisualDebugSettings target)
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
