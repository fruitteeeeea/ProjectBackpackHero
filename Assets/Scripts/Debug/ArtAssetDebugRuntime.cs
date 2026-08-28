using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>连接美术资源配置、调试面板和场景美术对象。</summary>
    [DefaultExecutionOrder(-9998)]
    public sealed class ArtAssetDebugRuntime : MonoBehaviour
    {
        private readonly HashSet<BackpackShipStyleTarget> shipTargets = new();
        [SerializeField] private ArtAssetDebugSettings defaultSettings;
        private ArtAssetDebugSettingsValue settings =
            ArtAssetDebugSettingsValue.Default;

        public static ArtAssetDebugRuntime Instance { get; private set; }
        public static event Action<ArtAssetDebugSettingsValue> SettingsChanged;
        public static event Action<ArtAssetDebugRuntime> InstanceAvailable;
        public static event Action InstanceUnavailable;

        public ArtAssetDebugSettings DefaultSettings => defaultSettings;
        public ArtAssetDebugSettingsValue Settings => settings;
        public static ArtAssetDebugSettingsValue CurrentSettings =>
            Instance != null ? Instance.settings : ArtAssetDebugSettingsValue.Default;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntime()
        {
            if (Instance != null) return;
            GameObject runtimeObject = new(nameof(ArtAssetDebugRuntime));
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<ArtAssetDebugRuntime>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            defaultSettings = GameDataCatalog.Load()?.ArtAsset;
            LoadSavedValues();
            InstanceAvailable?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            InstanceUnavailable?.Invoke();
        }

        public void SetSettings(ArtAssetDebugSettingsValue values)
        {
            settings = new ArtAssetDebugSettingsValue(values.BackpackShipStyle,
                values.ItemBaseStyle);
            ApplyToTargets();
            SettingsChanged?.Invoke(settings);
        }

        public void SetBackpackShipStyle(BackpackShipStyle style) =>
            SetSettings(settings.WithBackpackShipStyle(style));

        public void SetItemBaseStyle(ItemBaseStyle style) =>
            SetSettings(settings.WithItemBaseStyle(style));

        public static Sprite GetCurrentItemBaseSprite(
            ItemBaseShape shape, Sprite style1Fallback)
        {
            if (CurrentSettings.ItemBaseStyle != ItemBaseStyle.Style2)
            {
                return style1Fallback;
            }

            Sprite style2 = Instance?.defaultSettings?
                .GetStyle2ItemBaseSprite(shape);
            return style2 != null ? style2 : style1Fallback;
        }

        public void LoadSavedValues() => SetSettings(defaultSettings != null
            ? defaultSettings.GetValues()
            : ArtAssetDebugSettingsValue.Default);

        public void SetDefaultSettings(ArtAssetDebugSettings target)
        {
            if (target == null || defaultSettings == target) return;
            defaultSettings = target;
            LoadSavedValues();
        }

        internal void Register(BackpackShipStyleTarget target)
        {
            if (target == null) return;
            shipTargets.Add(target);
            target.ApplyStyle(settings.BackpackShipStyle);
        }

        internal void Unregister(BackpackShipStyleTarget target)
        {
            if (target != null) shipTargets.Remove(target);
        }

        private void ApplyToTargets()
        {
            shipTargets.RemoveWhere(target => target == null);
            foreach (BackpackShipStyleTarget target in shipTargets)
            {
                target.ApplyStyle(settings.BackpackShipStyle);
            }
        }
    }
}
