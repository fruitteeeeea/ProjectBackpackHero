using System;
using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>
    /// EditorWindow 与战斗代码之间的 Runtime 安全桥接层。
    /// 它在每次游戏启动时创建，并在所有场景中保持唯一。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class GamePacingDebugRuntime : MonoBehaviour
    {
        private const string DefaultSettingsResourceName =
            "GamePacingDebugSettings";

        [SerializeField]
        private GamePacingDebugSettings defaultSettings;
        private GamePacingMultipliers multipliers =
            GamePacingMultipliers.Default;
        private float gameSpeed = 1f;

        public static GamePacingDebugRuntime Instance { get; private set; }
        public static event Action<GamePacingDebugRuntime> InstanceAvailable;
        public static event Action InstanceUnavailable;
        public static event Action<GamePacingMultipliers> MultipliersChanged;

        public static float AircraftSpeedMultiplier =>
            Instance != null ? Instance.multipliers.AircraftSpeed : 1f;

        public static float ProjectileDamageMultiplier =>
            Instance != null ? Instance.multipliers.ProjectileDamage : 1f;

        public static float AircraftHealthMultiplier =>
            Instance != null ? Instance.multipliers.AircraftHealth : 1f;

        public static float GetBackpackHealthMultiplier(
            BackpackHero.Battle.BattleFaction faction)
        {
            if (Instance == null)
            {
                return 1f;
            }

            return faction == BackpackHero.Battle.BattleFaction.Enemy
                ? Instance.multipliers.EnemyBackpackHealth
                : Instance.multipliers.PlayerBackpackHealth;
        }

        /// <summary>游戏启动和“恢复默认”使用的配置资产。</summary>
        public GamePacingDebugSettings DefaultSettings =>
            defaultSettings;
        public GamePacingMultipliers Multipliers => multipliers;
        public float GameSpeed => gameSpeed;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntime()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject runtimeObject = new(
                nameof(GamePacingDebugRuntime));
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<GamePacingDebugRuntime>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            defaultSettings = Resources.Load<GamePacingDebugSettings>(
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

            ResetGameSpeed();
            Instance = null;
            InstanceUnavailable?.Invoke();
        }

        public void SetMultipliers(GamePacingMultipliers values)
        {
            multipliers = values;
            MultipliersChanged?.Invoke(multipliers);
        }

        public void LoadSavedValues()
        {
            SetMultipliers(defaultSettings != null
                ? defaultSettings.GetValues()
                : GamePacingMultipliers.Default);
        }

        /// <summary>
        /// 由编辑器桥接层显式绑定项目中的默认配置资产。
        /// Player构建仍会通过Resources自动加载同一资产。
        /// </summary>
        public void SetDefaultSettings(
            GamePacingDebugSettings newDefaultSettings)
        {
            if (newDefaultSettings == null ||
                defaultSettings == newDefaultSettings)
            {
                return;
            }

            defaultSettings = newDefaultSettings;
            LoadSavedValues();
        }

        public void RestoreRuntimeDefaults()
        {
            // “默认”是调试面板通过“保存至默认”写入的配置资产。
            // 只有配置资产缺失时，才使用项目的初始全 1 值。
            SetMultipliers(defaultSettings != null
                ? defaultSettings.GetValues()
                : GamePacingMultipliers.Default);
        }

        public bool SaveCurrentValuesAsDefault()
        {
            if (defaultSettings == null)
            {
                Debug.LogWarning(
                    "找不到 Resources/GamePacingDebugSettings 配置资产。",
                    this);
                return false;
            }

            defaultSettings.SetValues(multipliers);
            return true;
        }

        public void SetGameSpeed(float value)
        {
#if UNITY_EDITOR
            gameSpeed = Mathf.Clamp(value, 0.5f, 2f);
            Time.timeScale = gameSpeed;
#endif
        }

        public void ResetGameSpeed()
        {
            gameSpeed = 1f;
#if UNITY_EDITOR
            Time.timeScale = 1f;
#endif
        }
    }
}
