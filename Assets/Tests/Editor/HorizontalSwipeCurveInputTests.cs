using BackpackHero.Input;
using NUnit.Framework;

public sealed class HorizontalSwipeCurveInputTests
{
    private HorizontalSwipeValueModel model;

    [SetUp]
    public void SetUp()
    {
        model = new HorizontalSwipeValueModel();
    }

    [Test]
    public void Enable_ResetsValueToZero()
    {
        model.Enable();
        model.TryBegin(1, false);
        model.TryApplyDelta(1, 50f, 100f, 1f);

        model.Disable();
        model.Enable();

        Assert.That(model.CurrentValue, Is.Zero);
        Assert.That(model.HasActivePointer, Is.False);
    }

    [TestCase(50f, 100f, 1f, 0.5f)]
    [TestCase(-25f, 100f, 2f, -0.5f)]
    public void ApplyDelta_UsesScreenProportionAndSensitivity(float delta, float width, float sensitivity, float expected)
    {
        model.Enable();
        model.TryBegin(7, false);

        Assert.That(model.TryApplyDelta(7, delta, width, sensitivity), Is.True);
        Assert.That(model.CurrentValue, Is.EqualTo(expected).Within(0.0001f));
    }

    [Test]
    public void ApplyDelta_ClampsToMinusOneAndOne()
    {
        model.Enable();
        model.TryBegin(1, false);

        model.TryApplyDelta(1, 500f, 100f, 1f);
        Assert.That(model.CurrentValue, Is.EqualTo(1f));

        model.TryApplyDelta(1, -500f, 100f, 1f);
        Assert.That(model.CurrentValue, Is.EqualTo(-1f));
    }

    [Test]
    public void DisabledModel_IgnoresGestures()
    {
        Assert.That(model.TryBegin(1, false), Is.False);
        Assert.That(model.TryApplyDelta(1, 50f, 100f, 1f), Is.False);
        Assert.That(model.CurrentValue, Is.Zero);
    }

    [Test]
    public void GestureBeginningOverUi_IsIgnoredForItsEntireLifetime()
    {
        model.Enable();

        Assert.That(model.TryBegin(1, true), Is.False);
        Assert.That(model.TryApplyDelta(1, 50f, 100f, 1f), Is.False);
        Assert.That(model.CurrentValue, Is.Zero);
    }

    [Test]
    public void AdditionalPointer_CannotTakeOverActiveGesture()
    {
        model.Enable();
        Assert.That(model.TryBegin(10, false), Is.True);

        Assert.That(model.TryBegin(11, false), Is.False);
        Assert.That(model.TryApplyDelta(11, 100f, 100f, 1f), Is.False);
        Assert.That(model.TryApplyDelta(10, 25f, 100f, 1f), Is.True);
        Assert.That(model.CurrentValue, Is.EqualTo(0.25f));
    }

    [Test]
    public void EndingDifferentPointer_DoesNotCancelActiveGesture()
    {
        model.Enable();
        model.TryBegin(10, false);

        model.End(11);

        Assert.That(model.HasActivePointer, Is.True);
        Assert.That(model.ActivePointerId, Is.EqualTo(10));
    }
}
