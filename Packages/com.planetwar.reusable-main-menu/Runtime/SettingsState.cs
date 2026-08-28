namespace PlanetWar.ReusableMainMenu
{
    /// <summary>Host-owned values displayed by <see cref="SettingsView"/>.</summary>
    public readonly struct SettingsState
    {
        public SettingsState(bool soundEnabled, bool musicEnabled, bool vibrationEnabled)
        {
            SoundEnabled = soundEnabled;
            MusicEnabled = musicEnabled;
            VibrationEnabled = vibrationEnabled;
        }

        public bool SoundEnabled { get; }
        public bool MusicEnabled { get; }
        public bool VibrationEnabled { get; }
    }
}
