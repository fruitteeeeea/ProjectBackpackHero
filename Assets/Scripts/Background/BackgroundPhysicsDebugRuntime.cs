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

        public BackgroundPlanetPhysicsSettings PlanetSettings =>
            background != null && background.Planets.Count > 0
                ? background.Planets[0].PhysicsSettings
                : default;

        public BackgroundTouchPhysicsSettings TouchSettings =>
            background != null && background.TouchInteractor != null
                ? background.TouchInteractor.PhysicsSettings
                : default;

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
            if (background == null)
            {
                return;
            }

            foreach (var planet in background.Planets)
            {
                if (planet != null)
                {
                    planet.SetPhysicsSettings(settings);
                }
            }
        }

        public void SetTouchSettings(BackgroundTouchPhysicsSettings settings)
        {
            background?.TouchInteractor?.SetPhysicsSettings(settings);
        }
    }
}
