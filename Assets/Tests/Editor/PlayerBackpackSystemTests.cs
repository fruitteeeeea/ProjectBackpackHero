using BackpackHero.Battle;
using BackpackPrototype;
using MoreMountains.Feedbacks;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;

public sealed class PlayerBackpackSystemTests
{
    private const string PrefabPath =
        "Assets/Prefabs/Backpacks/" +
        "PlayerBackpackSystem.prefab";
    private const string MainScenePath =
        "Assets/Scenes/SampleScene.unity";

    [Test]
    public void ItemView_OnlySelectsFromDragInput()
    {
        Assert.That(
            typeof(IBeginDragHandler).IsAssignableFrom(
                typeof(ItemView)),
            Is.True);
        Assert.That(
            typeof(IPointerClickHandler).IsAssignableFrom(
                typeof(ItemView)),
            Is.False);
    }

    [Test]
    public void ShopRoll_UsesTwoAircraftOneEquipmentCompositionAndNeverReturnsThreeOfOneItem()
    {
        ItemShapeData shape = ScriptableObject.CreateInstance<ItemShapeData>();
        ItemData aircraft = ScriptableObject.CreateInstance<ItemData>();
        ItemData equipmentOne = ScriptableObject.CreateInstance<ItemData>();
        ItemData equipmentTwo = ScriptableObject.CreateInstance<ItemData>();

        try
        {
            shape.InitializeForTests("One Cell", null, new[] { Vector2Int.zero });
            aircraft.InitializeForTests("Aircraft", ItemType.Aircraft, 1f, shape);
            equipmentOne.InitializeForTests("Equipment One", ItemType.Equipment, 1f, shape);
            equipmentTwo.InitializeForTests("Equipment Two", ItemType.Equipment, 1f, shape);
            var deck = new List<ItemData>
            {
                aircraft,
                equipmentOne,
                equipmentTwo,
            };
            MethodInfo builder = typeof(PlayerBackpackSystem).GetMethod(
                "BuildShopRoll",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(builder, Is.Not.Null);

            for (int seed = 0; seed < 100; seed++)
            {
                Random.InitState(seed);
                bool expectsPreferredComposition = Random.value < .6f;
                Random.InitState(seed);
                var result = (List<ItemData>)builder.Invoke(
                    null,
                    new object[]
                    {
                        deck,
                        new List<ItemData>(),
                        new Dictionary<string, int>(),
                    });

                Assert.That(result, Has.Count.EqualTo(3));
                Assert.That(CountItem(result, aircraft), Is.LessThan(3));
                Assert.That(CountItem(result, equipmentOne), Is.LessThan(3));
                Assert.That(CountItem(result, equipmentTwo), Is.LessThan(3));

                if (expectsPreferredComposition)
                {
                    Assert.That(
                        CountItemsOfType(result, ItemType.Aircraft),
                        Is.EqualTo(2));
                    Assert.That(
                        CountItemsOfType(result, ItemType.Equipment),
                        Is.EqualTo(1));
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(aircraft);
            Object.DestroyImmediate(equipmentOne);
            Object.DestroyImmediate(equipmentTwo);
            Object.DestroyImmediate(shape);
        }
    }

    [Test]
    public void ShopRoll_LimitsAircraftAppearancesAcrossPreparationButNotEquipment()
    {
        ItemShapeData shape = ScriptableObject.CreateInstance<ItemShapeData>();
        ItemData aircraft = ScriptableObject.CreateInstance<ItemData>();
        ItemData equipment = ScriptableObject.CreateInstance<ItemData>();

        try
        {
            shape.InitializeForTests("One Cell", null, new[] { Vector2Int.zero });
            aircraft.InitializeForTests("Aircraft", ItemType.Aircraft, 1f, shape);
            aircraft.ConfigurePlayerProgressForTests("aircraft");
            equipment.InitializeForTests("Equipment", ItemType.Equipment, 1f, shape);
            equipment.ConfigurePlayerProgressForTests("equipment");

            var deck = new List<ItemData> { aircraft, equipment };
            var appearances = new Dictionary<string, int>();
            MethodInfo builder = typeof(PlayerBackpackSystem).GetMethod(
                "BuildShopRoll",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo recorder = typeof(PlayerBackpackSystem).GetMethod(
                "RecordAircraftAppearances",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(builder, Is.Not.Null);
            Assert.That(recorder, Is.Not.Null);

            int preferredSeed = FindPreferredShopCompositionSeed();
            Random.InitState(preferredSeed);
            var initialShop = (List<ItemData>)builder.Invoke(
                null,
                new object[] { deck, new List<ItemData>(), appearances });
            recorder.Invoke(null, new object[] { initialShop, appearances });

            Assert.That(CountItem(initialShop, aircraft), Is.EqualTo(2));
            Assert.That(appearances[aircraft.ItemId], Is.EqualTo(2));

            Random.InitState(preferredSeed);
            var rolledShop = (List<ItemData>)builder.Invoke(
                null,
                new object[] { deck, initialShop, appearances });

            Assert.That(CountItem(rolledShop, aircraft), Is.Zero);
            Assert.That(CountItem(rolledShop, equipment), Is.EqualTo(2));

            appearances.Clear();
            Random.InitState(preferredSeed);
            var nextPreparationShop = (List<ItemData>)builder.Invoke(
                null,
                new object[] { deck, rolledShop, appearances });

            Assert.That(CountItem(nextPreparationShop, aircraft), Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(aircraft);
            Object.DestroyImmediate(equipment);
            Object.DestroyImmediate(shape);
        }
    }

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
            Is.GreaterThan(0));
        Assert.That(
            serialized.FindProperty("itemViewPrefab")
                .objectReferenceValue,
            Is.Not.Null);
        Assert.That(
            serialized.FindProperty("rollsPerPreparation")
                .intValue,
            Is.EqualTo(3));

        RectTransform aircraftAnchor =
            FindRectTransform(
                prefab,
                "AircraftSpawnAnchor");
        RectTransform collisionAnchor =
            FindRectTransform(
                prefab,
                "CollisionCenterAnchor");

        Assert.That(aircraftAnchor, Is.Not.Null);
        Assert.That(collisionAnchor, Is.Not.Null);
        Assert.That(
            aircraftAnchor.parent.name,
            Is.EqualTo("PlayerBackpack"));
        Assert.That(
            collisionAnchor.parent,
            Is.SameAs(aircraftAnchor.parent));

        ItemInfoPanel infoPanel =
            prefab.GetComponentInChildren<ItemInfoPanel>(true);
        Assert.That(infoPanel, Is.Not.Null);

        SerializedObject infoPanelData =
            new SerializedObject(infoPanel);
        Assert.That(
            infoPanelData.FindProperty("playerBackpackSystem")
                .objectReferenceValue,
            Is.SameAs(system));
        Assert.That(
            infoPanelData.FindProperty("panelVisual")
                .objectReferenceValue,
            Is.Not.Null);
        Assert.That(
            infoPanelData.FindProperty("itemNameLabel")
                .objectReferenceValue,
            Is.Not.Null);
        Assert.That(
            infoPanelData.FindProperty("levelLabel")
                .objectReferenceValue,
            Is.Not.Null);
        Assert.That(
            infoPanelData.FindProperty("descriptionLabel")
                .objectReferenceValue,
            Is.Not.Null);
    }

    [Test]
    public void ShopPanel_UsesBackpackFrameAndLargeSlots()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);

        RectTransform shopPanel = FindRectTransform(
            prefab,
            "ShopPanel");
        RectTransform backpackBackground = FindRectTransform(
            prefab,
            "BackpackBackground");

        Assert.That(shopPanel, Is.Not.Null);
        Assert.That(backpackBackground, Is.Not.Null);
        Assert.That(
            shopPanel.sizeDelta,
            Is.EqualTo(new Vector2(1000f, 320f)));

        Image shopFrame = shopPanel.GetComponent<Image>();
        Image backpackFrame =
            backpackBackground.GetComponent<Image>();
        Assert.That(shopFrame, Is.Not.Null);
        Assert.That(
            shopFrame.sprite,
            Is.SameAs(backpackFrame.sprite));
        Assert.That(
            shopFrame.color,
            Is.EqualTo(new Color(1f, 202f / 255f,
                40f / 255f, 1f)));

        int shopSlotCount = 0;
        foreach (RectTransform rectTransform in
                 shopPanel.GetComponentsInChildren<RectTransform>(
                     true))
        {
            if (!rectTransform.name.StartsWith("ShopSlot"))
            {
                continue;
            }

            shopSlotCount++;
            Assert.That(
                rectTransform.sizeDelta,
                Is.EqualTo(new Vector2(320f, 320f)));
        }

        Assert.That(shopSlotCount, Is.GreaterThan(0));
    }

    [Test]
    public void ScreenPointMirror_ReflectsAcrossScreenCenter()
    {
        Vector2 mirrored =
            PlayerBackpackSystem
                .MirrorScreenPointVertically(
                    new Vector2(125f, 300f),
                    1920f);

        Assert.That(mirrored.x, Is.EqualTo(125f));
        Assert.That(mirrored.y, Is.EqualTo(1620f));
    }

    [Test]
    public void ScreenProjection_HitsRequestedWorldPlane()
    {
        GameObject cameraObject =
            new GameObject("Projection Camera");
        Camera camera =
            cameraObject.AddComponent<Camera>();

        try
        {
            camera.transform.position =
                new Vector3(0f, 0f, -10f);
            camera.transform.rotation =
                Quaternion.identity;
            camera.orthographic = false;

            bool projected =
                PlayerBackpackSystem
                    .TryScreenPointToWorldOnPlane(
                        camera,
                        new Vector2(
                            camera.pixelWidth * 0.5f,
                            camera.pixelHeight * 0.5f),
                        0f,
                        out Vector3 worldPoint);

            Assert.That(projected, Is.True);
            Assert.That(worldPoint.x, Is.EqualTo(0f)
                .Within(0.001f));
            Assert.That(worldPoint.y, Is.EqualTo(0f)
                .Within(0.001f));
            Assert.That(worldPoint.z, Is.EqualTo(0f)
                .Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void CombatRequest_WaitsForTransitionCompletion()
    {
        ResetBattleFlowStatics();
        GameObject root =
            new GameObject("Battle Flow Test");

        try
        {
            BattleFlowController flow =
                root.AddComponent<BattleFlowController>();
            SerializedObject serialized =
                new SerializedObject(flow);
            serialized.FindProperty("persistBetweenScenes")
                .boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InvokeAwake(flow);

            flow.SetPhase(BattlePhase.Combat);

            Assert.That(
                BattleFlowController.CurrentPhase,
                Is.EqualTo(
                    BattlePhase.CombatTransition));
            Assert.That(
                BattleFlowController.IsCombatPhase,
                Is.False);

            Assert.That(
                flow.CompleteCombatTransition(),
                Is.True);
            Assert.That(
                BattleFlowController.CurrentPhase,
                Is.EqualTo(BattlePhase.Combat));
            Assert.That(
                BattleFlowController.IsCombatPhase,
                Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
            ResetBattleFlowStatics();
        }
    }

    [Test]
    public void FighterSpawner_ClampsInjectedCurveSettings()
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

            spawner.SetMaximumBendDistance(-2f);
            Assert.That(
                spawner.MaximumBendDistance,
                Is.EqualTo(0f));

            spawner.SetMaximumBendDistance(3f);
            Assert.That(
                spawner.MaximumBendDistance,
                Is.EqualTo(3f));

            spawner.SetEnemyCurveValueRange(
                new Vector2(0.65f, -0.4f));
            Assert.That(
                spawner.EnemyCurveValueRange,
                Is.EqualTo(
                    new Vector2(-0.4f, 0.65f)));

            spawner.SetEnemyCurveValueRange(
                new Vector2(-3f, 4f));
            Assert.That(
                spawner.EnemyCurveValueRange,
                Is.EqualTo(new Vector2(-1f, 1f)));

            for (int index = 0; index < 32; index++)
            {
                Assert.That(
                    spawner.SampleEnemyCurveValue(),
                    Is.InRange(-1f, 1f));
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EnemyFighterSpawner_UsesConfiguredRandomCurveRange()
    {
        GameObject root =
            new GameObject("Enemy Spawner Test");

        try
        {
            FactionMember faction =
                root.AddComponent<FactionMember>();
            faction.SetFaction(BattleFaction.Enemy);

            BackpackFighterSpawner spawner =
                root.AddComponent<
                    BackpackFighterSpawner>();
            InvokeAwake(spawner);
            spawner.SetEnemyCurveValueRange(
                new Vector2(0.42f, 0.42f));

            MethodInfo resolver =
                typeof(BackpackFighterSpawner)
                    .GetMethod(
                        "ResolveCurveValue",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

            Assert.That(resolver, Is.Not.Null);
            Assert.That(
                (float)resolver.Invoke(
                    spawner,
                    new object[] { null }),
                Is.EqualTo(0.42f).Within(0.0001f));
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
                "无法应用背包布局：布局条目 1 无法放置。");

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

    [Test]
    public void DefaultLayout_ContainsAllAircraftAndEquipmentWithoutOverlap()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);
        GameObject instance = Object.Instantiate(prefab);

        try
        {
            BackpackCombatController controller =
                instance.GetComponent<BackpackCombatController>();
            InvokeAwake(controller);
            controller.RestoreDefaultLayout();

            Assert.That(controller.Items.Count, Is.EqualTo(5));
            AssertLayoutItem(
                controller.Items[0],
                "Aircraft_Shield.asset",
                Vector2Int.zero);
            AssertLayoutItem(
                controller.Items[1],
                "Equipment_First.asset",
                new Vector2Int(1, 0));
            AssertLayoutItem(
                controller.Items[2],
                "Aircraft_First.asset",
                new Vector2Int(1, 1));
            AssertLayoutItem(
                controller.Items[3],
                "Equipment_WaveEmitter.asset",
                new Vector2Int(3, 1));
            AssertLayoutItem(
                controller.Items[4],
                "Aircraft_Charge.asset",
                new Vector2Int(4, 1));

            Assert.That(
                controller.Backpack.GetAdjacentEquipmentItems(
                    controller.Items[0]).Count,
                Is.EqualTo(1));
            Assert.That(
                controller.Backpack.GetAdjacentEquipmentItems(
                    controller.Items[2]).Count,
                Is.EqualTo(2));
            Assert.That(
                controller.Backpack.GetAdjacentEquipmentItems(
                    controller.Items[4]).Count,
                Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void AssertLayoutItem(
        ItemInstance item,
        string itemAssetName,
        Vector2Int anchorCell)
    {
        ItemData expected =
            AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Data/Backpack/Items/" + itemAssetName);

        Assert.That(item.Data, Is.SameAs(expected));
        Assert.That(item.AnchorCell, Is.EqualTo(anchorCell));
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

    private static void ResetBattleFlowStatics()
    {
        typeof(BattleFlowController)
            .GetMethod(
                "ResetStaticState",
                BindingFlags.Static |
                BindingFlags.NonPublic)
            ?.Invoke(null, null);
    }

    private static RectTransform FindRectTransform(
        GameObject root,
        string objectName)
    {
        foreach (RectTransform rectTransform in
                 root.GetComponentsInChildren<
                     RectTransform>(true))
        {
            if (rectTransform.name == objectName)
            {
                return rectTransform;
            }
        }

        return null;
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
        Assert.That(
            sceneText,
            Does.Contain("maxBendDistance: 3"));

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
    public void DebugBridge_DescribesAircraftAndEveryAdjacentEquipmentColor()
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

            Assert.That(equipmentData.EquipmentColor,
                Is.EqualTo(Color.white));

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
                Does.Contain("冷却：无（由相邻装备触发）"));
            Assert.That(
                description,
                Does.Contain("临近装备颜色：2"));
            Assert.That(
                CountOccurrences(
                    description,
                    "Debug Equipment"),
                Is.EqualTo(2));
            Assert.That(description, Does.Contain("#FFFFFF"));
        }
        finally
        {
            Object.DestroyImmediate(shape);
            Object.DestroyImmediate(aircraftData);
            Object.DestroyImmediate(equipmentData);
        }
    }

    [Test]
    public void EquipmentAssets_UseConfiguredMarkerColors()
    {
        ItemData first =
            AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Data/Backpack/Items/" +
                "Equipment_First.asset");
        ItemData vertical =
            AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Data/Backpack/Items/" +
                "Equipment_1x2.asset");

        Assert.That(first.EquipmentColor,
            Is.EqualTo(new Color32(255, 193, 7, 255)));
        Assert.That(vertical.EquipmentColor,
            Is.EqualTo(new Color32(244, 67, 54, 255)));
    }

    [Test]
    public void EquipmentMarkerDisplay_ShowsAndHidesConfiguredColors()
    {
        var fighter = new GameObject("Fighter");
        var canvasObject = new GameObject(
            "Aircraft Health Bar Canvas",
            typeof(RectTransform),
            typeof(Canvas));
        canvasObject.transform.SetParent(fighter.transform);

        try
        {
            AircraftEquipmentMarkerDisplay display =
                fighter.AddComponent<
                    AircraftEquipmentMarkerDisplay>();
            display.SetColors(new List<Color>
            {
                Color.yellow,
                Color.red,
            });

            Transform container = canvasObject.transform.Find(
                "Equipment Color Markers");
            Assert.That(container, Is.Not.Null);
            Assert.That(container.gameObject.activeSelf, Is.True);
            Assert.That(container.childCount, Is.EqualTo(2));

            display.SetColors(new List<Color>());

            Assert.That(container.gameObject.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(fighter);
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
    public void DebugWindow_IsEditorOnlyAndHostedByDebugCenter()
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
            Does.Contain("敌人背包血量 - 25%"));
        Assert.That(
            windowSource,
            Does.Contain("玩家背包血量 - 25%"));
        Assert.That(
            File.ReadAllText("Assets/Editor/DebugCenterWindow.cs"),
            Does.Contain("Tools/Debug/程序测试面板"));
        Assert.That(
            File.ReadAllText("Assets/Editor/DebugCenterWindow.cs"),
            Does.Contain("typeof(ProgramTestDebugCenterWindow)"));
        Assert.That(
            File.ReadAllText(
                "Assets/Scripts/BackpackPrototype/" +
                "PlayerBackpackDebugBridge.cs"),
            Does.Not.Contain("using UnityEditor"));
        Assert.That(
            File.ReadAllText(
                "Assets/Scripts/BackpackPrototype/" +
                "PlayerBackpackSystem.cs"),
            Does.Contain("adjustmentCurve?.SetCurveValue"));
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

    private static int CountItem(
        IReadOnlyList<ItemData> items,
        ItemData expected)
    {
        int count = 0;
        foreach (ItemData item in items)
        {
            if (item == expected)
            {
                count++;
            }
        }

        return count;
    }

    private static int FindPreferredShopCompositionSeed()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            Random.InitState(seed);
            if (Random.value < .6f)
            {
                return seed;
            }
        }

        Assert.Fail("Could not find a preferred shop composition seed.");
        return 0;
    }

    private static int CountItemsOfType(
        IReadOnlyList<ItemData> items,
        ItemType type)
    {
        int count = 0;
        foreach (ItemData item in items)
        {
            if (item != null && item.ItemType == type)
            {
                count++;
            }
        }

        return count;
    }
}
