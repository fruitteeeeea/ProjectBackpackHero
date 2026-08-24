using BackpackHero.Input;
using NUnit.Framework;

public sealed class CurveAdjustmentVisualControllerTests
{
    [Test]
    public void State_EntersOnActivityAndExitsAfterIdleDelay()
    {
        var state = new CurveAdjustmentStateModel();

        state.RegisterActivity(5f);
        state.Update(5.99f, true, 1f);

        Assert.That(state.IsAdjusting, Is.True);

        state.Update(6f, true, 1f);

        Assert.That(state.IsAdjusting, Is.False);
    }

    [Test]
    public void State_ExitsImmediatelyWhenInputIsDisabled()
    {
        var state = new CurveAdjustmentStateModel();
        state.RegisterActivity(5f);

        state.Update(5.01f, false, 1f);

        Assert.That(state.IsAdjusting, Is.False);
    }

    [Test]
    public void State_WaitsForAnActivePulseBeforeExiting()
    {
        var state = new CurveAdjustmentStateModel();
        state.RegisterActivity(5f);

        state.Update(6f, true, 1f, true);

        Assert.That(state.IsAdjusting, Is.True);
        Assert.That(state.IsWaitingForPulseCompletion, Is.True);

        state.CompletePulseWait();

        Assert.That(state.IsAdjusting, Is.False);
        Assert.That(state.IsWaitingForPulseCompletion, Is.False);
    }

    [Test]
    public void State_NewActivityCancelsPendingPulseWait()
    {
        var state = new CurveAdjustmentStateModel();
        state.RegisterActivity(5f);
        state.Update(6f, true, 1f, true);

        state.RegisterActivity(6.1f);
        state.CompletePulseWait();

        Assert.That(state.IsAdjusting, Is.True);
        Assert.That(state.IsWaitingForPulseCompletion, Is.False);
    }
}
