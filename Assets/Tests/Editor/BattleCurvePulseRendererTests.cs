using BackpackHero.Input;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class BattleCurvePulseRendererTests
{
    [Test]
    public void PulseSettings_ClampsAllValuesToRendererRanges()
    {
        BattleCurvePulseSettings settings = new(-1f, -1f, -1f, 2f, 999, -1f);

        Assert.That(settings.TravelDuration,
            Is.EqualTo(BattleCurvePulseSettings.MinimumTravelDuration));
        Assert.That(settings.EndFadeDuration,
            Is.EqualTo(BattleCurvePulseSettings.MinimumEndFadeDuration));
        Assert.That(settings.CooldownDuration,
            Is.EqualTo(BattleCurvePulseSettings.MinimumCooldownDuration));
        Assert.That(settings.PulseLength,
            Is.EqualTo(BattleCurvePulseSettings.MaximumPulseLength));
        Assert.That(settings.SampleCount,
            Is.EqualTo(BattleCurvePulseRenderer.MaximumSampleCount));
        Assert.That(settings.PulseWidth,
            Is.EqualTo(BattleCurvePulseSettings.MinimumPulseWidth));
    }

    [TestCase(2.249f, true)]
    [TestCase(2.25f, false)]
    [TestCase(4.749f, false)]
    [TestCase(4.75f, true)]
    public void IsPulseVisibleAtCycleTime_HidesDuringCooldownAndRestartsAfterIt(
        float elapsedTime,
        bool expected)
    {
        bool visible = BattleCurvePulseRenderer.IsPulseVisibleAtCycleTime(
            elapsedTime, 2f, .25f, 2.5f);

        Assert.That(visible, Is.EqualTo(expected));
    }

    [Test]
    public void CooldownDuration_PreservesTheLegacyIntervalSerializedName()
    {
        FieldInfo field = typeof(BattleCurvePulseRenderer).GetField(
            "cooldownDuration", BindingFlags.Instance | BindingFlags.NonPublic);
        FormerlySerializedAsAttribute attribute = field.GetCustomAttribute<
            FormerlySerializedAsAttribute>();

        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute.oldName, Is.EqualTo("intervalDuration"));
    }

    [Test]
    public void SetSettings_AppliesClampedPulseSettings()
    {
        GameObject root = new("Pulse Test");
        try
        {
            BattleCurvePulseRenderer renderer =
                root.AddComponent<BattleCurvePulseRenderer>();

            renderer.SetSettings(new BattleCurvePulseSettings(
                .7f, .2f, 1.5f, .4f, 9, .3f));

            Assert.That(renderer.Settings, Is.EqualTo(
                new BattleCurvePulseSettings(.7f, .2f, 1.5f, .4f, 9,
                    .3f)));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [TestCase(0f, 0.16f, 0f, 0f)]
    [TestCase(0.5f, 0.16f, 0.34f, 0.5f)]
    [TestCase(1f, 0.16f, 0.84f, 1f)]
    [TestCase(2f, 2f, 0f, 1f)]
    public void CalculatePulseRange_ClampsTailAndHead(
        float progress,
        float length,
        float expectedTail,
        float expectedHead)
    {
        BattleCurvePulseRenderer.CalculatePulseRange(
            progress,
            length,
            out float tail,
            out float head);

        Assert.That(tail, Is.EqualTo(expectedTail).Within(0.0001f));
        Assert.That(head, Is.EqualTo(expectedHead).Within(0.0001f));
        Assert.That(tail, Is.InRange(0f, 1f));
        Assert.That(head, Is.InRange(0f, 1f));
        Assert.That(tail, Is.LessThanOrEqualTo(head));
    }

    [TestCase(0.2f, 0.55f, 0.25f, 1f)]
    [TestCase(0.55f, 0.55f, 0.25f, 1f)]
    [TestCase(0.675f, 0.55f, 0.25f, 0.5f)]
    [TestCase(0.8f, 0.55f, 0.25f, 0f)]
    public void CalculateEndFadeOpacity_FadesAfterPulseReachesEndpoint(
        float elapsedTime,
        float travelTime,
        float fadeTime,
        float expectedOpacity)
    {
        float opacity = BattleCurvePulseRenderer.CalculateEndFadeOpacity(
            elapsedTime,
            travelTime,
            fadeTime);

        Assert.That(opacity,
            Is.EqualTo(expectedOpacity).Within(0.0001f));
    }

    [TestCase(0f, 2f, 0.25f, true)]
    [TestCase(2.249f, 2f, 0.25f, true)]
    [TestCase(2.25f, 2f, 0.25f, false)]
    [TestCase(2.5f, 2f, 0.25f, false)]
    public void IsPulseActiveAtElapsedTime_IncludesTheEntireEndFade(
        float elapsedTime,
        float travelTime,
        float fadeTime,
        bool expectedActive)
    {
        bool isActive = BattleCurvePulseRenderer.IsPulseActiveAtElapsedTime(
            elapsedTime,
            travelTime,
            fadeTime);

        Assert.That(isActive, Is.EqualTo(expectedActive));
    }
}
