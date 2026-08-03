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
}
