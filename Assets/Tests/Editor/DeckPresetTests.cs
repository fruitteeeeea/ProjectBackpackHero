using System.Collections.Generic;
using BackpackPrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DeckPresetTests
{
    [Test]
    public void FirstProductionDeck_UsesTheSpecifiedHangerItems()
    {
        PlayerPrefs.DeleteKey(PlayerItemSystem.SaveKey);
        var root = new GameObject("ProductionDefaultDeckTest");
        try
        {
            PlayerItemSystem system = root.AddComponent<PlayerItemSystem>();
            IReadOnlyList<ItemData> deck = system.GetDeckItems();

            Assert.That(deck[0].ItemId, Is.EqualTo("aircraft_first"));
            Assert.That(deck[1].ItemId, Is.EqualTo("aircraft_charge"));
            Assert.That(deck[2].ItemId, Is.EqualTo("aircraft_shield"));
            Assert.That(deck[3].ItemId, Is.EqualTo("equipment_rapid_cannon"));
            Assert.That(deck[4].ItemId, Is.EqualTo("equipment_1x2"));
        }
        finally
        {
            Object.DestroyImmediate(root);
            PlayerPrefs.DeleteKey(PlayerItemSystem.SaveKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void DefaultPreset_ContainsTheSpecifiedFiveItems()
    {
        DeckPreset preset = AssetDatabase.LoadAssetAtPath<DeckPreset>(
            "Assets/Data/Backpack/DeckPresets/DeckPreset_01.asset");

        Assert.That(preset, Is.Not.Null);
        Assert.That(preset.IsValid(out _), Is.True);
        Assert.That(preset.Slots, Has.Count.EqualTo(5));
        Assert.That(preset.Slots[0].ItemId, Is.EqualTo("aircraft_first"));
        Assert.That(preset.Slots[1].ItemId, Is.EqualTo("aircraft_charge"));
        Assert.That(preset.Slots[2].ItemId, Is.EqualTo("aircraft_shield"));
        Assert.That(preset.Slots[3].ItemId, Is.EqualTo("equipment_rapid_cannon"));
        Assert.That(preset.Slots[4].ItemId, Is.EqualTo("equipment_1x2"));
        Assert.That(preset.StrategyHint, Does.Contain("守线突击"));
        Assert.That(preset.AdjacencyHint, Is.Not.Empty);
    }

    [TestCase("DeckPreset_01.asset", "aircraft_first", "aircraft_charge", "aircraft_shield", "equipment_rapid_cannon", "equipment_1x2")]
    [TestCase("DeckPreset_02.asset", "aircraft_l", "aircraft_explosive", "aircraft_shield", "equipment_first", "equipment_arc_coil")]
    [TestCase("DeckPreset_03.asset", "aircraft_sniper", "aircraft_laser", "aircraft_first", "equipment_rapid_cannon", "equipment_wave_emitter")]
    [TestCase("DeckPreset_04.asset", "aircraft_shotgun", "aircraft_charge", "aircraft_shield", "equipment_first", "equipment_1x2")]
    [TestCase("DeckPreset_05.asset", "aircraft_l", "aircraft_sniper", "aircraft_laser", "equipment_laser_link", "equipment_arc_coil")]
    public void ProductionDecks_HaveDistinctValidArchetypes(
        string fileName, string aircraftOne, string aircraftTwo,
        string aircraftThree, string equipmentOne, string equipmentTwo)
    {
        DeckPreset preset = AssetDatabase.LoadAssetAtPath<DeckPreset>(
            "Assets/Data/Backpack/DeckPresets/" + fileName);

        Assert.That(preset, Is.Not.Null);
        Assert.That(preset.IsValid(out _), Is.True, fileName);
        Assert.That(preset.Slots[0].ItemId, Is.EqualTo(aircraftOne));
        Assert.That(preset.Slots[1].ItemId, Is.EqualTo(aircraftTwo));
        Assert.That(preset.Slots[2].ItemId, Is.EqualTo(aircraftThree));
        Assert.That(preset.Slots[3].ItemId, Is.EqualTo(equipmentOne));
        Assert.That(preset.Slots[4].ItemId, Is.EqualTo(equipmentTwo));
        Assert.That(preset.StrategyHint, Is.Not.Empty);
        Assert.That(preset.AdjacencyHint, Is.Not.Empty);
    }

    [Test]
    public void DeckPreset_RejectsDuplicateAndWrongSlotTypes()
    {
        ItemData aircraft = NewItem("aircraft", ItemType.Aircraft);
        ItemData equipment = NewItem("equipment", ItemType.Equipment);
        DeckPreset preset = ScriptableObject.CreateInstance<DeckPreset>();

        try
        {
            preset.SetSlots(new[] { aircraft, aircraft, null, equipment, null });
            Assert.That(preset.IsValid(out _), Is.False);

            preset.SetSlots(new[] { equipment, null, null, null, null });
            Assert.That(preset.IsValid(out _), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(preset);
            Object.DestroyImmediate(aircraft);
            Object.DestroyImmediate(equipment);
        }
    }

    [Test]
    public void PlayerDeck_RejectsInvalidPresetInputWithoutChangingDeck()
    {
        ItemData aircraft = NewItem("aircraft", ItemType.Aircraft);
        ItemData equipment = NewItem("equipment", ItemType.Equipment);
        var root = new GameObject("DeckPresetPlayerTest");
        var catalog = ScriptableObject.CreateInstance<PlayerItemCatalog>();
        catalog.SetItemsForTests(new[] { aircraft, equipment });
        PlayerItemSystem system = root.AddComponent<PlayerItemSystem>();
        system.SetCatalog(catalog);
        IReadOnlyList<ItemData> before = system.GetDeckItems();

        try
        {
            Assert.That(system.TryApplyDeck(new[] { aircraft, aircraft, null, equipment, null }), Is.EqualTo(PlayerDeckResult.Duplicate));
            Assert.That(system.GetDeckItems()[0], Is.SameAs(before[0]));
            Assert.That(system.GetDeckItems()[3], Is.SameAs(before[3]));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(aircraft);
            Object.DestroyImmediate(equipment);
        }
    }

    [Test]
    public void EnemyPrefab_DefaultsToSharedDeckPreset()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Backpacks/EnemyBackpack.prefab");
        Assert.That(prefab.GetComponent<EnemyBackpackSystem>().DefaultDeckPreset, Is.Not.Null);
    }

    private static ItemData NewItem(string id, ItemType type)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.InitializeForTests(id, type, 1f, null);
        item.ConfigurePlayerProgressForTests(id);
        return item;
    }
}
