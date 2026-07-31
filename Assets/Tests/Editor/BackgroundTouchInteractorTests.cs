using BackpackHero.Background;
using NUnit.Framework;
using UnityEngine;

public sealed class BackgroundTouchInteractorTests
{
    [Test]
    public void PlanetPointerDrag_IsClampedToItsActivityRadius()
    {
        var planetObject = new GameObject("Planet");
        try
        {
            var planet = planetObject.AddComponent<BackgroundPlanet>();
            planet.SetAnchor(Vector3.zero, 1f);
            planet.ApplyPointerDrag(Vector2.right * 10f, 1f, 1f, 0f);

            Assert.That(Vector2.Distance(planet.WorldPosition, Vector2.zero), Is.LessThanOrEqualTo(.6001f));
        }
        finally
        {
            Object.DestroyImmediate(planetObject);
        }
    }

    [Test]
    public void DistanceToSegment_UsesNearestPointOnTheWholeSwipe()
    {
        var distance = BackgroundTouchInteractor.DistanceToSegment(new Vector2(5f, 2f), Vector2.zero, new Vector2(10f, 0f));
        Assert.That(distance, Is.EqualTo(2f).Within(.0001f));
    }

    [Test]
    public void DistanceToSegment_HandlesZeroLengthSwipe()
    {
        var distance = BackgroundTouchInteractor.DistanceToSegment(new Vector2(3f, 4f), Vector2.zero, Vector2.zero);
        Assert.That(distance, Is.EqualTo(5f).Within(.0001f));
    }
}
