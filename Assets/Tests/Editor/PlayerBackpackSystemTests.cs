using BackpackHero.Battle;
using BackpackPrototype;
using MoreMountains.Feedbacks;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PlayerBackpackSystemTests
{
    private const string PrefabPath =
        "Assets/Prefabs/Backpacks/" +
        "PlayerBackpackSystem.prefab";
    private const string MainScenePath =
        "Assets/Scenes/SampleScene.unity";

    [Test]
    public void CompletePrefab_HasRequiredRuntimeAndUiReferences()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);

        Assert.That(prefab, Is.Not.Null);

        PlayerBackpackSystem system =
            prefab.GetComponent<PlayerBackpackSystem>();

        Assert.That(system, Is.Not.Null);
        Assert.That(
            prefab.GetComponent<BackpackCombatController>(),
            Is.Not.Null);
        Assert.That(
            prefab.GetComponent<BackpackFighterSpawner>(),
            Is.Not.Null);
        Assert.That(
            prefab.GetComponentInChildren<Canvas>(true),
            Is.Not.Null);
        Assert.That(
            prefab.GetComponentInChildren<
                BackpackGridView>(true),
            Is.Not.Null);

        PreparationActionsPhaseMotion phaseMotion =
            prefab.GetComponentInChildren<
                PreparationActionsPhaseMotion>(true);
        Assert.That(phaseMotion, Is.Not.Null);

        SerializedObject phaseMotionData =
            new SerializedObject(phaseMotion);
        MMF_Player showFeedbacks =
            phaseMotionData.FindProperty(
                    "showFeedbacks")
                .objectReferenceValue as MMF_Player;
        MMF_Player hideFeedbacks =
            phaseMotionData.FindProperty(
                    "hideFeedbacks")
                .objectReferenceValue as MMF_Player;

        Assert.That(showFeedbacks, Is.Not.Null);
        Assert.That(hideFeedbacks, Is.Not.Null);
        Assert.That(
            showFeedbacks.FeedbacksList.Count,
            Is.GreaterThanOrEqualTo(3));
        Assert.That(
            hideFeedbacks.FeedbacksList.Count,
            Is.GreaterThanOrEqualTo(3));

        SerializedObject serialized =
            new SerializedObject(system);

        Assert.That(
            serialized.FindProperty("gridView")
                .objectReferenceValue,
            Is.Not.Null);
        Assert.That(
            serialized.FindProperty("itemLayer")
                .objectReferenceValue,
            Is.Not.Null);
        Assert.That(
            serialized.FindProperty("dragLayer")
                .objectReferenceValue,
            Is.Not.Null);
        Assert.That(
            serialized.FindProperty("trashZone")
                .objectReferenceValue,
            Is.Not.Null);
        Assert.That(
            serialized.FindProperty("shopSlots")
                .arraySize,
            Is.EqualTo(3));
        Assert.That(
            serialized.FindProperty("itemCatalog")
                .arraySize,
            Is.EqualTo(4));
    }

    [Test]
    public void FighterSpawner_ClampsInjectedCurveValue()
    {
        GameObject root =
            new GameObject("Spawner Test");

        try
        {
            root.AddComponent<FactionMember>();
            BackpackFighterSpawner spawner =
                root.AddComponent<
                    BackpackFighterSpawner>();

            spawner.SetCurveValue(3f);
            Assert.That(
                spawner.CurrentCurveValue,
                Is.EqualTo(1f));

            spawner.SetCurveValue(-4f);
            Assert.That(
                spawner.CurrentCurveValue,
                Is.EqualTo(-1f));

            spawner.SetCurveValue(0.35f);
            Assert.That(
                spawner.CurrentCurveValue,
                Is.EqualTo(0.35f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void LoadLayout_RejectsOverlapWithoutChangingCurrentLayout()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);
        GameObject instance =
            Object.Instantiate(prefab);

        try
        {
            PlayerBackpackSystem system =
                instance.GetComponent<
                    PlayerBackpackSystem>();

            InvokeAwake(
                instance.GetComponent<
                    BackpackCombatController>());
            InvokeAwake(
                instance.GetComponent<
                    BackpackFighterSpawner>());
            InvokeAwake(system);

            ItemData aircraft =
                AssetDatabase.LoadAssetAtPath<ItemData>(
                    "Assets/Data/Backpack/Items/" +
                    "Aircraft_First.asset");
            ItemData equipment =
                AssetDatabase.LoadAssetAtPath<ItemData>(
                    "Assets/Data/Backpack/Items/" +
                    "Equipment_First.asset");

            Assert.That(
                system.LoadLayout(
                    new[]
                    {
                        new BackpackLayoutItem(
                            aircraft,
                            new Vector2Int(1, 1)),
                        new BackpackLayoutItem(
                            equipment,
                            new Vector2Int(1, 0)),
                    }),
                Is.True);
            Assert.That(system.Items.Count, Is.EqualTo(2));

            LogAssert.Expect(
                LogType.Error,
                "布局条目 1 无法放置在 (0, 0)。");

            Assert.That(
                system.LoadLayout(
                    new[]
                    {
                        new BackpackLayoutItem(
                            equipment,
                            new Vector2Int(0, 0)),
                        new BackpackLayoutItem(
                            equipment,
                            new Vector2Int(0, 0)),
                    }),
                Is.False);
            Assert.That(system.Items.Count, Is.EqualTo(2));
            Assert.That(
                system.Items[0].Data,
                Is.SameAs(aircraft));
            Assert.That(
                system.Items[1].Data,
                Is.SameAs(equipment));
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void InvokeAwake(object target)
    {
        target.GetType()
            .GetMethod(
                "Awake",
                BindingFlags.Instance |
                BindingFlags.NonPublic)
            ?.Invoke(target, null);
    }

    [Test]
    public void CompletePrefab_UsesFormalActionsBinding()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);

        PreparationActionsUI actions =
            prefab.GetComponentInChildren<
                PreparationActionsUI>(true);

        Assert.That(actions, Is.Not.Null);

        SerializedObject serialized =
            new SerializedObject(actions);

        Assert.That(
            serialized.FindProperty(
                    "playerBackpackSystem")
                .objectReferenceValue,
            Is.Not.Null);
    }

    [Test]
    public void MainScene_UsesSystemPrefabAndBindsCurveInput()
    {
        string sceneText =
            File.ReadAllText(MainScenePath);
        string prefabGuid =
            AssetDatabase.AssetPathToGUID(
                PrefabPath);

        Assert.That(
            sceneText,
            Does.Contain($"guid: {prefabGuid}"));
        Assert.That(
            sceneText,
            Does.Contain("propertyPath: curveInput"));
        Assert.That(
            sceneText,
            Does.Contain(
                "value: PlayerBackpackSystem"));

        const string debugPath =
            "Assets/Scripts/Battle/Debug/" +
            "BattleDebugRuntime.cs";
        string debugGuid =
            AssetDatabase.AssetPathToGUID(debugPath);

        Assert.That(
            sceneText,
            Does.Not.Contain($"guid: {debugGuid}"));
    }

    [Test]
    public void DebugBridge_DescribesAircraftCooldownAndEveryAdjacentBuff()
    {
        ItemShapeData shape =
            ScriptableObject.CreateInstance<
                ItemShapeData>();
        ItemData aircraftData =
            ScriptableObject.CreateInstance<ItemData>();
        ItemData equipmentData =
            ScriptableObject.CreateInstance<ItemData>();

        try
        {
            shape.InitializeForTests(
                "One Cell",
                null,
                new[] { Vector2Int.zero });
            aircraftData.InitializeForTests(
                "Debug Aircraft",
                ItemType.Aircraft,
                4f,
                shape);
            equipmentData.InitializeForTests(
                "Debug Equipment",
                ItemType.Equipment,
                -1f,
                shape);

            var backpack = new BackpackController(4, 4);
            var aircraft =
                new ItemInstance(
                    "aircraft",
                    aircraftData,
                    new Vector2Int(1, 1));
            var leftEquipment =
                new ItemInstance(
                    "left",
                    equipmentData,
                    new Vector2Int(0, 1));
            var rightEquipment =
                new ItemInstance(
                    "right",
                    equipmentData,
                    new Vector2Int(2, 1));

            Assert.That(
                backpack.PlaceItem(
                    aircraft,
                    aircraft.AnchorCell),
                Is.True);
            Assert.That(
                backpack.PlaceItem(
                    leftEquipment,
                    leftEquipment.AnchorCell),
                Is.True);
            Assert.That(
                backpack.PlaceItem(
                    rightEquipment,
                    rightEquipment.AnchorCell),
                Is.True);

            aircraft.BeginCooldown();

            string description =
                PlayerBackpackDebugBridge
                    .BuildItemDescription(
                        backpack,
                        aircraft);

            Assert.That(
                description,
                Does.Contain("Debug Aircraft"));
            Assert.That(
                description,
                Does.Contain("冷却：4.00s"));
            Assert.That(
                description,
                Does.Contain("临近增益：2"));
            Assert.That(
                CountOccurrences(
                    description,
                    "Debug Equipment"),
                Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(shape);
            Object.DestroyImmediate(aircraftData);
            Object.DestroyImmediate(equipmentData);
        }
    }

    [Test]
    public void MainScene_ContainsPlayerBackpackDebugBridge()
    {
        string sceneText =
            File.ReadAllText(MainScenePath);
        string bridgeGuid =
            AssetDatabase.AssetPathToGUID(
                "Assets/Scripts/BackpackPrototype/" +
                "PlayerBackpackDebugBridge.cs");

        Assert.That(
            sceneText,
            Does.Contain($"guid: {bridgeGuid}"));
        Assert.That(
            sceneText,
            Does.Contain(
                "m_Name: PlayerBackpackDebugBridge"));
    }

    [Test]
    public void DebugWindow_IsEditorOnlyAndHasMenuEntry()
    {
        const string windowPath =
            "Assets/Editor/Backpack/" +
            "PlayerBackpackDebugWindow.cs";
        string windowSource =
            File.ReadAllText(windowPath);

        Assert.That(
            windowSource,
            Does.Contain(
                "class PlayerBackpackDebugWindow"));
        Assert.That(
            windowSource,
            Does.Contain(
                "Tools/Backpack/Player Backpack Debug"));
        Assert.That(
            File.ReadAllText(
                "Assets/Scripts/BackpackPrototype/" +
                "PlayerBackpackDebugBridge.cs"),
            Does.Not.Contain("using UnityEditor"));
    }

    private static int CountOccurrences(
        string source,
        string value)
    {
        int count = 0;
        int index = 0;

        while ((index =
                   source.IndexOf(
                       value,
                       index,
                       System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
