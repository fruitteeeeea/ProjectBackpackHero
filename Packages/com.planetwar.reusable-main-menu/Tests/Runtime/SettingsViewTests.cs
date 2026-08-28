using NUnit.Framework;
using PlanetWar.ReusableMainMenu;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsViewTests
{
    [Test]
    public void ApplySettingsUpdatesTogglesWithoutPublishingThenPublishesCompleteState()
    {
        var root = new GameObject("Settings");
        root.SetActive(false);
        var sound = CreateToggle("Sound", root.transform);
        var music = CreateToggle("Music", root.transform);
        var vibration = CreateToggle("Vibration", root.transform);
        var view = root.AddComponent<SettingsView>();
        SetField(view, "soundToggle", sound);
        SetField(view, "musicToggle", music);
        SetField(view, "vibrationToggle", vibration);
        var eventCount = 0;
        SettingsState received = default;
        view.SettingsChanged += state => { eventCount++; received = state; };
        root.SetActive(true);

        view.ApplySettings(new SettingsState(false, true, false));
        Assert.AreEqual(0, eventCount);
        Assert.IsFalse(sound.isOn);
        Assert.IsTrue(music.isOn);
        Assert.IsFalse(vibration.isOn);

        sound.isOn = true;
        Assert.AreEqual(1, eventCount);
        Assert.IsTrue(received.SoundEnabled);
        Assert.IsTrue(received.MusicEnabled);
        Assert.IsFalse(received.VibrationEnabled);
        Object.DestroyImmediate(root);
    }

    private static Toggle CreateToggle(string name, Transform parent)
    {
        var toggle = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        toggle.transform.SetParent(parent, false);
        return toggle.GetComponent<Toggle>();
    }

    private static void SetField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.SetValue(target, value);
    }
}
