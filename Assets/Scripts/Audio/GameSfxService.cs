using System.Collections.Generic;
using BackpackHero.Battle;
using PlanetWar.ReusableMainMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BackpackHero.Audio
{
    public enum GameSfxId { UiClick, BackpackItem, FighterHit, DefaultProjectile, EquipmentProjectile, LaserProjectile, SpreadProjectile, Victory }

    public sealed class GameSfxRateLimiter
    {
        private readonly Dictionary<GameSfxId, float> lastPlayedAt = new();

        public bool CanPlay(GameSfxId id, float now, float minimumInterval)
        {
            if (minimumInterval <= 0f) return true;
            if (lastPlayedAt.TryGetValue(id, out float last) && now - last < minimumInterval) return false;
            lastPlayedAt[id] = now;
            return true;
        }
    }

    public sealed class GameSfxService : MonoBehaviour
    {
        private const string EnabledPreferenceKey = "BackpackHero.SfxEnabled";
        private const float UiVolume = 0.8f;
        private const float CombatVolume = 0.7f;
        private static readonly Dictionary<GameSfxId, float> MinimumIntervals = new()
        {
            { GameSfxId.FighterHit, 0.08f }, { GameSfxId.DefaultProjectile, 0.06f },
            { GameSfxId.EquipmentProjectile, 0.06f }, { GameSfxId.LaserProjectile, 0.12f },
            { GameSfxId.SpreadProjectile, 0.1f }
        };

        public static GameSfxService Instance { get; private set; }
        public bool IsEnabled { get; private set; }

        private readonly GameSfxRateLimiter rateLimiter = new();
        private AudioSource source;
        private GameSfxCatalog catalog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBeforeSceneLoad()
        {
            if (Instance != null) return;
            GameObject root = new("GameSfxService");
            root.AddComponent<GameSfxService>();
            root.AddComponent<UiSfxAutoBinder>();
            root.AddComponent<SfxSettingsBridge>();
            DontDestroyOnLoad(root);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            catalog = Resources.Load<GameSfxCatalog>("Audio/GameSfxCatalog");
            IsEnabled = PlayerPrefs.GetInt(EnabledPreferenceKey, 1) != 0;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            PlayerPrefs.SetInt(EnabledPreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Play(GameSfxId id) => Play(ResolveClip(id), id);

        public void PlayUi(AudioClip clip) => Play(clip != null ? clip : ResolveClip(GameSfxId.UiClick), GameSfxId.UiClick);

        public void PlayProjectile(BattleAttack2D prefab, bool isEquipment, bool isSpread, bool isLaser)
        {
            GameSfxId id = ResolveProjectileSfxId(isEquipment, isSpread, isLaser);
            AudioClip overrideClip = id == GameSfxId.EquipmentProjectile && catalog != null
                ? catalog.FindEquipmentProjectileOverride(prefab)
                : null;
            Play(overrideClip != null ? overrideClip : ResolveClip(id), id);
        }

        public static GameSfxId ResolveProjectileSfxId(bool isEquipment, bool isSpread, bool isLaser) =>
            isLaser ? GameSfxId.LaserProjectile :
            isSpread ? GameSfxId.SpreadProjectile :
            isEquipment ? GameSfxId.EquipmentProjectile : GameSfxId.DefaultProjectile;

        private void Play(AudioClip clip, GameSfxId id)
        {
            if (!IsEnabled || clip == null || source == null || IsRateLimited(id)) return;
            source.PlayOneShot(clip, id == GameSfxId.UiClick || id == GameSfxId.BackpackItem ? UiVolume : CombatVolume);
        }

        private bool IsRateLimited(GameSfxId id)
        {
            if (!MinimumIntervals.TryGetValue(id, out float interval)) return false;
            return !rateLimiter.CanPlay(id, Time.unscaledTime, interval);
        }

        private AudioClip ResolveClip(GameSfxId id)
        {
            if (catalog != null)
            {
                AudioClip clip = id switch
                {
                    GameSfxId.UiClick => catalog.UiClick, GameSfxId.BackpackItem => catalog.BackpackItem,
                    GameSfxId.FighterHit => catalog.FighterHit, GameSfxId.DefaultProjectile => catalog.DefaultProjectile,
                    GameSfxId.EquipmentProjectile => catalog.EquipmentProjectile, GameSfxId.LaserProjectile => catalog.LaserProjectile,
                    GameSfxId.SpreadProjectile => catalog.SpreadProjectile, GameSfxId.Victory => catalog.Victory, _ => null
                };
                if (clip != null) return clip;
            }
            return Resources.Load<AudioClip>(id switch
            {
                GameSfxId.UiClick => "Audio/SFX/voice_button_1", GameSfxId.BackpackItem => "Audio/SFX/voice_button_2",
                GameSfxId.FighterHit => "Audio/SFX/Die_01", GameSfxId.DefaultProjectile => "Audio/SFX/voice_shoot_1",
                GameSfxId.EquipmentProjectile => "Audio/SFX/voice_shoot_2", GameSfxId.LaserProjectile => "Audio/SFX/voice_shoot_6",
                GameSfxId.SpreadProjectile => "Audio/SFX/voice_shoot_7", GameSfxId.Victory => "Audio/SFX/song_victory_2", _ => string.Empty
            });
        }
    }

    public sealed class UiSfxAutoBinder : MonoBehaviour
    {
        private readonly HashSet<Button> bound = new();
        private float nextScanAt;
        private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; Scan(); }
        private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
        private void Update() { if (Time.unscaledTime >= nextScanAt) { nextScanAt = Time.unscaledTime + 0.35f; Scan(); } }
        private void OnSceneLoaded(Scene _, LoadSceneMode __) => Scan();
        private void Scan()
        {
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Exclude))
            {
                if (button == null || !bound.Add(button)) continue;
                AudioClip clip = button.GetComponentInParent<UiSfxSceneBinder>()?.ButtonClickClip;
                button.onClick.AddListener(() => PlayClick(clip));
            }
            bound.RemoveWhere(button => button == null);
        }
        private static void PlayClick(AudioClip clip) => GameSfxService.Instance?.PlayUi(clip);
    }

    public sealed class SfxSettingsBridge : MonoBehaviour
    {
        private SettingsView view;
        private void Update()
        {
            if (view != null) return;
            view = FindAnyObjectByType<SettingsView>(FindObjectsInactive.Include);
            if (view == null) return;
            view.ApplySettings(new SettingsState(GameSfxService.Instance?.IsEnabled ?? true, view.State.MusicEnabled, view.State.VibrationEnabled));
            view.SettingsChanged += HandleSettingsChanged;
        }
        private void OnDestroy() { if (view != null) view.SettingsChanged -= HandleSettingsChanged; }
        private static void HandleSettingsChanged(SettingsState state) => GameSfxService.Instance?.SetEnabled(state.SoundEnabled);
    }
}
