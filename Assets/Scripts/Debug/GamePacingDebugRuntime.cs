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
        [SerializeField]
        private GamePacingDebugSettings defaultSettings;
        private GamePacingMultipliers multipliers =
            GamePacingMultipliers.Default;
        private float damageFloatingTextMagicNumber = 16f;
        private float gameSpeed = 1f;

        public static GamePacingDebugRuntime Instance { get; private set; }
        public static event Action<GamePacingDebugRuntime> InstanceAvailable;
        public static event Action InstanceUnavailable;
        public static event Action<GamePacingMultipliers> MultipliersChanged;

        public static float GetOverallStrengthMultiplier(
            BackpackHero.Battle.BattleFaction faction)
        {
            if (Instance == null)
            {
                return 1f;
            }

            return faction == BackpackHero.Battle.BattleFaction.Enemy
                ? Instance.multipliers.EnemyOverallStrength
                : Instance.multipliers.PlayerOverallStrength;
        }

        public static float GetAircraftSpeedMultiplier(
            BackpackHero.Battle.BattleFaction faction)
        {
            return (Instance != null
                    ? Instance.multipliers.AircraftSpeed
                    : 1f) * GetOverallStrengthMultiplier(faction);
        }

        public static float GetProjectileDamageMultiplier(
            BackpackHero.Battle.BattleFaction faction)
        {
            return (Instance != null
                    ? Instance.multipliers.ProjectileDamage
                    : 1f) * GetOverallStrengthMultiplier(faction);
        }

        public static float GetAircraftHealthMultiplier(
            BackpackHero.Battle.BattleFaction faction)
        {
            return (Instance != null
                    ? Instance.multipliers.AircraftHealth
                    : 1f) * GetOverallStrengthMultiplier(faction);
        }

        public static float GetBackpackHealthMultiplier(
            BackpackHero.Battle.BattleFaction faction)
        {
            if (Instance == null)
            {
                return 1f;
            }

            float backpackHealth =
                faction == BackpackHero.Battle.BattleFaction.Enemy
                ? Instance.multipliers.EnemyBackpackHealth
                : Instance.multipliers.PlayerBackpackHealth;

            return backpackHealth *
                GetOverallStrengthMultiplier(faction);
        }

        /// <summary>
        /// 仅用于背包中飞机物品开始下一轮生成冷却时的时长倍率。
        /// 已经开始的冷却会保存其开始时的时长，不会因面板更新而改变。
        /// </summary>
        public static float GetWhiteboardCooldownMultiplier()
        {
            return Instance != null
                ? Instance.multipliers.WhiteboardCooldown
                : 1f;
        }

        /// <summary>
        /// 伤害飘字的纯显示倍率；绝不参与伤害或生命值结算。
        /// </summary>
        public static float GetDamageFloatingTextMagicNumber()
        {
            return Instance != null
                ? Instance.damageFloatingTextMagicNumber
                : 16f;
        }

        /// <summary>游戏启动和“恢复默认”使用的配置资产。</summary>
        public GamePacingDebugSettings DefaultSettings =>
            defaultSettings;
        public GamePacingMultipliers Multipliers => multipliers;
        public float GameSpeed => gameSpeed;
        public float DamageFloatingTextMagicNumber =>
            damageFloatingTextMagicNumber;

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
            defaultSettings = GameDataCatalog.Load()?.GamePacing;
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
            SetDamageFloatingTextMagicNumber(
                defaultSettings != null
                    ? defaultSettings.DamageFloatingTextMagicNumber
                    : 16f);
        }

        /// <summary>
        /// 由编辑器桥接层显式绑定项目中的默认配置资产。
        /// Player 构建通过 GameDataCatalog 引用同一正式资产。
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
            LoadSavedValues();
        }

        public void SetDamageFloatingTextMagicNumber(
            float value)
        {
            damageFloatingTextMagicNumber =
                Mathf.Max(0f, value);
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
