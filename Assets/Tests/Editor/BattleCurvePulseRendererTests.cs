using BackpackHero.Input;
using NUnit.Framework;

public sealed class BattleCurvePulseRendererTests
{
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
