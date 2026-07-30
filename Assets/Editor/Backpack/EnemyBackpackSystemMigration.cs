using System;
using System.Linq;
using BackpackPrototype;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BackpackHero.EditorTools
{
    public static class EnemyBackpackSystemMigration
    {
        private const string PlayerSystemPrefabPath =
            "Assets/Prefabs/Backpacks/PlayerBackpackSystem.prefab";
        private const string EnemyPrefabPath =
            "Assets/Prefabs/Backpacks/EnemyBackpack.prefab";
        private const string EnemyUiPrefabPath =
            "Assets/Prefabs/BackpackUI/EnemyBackpackUIAnimated.prefab";
        private const string EnemyDataFolder =
            "Assets/Data/Backpack/EnemyBackpacks";
        private const string DefaultEnemyDataPath =
            EnemyDataFolder + "/EnemyBackpack_Default.asset";
        private const string MainScenePath =
            "Assets/Scenes/SampleScene.unity";

        [MenuItem(
            "Tools/Backpack/Build Complete Enemy Backpack")]
        public static void BuildFromMenu()
        {
            BuildAll();
            Debug.Log(
                "EnemyBackpackData、敌人动画UI、敌人Prefab和" +
                "SampleScene接入已完成。");
        }

        public static void BuildAll()
        {
            EnsureFolder(EnemyDataFolder);

            EnemyBackpackData defaultData =
                BuildDefaultData();
            GameObject enemyUi =
                BuildEnemyUiPrefab();
            BuildEnemyBackpackPrefab(
                defaultData,
                enemyUi);
            IntegrateMainScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static EnemyBackpackData BuildDefaultData()
        {
            EnemyBackpackData data =
                AssetDatabase.LoadAssetAtPath<
                    EnemyBackpackData>(
                    DefaultEnemyDataPath);

            if (data == null)
            {
                data =
                    ScriptableObject.CreateInstance<
                        EnemyBackpackData>();
                AssetDatabase.CreateAsset(
                    data,
                    DefaultEnemyDataPath);
            }

            ItemData aircraft =
                AssetDatabase.LoadAssetAtPath<ItemData>(
                    "Assets/Data/Backpack/Items/" +
                    "Aircraft_First.asset");
            ItemData equipment =
                AssetDatabase.LoadAssetAtPath<ItemData>(
                    "Assets/Data/Backpack/Items/" +
                    "Equipment_First.asset");

            data.InitializeForTests(
                new[]
                {
                    new BackpackLayoutEntry(
                        aircraft,
                        new Vector2Int(1, 1)),
                    new BackpackLayoutEntry(
                        equipment,
                        new Vector2Int(1, 0)),
                });

            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject BuildEnemyUiPrefab()
        {
            GameObject playerSystem =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PlayerSystemPrefabPath);

            if (playerSystem == null)
            {
                throw new InvalidOperationException(
                    "找不到PlayerBackpackSystem Prefab。");
            }

            GameObject playerUi =
                FindTransform(
                    playerSystem,
                    "PlayerBackpackUI")?.gameObject;

            if (playerUi == null)
            {
                throw new InvalidOperationException(
                    "玩家完整Prefab中缺少PlayerBackpackUI。");
            }

            GameObject instance =
                UnityEngine.Object.Instantiate(playerUi);

            try
            {
                instance.name =
                    "EnemyBackpackUIAnimated";

                DisableNamedObject(instance, "ShopPanel");
                DisableNamedObject(
                    instance,
                    "BottomButtonRow");
                DisableNamedObject(instance, "TrashZone");
                DisableNamedObject(
                    instance,
                    "BattlePhaseLabel");

                PreparationActionsUI actions =
                    instance.GetComponentInChildren<
                        PreparationActionsUI>(true);
                if (actions != null)
                {
                    actions.enabled = false;
                }

                Transform board =
                    FindTransform(
                        instance,
                        "PlayerBackpack");
                if (board == null)
                {
                    throw new InvalidOperationException(
                        "玩家UI中缺少PlayerBackpack。");
                }

                RemoveDuplicateBackpackBoards(
                    instance,
                    board);
                board.name = "EnemyBackpack";

                RectTransform scaleRoot =
                    FindTransform(
                            instance,
                            "PlayerBackpackScaleRoot")
                        as RectTransform;
                if (scaleRoot != null)
                {
                    scaleRoot.name =
                        "EnemyBackpackScaleRoot";
                    Vector2 position =
                        scaleRoot.anchoredPosition;
                    position.y = -position.y;
                    scaleRoot.anchoredPosition = position;
                }

                RectTransform spawnAnchor =
                    FindTransform(
                            instance,
                            "AircraftSpawnAnchor")
                        as RectTransform;
                if (spawnAnchor != null)
                {
                    Vector2 position =
                        spawnAnchor.anchoredPosition;
                    position.y =
                        -Mathf.Abs(position.y);
                    spawnAnchor.anchoredPosition =
                        position;
                }

                foreach (PreparationActionsPhaseMotion motion
                         in instance.GetComponentsInChildren<
                             PreparationActionsPhaseMotion>(
                             true))
                {
                    SerializedObject serialized =
                        new SerializedObject(motion);
                    serialized.FindProperty(
                            "completesCombatTransition")
                        .boolValue = false;
                    serialized
                        .ApplyModifiedPropertiesWithoutUndo();
                }

                foreach (MMF_Player feedbacks in
                         instance.GetComponentsInChildren<
                             MMF_Player>(true))
                {
                    foreach (MMF_Feedback feedback in
                             feedbacks.FeedbacksList)
                    {
                        if (feedback is not
                            MMF_Position position)
                        {
                            continue;
                        }

                        position.InitialPosition =
                            MirrorY(
                                position.InitialPosition);
                        position.DestinationPosition =
                            MirrorY(
                                position
                                    .DestinationPosition);
                    }

                    EditorUtility.SetDirty(feedbacks);
                }

                return PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    EnemyUiPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    instance);
            }
        }

        private static void RemoveDuplicateBackpackBoards(
            GameObject root,
            Transform retainedBoard)
        {
            for (int index =
                     root.transform.childCount - 1;
                 index >= 0;
                 index--)
            {
                Transform candidate =
                    root.transform.GetChild(index);
                if (candidate == retainedBoard ||
                    retainedBoard.IsChildOf(candidate) ||
                    !candidate.name.StartsWith(
                        "PlayerBackpack",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(
                    candidate.gameObject);
            }
        }

        private static void BuildEnemyBackpackPrefab(
            EnemyBackpackData defaultData,
            GameObject enemyUiPrefab)
        {
            GameObject root =
                PrefabUtility.LoadPrefabContents(
                    EnemyPrefabPath);

            try
            {
                Transform oldCanvas =
                    root.transform.Find(
                        "Enemy Backpack Canvas");
                if (oldCanvas != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        oldCanvas.gameObject);
                }

                GameObject canvasObject =
                    new GameObject(
                        "Enemy Backpack Canvas",
                        typeof(RectTransform),
                        typeof(Canvas),
                        typeof(CanvasScaler));
                canvasObject.transform.SetParent(
                    root.transform,
                    false);

                Canvas canvas =
                    canvasObject.GetComponent<Canvas>();
                canvas.renderMode =
                    RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 19;

                CanvasScaler scaler =
                    canvasObject.GetComponent<
                        CanvasScaler>();
                scaler.uiScaleMode =
                    CanvasScaler.ScaleMode
                        .ScaleWithScreenSize;
                scaler.referenceResolution =
                    new Vector2(1080f, 1920f);
                scaler.screenMatchMode =
                    CanvasScaler.ScreenMatchMode
                        .MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                GameObject uiRoot =
                    (GameObject)PrefabUtility
                        .InstantiatePrefab(
                            enemyUiPrefab,
                            canvasObject.transform);
                uiRoot.name = "EnemyBackpackUI";

                RectTransform uiRect =
                    uiRoot.GetComponent<RectTransform>();
                uiRect.anchorMin = Vector2.zero;
                uiRect.anchorMax = Vector2.one;
                uiRect.offsetMin = Vector2.zero;
                uiRect.offsetMax = Vector2.zero;

                EnemyBackpackSystem system =
                    root.GetComponent<
                        EnemyBackpackSystem>() ??
                    root.AddComponent<
                        EnemyBackpackSystem>();

                ConfigureEnemySystem(
                    system,
                    uiRoot,
                    defaultData);

                BackpackCombatController combat =
                    root.GetComponent<
                        BackpackCombatController>();
                SerializedObject combatData =
                    new SerializedObject(combat);
                combatData.FindProperty(
                        "defaultPlacements")
                    .arraySize = 0;
                combatData
                    .ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureEnemySystem(
            EnemyBackpackSystem system,
            GameObject uiRoot,
            EnemyBackpackData defaultData)
        {
            BackpackGridView grid =
                uiRoot.GetComponentInChildren<
                    BackpackGridView>(true);
            RectTransform itemLayer =
                FindTransform(
                        uiRoot,
                        "ItemLayer")
                    as RectTransform;
            RectTransform spawnAnchor =
                FindTransform(
                        uiRoot,
                        "AircraftSpawnAnchor")
                    as RectTransform;
            RectTransform centerAnchor =
                FindTransform(
                        uiRoot,
                        "CollisionCenterAnchor")
                    as RectTransform;

            PlayerBackpackSystem playerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                        PlayerSystemPrefabPath)
                    .GetComponent<PlayerBackpackSystem>();
            SerializedObject playerData =
                new SerializedObject(playerPrefab);
            SerializedProperty playerCatalog =
                playerData.FindProperty("itemCatalog");

            SerializedObject enemyData =
                new SerializedObject(system);
            enemyData.FindProperty("defaultData")
                .objectReferenceValue = defaultData;
            enemyData.FindProperty("gridView")
                .objectReferenceValue = grid;
            enemyData.FindProperty("itemLayer")
                .objectReferenceValue = itemLayer;
            enemyData.FindProperty(
                    "aircraftSpawnAnchor")
                .objectReferenceValue = spawnAnchor;
            enemyData.FindProperty(
                    "collisionCenterAnchor")
                .objectReferenceValue = centerAnchor;

            SerializedProperty enemyCatalog =
                enemyData.FindProperty("itemCatalog");
            enemyCatalog.arraySize =
                playerCatalog.arraySize;

            for (int index = 0;
                 index < playerCatalog.arraySize;
                 index++)
            {
                SerializedProperty source =
                    playerCatalog.GetArrayElementAtIndex(
                        index);
                SerializedProperty destination =
                    enemyCatalog.GetArrayElementAtIndex(
                        index);
                destination.FindPropertyRelative("data")
                    .objectReferenceValue =
                    source.FindPropertyRelative("data")
                        .objectReferenceValue;
                destination.FindPropertyRelative(
                        "prefab")
                    .objectReferenceValue =
                    source.FindPropertyRelative(
                            "prefab")
                        .objectReferenceValue;
            }

            enemyData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void IntegrateMainScene()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    MainScenePath,
                    OpenSceneMode.Single);
            EnemyBackpackSystem enemy =
                UnityEngine.Object.FindAnyObjectByType<
                    EnemyBackpackSystem>(
                    FindObjectsInactive.Include);

            if (enemy == null)
            {
                throw new InvalidOperationException(
                    "SampleScene中没有EnemyBackpackSystem。");
            }

            if (FindSceneTransform("Fighters") == null)
            {
                throw new InvalidOperationException(
                    "SampleScene中缺少Fighters容器。");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Vector3 MirrorY(Vector3 value)
        {
            value.y = -value.y;
            return value;
        }

        private static void DisableNamedObject(
            GameObject root,
            string objectName)
        {
            foreach (Transform target in
                     root.GetComponentsInChildren<
                         Transform>(true)
                         .Where(
                             item =>
                                 item.name ==
                                 objectName))
            {
                target.gameObject.SetActive(false);
            }
        }

        private static Transform FindTransform(
            GameObject root,
            string objectName)
        {
            return root
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(
                    item => item.name == objectName);
        }

        private static Transform FindSceneTransform(
            string objectName)
        {
            return UnityEngine.Object
                .FindObjectsByType<Transform>(
                    FindObjectsInactive.Include)
                .FirstOrDefault(
                    item => item.name == objectName);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int index = 1;
                 index < parts.Length;
                 index++)
            {
                string next =
                    $"{current}/{parts[index]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        parts[index]);
                }

                current = next;
            }
        }
    }
}
