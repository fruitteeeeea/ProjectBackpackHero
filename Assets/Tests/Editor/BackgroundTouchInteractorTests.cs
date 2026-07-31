using BackpackHero.Background;
using NUnit.Framework;
using UnityEngine;

public sealed class BackgroundTouchInteractorTests
{
    [Test]
    public void RandomInteraction_RequiresThePlanetToSettleBeforeRetriggering()
    {
        var planetObject = new GameObject("Planet");
        try
        {
            var planet = planetObject.AddComponent<BackgroundPlanet>();
            planet.SetAnchor(Vector3.zero, 1f);
            Assert.That(planet.TryTriggerRandomMotion(1f, 1f, 0f, .1f), Is.True);
            Assert.That(planet.TryTriggerRandomMotion(1f, 1f, 0f, .1f), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(planetObject);
        }
    }

    [Test]
    public void RandomInteraction_StartsCooldownAfterTheFirstTrigger()
    {
        var planetObject = new GameObject("Planet");
        try
        {
            var planet = planetObject.AddComponent<BackgroundPlanet>();
            planet.SetAnchor(Vector3.zero, 1f);
            Assert.That(planet.TryTriggerRandomMotion(1f, 1f, 1f, 1f), Is.True);
            Assert.That(planet.TryTriggerRandomMotion(1f, 1f, 1f, 1f), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(planetObject);
        }
    }
}
