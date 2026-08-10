using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class ItemPlacementRotationTests
{
    [TestCase(0f, 0f, 0f)]
    [TestCase(31f, -18f, 0f)]
    [TestCase(-42.5f, 11.25f, 8f)]
    public void CalculateCorrectedWorldPosition_KeepsCenterFixed(
        float centerX,
        float centerY,
        float centerZ)
    {
        Vector3 currentPosition = new(12f, -7f, 3f);
        Vector3 fixedCenter = new(centerX, centerY, centerZ);
        Vector3 rotatedCenter = new(-4f, 9f, 2f);

        Vector3 corrected =
            ItemPlacementRotation.CalculateCorrectedWorldPosition(
                currentPosition,
                fixedCenter,
                rotatedCenter);

        Assert.That(
            corrected + rotatedCenter - currentPosition,
            Is.EqualTo(fixedCenter));
    }
}
