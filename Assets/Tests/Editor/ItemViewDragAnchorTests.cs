using System.Reflection;
using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class ItemViewDragAnchorTests
{
    [TestCase(80f, 0f, 0f)]
    [TestCase(80f, .5f, 40f)]
    [TestCase(120f, .5f, 60f)]
    public void DragAnchorLocalOffset_InsetsFromBottomOfGrabbedCell(
        float cellHeight,
        float bottomInsetRatio,
        float expectedY)
    {
        MethodInfo method = typeof(ItemView).GetMethod(
            "CalculateGrabAnchorLocalOffset",
            BindingFlags.Static | BindingFlags.NonPublic);

        Vector3 localAnchor = (Vector3)method.Invoke(null, new object[]
        {
            new Rect(-80f, -cellHeight, 160f, cellHeight * 2f),
            new Vector2(80f, cellHeight),
            Vector2.zero,
            Vector2.zero,
            bottomInsetRatio,
        });

        Assert.That(localAnchor.x, Is.EqualTo(-40f));
        Assert.That(localAnchor.y, Is.EqualTo(expectedY).Within(.001f));
    }
}
