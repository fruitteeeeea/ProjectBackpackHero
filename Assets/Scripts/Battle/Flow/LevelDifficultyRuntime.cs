using System;
using BackpackHero.Config;
using BackpackHero.Debugging;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>关卡配置的运行时入口；倍率随当前关卡和回合即时解析。</summary>
    [DefaultExecutionOrder(-9999)]
    public sealed class LevelDifficultyRuntime : MonoBehaviour
    {
        [SerializeField] private LevelDifficultySettings settings;
        private LevelDifficultySettings activeValues;
        private bool enemyStrengthEnabled;

        public static LevelDifficultyRuntime Instance { get; private set; }
        public static event Action Changed;
        public LevelDifficultySettings Settings => settings;
        /// <summary>
        /// 是否让新生成的敌机应用关卡敌人生命与伤害倍率。
        /// 该状态仅在当前 Play Mode 中保存。
        /// </summary>
        public bool EnemyStrengthEnabled => enemyStrengthEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() { Instance = null; Changed = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntime()
        {
            if (Instance != null) return;
            GameObject root = new(nameof(LevelDifficultyRuntime));
            DontDestroyOnLoad(root);
            root.AddComponent<LevelDifficultyRuntime>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            enemyStrengthEnabled = false;
            settings = GameDataCatalog.Load()?.LevelDifficulty;
            if (settings != null)
            {
                activeValues = ScriptableObject.CreateInstance<LevelDifficultySettings>();
                activeValues.CopyFrom(settings);
                if (GameConfigService.Level != null)
                    activeValues.ApplyConfig(GameConfigService.Level);
            }
            LevelManager.LevelChanged += HandleLevelChanged;
            LevelFlowController.RoundStarted += HandleRoundStarted;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            LevelManager.LevelChanged -= HandleLevelChanged;
            LevelFlowController.RoundStarted -= HandleRoundStarted;
            if (activeValues != null) Destroy(activeValues);
            Instance = null;
        }

        public static float GetAircraftHealthMultiplier(BattleFaction faction)
        {
            if (Instance?.activeValues == null) return 1f;
            return faction == BattleFaction.Player
                ? Instance.activeValues.PlayerAircraftHealthMultiplier
                : Instance.activeValues.GetEnemyStrength(LevelManager.CurrentLevel, LevelFlowController.CurrentRound).Health;
        }

        public static float GetProjectileDamageMultiplier(BattleFaction faction)
        {
            if (Instance?.activeValues == null) return 1f;
            return faction == BattleFaction.Player
                ? Instance.activeValues.PlayerAircraftDamageMultiplier
                : Instance.activeValues.GetEnemyStrength(LevelManager.CurrentLevel, LevelFlowController.CurrentRound).Damage;
        }

        public static float GetBackpackRoundHealthMultiplier()
        {
            return Instance?.activeValues != null
                ? Instance.activeValues
                    .GetBackpackRoundHealthMultiplier(
                        LevelFlowController.CurrentRound)
                : 1f;
        }

        /// <summary>
        /// 更新新生成敌机的关卡强度开关，不通知已有战斗单位刷新数值。
        /// </summary>
        public void SetEnemyStrengthEnabled(bool enabled)
        {
            enemyStrengthEnabled = enabled;
        }

        public void SetSettings(LevelDifficultySettings nextSettings)
        {
            if (nextSettings == null) return;
            settings = nextSettings;
            ApplyValues(nextSettings);
        }

        /// <summary>仅更新 Play Mode 中的有效值，不写入资产。</summary>
        public void ApplyValues(LevelDifficultySettings values)
        {
            if (values == null) return;
            if (activeValues == null)
                activeValues = ScriptableObject.CreateInstance<LevelDifficultySettings>();
            activeValues.CopyFrom(values);
            NotifyChanged();
        }

        public void NotifyChanged() => Changed?.Invoke();
        private void HandleLevelChanged(int _) => NotifyChanged();
        private void HandleRoundStarted(int _) => NotifyChanged();
    }
}
