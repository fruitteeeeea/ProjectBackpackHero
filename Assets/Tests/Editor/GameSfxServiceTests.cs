using BackpackHero.Audio;
using NUnit.Framework;
using PlanetWar.ReusableMainMenu;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameSfxServiceTests
{
    private const string NonUiPitchPreferenceKey = "BackpackHero.NonUiSfxPitch";
    private const string NonUiRandomPitchPreferenceKey = "BackpackHero.NonUiSfxRandomPitch";
    private const string NonUiVolumePreferenceKey = "BackpackHero.NonUiSfxVolume";

    [Test]
    public void NonUiSfxTuning_ClampsToSafeRanges()
    {
        GameSfxTuning tuning = new(-1f, 99f, 99f);

        Assert.That(tuning.BasePitch, Is.EqualTo(GameSfxTuning.MinimumPitch));
        Assert.That(tuning.RandomPitchOffset, Is.EqualTo(GameSfxTuning.MaximumRandomPitchOffset));
        Assert.That(tuning.VolumeMultiplier, Is.EqualTo(GameSfxTuning.MaximumVolumeMultiplier));
    }

    [Test]
    public void NonUiSfxTuning_AppliesWithoutSavingThenPersistsOnExplicitSave()
    {
        ClearNonUiTuningPreferences();
        GameObject firstRoot = new("FirstSfxTuningService");
        GameObject secondRoot = null;
        try
        {
            GameSfxService first = firstRoot.AddComponent<GameSfxService>();
            Assert.That(first.NonUiTuning.BasePitch, Is.EqualTo(1f));
            Assert.That(first.NonUiTuning.RandomPitchOffset, Is.Zero);
            Assert.That(first.NonUiTuning.VolumeMultiplier, Is.EqualTo(1f));

            GameSfxTuning preview = new(.8f, .12f, .45f);
            first.ApplyNonUiTuning(preview);
            Assert.That(first.NonUiTuning.BasePitch, Is.EqualTo(.8f));
            Assert.That(PlayerPrefs.HasKey(NonUiPitchPreferenceKey), Is.False);

            first.SaveNonUiTuning(preview);
            Object.DestroyImmediate(firstRoot);
            firstRoot = null;

            secondRoot = new GameObject("SecondSfxTuningService");
            GameSfxService second = secondRoot.AddComponent<GameSfxService>();
            Assert.That(second.NonUiTuning.BasePitch, Is.EqualTo(.8f));
            Assert.That(second.NonUiTuning.RandomPitchOffset, Is.EqualTo(.12f));
            Assert.That(second.NonUiTuning.VolumeMultiplier, Is.EqualTo(.45f));
        }
        finally
        {
            if (firstRoot != null) Object.DestroyImmediate(firstRoot);
            if (secondRoot != null) Object.DestroyImmediate(secondRoot);
            ClearNonUiTuningPreferences();
        }
    }

    [Test]
    public void ProjectileSfxPriority_IsLaserThenSpreadThenEquipmentThenDefault()
    {
        Assert.That(GameSfxService.ResolveProjectileSfxId(false, false, false), Is.EqualTo(GameSfxId.DefaultProjectile));
        Assert.That(GameSfxService.ResolveProjectileSfxId(true, false, false), Is.EqualTo(GameSfxId.EquipmentProjectile));
        Assert.That(GameSfxService.ResolveProjectileSfxId(true, true, false), Is.EqualTo(GameSfxId.SpreadProjectile));
        Assert.That(GameSfxService.ResolveProjectileSfxId(true, true, true), Is.EqualTo(GameSfxId.LaserProjectile));
    }

    [Test]
    public void RateLimiter_LimitsOnlyTheSameSfxType()
    {
        GameSfxRateLimiter limiter = new();

        Assert.That(limiter.CanPlay(GameSfxId.DefaultProjectile, 1f, 0.1f), Is.True);
        Assert.That(limiter.CanPlay(GameSfxId.DefaultProjectile, 1.05f, 0.1f), Is.False);
        Assert.That(limiter.CanPlay(GameSfxId.EquipmentProjectile, 1.05f, 0.1f), Is.True);
        Assert.That(limiter.CanPlay(GameSfxId.DefaultProjectile, 1.1f, 0.1f), Is.True);
    }

    [Test]
    public void SfxSettingsBridge_BindsEverySettingsViewAndSynchronizesSoundState()
    {
        const string preferenceKey = "BackpackHero.SfxEnabled";
        PlayerPrefs.DeleteKey(preferenceKey);
        PlayerPrefs.Save();

        GameObject serviceRoot = new("SfxServiceTest");
        GameObject bridgeRoot = new("SfxBridgeTest");
        GameObject firstRoot = CreateSettingsView("First", out Toggle firstSound);
        GameObject secondRoot = CreateSettingsView("Second", out Toggle secondSound);
        try
        {
            GameSfxService service = serviceRoot.AddComponent<GameSfxService>();
            SfxSettingsBridge bridge = bridgeRoot.AddComponent<SfxSettingsBridge>();
            InvokeUpdate(bridge);

            firstSound.isOn = false;

            Assert.That(service.IsEnabled, Is.False);
            Assert.That(secondSound.isOn, Is.False);
            Assert.That(PlayerPrefs.GetInt(preferenceKey, 1), Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(firstRoot);
            Object.DestroyImmediate(secondRoot);
            Object.DestroyImmediate(bridgeRoot);
            Object.DestroyImmediate(serviceRoot);
            PlayerPrefs.DeleteKey(preferenceKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void UiSfxAutoBinder_RebindsAfterBusinessCodeClearsOnClickWithoutDuplicatingBusinessCallback()
    {
        GameObject buttonRoot = new("HangarCardButton", typeof(RectTransform), typeof(Button));
        GameObject binderRoot = new("UiSfxBinderTest");
        try
        {
            Button button = buttonRoot.GetComponent<Button>();
            UiSfxAutoBinder binder = binderRoot.AddComponent<UiSfxAutoBinder>();
            int businessCalls = 0;

            // Mirrors HangarCardItem / HangarView rebuilding the button event at runtime.
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => businessCalls++);
            InvokeScan(binder);
            InvokeScan(binder);

            button.onClick.Invoke();

            Assert.That(businessCalls, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(binderRoot);
            Object.DestroyImmediate(buttonRoot);
        }
    }

    private static GameObject CreateSettingsView(string name, out Toggle sound)
    {
        GameObject root = new(name);
        root.SetActive(false);
        SettingsView view = root.AddComponent<SettingsView>();
        sound = new GameObject("Sound", typeof(RectTransform), typeof(Toggle)).GetComponent<Toggle>();
        Toggle music = new GameObject("Music", typeof(RectTransform), typeof(Toggle)).GetComponent<Toggle>();
        Toggle vibration = new GameObject("Vibration", typeof(RectTransform), typeof(Toggle)).GetComponent<Toggle>();
        sound.transform.SetParent(root.transform, false);
        music.transform.SetParent(root.transform, false);
        vibration.transform.SetParent(root.transform, false);
        SetPrivateField(view, "soundToggle", sound);
        SetPrivateField(view, "musicToggle", music);
        SetPrivateField(view, "vibrationToggle", vibration);
        root.SetActive(true);
        return root;
    }

    private static void InvokeUpdate(SfxSettingsBridge bridge) =>
        typeof(SfxSettingsBridge).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(bridge, null);

    private static void InvokeScan(UiSfxAutoBinder binder) =>
        typeof(UiSfxAutoBinder).GetMethod("Scan", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(
            binder, new object[] { "test", false });

    private static void SetPrivateField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);

    private static void ClearNonUiTuningPreferences()
    {
        PlayerPrefs.DeleteKey(NonUiPitchPreferenceKey);
        PlayerPrefs.DeleteKey(NonUiRandomPitchPreferenceKey);
        PlayerPrefs.DeleteKey(NonUiVolumePreferenceKey);
        PlayerPrefs.Save();
    }
}
