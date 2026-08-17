using BackpackHero.Input;
using NUnit.Framework;
using UnityEngine;

public sealed class CurveBeaconDragStateModelTests
{
    [Test]
    public void BeaconDrag_UsesCompleteCurvePosition()
    {
        var model = new CurveBeaconDragStateModel();
        model.Begin(true);

        Vector3 position = model.ResolvePosition(
            new Vector3(3f, 4f, 5f),
            new Vector3(1f, 2f, 6f));

        Assert.That(position, Is.EqualTo(new Vector3(3f, 4f, 5f)));
    }

    [Test]
    public void OutsideDrag_OnlyUpdatesXAndKeepsWorldY()
    {
        var model = new CurveBeaconDragStateModel();
        model.Begin(false);

        Vector3 position = model.ResolvePosition(
            new Vector3(3f, 4f, 5f),
            new Vector3(1f, 2f, 6f));

        Assert.That(position, Is.EqualTo(new Vector3(3f, 2f, 6f)));
    }

    [Test]
    public void EndingDrag_PreventsTheNextPositionFromUsingFullCurveCoordinates()
    {
        var model = new CurveBeaconDragStateModel();
        model.Begin(true);
        model.End();

        Vector3 position = model.ResolvePosition(
            new Vector3(3f, 4f, 5f),
            new Vector3(1f, 2f, 6f));

        Assert.That(model.IsDraggingBeacon, Is.False);
        Assert.That(position, Is.EqualTo(new Vector3(3f, 2f, 6f)));
    }
}
