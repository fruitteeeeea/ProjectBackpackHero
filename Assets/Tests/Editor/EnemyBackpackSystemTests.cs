using System.Reflection;
using BackpackHero.Battle;
using BackpackPrototype;
using MoreMountains.Feedbacks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class EnemyBackpackSystemTests
{
    private const string EnemyPrefabPath =
        "Assets/Prefabs/Backpacks/EnemyBackpack.prefab";
    private const string PlayerPrefabPath =
        "Assets/Prefabs/Backpacks/PlayerBackpackSystem.prefab";
    private const string EnemyUiPath =
        "Assets/Prefabs/BackpackUI/" +
        "EnemyBackpackUIAnimated.prefab";
    private const string DefaultDataPath =
        "Assets/Data/Backpack/EnemyBackpacks/" +
        "EnemyBackpack_Default.asset";

    [Test]
    public void DefaultData_MatchesPlayerDefaultLayout()
    {
        EnemyBackpackData data =
            AssetDatabase.LoadAssetAtPath<
                EnemyBackpackData>(DefaultDataPath);
        ItemData shieldAircraft =
            AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Data/Backpack/Items/" +
                "Aircraft_Shield.asset");
        ItemData equipment =
            AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Data/Backpack/Items/" +
                "Equipment_First.asset");
        ItemData aircraft =
            AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Data/Backpack/Items/" +
                "Aircraft_First.asset");
        ItemData sineEquipment =
            AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Data/Backpack/Items/" +
                "Equipment_WaveEmitter.asset");
        ItemData chargeAircraft =
            AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Data/Backpack/Items/" +
                "Aircraft_Charge.asset");

        Assert.That(data, Is.Not.Null);
        Assert.That(data.Placements.Count, Is.EqualTo(5));
        Assert.That(
            data.Placements[0].Data,
            Is.SameAs(shieldAircraft));
        Assert.That(
            data.Placements[0].AnchorCell,
            Is.EqualTo(Vector2Int.zero));
        Assert.That(
            data.Placements[1].Data,
            Is.SameAs(equipment));
        Assert.That(
            data.Placements[1].AnchorCell,
            Is.EqualTo(new Vector2Int(1, 0)));
        Assert.That(
            data.Placements[2].Data,
            Is.SameAs(aircraft));
        Assert.That(
            data.Placements[2].AnchorCell,
            Is.EqualTo(new Vector2Int(1, 1)));
        Assert.That(
            data.Placements[3].Data,
            Is.SameAs(sineEquipment));
        Assert.That(
            data.Placements[3].AnchorCell,
            Is.EqualTo(new Vector2Int(3, 1)));
        Assert.That(
            data.Placements[4].Data,
            Is.SameAs(chargeAircraft));
        Assert.That(
            data.Placements[4].AnchorCell,
            Is.EqualTo(new Vector2Int(4, 1)));
    }

    [Test]
    public void EnemyPrefab_HasCompleteReadOnlySystem()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyPrefabPath);
        EnemyBackpackSystem system =
            prefab.GetComponent<EnemyBackpackSystem>();

        Assert.That(system, Is.Not.Null);
        Assert.That(
            prefab.GetComponent<BackpackCombatController>(),
            Is.Not.Null);
        Assert.That(
            prefab.GetComponent<BackpackFighterSpawner>(),
            Is.Not.Null);
            Assert.That(
                prefab.GetComponentInChildren<
                    BackpackGridView>(true),
                Is.Not.Null);
            Assert.That(
                prefab.GetComponentsInChildren<
                    BackpackGridView>(true).Length,
                Is.EqualTo(1),
                "敌人UI只能包含一个背包网格。");
        Assert.That(
            system.DefaultData,
            Is.Not.Null);
        Assert.That(
            system.AircraftSpawnAnchor,
            Is.Not.Null);
        Assert.That(
            system.CollisionCenterAnchor,
            Is.Not.Null);
        Assert.That(
            system.AircraftSpawnAnchor
                .anchoredPosition.y,
            Is.LessThan(0f));
    }

    [Test]
    public void EnemyPrefab_UsesTheFiveSharedDeckPresets()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        EnemyBackpackSystem system = prefab.GetComponent<EnemyBackpackSystem>();

        Assert.That(system, Is.Not.Null);
        Assert.That(system.PresetPool.Count, Is.EqualTo(5));
        foreach (DeckPreset preset in system.PresetPool)
        {
            Assert.That(preset, Is.Not.Null);
            Assert.That(preset.IsValid(out _), Is.True);
        }
    }

    [Test]
    public void EnemyLayoutPlanner_BuildsAValidCenteredLayout()
    {
        ItemData aircraft = AssetDatabase.LoadAssetAtPath<ItemData>(
            "Assets/Data/Backpack/Items/Aircraft_First.asset");
        ItemData equipment = AssetDatabase.LoadAssetAtPath<ItemData>(
            "Assets/Data/Backpack/Items/Equipment_First.asset");

        Assert.That(EnemyBackpackLayoutPlanner.TryBuild(
            new[] { aircraft, equipment }, 7, 4, out var layout), Is.True);
        Assert.That(layout, Has.Count.EqualTo(2));

        BackpackController validation = new(7, 4);
        foreach (BackpackLayoutItem placement in layout)
        {
            Assert.That(validation.PlaceItem(
                new ItemInstance("test", placement.Data, placement.AnchorCell),
                placement.AnchorCell), Is.True);
        }

        foreach (BackpackLayoutItem placement in layout)
        {
            Assert.That(placement.AnchorCell.x, Is.InRange(1, 5));
        }
    }

    [Test]
    public void EnemyUi_HidesInteractionAndMirrorsMotion()
    {
        GameObject enemyUi =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyUiPath);
        GameObject player =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PlayerPrefabPath);

        Assert.That(
            Find(enemyUi, "ShopPanel").activeSelf,
            Is.False);
        Assert.That(
            Find(enemyUi, "BottomButtonRow").activeSelf,
            Is.False);
        Assert.That(
            Find(enemyUi, "TrashZone").activeSelf,
            Is.False);
        Assert.That(
            Find(enemyUi, "EnemyBackpack")
                .transform.localScale.y,
            Is.GreaterThan(0f));

        PreparationActionsPhaseMotion enemyMotion =
            enemyUi.GetComponentInChildren<
                PreparationActionsPhaseMotion>(true);
        PreparationActionsPhaseMotion playerMotion =
            player.GetComponentInChildren<
                PreparationActionsPhaseMotion>(true);
        SerializedObject enemyMotionData =
            new SerializedObject(enemyMotion);
        SerializedObject playerMotionData =
            new SerializedObject(playerMotion);

        Assert.That(
            enemyMotionData.FindProperty(
                    "completesCombatTransition")
                .boolValue,
            Is.False);
        Assert.That(
            playerMotionData.FindProperty(
                    "completesCombatTransition")
                .boolValue,
            Is.True);

        MMF_Position enemyExit =
            FindLargestVerticalPosition(
                enemyUi,
                true);
        MMF_Position playerExit =
            FindLargestVerticalPosition(
                player,
                true);

        Assert.That(enemyExit, Is.Not.Null);
        Assert.That(playerExit, Is.Not.Null);
        Assert.That(
            enemyExit.DestinationPosition.y,
            Is.EqualTo(
                    -playerExit.DestinationPosition.y)
                .Within(0.01f));
    }

    [Test]
    public void ApplyData_IsRepeatableAndAtomic()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyPrefabPath);
        GameObject instance =
            Object.Instantiate(prefab);
        EnemyBackpackData valid =
            ScriptableObject.CreateInstance<
                EnemyBackpackData>();
        EnemyBackpackData invalid =
            ScriptableObject.CreateInstance<
                EnemyBackpackData>();

        try
        {
            Initialize(instance);
            EnemyBackpackSystem system =
                instance.GetComponent<
                    EnemyBackpackSystem>();
            ItemData aircraft =
                AssetDatabase.LoadAssetAtPath<ItemData>(
                    "Assets/Data/Backpack/Items/" +
                    "Aircraft_First.asset");
            ItemData equipment =
                AssetDatabase.LoadAssetAtPath<ItemData>(
                    "Assets/Data/Backpack/Items/" +
                    "Equipment_First.asset");

            valid.InitializeForTests(
                new[]
                {
                    new BackpackLayoutEntry(
                        aircraft,
                        new Vector2Int(2, 1)),
                    new BackpackLayoutEntry(
                        equipment,
                        new Vector2Int(2, 0)),
                });

            Assert.That(
                system.ApplyData(valid),
                Is.True);
            Assert.That(system.Items.Count, Is.EqualTo(2));
            Assert.That(
                system.ApplyData(valid),
                Is.True);
            Assert.That(system.Items.Count, Is.EqualTo(2));
            Assert.That(
                system.CurrentData,
                Is.SameAs(valid));

            invalid.InitializeForTests(
                new[]
                {
                    new BackpackLayoutEntry(
                        equipment,
                        Vector2Int.zero),
                    new BackpackLayoutEntry(
                        equipment,
                        Vector2Int.zero),
                });
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    "无法放置"));

            Assert.That(
                system.ApplyData(invalid),
                Is.False);
            Assert.That(system.Items.Count, Is.EqualTo(2));
            Assert.That(
                system.CurrentData,
                Is.SameAs(valid));
        }
        finally
        {
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(valid);
            Object.DestroyImmediate(invalid);
        }
    }

    [Test]
    public void ApplyData_RejectsCombatPhase()
    {
        ResetBattleFlowStatics();
        GameObject flowObject =
            new GameObject("Enemy Data Flow Test");
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyPrefabPath);
        GameObject instance =
            Object.Instantiate(prefab);

        try
        {
            Initialize(instance);
            BattleFlowController flow =
                flowObject.AddComponent<
                    BattleFlowController>();
            SerializedObject flowData =
                new SerializedObject(flow);
            flowData.FindProperty(
                    "persistBetweenScenes")
                .boolValue = false;
            flowData
                .ApplyModifiedPropertiesWithoutUndo();
            Invoke(flow, "Awake");
            flow.SetPhase(BattlePhase.Combat);

            EnemyBackpackSystem system =
                instance.GetComponent<
                    EnemyBackpackSystem>();

            Assert.That(
                system.ApplyData(system.DefaultData),
                Is.False);
            Assert.That(system.Items.Count, Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(flowObject);
            ResetBattleFlowStatics();
        }
    }

    [Test]
    public void ApplyData_RejectsInvalidSourcesWithoutMutation()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyPrefabPath);
        GameObject instance =
            Object.Instantiate(prefab);
        GameObject missingCatalogInstance =
            Object.Instantiate(prefab);
        EnemyBackpackData empty =
            ScriptableObject.CreateInstance<
                EnemyBackpackData>();
        EnemyBackpackData outOfBounds =
            ScriptableObject.CreateInstance<
                EnemyBackpackData>();
        EnemyBackpackData missingCatalog =
            ScriptableObject.CreateInstance<
                EnemyBackpackData>();

        try
        {
            Initialize(instance);
            EnemyBackpackSystem system =
                instance.GetComponent<
                    EnemyBackpackSystem>();
            Assert.That(
                system.ApplyData(system.DefaultData),
                Is.True);
            EnemyBackpackData baseline =
                system.CurrentData;

            empty.InitializeForTests(
                System.Array.Empty<
                    BackpackLayoutEntry>());
            Assert.That(
                system.ApplyData(empty),
                Is.False);
            Assert.That(
                system.CurrentData,
                Is.SameAs(baseline));

            ItemData aircraft =
                AssetDatabase.LoadAssetAtPath<ItemData>(
                    "Assets/Data/Backpack/Items/" +
                    "Aircraft_First.asset");
            ItemData equipment =
                AssetDatabase.LoadAssetAtPath<ItemData>(
                    "Assets/Data/Backpack/Items/" +
                    "Equipment_First.asset");
            outOfBounds.InitializeForTests(
                new[]
                {
                    new BackpackLayoutEntry(
                        aircraft,
                        new Vector2Int(6, 3)),
                });
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    "无法放置"));
            Assert.That(
                system.ApplyData(outOfBounds),
                Is.False);
            Assert.That(system.Items.Count, Is.EqualTo(2));

            EnemyBackpackSystem missingSystem =
                missingCatalogInstance.GetComponent<
                    EnemyBackpackSystem>();
            SerializedObject missingSerialized =
                new SerializedObject(missingSystem);
            missingSerialized.FindProperty("itemViewPrefab")
                .objectReferenceValue = null;
            missingSerialized
                .ApplyModifiedPropertiesWithoutUndo();
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    "EnemyBackpackSystem"));
            Initialize(missingCatalogInstance);

            missingCatalog.InitializeForTests(
                new[]
                {
                    new BackpackLayoutEntry(
                        equipment,
                        Vector2Int.zero),
                });
            Assert.That(
                missingSystem.ApplyData(missingCatalog),
                Is.False);
            Assert.That(
                missingSystem.Items.Count,
                Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(
                missingCatalogInstance);
            Object.DestroyImmediate(empty);
            Object.DestroyImmediate(outOfBounds);
            Object.DestroyImmediate(missingCatalog);
        }
    }

    private static void Initialize(GameObject instance)
    {
        Invoke(
            instance.GetComponent<
                BackpackCombatController>(),
            "Awake");
        Invoke(
            instance.GetComponent<
                BackpackFighterSpawner>(),
            "Awake");
        Invoke(
            instance.GetComponent<
                EnemyBackpackSystem>(),
            "Awake");
    }

    private static void Invoke(
        object target,
        string method)
    {
        target.GetType()
            .GetMethod(
                method,
                BindingFlags.Instance |
                BindingFlags.NonPublic)
            ?.Invoke(target, null);
    }

    private static GameObject Find(
        GameObject root,
        string name)
    {
        foreach (Transform child in
                 root.GetComponentsInChildren<
                     Transform>(true))
        {
            if (child.name == name)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static MMF_Position
        FindLargestVerticalPosition(
            GameObject root,
            bool destination)
    {
        MMF_Position result = null;
        float largest = 0f;

        foreach (MMF_Player player in
                 root.GetComponentsInChildren<
                     MMF_Player>(true))
        {
            foreach (MMF_Feedback feedback in
                     player.FeedbacksList)
            {
                if (feedback is not
                    MMF_Position position)
                {
                    continue;
                }

                float value =
                    destination
                        ? Mathf.Abs(
                            position
                                .DestinationPosition.y)
                        : Mathf.Abs(
                            position
                                .InitialPosition.y);

                if (value > largest)
                {
                    result = position;
                    largest = value;
                }
            }
        }

        return result;
    }

    private static void ResetBattleFlowStatics()
    {
        typeof(BattleFlowController)
            .GetMethod(
                "ResetStaticState",
                BindingFlags.Static |
                BindingFlags.NonPublic)
            ?.Invoke(null, null);
    }
}
