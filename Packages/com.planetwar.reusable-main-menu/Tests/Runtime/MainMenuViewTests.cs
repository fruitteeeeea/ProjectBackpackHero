using System.Reflection;
using NUnit.Framework;
using PlanetWar.ReusableMainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainMenuViewTests
{
    [Test]
    public void MenuBuildsAndEmitsSemanticAction()
    {
        var menu = new GameObject("Menu", typeof(RectTransform), typeof(MainMenuActionRelay), typeof(MainMenuView)).GetComponent<MainMenuView>();
        var view = new GameObject("UIMain", typeof(UIMain)).GetComponent<UIMain>();
        view.transform.SetParent(menu.transform, false);
        MainMenuAction received = default;
        menu.ActionInvoked += action => received = action;
        view.OnClickConfirm();
        Assert.AreEqual(MainMenuAction.Start, received);
        Object.DestroyImmediate(menu.gameObject);
    }

    [Test]
    public void MainProgress_ShowsCollectionLevelAndCurrentPointsOnly()
    {
        GameObject root = new GameObject("UIMain", typeof(UIMain));
        UIMain view = root.GetComponent<UIMain>();
        TMP_Text points = new GameObject("Points", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        TMP_Text progress = new GameObject("Progress", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        TMP_Text level = new GameObject("Level", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        Slider slider = new GameObject("Slider", typeof(Slider)).GetComponent<Slider>();

        SetField(view, "rankPointsText", points);
        SetField(view, "rankProgressText", progress);
        SetField(view, "rankNameText", level);
        SetField(view, "rankProgressSlider", slider);

        view.ApplyMainProgress(12345, 50000, 6);

        Assert.That(points.text, Is.EqualTo("12345"));
        Assert.That(progress.text, Is.EqualTo("12,345"));
        Assert.That(level.text, Is.EqualTo("Level 6"));
        Assert.That(slider.value, Is.EqualTo(12345f / 50000f).Within(0.0001f));

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(points.gameObject);
        Object.DestroyImmediate(progress.gameObject);
        Object.DestroyImmediate(level.gameObject);
        Object.DestroyImmediate(slider.gameObject);
    }

    [Test]
    public void RankProgress_RestoresOriginalLevelTextAndMaximumPointDisplay()
    {
        GameObject root = new GameObject("UIMain", typeof(UIMain));
        UIMain view = root.GetComponent<UIMain>();
        TMP_Text progress = new GameObject("Progress", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        TMP_Text level = new GameObject("Level", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        Slider slider = new GameObject("Slider", typeof(Slider)).GetComponent<Slider>();
        level.text = "Level 1";

        SetField(view, "rankProgressText", progress);
        SetField(view, "rankNameText", level);
        SetField(view, "rankProgressSlider", slider);

        view.ApplyRankProgress(12345, 50000, "Glow Belt");

        Assert.That(progress.text, Is.EqualTo("12,345/50,000"));
        Assert.That(level.text, Is.EqualTo("Level 1"));
        Assert.That(slider.value, Is.EqualTo(12345f / 50000f).Within(0.0001f));

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(progress.gameObject);
        Object.DestroyImmediate(level.gameObject);
        Object.DestroyImmediate(slider.gameObject);
    }

    [Test]
    public void ProgressPresentation_CanSwitchBetweenMilestoneModes()
    {
        GameObject root = new GameObject("UIMain", typeof(UIMain));
        UIMain view = root.GetComponent<UIMain>();
        TMP_Text progress = new GameObject("Progress", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        TMP_Text level = new GameObject("Level", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        level.text = "Level 1";

        SetField(view, "rankProgressText", progress);
        SetField(view, "rankNameText", level);

        view.ApplyMainProgress(12345, 50000, 6);
        Assert.That(progress.text, Is.EqualTo("12,345"));
        Assert.That(level.text, Is.EqualTo("Level 6"));

        view.ApplyRankProgress(12345, 50000, "Glow Belt");
        Assert.That(progress.text, Is.EqualTo("12,345/50,000"));
        Assert.That(level.text, Is.EqualTo("Level 1"));

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(progress.gameObject);
        Object.DestroyImmediate(level.gameObject);
    }

    private static void SetField(UIMain view, string fieldName, Object value)
    {
        FieldInfo field = typeof(UIMain).GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing {fieldName} binding.");
        field.SetValue(view, value);
    }
}
