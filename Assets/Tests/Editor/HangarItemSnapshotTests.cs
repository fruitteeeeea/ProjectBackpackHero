using BackpackHero.Battle;
using BackpackPrototype;
using NUnit.Framework;
using PlanetWar.ReusableMainMenu;
using UnityEngine;

public sealed class HangarItemSnapshotTests
{
    [Test]
    public void Snapshot_UsesEquipmentKindAndPreservesEquipmentTerminology()
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        try
        {
            item.InitializeForTests("Equipment", ItemType.Equipment, 2.5f, null);
            Assert.That(item.ItemType, Is.EqualTo(ItemType.Equipment));
            Assert.That(item.Cd, Is.EqualTo(2.5f));
        }
        finally { Object.DestroyImmediate(item); }
    }

    [Test]
    public void DetailAttributes_KeepLabelsValuesAndVisibility()
    {
        var hidden = new HangarDetailAttribute("Damage", "0", false);
        var visible = new HangarDetailAttribute("Cooldown", "2s", "+0.2s");
        Assert.That(hidden.Label, Is.EqualTo("Damage"));
        Assert.That(hidden.Visible, Is.False);
        Assert.That(visible.Value, Is.EqualTo("2s"));
        Assert.That(visible.AdditionalValue, Is.EqualTo("+0.2s"));
        Assert.That(visible.Visible, Is.True);
    }

    [Test]
    public void Snapshot_PreservesTheEnglishRankUnlockPresentation()
    {
        var snapshot = new HangarItemSnapshot(
            HangarItemKind.Aircraft, "Exploder", "", null, null, false,
            1, 0, 2, 0, 2f, 1, null, Color.white,
            "Unlocked at Rank 2", itemId: "aircraft_explosive",
            unlockRank: 2);

        Assert.That(snapshot.UnlockRequirementText,
            Is.EqualTo("Unlocked at Rank 2"));
        Assert.That(snapshot.UnlockRank, Is.EqualTo(2));
    }
}
