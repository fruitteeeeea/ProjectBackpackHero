using System;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>风格倾向配置的运行时桥接层。</summary>
    [DefaultExecutionOrder(-9999)]
    public sealed class StyleTendencyDebugRuntime : MonoBehaviour
    {
        private const string DefaultSettingsResourceName =
            "StyleTendency/Strategy";

        [SerializeField]
        private StyleTendencyDebugSettings defaultSettings;
        private StyleTendencyMultipliers multipliers =
            StyleTendencyMultipliers.Default;

        public static StyleTendencyDebugRuntime Instance { get; private set; }
        public static event Action<StyleTendencyMultipliers> MultipliersChanged;
        public static event Action<CooldownItemType> CooldownItemTypeChanged;

        public StyleTendencyDebugSettings DefaultSettings => defaultSettings;
        public StyleTendencyMultipliers Multipliers => multipliers;

        public static float GetItemCooldownSpeedMultiplier() =>
            Instance != null ? Instance.multipliers.ItemCooldownSpeed : 1f;

        public static float GetAircraftTargetingArcAngleMultiplier() =>
            Instance != null
                ? Instance.multipliers.AircraftTargetingArcAngle
                : 1f;

        public static float GetAircraftAttackRangeMultiplier() =>
            Instance != null ? Instance.multipliers.AircraftAttackRange : 1f;

        public static float GetAircraftAttackSpeedMultiplier() =>
            Instance != null ? Instance.multipliers.AircraftAttackSpeed : 1f;

        public static float GetAircraftFlightSpeedMultiplier() =>
            Instance != null ? Instance.multipliers.AircraftFlightSpeed : 1f;

        public static float GetAircraftLifetimeMultiplier() =>
            Instance != null ? Instance.multipliers.AircraftLifetime : 1f;

        public static float GetProjectileSpeedMultiplier() =>
            Instance != null ? Instance.multipliers.ProjectileSpeed : 1f;

        public static float GetProjectileLifetimeMultiplier() =>
            Instance != null ? Instance.multipliers.ProjectileLifetime : 1f;

        public static CooldownItemType GetCooldownItemType() =>
            Instance != null
                ? Instance.multipliers.CooldownItemType
                : BackpackPrototype.CooldownItemType.Equipment;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntime()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject runtimeObject = new(nameof(StyleTendencyDebugRuntime));
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<StyleTendencyDebugRuntime>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            defaultSettings = Resources.Load<StyleTendencyDebugSettings>(
                DefaultSettingsResourceName);
            LoadSavedValues();
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            Instance = null;
        }

        public void SetMultipliers(StyleTendencyMultipliers values)
        {
            bool cooldownItemTypeChanged =
                multipliers.CooldownItemType != values.CooldownItemType;
            multipliers = values;
            MultipliersChanged?.Invoke(multipliers);
            if (cooldownItemTypeChanged)
            {
                CooldownItemTypeChanged?.Invoke(
                    multipliers.CooldownItemType);
            }
        }

        public void LoadSavedValues()
        {
            SetMultipliers(defaultSettings != null
                ? defaultSettings.GetValues()
                : StyleTendencyMultipliers.Default);
        }

        public void SetDefaultSettings(StyleTendencyDebugSettings settings)
        {
            if (settings == null || defaultSettings == settings)
            {
                return;
            }

            defaultSettings = settings;
            LoadSavedValues();
        }
    }
}
