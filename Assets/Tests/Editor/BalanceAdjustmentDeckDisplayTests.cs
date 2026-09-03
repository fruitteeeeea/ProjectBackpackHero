using System.Collections.Generic;
using BackpackHero.EditorTools;
using BackpackPrototype;
using NUnit.Framework;
using UnityEditor;

public sealed class BalanceAdjustmentDeckDisplayTests
{
    private const string DefaultPresetPath =
        "Assets/Data/Backpack/DeckPresets/DeckPreset_01.asset";

    [Test]
    public void DescribePlayer_UsesSharedPresetName_WhenSlotsMatch()
    {
        DeckPreset preset = LoadDefaultPreset();

        Assert.That(BalanceAdjustmentDeckDisplay.DescribePlayer(preset.Slots),
            Is.EqualTo("DeckPreset_01_GuardlineAssault"));
    }

    [Test]
    public void DescribeEnemy_UsesSharedPresetName_WhenEquivalentRuntimeDeckMatches()
    {
        DeckPreset preset = LoadDefaultPreset();
        var runtimeDeck = new List<ItemData>(preset.Slots);

        Assert.That(BalanceAdjustmentDeckDisplay.DescribeEnemy(runtimeDeck,
                "Runtime Deck"),
            Is.EqualTo("DeckPreset_01_GuardlineAssault"));
    }

    [Test]
    public void SlotsMatch_RequiresTheSameItemInTheSameSlot_IncludingEmptySlots()
    {
        DeckPreset preset = LoadDefaultPreset();
        var reordered = new List<ItemData>(preset.Slots);
        (reordered[0], reordered[1]) = (reordered[1], reordered[0]);
        var movedToEmptySlot = new List<ItemData>(preset.Slots);
        movedToEmptySlot[0] = null;
        movedToEmptySlot[2] = preset.Slots[0];

        Assert.That(BalanceAdjustmentDeckDisplay.SlotsMatch(preset.Slots,
            reordered), Is.False);
        Assert.That(BalanceAdjustmentDeckDisplay.SlotsMatch(preset.Slots,
            movedToEmptySlot), Is.False);
    }

    [Test]
    public void DescribeDecks_KeepTheirOriginalFallbackText_WhenNoSharedPresetMatches()
    {
        DeckPreset preset = LoadDefaultPreset();
        var unmatchedDeck = new List<ItemData>(preset.Slots);
        unmatchedDeck[0] = null;

        Assert.That(BalanceAdjustmentDeckDisplay.DescribePlayer(unmatchedDeck),
            Does.StartWith("当前玩家 Deck：空 / "));
        Assert.That(BalanceAdjustmentDeckDisplay.DescribeEnemy(unmatchedDeck,
                "Rank_1_Stage_1"),
            Is.EqualTo("Rank_1_Stage_1"));
    }

    private static DeckPreset LoadDefaultPreset()
    {
        DeckPreset preset = AssetDatabase.LoadAssetAtPath<DeckPreset>(
            DefaultPresetPath);
        Assert.That(preset, Is.Not.Null);
        return preset;
    }
}
