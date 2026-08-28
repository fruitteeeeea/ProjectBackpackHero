using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>
    /// Presentation-only settings panel. The consuming project owns persistence and effects.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsView : MonoBehaviour
    {
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle vibrationToggle;

        private SettingsState state = new SettingsState(true, true, true);

        public event Action<SettingsState> SettingsChanged;
        public event Action CloseRequested;
        public SettingsState State => state;

        private void Awake()
        {
            if (soundToggle == null || musicToggle == null || vibrationToggle == null)
            {
                Debug.LogError("[MainMenu] SettingsView requires sound, music, and vibration Toggle references.", this);
                enabled = false;
                return;
            }

            soundToggle.onValueChanged.AddListener(OnSoundChanged);
            musicToggle.onValueChanged.AddListener(OnMusicChanged);
            vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
            ApplySettings(state);
        }

        private void OnDestroy()
        {
            if (soundToggle != null) soundToggle.onValueChanged.RemoveListener(OnSoundChanged);
            if (musicToggle != null) musicToggle.onValueChanged.RemoveListener(OnMusicChanged);
            if (vibrationToggle != null) vibrationToggle.onValueChanged.RemoveListener(OnVibrationChanged);
        }

        public void ApplySettings(SettingsState value)
        {
            state = value;
            soundToggle.SetIsOnWithoutNotify(value.SoundEnabled);
            musicToggle.SetIsOnWithoutNotify(value.MusicEnabled);
            vibrationToggle.SetIsOnWithoutNotify(value.VibrationEnabled);
        }

        public void OnClickClose() => CloseRequested?.Invoke();

        // Kept as prefab-serialization compatibility points for the original PlanetWar UI.
        // Toggle.onValueChanged already publishes the changed aggregate state above.
        public void OnClickToggleSound() { }
        public void OnClickToggleMusic() { }
        public void OnClickToggleVibration() { }

        private void OnSoundChanged(bool value) => Publish(value, state.MusicEnabled, state.VibrationEnabled);
        private void OnMusicChanged(bool value) => Publish(state.SoundEnabled, value, state.VibrationEnabled);
        private void OnVibrationChanged(bool value) => Publish(state.SoundEnabled, state.MusicEnabled, value);

        private void Publish(bool soundEnabled, bool musicEnabled, bool vibrationEnabled)
        {
            state = new SettingsState(soundEnabled, musicEnabled, vibrationEnabled);
            SettingsChanged?.Invoke(state);
        }
    }
}
