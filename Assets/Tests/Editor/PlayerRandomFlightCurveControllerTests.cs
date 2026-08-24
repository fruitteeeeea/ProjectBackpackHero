using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class PlayerRandomFlightCurveControllerTests
{
    [Test]
    public void PeacefulProfile_UsesExpectedIntervalAndSpeed()
    {
        Vector2 interval = PlayerRandomFlightCurveController
            .GetOperationInterval(RandomFlightCurveMode.Peaceful);

        Assert.That(interval.x, Is.EqualTo(1.5f));
        Assert.That(interval.y, Is.EqualTo(2.5f));
        Assert.That(
            PlayerRandomFlightCurveController.GetAdjustmentSpeed(
                RandomFlightCurveMode.Peaceful,
                2f),
            Is.EqualTo(1f));
    }

    [Test]
    public void IntenseProfile_UsesExpectedIntervalAndSpeed()
    {
        Vector2 interval = PlayerRandomFlightCurveController
            .GetOperationInterval(RandomFlightCurveMode.Intense);

        Assert.That(interval.x, Is.EqualTo(.8f));
        Assert.That(interval.y, Is.EqualTo(1.5f));
        Assert.That(
            PlayerRandomFlightCurveController.GetAdjustmentSpeed(
                RandomFlightCurveMode.Intense,
                .5f),
            Is.EqualTo(.5f));
    }

    [Test]
    public void OffProfile_HasNoIntervalOrAdjustmentSpeed()
    {
        Assert.That(
            PlayerRandomFlightCurveController.GetOperationInterval(
                RandomFlightCurveMode.Off),
            Is.EqualTo(Vector2.zero));
        Assert.That(
            PlayerRandomFlightCurveController.GetAdjustmentSpeed(
                RandomFlightCurveMode.Off,
                1f),
            Is.Zero);
    }

    [Test]
    public void OperationChoice_UsesSeventyFivePercentBlockThreshold()
    {
        Assert.That(
            PlayerRandomFlightCurveController.ShouldBlockEnemy(.7499f),
            Is.True);
        Assert.That(
            PlayerRandomFlightCurveController.ShouldBlockEnemy(.75f),
            Is.False);
    }

    [Test]
    public void OutCubic_UsesExpectedEasingCurve()
    {
        Assert.That(
            PlayerRandomFlightCurveController.EvaluateOutCubic(0f),
            Is.EqualTo(0f));
        Assert.That(
            PlayerRandomFlightCurveController.EvaluateOutCubic(1f),
            Is.EqualTo(1f));
        Assert.That(
            PlayerRandomFlightCurveController.EvaluateOutCubic(.5f),
            Is.GreaterThan(.5f));
    }

    [Test]
    public void AdjustmentDuration_ScalesWithDistanceAndSpeed()
    {
        Assert.That(
            PlayerRandomFlightCurveController.GetAdjustmentDuration(
                -.5f,
                .5f,
                .5f),
            Is.EqualTo(2f));
        Assert.That(
            PlayerRandomFlightCurveController.GetAdjustmentDuration(
                -.5f,
                .5f,
                0f),
            Is.Zero);
    }
}
