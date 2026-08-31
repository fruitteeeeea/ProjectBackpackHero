using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackPrototype;
using BackpackHero.UI;
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
    public void ResetAllProgression_SetsEveryItemToLevelOneAndClearsFragments()
    {
        ItemData aircraft = NewItem("reset_aircraft", type: ItemType.Aircraft);
        ItemData equipment = NewItem("reset_equipment");
        PlayerItemSystem system = NewSystem(aircraft, equipment);
        system.AddFragments(aircraft, 7);
        system.AddFragments(equipment, 3);

        system.ResetAllProgression();

        Assert.That(system.GetLevel(aircraft), Is.EqualTo(PlayerItemSystem.DefaultLevel));
        Assert.That(system.GetLevel(equipment), Is.EqualTo(PlayerItemSystem.DefaultLevel));
        Assert.That(system.GetFragments(aircraft), Is.Zero);
        Assert.That(system.GetFragments(equipment), Is.Zero);
        Assert.That(system.IsUnlocked(aircraft), Is.True);
    }

    [Test]
    public void RestoreInitialProgression_RestoresFiveStarterCardsDeckAndCurrency()
    {
        ItemData charge = NewItem("aircraft_charge", type: ItemType.Aircraft);
        ItemData first = NewItem("aircraft_first", type: ItemType.Aircraft);
        ItemData shield = NewItem("aircraft_shield", type: ItemType.Aircraft);
        ItemData equipment = NewItem("equipment_1x2");
        ItemData coil = NewItem("equipment_arc_coil");
        ItemData locked = NewItem("locked_item");
        PlayerItemSystem system = NewSystem(charge, first, shield, equipment, coil, locked);
        int changeCount = 0;
        system.Changed += () => changeCount++;
        system.RestoreDefaultProgression();
        system.AddCurrency(900, 50);
        system.AddFragments(locked, 7);

        system.RestoreInitialProgression();

        Assert.That(system.Gold, Is.EqualTo(100));
        Assert.That(system.Diamond, Is.Zero);
        Assert.That(changeCount, Is.GreaterThanOrEqualTo(4));
        Assert.That(system.GetDeckItems(), Is.EqualTo(new[] { charge, first, shield, equipment, coil }));
        foreach (ItemData item in system.GetAllItems())
        {
            Assert.That(system.GetLevel(item), Is.EqualTo(PlayerItemSystem.DefaultLevel));
            Assert.That(system.GetFragments(item), Is.Zero);
        }
        Assert.That(system.IsUnlocked(charge), Is.True);
        Assert.That(system.IsUnlocked(first), Is.True);
        Assert.That(system.IsUnlocked(shield), Is.True);
        Assert.That(system.IsUnlocked(equipment), Is.True);
        Assert.That(system.IsUnlocked(coil), Is.True);
        Assert.That(system.IsUnlocked(locked), Is.False);
    }

    [Test]
    public void HangarCollection_SeparatesTypesAndSortsLockedItemsByUnlockRank()
    {
        ItemData aircraftUnlocked = NewItem("aircraft_unlocked", type: ItemType.Aircraft);
        ItemData aircraftRankTwoFirst = NewItem("aircraft_rank_two_first", type: ItemType.Aircraft);
        ItemData aircraftRankFour = NewItem("aircraft_rank_four", type: ItemType.Aircraft);
        ItemData aircraftRankTwoSecond = NewItem("aircraft_rank_two_second", type: ItemType.Aircraft);
        ItemData aircraftEquipped = NewItem("aircraft_equipped", type: ItemType.Aircraft);
        ItemData equipmentUnlocked = NewItem("equipment_unlocked");
        ItemData equipmentRankTwo = NewItem("equipment_rank_two");
        ItemData[] catalog =
        {
            aircraftRankTwoFirst, aircraftUnlocked, aircraftRankFour,
            aircraftRankTwoSecond, aircraftEquipped, equipmentRankTwo, equipmentUnlocked
        };
        var unlocked = new HashSet<ItemData> { aircraftUnlocked, equipmentUnlocked };
        var ranks = new Dictionary<ItemData, int>
        {
            [aircraftRankTwoFirst] = 2,
            [aircraftRankFour] = 4,
            [aircraftRankTwoSecond] = 2,
            [aircraftEquipped] = 1,
            [equipmentRankTwo] = 2
        };
        MethodInfo method = typeof(PlayerItemHangarPresenter).GetMethod(
            "OrderCollectionItems", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);

        IEnumerable<ItemData> result = (IEnumerable<ItemData>)method.Invoke(null,
            new object[]
            {
                catalog,
                new System.Func<ItemData, bool>(item => item == aircraftEquipped),
                new System.Func<ItemData, bool>(unlocked.Contains),
                new System.Func<ItemData, int>(item => ranks.TryGetValue(item, out int rank) ? rank : 1)
            });

        Assert.That(result.ToArray(), Is.EqualTo(new[]
        {
            aircraftUnlocked, aircraftRankTwoFirst, aircraftRankTwoSecond,
            aircraftRankFour, equipmentUnlocked, equipmentRankTwo
        }));
    }

    [Test]
    public void CollectionLevel_UsesUnlockedItemsAndRoundsDown()
    {
        ItemData levelTwo = NewItem("level_two");
        ItemData levelFive = NewItem("level_five");
        ItemData lockedLevelTen = NewItem("locked_level_ten");
        PlayerItemSystem system = NewSystem(levelTwo, levelFive, lockedLevelTen);
        system.GetState(levelTwo).Level = 2;
        system.GetState(levelFive).Level = 5;
        system.GetState(lockedLevelTen).Level = 10;
        system.GetState(lockedLevelTen).Unlocked = false;

        Assert.That(MenuDisplayProfile.CalculateCollectionLevel(system), Is.EqualTo(3));
    }

    [Test]
    public void CollectionLevel_WithoutUnlockedItemsFallsBackToDefaultLevel()
    {
        ItemData item = NewItem("locked");
        PlayerItemSystem system = NewSystem(item);
        system.GetState(item).Unlocked = false;

        Assert.That(MenuDisplayProfile.CalculateCollectionLevel(system),
            Is.EqualTo(PlayerItemSystem.DefaultLevel));
    }

    [Test]
    public void AddFragmentsToAllAircraft_ChangesOnlyAircraft()
    {
        ItemData firstAircraft = NewItem("first_aircraft", type: ItemType.Aircraft);
        ItemData secondAircraft = NewItem("second_aircraft", type: ItemType.Aircraft);
        ItemData equipment = NewItem("equipment");
        PlayerItemSystem system = NewSystem(firstAircraft, secondAircraft, equipment);

        system.AddFragmentsToAllAircraft(5);

        Assert.That(system.GetFragments(firstAircraft), Is.EqualTo(5));
        Assert.That(system.GetFragments(secondAircraft), Is.EqualTo(5));
        Assert.That(system.GetFragments(equipment), Is.Zero);
    }

    [Test]
    public void RestoreDefaultProgression_UnlocksAndMaxesEveryItemAndClearsFragments()
    {
        ItemData aircraft = NewItem("default_aircraft", type: ItemType.Aircraft);
        ItemData equipment = NewItem("default_equipment");
        PlayerItemSystem system = NewSystem(aircraft, equipment);
        system.ResetAllProgression();
        system.GetState(aircraft).Unlocked = false;
        system.AddFragments(aircraft, 8);
        system.AddFragments(equipment, 3);

        system.RestoreDefaultProgression();

        Assert.That(system.IsUnlocked(aircraft), Is.True);
        Assert.That(system.GetLevel(aircraft), Is.EqualTo(PlayerItemSystem.MaximumLevel));
        Assert.That(system.GetLevel(equipment), Is.EqualTo(PlayerItemSystem.MaximumLevel));
        Assert.That(system.GetFragments(aircraft), Is.Zero);
        Assert.That(system.GetFragments(equipment), Is.Zero);
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
