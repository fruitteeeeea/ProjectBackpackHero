using System;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>关卡配置的运行时入口；倍率随当前关卡和回合即时解析。</summary>
    [DefaultExecutionOrder(-9999)]
    public sealed class LevelDifficultyRuntime : MonoBehaviour
    {
        private const string ResourceName = "LevelDifficultySettings";
        [SerializeField] private LevelDifficultySettings settings;
        private LevelDifficultySettings activeValues;

        public static LevelDifficultyRuntime Instance { get; private set; }
        public static event Action Changed;
        public LevelDifficultySettings Settings => settings;

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
            settings = Resources.Load<LevelDifficultySettings>(ResourceName);
            if (settings != null)
            {
                activeValues = ScriptableObject.CreateInstance<LevelDifficultySettings>();
                activeValues.CopyFrom(settings);
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
