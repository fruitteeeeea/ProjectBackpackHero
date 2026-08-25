using System;
using System.Collections.Generic;
using BackpackPrototype;
using NUnit.Framework;
using UnityEngine;

public sealed class PackSystemTests
{
    readonly List<GameObject> roots = new();

    [SetUp]
    public void SetUp()
    {
        if (PackSystem.Instance != null) UnityEngine.Object.DestroyImmediate(PackSystem.Instance.gameObject);
        PlayerPrefs.DeleteKey(PackSystem.SaveKey);
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject root in roots)
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        if (PackSystem.Instance != null) UnityEngine.Object.DestroyImmediate(PackSystem.Instance.gameObject);
        PlayerPrefs.DeleteKey(PackSystem.SaveKey);
        PlayerPrefs.Save();
    }

    [Test]
    public void StartingOnePack_LocksEveryOtherUnstartedPack()
    {
        PackSystem packs = CreateSystem();
        Assert.That(packs.TryAddPack(PackId.Green), Is.True);
        Assert.That(packs.TryAddPack(PackId.Blue), Is.True);

        Assert.That(packs.TryStartPack(0), Is.True);
        Assert.That(packs.ActiveOpeningSlotIndex, Is.EqualTo(0));
        Assert.That(packs.GetSlotState(0), Is.EqualTo(PackState.Opening));
        Assert.That(packs.GetSlotState(1), Is.EqualTo(PackState.Locked));
        Assert.That(packs.TryStartPack(1), Is.False);
    }

    [Test]
    public void LegacyParallelCountdowns_KeepEarliestAndResetTheOthers()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var save = new PackSaveData
        {
            Slots = new List<PackSlotData>
            {
                new() { Id = PackId.Green, HasPack = true, OpenTimeUtcMs = now - 1_000 },
                new() { Id = PackId.Blue, HasPack = true, OpenTimeUtcMs = now - 2_000 },
                new(), new()
            }
        };
        PlayerPrefs.SetString(PackSystem.SaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();

        PackSystem packs = CreateSystem();

        Assert.That(packs.ActiveOpeningSlotIndex, Is.EqualTo(1));
        Assert.That(packs.GetSlotState(1), Is.EqualTo(PackState.Opening));
        Assert.That(packs.GetSlotState(0), Is.EqualTo(PackState.Locked));
        Assert.That(packs.GetSlots()[0].OpenTimeUtcMs, Is.Zero);
    }

    [Test]
    public void OnlyZeroRemainingTime_CompletesAndReleasesTheQueue()
    {
        PackSystem packs = CreateSystem();
        packs.TryAddPack(PackId.Green);
        packs.TryAddPack(PackId.Blue);
        Assert.That(packs.TryStartPack(0), Is.True);

        int remaining = packs.GetRemainingSeconds(0);
        Assert.That(packs.ReduceOpenTime(0, remaining - 60), Is.True);
        Assert.That(packs.GetRemainingSeconds(0), Is.InRange(1, 60));
        Assert.That(packs.GetSlotState(0), Is.EqualTo(PackState.Opening));
        Assert.That(packs.GetSlotState(1), Is.EqualTo(PackState.Locked));

        Assert.That(packs.ReduceOpenTime(0, packs.GetRemainingSeconds(0)), Is.True);
        Assert.That(packs.GetSlotState(0), Is.EqualTo(PackState.Opened));
        Assert.That(packs.GetSlotState(1), Is.EqualTo(PackState.Start));
    }

    PackSystem CreateSystem()
    {
        GameObject root = new GameObject("PackSystemTests");
        roots.Add(root);
        return root.AddComponent<PackSystem>();
    }
}
