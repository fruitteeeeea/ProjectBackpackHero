using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class BackpackStrengthCalculatorTests
{
    [Test]
    public void Calculate_NullBackpack_ReturnsZeroScore()
    {
        BackpackStrengthScore score =
            BackpackStrengthCalculator.Calculate(null);

        Assert.That(score.AircraftCount, Is.Zero);
        Assert.That(score.EquipmentCount, Is.Zero);
        Assert.That(score.TotalScore, Is.Zero);
    }

    [Test]
    public void Calculate_EmptyBackpack_ReturnsZeroScore()
    {
        BackpackStrengthScore score =
            BackpackStrengthCalculator.Calculate(new BackpackController());

        Assert.That(score.AircraftCount, Is.Zero);
        Assert.That(score.EquipmentCount, Is.Zero);
        Assert.That(score.AircraftScore, Is.Zero);
        Assert.That(score.EquipmentScore, Is.Zero);
        Assert.That(score.TotalScore, Is.Zero);
    }

    [TestCase(1, 10f)]
    [TestCase(2, 12.5f)]
    [TestCase(3, 15f)]
    public void Calculate_AircraftAtEachLevel_AppliesLevelMultiplier(
        int level,
        float expectedScore)
    {
        BackpackController backpack = new BackpackController();
        Assert.That(backpack.PlaceItem(NewItem("aircraft", ItemType.Aircraft, level), Vector2Int.zero), Is.True);

        BackpackStrengthScore score = BackpackStrengthCalculator.Calculate(backpack);

        Assert.That(score.AircraftCount, Is.EqualTo(1));
        Assert.That(score.AircraftScore, Is.EqualTo(expectedScore));
        Assert.That(score.TotalScore, Is.EqualTo(expectedScore));
    }

    [TestCase(1, 10f)]
    [TestCase(2, 12.5f)]
    [TestCase(3, 15f)]
    public void Calculate_AdjacentEquipmentAtEachLevel_AppliesMultiplierToWholeContribution(
        int level,
        float expectedEquipmentScore)
    {
        BackpackController backpack = new BackpackController();
        Assert.That(backpack.PlaceItem(NewItem("aircraft", ItemType.Aircraft), Vector2Int.zero), Is.True);
        Assert.That(backpack.PlaceItem(NewItem("equipment", ItemType.Equipment, level), Vector2Int.right), Is.True);

        BackpackStrengthScore score = BackpackStrengthCalculator.Calculate(backpack);

        Assert.That(score.EquipmentCount, Is.EqualTo(1));
        Assert.That(score.EquipmentScore, Is.EqualTo(expectedEquipmentScore));
    }

    [Test]
    public void Calculate_EquipmentAdjacentToTwoAircraft_CountsBothAircraft()
    {
        BackpackController backpack = new BackpackController(3, 2);
        Assert.That(backpack.PlaceItem(NewItem("left", ItemType.Aircraft), Vector2Int.zero), Is.True);
        Assert.That(backpack.PlaceItem(NewItem("equipment", ItemType.Equipment), Vector2Int.right), Is.True);
        Assert.That(backpack.PlaceItem(NewItem("right", ItemType.Aircraft), new Vector2Int(2, 0)), Is.True);

        BackpackStrengthScore score = BackpackStrengthCalculator.Calculate(backpack);

        Assert.That(score.EquipmentScore, Is.EqualTo(18f));
        Assert.That(score.TotalScore, Is.EqualTo(38f));
    }

    [Test]
    public void Calculate_MultiCellEquipmentTouchingSameAircraft_CountsAircraftOnce()
    {
        BackpackController backpack = new BackpackController(3, 2);
        Assert.That(backpack.PlaceItem(NewItem("aircraft", ItemType.Aircraft, 1, new[] { Vector2Int.zero, Vector2Int.up }), Vector2Int.zero), Is.True);
        Assert.That(backpack.PlaceItem(NewItem("equipment", ItemType.Equipment, 1, new[] { Vector2Int.zero, Vector2Int.up }), Vector2Int.right), Is.True);

        BackpackStrengthScore score = BackpackStrengthCalculator.Calculate(backpack);

        Assert.That(score.EquipmentScore, Is.EqualTo(10f));
        Assert.That(score.TotalScore, Is.EqualTo(20f));
    }

    [Test]
    public void Calculate_MixedBackpack_ReturnsSeparatedAndTotalScores()
    {
        BackpackController backpack = new BackpackController(3, 2);
        Assert.That(backpack.PlaceItem(NewItem("aircraft-one", ItemType.Aircraft, 1), Vector2Int.zero), Is.True);
        Assert.That(backpack.PlaceItem(NewItem("equipment", ItemType.Equipment, 2), Vector2Int.right), Is.True);
        Assert.That(backpack.PlaceItem(NewItem("aircraft-three", ItemType.Aircraft, 3), new Vector2Int(2, 0)), Is.True);

        BackpackStrengthScore score = BackpackStrengthCalculator.Calculate(backpack);

        Assert.That(score.AircraftCount, Is.EqualTo(2));
        Assert.That(score.EquipmentCount, Is.EqualTo(1));
        Assert.That(score.AircraftScore, Is.EqualTo(25f));
        Assert.That(score.EquipmentScore, Is.EqualTo(22.5f));
        Assert.That(score.TotalScore, Is.EqualTo(47.5f));
    }

    private static ItemInstance NewItem(
        string id,
        ItemType type,
        int level = 1,
        Vector2Int[] offsets = null)
    {
        ItemShapeData shape = ScriptableObject.CreateInstance<ItemShapeData>();
        shape.InitializeForTests(id, null, offsets ?? new[] { Vector2Int.zero });
        ItemData data = ScriptableObject.CreateInstance<ItemData>();
        data.InitializeForTests(id, type, 1f, shape);
        return new ItemInstance(id, data, Vector2Int.zero, level);
    }
}
