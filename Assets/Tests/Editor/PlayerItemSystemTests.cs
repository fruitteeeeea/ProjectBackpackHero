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
        Assert.That(system.GetLevel(equipment), Is.EqualTo(ItemInstance.MaximumLevel));
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
        Assert.That(system.GetLevel(added), Is.EqualTo(ItemInstance.MaximumLevel));
        Assert.That(system.IsUnlocked(added), Is.True);
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
    public void TryUpgrade_UsesGoldAndSameItemFragments_AndDoesNotChangeOnFailure()
    {
        ItemData item = NewItem("upgrade", 10, 3);
        PlayerItemSystem system = NewSystem(item);
        PlayerItemState state = system.GetState(item);
        state.Level = 1;
        system.AddCurrency(10);
        system.AddFragments(item, 3);

        Assert.That(system.TryUpgrade(item), Is.EqualTo(PlayerItemUpgradeResult.Success));
        Assert.That(system.Gold, Is.Zero);
        Assert.That(system.GetFragments(item), Is.Zero);
        Assert.That(system.GetLevel(item), Is.EqualTo(2));
        Assert.That(system.TryUpgrade(item), Is.EqualTo(PlayerItemUpgradeResult.MaxLevel));
        Assert.That(system.Gold, Is.Zero);
    }

    private PlayerItemSystem NewSystem(params ItemData[] items)
    {
        GameObject systemRoot = new GameObject("TestSystem");
        created.Add(systemRoot);
        PlayerItemSystem system = systemRoot.AddComponent<PlayerItemSystem>();
        system.SetCatalog(NewCatalog(items));
        return system;
    }

    private ItemData NewItem(string id, int gold = 0, int fragments = 0)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.InitializeForTests(id, ItemType.Equipment, 1f, null);
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
