using System.Collections.Generic;
using BackpackHero.Battle;
using PlanetWar.ReusableMainMenu;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
#endif

namespace BackpackHero.Audio
{
    public enum GameSfxId { UiClick, BackpackItem, FighterHit, DefaultProjectile, EquipmentProjectile, LaserProjectile, SpreadProjectile, Victory }

    public readonly struct GameSfxTuning
    {
        public const float MinimumPitch = 0.5f;
        public const float MaximumPitch = 1.25f;
        public const float MaximumRandomPitchOffset = 0.25f;
        public const float MinimumVolumeMultiplier = 0f;
        public const float MaximumVolumeMultiplier = 1f;

        public GameSfxTuning(float basePitch, float randomPitchOffset, float volumeMultiplier)
        {
            BasePitch = Mathf.Clamp(basePitch, MinimumPitch, MaximumPitch);
            RandomPitchOffset = Mathf.Clamp(randomPitchOffset, 0f, MaximumRandomPitchOffset);
            VolumeMultiplier = Mathf.Clamp(volumeMultiplier, MinimumVolumeMultiplier, MaximumVolumeMultiplier);
        }

        public float BasePitch { get; }
        public float RandomPitchOffset { get; }
        public float VolumeMultiplier { get; }

        public static GameSfxTuning Default => new(1f, 0f, 1f);
    }

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
        private const string NonUiPitchPreferenceKey = "BackpackHero.NonUiSfxPitch";
        private const string NonUiRandomPitchPreferenceKey = "BackpackHero.NonUiSfxRandomPitch";
        private const string NonUiVolumePreferenceKey = "BackpackHero.NonUiSfxVolume";
        private const float UiVolume = 0.8f;
        private const float CombatVolume = 0.7f;
        private const int NonUiSourcePoolSize = 8;
        private static readonly Dictionary<GameSfxId, float> MinimumIntervals = new()
        {
            { GameSfxId.FighterHit, 0.08f }, { GameSfxId.DefaultProjectile, 0.06f },
            { GameSfxId.EquipmentProjectile, 0.06f }, { GameSfxId.LaserProjectile, 0.12f },
            { GameSfxId.SpreadProjectile, 0.1f }
        };

        public static GameSfxService Instance { get; private set; }
        public bool IsEnabled { get; private set; }
        /// <summary>Editor-only diagnostic state. It is intentionally not persisted with player settings.</summary>
        public bool UiDiagnosticLoggingEnabled { get; private set; }
        public bool HasAudioSource => source != null;
        public bool HasCatalog => catalog != null;
        public string DefaultUiClipName => ResolveClip(GameSfxId.UiClick)?.name ?? "<missing>";
        public GameSfxTuning NonUiTuning { get; private set; } = GameSfxTuning.Default;

        private readonly GameSfxRateLimiter rateLimiter = new();
        private readonly List<AudioSource> nonUiSources = new();
        private AudioSource source;
        private GameSfxCatalog catalog;
        private int nextNonUiSourceIndex;

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
            for (int index = 0; index < NonUiSourcePoolSize; index++)
            {
                nonUiSources.Add(CreateTwoDimensionalSource());
            }
            catalog = Resources.Load<GameSfxCatalog>("Audio/GameSfxCatalog");
            IsEnabled = PlayerPrefs.GetInt(EnabledPreferenceKey, 1) != 0;
            NonUiTuning = LoadNonUiTuning();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            PlayerPrefs.SetInt(EnabledPreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public GameSfxTuning LoadSavedNonUiTuning() => LoadNonUiTuning();

        public void ApplyNonUiTuning(GameSfxTuning tuning)
        {
            NonUiTuning = tuning;
        }

        public void SaveNonUiTuning(GameSfxTuning tuning)
        {
            ApplyNonUiTuning(tuning);
            PlayerPrefs.SetFloat(NonUiPitchPreferenceKey, NonUiTuning.BasePitch);
            PlayerPrefs.SetFloat(NonUiRandomPitchPreferenceKey, NonUiTuning.RandomPitchOffset);
            PlayerPrefs.SetFloat(NonUiVolumePreferenceKey, NonUiTuning.VolumeMultiplier);
            PlayerPrefs.Save();
        }

        public void SetUiDiagnosticLoggingEnabled(bool enabled)
        {
            UiDiagnosticLoggingEnabled = enabled;
            if (enabled) LogDiagnosticSnapshot("diagnostics enabled");
        }

        public void LogDiagnosticSnapshot(string reason = "manual snapshot")
        {
            if (!UiDiagnosticLoggingEnabled) return;
            UiSfxAutoBinder binder = GetComponent<UiSfxAutoBinder>();
            Debug.Log($"[SFX-DIAG] {reason}; scene={SceneManager.GetActiveScene().name}; " +
                      $"service={name}; sfxEnabled={IsEnabled}; audioSource={source != null}; " +
                      $"catalog={catalog != null}; defaultUiClip={DefaultUiClipName}; " +
                      $"nonUiPitch={NonUiTuning.BasePitch:F2}; nonUiRandomPitch={NonUiTuning.RandomPitchOffset:F2}; " +
                      $"nonUiVolume={NonUiTuning.VolumeMultiplier:F2}; " +
                      $"boundButtons={binder?.BoundButtonCount ?? 0}", this);
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
            if (!IsEnabled)
            {
                LogPlayOutcome(id, clip, "skipped: SFX disabled");
                return;
            }
            if (clip == null)
            {
                LogPlayOutcome(id, clip, "skipped: clip missing");
                return;
            }
            AudioSource playbackSource = id == GameSfxId.UiClick ? source : GetNextNonUiSource();
            if (playbackSource == null)
            {
                LogPlayOutcome(id, clip, "skipped: AudioSource missing");
                return;
            }
            if (IsRateLimited(id))
            {
                LogPlayOutcome(id, clip, "skipped: rate limited");
                return;
            }
            if (id == GameSfxId.UiClick)
            {
                playbackSource.pitch = 1f;
                playbackSource.PlayOneShot(clip, UiVolume);
            }
            else
            {
                playbackSource.pitch = GetRandomizedNonUiPitch();
                float baseVolume = id == GameSfxId.BackpackItem ? UiVolume : CombatVolume;
                playbackSource.PlayOneShot(clip, baseVolume * NonUiTuning.VolumeMultiplier);
            }
            LogPlayOutcome(id, clip, "PlayOneShot invoked");
        }

        private AudioSource CreateTwoDimensionalSource()
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            return audioSource;
        }

        private AudioSource GetNextNonUiSource()
        {
            if (nonUiSources.Count == 0) return null;
            for (int attempt = 0; attempt < nonUiSources.Count; attempt++)
            {
                int index = (nextNonUiSourceIndex + attempt) % nonUiSources.Count;
                AudioSource candidate = nonUiSources[index];
                if (candidate == null || candidate.isPlaying) continue;
                nextNonUiSourceIndex = (index + 1) % nonUiSources.Count;
                return candidate;
            }

            AudioSource fallback = nonUiSources[nextNonUiSourceIndex];
            nextNonUiSourceIndex = (nextNonUiSourceIndex + 1) % nonUiSources.Count;
            return fallback;
        }

        private float GetRandomizedNonUiPitch()
        {
            float offset = Random.Range(-NonUiTuning.RandomPitchOffset, NonUiTuning.RandomPitchOffset);
            return Mathf.Clamp(NonUiTuning.BasePitch + offset,
                GameSfxTuning.MinimumPitch, GameSfxTuning.MaximumPitch);
        }

        private static GameSfxTuning LoadNonUiTuning()
        {
            GameSfxTuning defaults = GameSfxTuning.Default;
            return new GameSfxTuning(
                PlayerPrefs.GetFloat(NonUiPitchPreferenceKey, defaults.BasePitch),
                PlayerPrefs.GetFloat(NonUiRandomPitchPreferenceKey, defaults.RandomPitchOffset),
                PlayerPrefs.GetFloat(NonUiVolumePreferenceKey, defaults.VolumeMultiplier));
        }

        private void LogPlayOutcome(GameSfxId id, AudioClip clip, string outcome)
        {
            if (!UiDiagnosticLoggingEnabled || id != GameSfxId.UiClick) return;
            Debug.Log($"[SFX-DIAG] ui-play; clip={clip?.name ?? "<missing>"}; {outcome}", this);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly ProfilerMarker ScanMarker =
            new("SampleScenePerf.UiSfxAutoBinder.Scan");
#endif

        private readonly Dictionary<Button, UnityAction> bindings = new();
        private float nextScanAt;
        public int BoundButtonCount => bindings.Count;

        private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; Scan("enabled", true); }
        private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
        private void Update() { if (Time.unscaledTime >= nextScanAt) { nextScanAt = Time.unscaledTime + 0.35f; Scan("periodic", false); } }
        private void OnSceneLoaded(Scene scene, LoadSceneMode _) => Scan($"scene loaded: {scene.name}", true);
        private void Scan(string reason, bool logEvenWithoutNewBindings)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (ScanMarker.Auto())
            {
#endif
            int discovered = 0;
            int newlyBound = 0;
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Exclude))
            {
                discovered++;
                if (button == null) continue;

                if (!bindings.TryGetValue(button, out UnityAction listener))
                {
                    listener = () => PlayClick(button);
                    bindings.Add(button, listener);
                    newlyBound++;
                }

                // Hangar cards and their detail actions rebuild their UnityEvent at runtime.
                // Remove only our own cached callback before re-adding it, preserving every
                // business callback while recovering the UI SFX callback after such a reset.
                button.onClick.RemoveListener(listener);
                button.onClick.AddListener(listener);
            }

            RemoveDestroyedBindings();
            GameSfxService service = GameSfxService.Instance;
            if (service != null && service.UiDiagnosticLoggingEnabled && (logEvenWithoutNewBindings || newlyBound > 0))
            {
                Debug.Log($"[SFX-DIAG] ui-scan; reason={reason}; scene={SceneManager.GetActiveScene().name}; " +
                          $"activeButtons={discovered}; newlyBound={newlyBound}; reboundCallbacks={discovered}; " +
                          $"totalBound={bindings.Count}", this);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        private void RemoveDestroyedBindings()
        {
            List<Button> destroyed = null;
            foreach (Button button in bindings.Keys)
            {
                if (button != null) continue;
                destroyed ??= new List<Button>();
                destroyed.Add(button);
            }
            if (destroyed == null) return;
            foreach (Button button in destroyed) bindings.Remove(button);
        }

        private static void PlayClick(Button button)
        {
            GameSfxService service = GameSfxService.Instance;
            AudioClip clip = button.GetComponentInParent<UiSfxSceneBinder>()?.ButtonClickClip;
            if (service != null && service.UiDiagnosticLoggingEnabled)
            {
                Debug.Log($"[SFX-DIAG] ui-click; path={GetHierarchyPath(button.transform)}; " +
                          $"active={button.gameObject.activeInHierarchy}; enabled={button.enabled}; " +
                          $"interactable={button.interactable}; sceneClip={clip?.name ?? "<none>"}; service=true", button);
            }
            service?.PlayUi(clip);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }
            return path;
        }
    }

    public sealed class SfxSettingsBridge : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly ProfilerMarker UpdateMarker =
            new("SampleScenePerf.SfxSettingsBridge.Update");
#endif

        private readonly HashSet<SettingsView> boundViews = new();

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (UpdateMarker.Auto())
            {
#endif
            foreach (SettingsView view in FindObjectsByType<SettingsView>(FindObjectsInactive.Include))
            {
                if (view == null || !boundViews.Add(view)) continue;
                ApplySfxState(view);
                view.SettingsChanged += HandleSettingsChanged;
            }

            boundViews.RemoveWhere(view => view == null);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        private void OnDestroy()
        {
            foreach (SettingsView view in boundViews)
            {
                if (view != null) view.SettingsChanged -= HandleSettingsChanged;
            }
            boundViews.Clear();
        }

        private void HandleSettingsChanged(SettingsState state)
        {
            GameSfxService.Instance?.SetEnabled(state.SoundEnabled);
            foreach (SettingsView view in boundViews)
            {
                if (view != null) ApplySfxState(view);
            }
        }

        private static void ApplySfxState(SettingsView view)
        {
            view.ApplySettings(new SettingsState(
                GameSfxService.Instance?.IsEnabled ?? true,
                view.State.MusicEnabled,
                view.State.VibrationEnabled));
        }
    }
}
