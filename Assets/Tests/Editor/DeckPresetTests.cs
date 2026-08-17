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

            Assert.That(deck[0].ItemId, Is.EqualTo("aircraft_charge"));
            Assert.That(deck[1].ItemId, Is.EqualTo("aircraft_first"));
            Assert.That(deck[2].ItemId, Is.EqualTo("aircraft_shield"));
            Assert.That(deck[3].ItemId, Is.EqualTo("equipment_1x2"));
            Assert.That(deck[4].ItemId, Is.EqualTo("equipment_arc_coil"));
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
        Assert.That(preset.Slots[0].ItemId, Is.EqualTo("aircraft_charge"));
        Assert.That(preset.Slots[1].ItemId, Is.EqualTo("aircraft_first"));
        Assert.That(preset.Slots[2].ItemId, Is.EqualTo("aircraft_shield"));
        Assert.That(preset.Slots[3].ItemId, Is.EqualTo("equipment_1x2"));
        Assert.That(preset.Slots[4].ItemId, Is.EqualTo("equipment_arc_coil"));
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
