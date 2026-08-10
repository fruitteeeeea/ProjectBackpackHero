using BackpackHero.Input;
using NUnit.Framework;
using UnityEngine;

public sealed class CurvedConnectionRendererVisualTests
{
    [Test]
    public void VisualProperties_ClampWidthAndOpacity()
    {
        var gameObject = new GameObject("Curve Test");
        try
        {
            var curve = gameObject.AddComponent<CurvedConnectionRenderer>();

            curve.LineWidth = -2f;
            curve.LineOpacity = 4f;

            Assert.That(curve.LineWidth, Is.EqualTo(0.001f));
            Assert.That(curve.LineOpacity, Is.EqualTo(1f));

            curve.LineOpacity = -1f;
            Assert.That(curve.LineOpacity, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void BattleWorldEndpointReadiness_RequiresExplicitSynchronization()
    {
        var curveObject = new GameObject("Curve Test");
        var player = new GameObject("Player Endpoint");
        var enemy = new GameObject("Enemy Endpoint");
        try
        {
            var curve = curveObject.AddComponent<CurvedConnectionRenderer>();

            curve.SetEndpoints(player.transform, enemy.transform);
            Assert.That(curve.HasBattleWorldEndpoints, Is.False);

            curve.SetBattleWorldEndpoints(player.transform, enemy.transform);
            Assert.That(curve.HasBattleWorldEndpoints, Is.True);

            curve.SetBattleWorldEndpointReadiness(false);
            Assert.That(curve.HasBattleWorldEndpoints, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(curveObject);
        }
    }
}
