using BackpackHero.Battle;
using BackpackHero.Input;
using NUnit.Framework;
using UnityEngine;

public sealed class CurvedConnectionRendererTests
{
    private static readonly Vector3 Player = new(0f, -3.5f, 0f);
    private static readonly Vector3 Enemy = new(0f, 3.5f, 0f);

    [Test]
    public void CalculatePoint_ZeroValueProducesStraightLine()
    {
        for (var index = 0; index <= 10; index++)
        {
            var point = CurvedConnectionRenderer.CalculatePoint(Player, Enemy, 0f, 3f, index / 10f);
            Assert.That(point.x, Is.EqualTo(0f).Within(0.0001f));
        }
    }

    [TestCase(-1f, -3f)]
    [TestCase(1f, 3f)]
    [TestCase(0.5f, 1.5f)]
    public void CalculatePoint_MidpointReachesRequestedBend(float curveValue, float expectedX)
    {
        var point = CurvedConnectionRenderer.CalculatePoint(Player, Enemy, curveValue, 3f, 0.5f);

        Assert.That(point.x, Is.EqualTo(expectedX).Within(0.0001f));
        Assert.That(point.y, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void CalculatePoint_ClampsCurveValueAndNegativeBendDistance()
    {
        var clampedValuePoint = CurvedConnectionRenderer.CalculatePoint(Player, Enemy, 4f, 3f, 0.5f);
        var clampedDistancePoint = CurvedConnectionRenderer.CalculatePoint(Player, Enemy, 1f, -3f, 0.5f);

        Assert.That(clampedValuePoint.x, Is.EqualTo(3f).Within(0.0001f));
        Assert.That(clampedDistancePoint.x, Is.Zero.Within(0.0001f));
    }

    [Test]
    public void FindClosestNormalizedTime_CurvedSamplesReturnsNearbyPoint()
    {
        const int sampleCount = 64;
        var samples = new Vector2[sampleCount + 1];

        for (var index = 0; index <= sampleCount; index++)
        {
            Vector3 point = CurvedConnectionRenderer.CalculatePoint(
                Player,
                Enemy,
                0.75f,
                3f,
                (float)index / sampleCount);
            samples[index] = point;
        }

        Vector3 targetPoint = CurvedConnectionRenderer.CalculatePoint(
            Player,
            Enemy,
            0.75f,
            3f,
            0.35f);
        float closestTime = BattleCurve2D.FindClosestNormalizedTime(
            new Vector2(targetPoint.x + 0.05f, targetPoint.y),
            samples);

        Assert.That(closestTime, Is.EqualTo(0.35f).Within(0.02f));
    }

    [TestCase(-5f, 0f, 0f)]
    [TestCase(5f, 0f, 1f)]
    public void FindClosestNormalizedTime_OutsideEndpointsClampsToEndpoint(
        float x,
        float y,
        float expectedTime)
    {
        var samples = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(2f, 0f),
        };

        float closestTime = BattleCurve2D.FindClosestNormalizedTime(
            new Vector2(x, y),
            samples);

        Assert.That(closestTime, Is.EqualTo(expectedTime).Within(0.0001f));
        Assert.That(closestTime, Is.InRange(0f, 1f));
    }
}
