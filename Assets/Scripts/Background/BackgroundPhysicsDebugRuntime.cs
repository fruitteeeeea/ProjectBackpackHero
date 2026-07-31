using System;
using UnityEngine;

namespace BackpackHero.Background
{
    /// <summary>星图物理调试面板的运行时桥接层，不依赖 UnityEditor。</summary>
    public sealed class BackgroundPhysicsDebugRuntime : MonoBehaviour
    {
        public static BackgroundPhysicsDebugRuntime Instance { get; private set; }
        public static event Action<BackgroundPhysicsDebugRuntime> InstanceAvailable;
        public static event Action InstanceUnavailable;

        private ConstellationBackgroundBootstrap background;
        private BackgroundPlanetPhysicsSettings planetSettings;
        private BackgroundTouchPhysicsSettings touchSettings;

        public BackgroundPlanetPhysicsSettings PlanetSettings => planetSettings;
        public BackgroundTouchPhysicsSettings TouchSettings => touchSettings;

        public int PlanetCount => background?.Planets.Count ?? 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            background = GetComponent<ConstellationBackgroundBootstrap>();
            if (background == null)
            {
                enabled = false;
                return;
            }

            // 调试器的初始状态只读取脚本默认配置，不读取场景遗留的序列化值。
            planetSettings = BackgroundPlanet.DefaultPhysicsSettings;
            touchSettings = BackgroundTouchInteractor.DefaultPhysicsSettings;
            ApplyPlanetSettings();
            ApplyTouchSettings();
            Instance = this;
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

        public void SetPlanetSettings(BackgroundPlanetPhysicsSettings settings)
        {
            planetSettings = settings;
            ApplyPlanetSettings();
        }

        public void SetTouchSettings(BackgroundTouchPhysicsSettings settings)
        {
            touchSettings = settings;
            ApplyTouchSettings();
        }

        private void ApplyPlanetSettings()
        {
            if (background == null)
            {
                return;
            }
            foreach (var planet in background.Planets)
            {
                if (planet != null)
                {
                    planet.SetPhysicsSettings(planetSettings);
                }
            }
        }

        private void ApplyTouchSettings()
        {
            background?.TouchInteractor?.SetPhysicsSettings(touchSettings);
        }
    }
}
