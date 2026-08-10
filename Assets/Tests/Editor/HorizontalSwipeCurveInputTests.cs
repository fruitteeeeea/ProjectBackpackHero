using BackpackHero.Input;
using NUnit.Framework;

public sealed class HorizontalSwipeCurveInputTests
{
    private HorizontalSwipeValueModel model;

    [Test]
    public void DefaultSwipeSensitivity_IsFive()
    {
        var gameObject = new UnityEngine.GameObject("Swipe Input Test");
        try
        {
            var input = gameObject.AddComponent<HorizontalSwipeCurveInput>();
            Assert.That(input.SwipeSensitivity, Is.EqualTo(5f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

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
    public void PointerBegan_OnlyPublishesForSuccessfulNonUiGesture()
    {
        var gameObject = new UnityEngine.GameObject("Swipe Input Event Test");
        try
        {
            var input = gameObject.AddComponent<HorizontalSwipeCurveInput>();
            var receivedPositions = new System.Collections.Generic.List<UnityEngine.Vector2>();
            input.PointerBegan += receivedPositions.Add;
            input.SetInputEnabled(true);

            Assert.That(input.TryBeginPointer(1, true, new UnityEngine.Vector2(10f, 20f)), Is.False);
            Assert.That(input.TryBeginPointer(2, false, new UnityEngine.Vector2(30f, 40f)), Is.True);

            Assert.That(receivedPositions, Has.Count.EqualTo(1));
            Assert.That(receivedPositions[0], Is.EqualTo(new UnityEngine.Vector2(30f, 40f)));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
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
