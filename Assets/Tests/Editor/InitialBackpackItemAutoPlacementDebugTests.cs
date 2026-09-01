using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BackpackHero.Debugging;
using BackpackPrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class InitialBackpackItemAutoPlacementDebugTests
{
    [SetUp]
    public void SetUp()
    {
        InitialBackpackItemAutoPlacementDebug.SetEnabled(false);
    }

    [TearDown]
    public void TearDown()
    {
        InitialBackpackItemAutoPlacementDebug.SetEnabled(false);
    }

    [Test]
    public void AutoPlacement_DefaultsToDisabled_AndCanBeToggled()
    {
        Assert.That(InitialBackpackItemAutoPlacementDebug.IsEnabled, Is.False);

        InitialBackpackItemAutoPlacementDebug.SetEnabled(true);
        Assert.That(InitialBackpackItemAutoPlacementDebug.IsEnabled, Is.True);

        InitialBackpackItemAutoPlacementDebug.SetEnabled(false);
        Assert.That(InitialBackpackItemAutoPlacementDebug.IsEnabled, Is.False);
    }

    [Test]
    public void BalancePreset_CopyAndComparison_IncludeAutoPlacement()
    {
        BalanceAdjustmentTestPreset enabled =
            ScriptableObject.CreateInstance<BalanceAdjustmentTestPreset>();
        BalanceAdjustmentTestPreset disabled =
            ScriptableObject.CreateInstance<BalanceAdjustmentTestPreset>();
        BalanceAdjustmentTestPreset copy =
            ScriptableObject.CreateInstance<BalanceAdjustmentTestPreset>();

        try
        {
            enabled.SetRuntimeState(1f, RandomFlightCurveMode.Off, true,
                1, 1, null, null, null, null, null);
            disabled.SetRuntimeState(1f, RandomFlightCurveMode.Off, false,
                1, 1, null, null, null, null, null);

            copy.CopyFrom(enabled);

            Assert.That(copy.AutoAddConfiguredItems, Is.True);
            Assert.That(copy.ContentEquals(enabled), Is.True);
            Assert.That(copy.ContentEquals(disabled), Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(enabled);
            UnityEngine.Object.DestroyImmediate(disabled);
            UnityEngine.Object.DestroyImmediate(copy);
        }
    }

    [Test]
    public void EmptyInitialLayout_ClearsExistingEnemyItems()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Backpacks/EnemyBackpack.prefab");
        GameObject instance = UnityEngine.Object.Instantiate(prefab);

        try
        {
            InvokeAwake(instance.GetComponent<BackpackCombatController>());
            InvokeAwake(instance.GetComponent<BackpackFighterSpawner>());
            EnemyBackpackSystem system =
                instance.GetComponent<EnemyBackpackSystem>();
            InvokeAwake(system);

            Assert.That(system.ApplyData(system.DefaultData), Is.True);
            Assert.That(system.Items, Is.Not.Empty);

            Assert.That(system.LoadLayout(Array.Empty<BackpackLayoutItem>()),
                Is.True);
            Assert.That(system.Items, Is.Empty);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void CombatControllerStart_DoesNotRestoreDefaults_WhenReadyPlayerSystemOwnsBackpack()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Backpacks/PlayerBackpackSystem.prefab");
        GameObject instance = UnityEngine.Object.Instantiate(prefab);

        try
        {
            BackpackCombatController controller =
                instance.GetComponent<BackpackCombatController>();
            InvokeAwake(controller);
            InvokeAwake(instance.GetComponent<BackpackFighterSpawner>());
            PlayerBackpackSystem system =
                instance.GetComponent<PlayerBackpackSystem>();
            InvokeAwake(system);

            Assert.That(system.IsReady, Is.True);
            Assert.That(controller.Items, Is.Empty);

            InvokeStart(controller);

            Assert.That(controller.Items, Is.Empty);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void FormalEnemyOperation_EmptyShopWithRoll_RefreshesShopAndConsumesBothAllowances()
    {
        GameObject instance = CreateReadyEnemyWithEmptyShop(
            out EnemyBackpackSystem system);

        try
        {
            RunOneFormalEnemyOperation(system);

            Assert.That(system.ShopItems, Has.Count.EqualTo(3));
            Assert.That(system.RemainingShopRolls, Is.EqualTo(0));
            Assert.That(system.RemainingOperations,
                Is.EqualTo(system.MaximumOperations - 1));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void FormalEnemyOperation_NonEmptyShopWithoutDirectGain_ExploresRoll()
    {
        GameObject instance = CreateReadyEnemyWithEmptyShop(
            out EnemyBackpackSystem system);

        try
        {
            ((List<ItemData>)GetPrivateField(system, "shopItems")).Add(null);
            RunOneFormalEnemyOperation(system);

            Assert.That(system.ShopItems, Has.Count.EqualTo(3));
            Assert.That(system.RemainingShopRolls, Is.EqualTo(0));
            Assert.That(system.RemainingOperations,
                Is.EqualTo(system.MaximumOperations - 1));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void FormalEnemyOperation_EmptyShopWithoutRoll_DoesNotConsumeOperationAllowance()
    {
        GameObject instance = CreateReadyEnemyWithEmptyShop(
            out EnemyBackpackSystem system);

        try
        {
            SetPrivateField(system, "remainingShopRolls", 0);
            RunOneFormalEnemyOperation(system);

            Assert.That(system.ShopItems, Is.Empty);
            Assert.That(system.RemainingOperations,
                Is.EqualTo(system.MaximumOperations));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ExploreMode_EmptyShopWithRoll_StillSelectsRoll()
    {
        BackpackController backpack = new(7, 4);

        Assert.That(BackpackOperationPlanner.TrySelectBest(
            backpack, Array.Empty<ItemData>(), 1,
            BackpackOperationSelectionMode.ExploreNonDecreasing,
            new HashSet<string>(), out BackpackOperation operation), Is.True);
        Assert.That(operation.Kind, Is.EqualTo(BackpackOperationKind.RollShop));
    }

    private static GameObject CreateReadyEnemyWithEmptyShop(
        out EnemyBackpackSystem system)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Backpacks/EnemyBackpack.prefab");
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        InvokeAwake(instance.GetComponent<BackpackCombatController>());
        InvokeAwake(instance.GetComponent<BackpackFighterSpawner>());
        system = instance.GetComponent<EnemyBackpackSystem>();
        InvokeAwake(system);
        Assert.That(system.SetCurrentDeckPreset(system.DefaultDeckPreset),
            Is.True);
        ((List<ItemData>)GetPrivateField(system, "shopItems")).Clear();
        return instance;
    }

    private static void RunOneFormalEnemyOperation(EnemyBackpackSystem system)
    {
        IEnumerator operation = (IEnumerator)system.GetType().GetMethod(
            "RunOperation", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(system, null);
        Assert.That(operation.MoveNext(), Is.True);
    }

    private static object GetPrivateField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return field.GetValue(target);
    }

    private static void SetPrivateField(object target, string name,
        object value)
    {
        FieldInfo field = target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static void InvokeAwake(object target)
    {
        MethodInfo awake = target.GetType().GetMethod("Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(awake, Is.Not.Null);
        awake.Invoke(target, null);
    }

    private static void InvokeStart(object target)
    {
        MethodInfo start = target.GetType().GetMethod("Start",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(start, Is.Not.Null);
        start.Invoke(target, null);
    }
}
