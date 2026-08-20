using System.Collections.Generic;
using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class PlayerItemSystemTests
{
    private readonly List<Object> created = new();
    private GameObject root;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(PlayerItemSystem.SaveKey);
        PlayerPrefs.Save();
        root = new GameObject("PlayerItemSystemTests");
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(PlayerItemSystem.SaveKey);
        PlayerPrefs.Save();
        foreach (Object asset in created) Object.DestroyImmediate(asset);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void FirstLoad_CreatesUnlockedMaxLevelEntries_AndPersistsCurrencyAndFragments()
    {
        ItemData aircraft = NewItem("aircraft_test");
        ItemData equipment = NewItem("equipment_test");
        PlayerItemSystem system = NewSystem(aircraft, equipment);

        Assert.That(system.GetAllItems(), Has.Count.EqualTo(2));
        Assert.That(system.IsUnlocked(aircraft), Is.True);
        Assert.That(system.GetLevel(equipment), Is.EqualTo(PlayerItemSystem.MaximumLevel));
        system.AddCurrency(8, 3);
        system.AddFragments(aircraft, 5);

        Object.DestroyImmediate(system.gameObject);
        PlayerItemSystem reload = NewSystem(aircraft, equipment);
        Assert.That(reload.Gold, Is.EqualTo(8));
        Assert.That(reload.Diamond, Is.EqualTo(3));
        Assert.That(reload.GetFragments(aircraft), Is.EqualTo(5));
    }

    [Test]
    public void Load_NewCatalogItemAddsOnlyMissingEntry()
    {
        ItemData first = NewItem("first");
        ItemData added = NewItem("added");
        PlayerItemSystem system = NewSystem(first);
        system.AddFragments(first, 4);
        system.SetCatalog(NewCatalog(first, added));

        Assert.That(system.GetFragments(first), Is.EqualTo(4));
        Assert.That(system.GetLevel(added), Is.EqualTo(PlayerItemSystem.MaximumLevel));
        Assert.That(system.IsUnlocked(added), Is.True);
    }

    [Test]
    public void PlayerCombatItems_StartAtLocalLevelOneDespiteMaxOutOfMatchLevel()
    {
        ItemData item = NewItem("local_level");
        PlayerItemSystem system = NewSystem(item);
        GameObject combatRoot = new GameObject("Player Combat");
        created.Add(combatRoot);
        BackpackCombatController combat =
            combatRoot.AddComponent<BackpackCombatController>();

        Assert.That(
            system.GetLevel(item),
            Is.EqualTo(PlayerItemSystem.MaximumLevel));
        Assert.That(ItemInstance.MaximumLevel, Is.EqualTo(3));

        ItemInstance localItem = combat.AddItem(
            item,
            Vector2Int.zero);

        Assert.That(localItem, Is.Not.Null);
        Assert.That(
            localItem.Level,
            Is.EqualTo(ItemInstance.DefaultLevel));
        Assert.That(localItem.ProgressionLevel,
            Is.EqualTo(PlayerItemSystem.MaximumLevel));
    }

    [Test]
    public void Catalog_RejectsEmptyAndDuplicateItemIds()
    {
        ItemData first = NewItem("same");
        ItemData duplicate = NewItem("same");
        PlayerItemCatalog catalog = NewCatalog(first, duplicate);
        Assert.That(catalog.IsValid(out _), Is.False);
        duplicate.ConfigurePlayerProgressForTests(" ");
        Assert.That(catalog.IsValid(out _), Is.False);
    }

    [Test]
    public void TryUpgrade_UsesOnlySameItemFragments_AndDoesNotChangeOnFailure()
    {
        ItemData item = NewItem("upgrade");
        PlayerItemSystem system = NewSystem(item);
        PlayerItemState state = system.GetState(item);
        state.Level = 1;
        system.AddFragments(item, 2);

        Assert.That(system.TryUpgrade(item), Is.EqualTo(PlayerItemUpgradeResult.Success));
        Assert.That(system.GetFragments(item), Is.Zero);
        Assert.That(system.GetLevel(item), Is.EqualTo(2));
        Assert.That(system.TryUpgrade(item), Is.EqualTo(PlayerItemUpgradeResult.FragmentsNotEnough));
    }

    [Test]
    public void Deck_EnforcesFixedTypesAndRejectsDuplicates()
    {
        ItemData aircraft = NewItem("deck_aircraft", type: ItemType.Aircraft);
        ItemData equipment = NewItem("deck_equipment", type: ItemType.Equipment);
        PlayerItemSystem system = NewSystem(aircraft, equipment);

        Assert.That(system.TryEquipDeckSlot(0, aircraft), Is.EqualTo(PlayerDeckResult.Success));
        Assert.That(system.TryEquipDeckSlot(1, aircraft), Is.EqualTo(PlayerDeckResult.Duplicate));
        Assert.That(system.TryEquipDeckSlot(3, aircraft), Is.EqualTo(PlayerDeckResult.WrongType));
        Assert.That(system.TryEquipDeckSlot(3, equipment), Is.EqualTo(PlayerDeckResult.Success));
        Assert.That(system.GetDeckItem(0), Is.SameAs(aircraft));
        Assert.That(system.GetDeckItem(3), Is.SameAs(equipment));
    }

    [Test]
    public void LegacySave_Equipment_IsUnlockedMaxLevelAndKeptInDeck()
    {
        ItemData equipment = NewItem("legacy_equipment");
        PlayerPrefs.SetString(PlayerItemSystem.SaveKey, JsonUtility.ToJson(new PlayerItemSaveData
        {
            Items = new List<PlayerItemState>
            {
                new() { ItemId = equipment.ItemId, Unlocked = false, Level = 1 }
            },
            DeckItemIds = new List<string> { null, null, null, equipment.ItemId, null }
        }));
        PlayerPrefs.Save();

        PlayerItemSystem system = NewSystem(equipment);

        Assert.That(system.IsUnlocked(equipment), Is.True);
        Assert.That(system.GetLevel(equipment), Is.EqualTo(PlayerItemSystem.MaximumLevel));
        Assert.That(system.GetDeckItem(3), Is.SameAs(equipment));
    }

    [Test]
    public void PersistedCurrentSave_PreservesProgression()
    {
        ItemData aircraft = NewItem("current_aircraft", type: ItemType.Aircraft);
        ItemData equipment = NewItem("current_equipment");
        PlayerPrefs.SetString(PlayerItemSystem.SaveKey, JsonUtility.ToJson(new PlayerItemSaveData
        {
            ProgressionVersion = 1,
            Items = new List<PlayerItemState>
            {
                new() { ItemId = aircraft.ItemId, Unlocked = true, Level = 3, FragmentCount = 6 },
                new() { ItemId = equipment.ItemId, Unlocked = true, Level = 4, FragmentCount = 4 }
            }
        }));
        PlayerPrefs.Save();

        PlayerItemSystem system = NewSystem(aircraft, equipment);

        Assert.That(system.IsUnlocked(aircraft), Is.True);
        Assert.That(system.GetLevel(aircraft), Is.EqualTo(3));
        Assert.That(system.IsUnlocked(equipment), Is.True);
        Assert.That(system.GetLevel(equipment), Is.EqualTo(4));
        Assert.That(system.GetFragments(aircraft), Is.EqualTo(6));
        Assert.That(system.GetFragments(equipment), Is.EqualTo(4));
    }

    [Test]
    public void ProgressionCostAndMultipliers_InterpolateAcrossTenLevels()
    {
        ItemData aircraft = NewItem("curves", type: ItemType.Aircraft);
        aircraft.SetOutOfMatchProgressionForTests(2, 100,
            new Vector2(1f, 1.4f), new Vector2(1f, 1.4f),
            new Vector2(1f, 0.8f));

        Assert.That(aircraft.GetUpgradeFragmentCost(1), Is.EqualTo(2));
        Assert.That(aircraft.GetUpgradeFragmentCost(3), Is.EqualTo(26));
        Assert.That(aircraft.GetUpgradeFragmentCost(9), Is.EqualTo(100));
        Assert.That(aircraft.GetAircraftHealthMultiplierForProgressionLevel(1), Is.EqualTo(1f));
        Assert.That(aircraft.GetAircraftDamageMultiplierForProgressionLevel(10), Is.EqualTo(1.4f));
        Assert.That(aircraft.GetEquipmentIntervalMultiplierForProgressionLevel(10), Is.EqualTo(1f));
    }

    private PlayerItemSystem NewSystem(params ItemData[] items)
    {
        GameObject systemRoot = new GameObject("TestSystem");
        created.Add(systemRoot);
        PlayerItemSystem system = systemRoot.AddComponent<PlayerItemSystem>();
        system.SetCatalog(NewCatalog(items));
        return system;
    }

    private ItemData NewItem(string id, int gold = 0, int fragments = 0, ItemType type = ItemType.Equipment)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.InitializeForTests(id, type, 1f, null);
        item.ConfigurePlayerProgressForTests(id, gold, fragments);
        created.Add(item);
        return item;
    }

    private PlayerItemCatalog NewCatalog(params ItemData[] items)
    {
        PlayerItemCatalog catalog = ScriptableObject.CreateInstance<PlayerItemCatalog>();
        catalog.SetItemsForTests(items);
        created.Add(catalog);
        return catalog;
    }
}
