using BackpackPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BackpackPrototypeEditor
{
    public static class BackpackCombatMigration
    {
        private const string BasePrefabPath =
            "Assets/Prefabs/Backpacks/" +
            "BattleBackpackBase.prefab";
        private const string EnemyPrefabPath =
            "Assets/Prefabs/Backpacks/" +
            "EnemyBackpack.prefab";
        private const string FighterPrefabPath =
            "Assets/Prefabs/Battle/Fighter.prefab";
        private const string AircraftPath =
            "Assets/Data/Backpack/Items/" +
            "Aircraft_First.asset";
        private const string EquipmentPath =
            "Assets/Data/Backpack/Items/" +
            "Equipment_First.asset";
        private const string DebugScenePath =
            "Assets/Scenes/BackpackDebugScene.unity";

        [MenuItem(
            "Tools/Backpack/Migrate Unified Combat Backpacks")]
        public static void Migrate()
        {
            ConfigureBasePrefab();
            ConfigureDebugScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("统一背包战斗组件迁移完成。");
        }

        private static void ConfigureBasePrefab()
        {
            GameObject root =
                PrefabUtility.LoadPrefabContents(
                    BasePrefabPath);

            try
            {
                BackpackFighterSpawner spawner =
                    root.GetComponent<
                        BackpackFighterSpawner>() ??
                    root.AddComponent<
                        BackpackFighterSpawner>();

                BackpackCombatController controller =
                    root.GetComponent<
                        BackpackCombatController>() ??
                    root.AddComponent<
                        BackpackCombatController>();

                Transform spawnPoint =
                    root.transform.Find(
                        "FighterSpawnPoint");

                if (spawnPoint == null)
                {
                    spawnPoint =
                        new GameObject(
                            "FighterSpawnPoint")
                            .transform;
                    spawnPoint.SetParent(
                        root.transform,
                        false);
                }

                SerializedObject spawnerObject =
                    new SerializedObject(spawner);
                spawnerObject.FindProperty(
                        "fighterPrefab")
                    .objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<
                            GameObject>(
                            FighterPrefabPath);
                spawnerObject.FindProperty(
                        "fighterSpawnPoint")
                    .objectReferenceValue =
                        spawnPoint;
                spawnerObject.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject controllerObject =
                    new SerializedObject(controller);
                controllerObject.FindProperty("width")
                    .intValue = 7;
                controllerObject.FindProperty("height")
                    .intValue = 4;

                SerializedProperty placements =
                    controllerObject.FindProperty(
                        "defaultPlacements");
                placements.arraySize = 2;
                SetPlacement(
                    placements.GetArrayElementAtIndex(0),
                    AssetDatabase.LoadAssetAtPath<
                        ItemData>(AircraftPath),
                    new Vector2Int(1, 1));
                SetPlacement(
                    placements.GetArrayElementAtIndex(1),
                    AssetDatabase.LoadAssetAtPath<
                        ItemData>(EquipmentPath),
                    new Vector2Int(1, 0));
                controllerObject
                    .ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    BasePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureDebugScene()
        {
            var scene =
                EditorSceneManager.OpenScene(
                    DebugScenePath,
                    OpenSceneMode.Single);

            BackpackDebugRuntime runtime =
                Object.FindAnyObjectByType<
                    BackpackDebugRuntime>();

            if (runtime == null)
            {
                Debug.LogError(
                    "BackpackDebugScene缺少BackpackDebugRuntime。");
                return;
            }

            GameObject runtimeObject =
                runtime.gameObject;

            if (runtimeObject.GetComponent<
                    BackpackHero.Battle.FactionMember>() ==
                null)
            {
                runtimeObject.AddComponent<
                    BackpackHero.Battle.FactionMember>();
            }

            BackpackFighterSpawner spawner =
                runtimeObject.GetComponent<
                    BackpackFighterSpawner>() ??
                runtimeObject.AddComponent<
                    BackpackFighterSpawner>();

            BackpackCombatController controller =
                runtimeObject.GetComponent<
                    BackpackCombatController>() ??
                runtimeObject.AddComponent<
                    BackpackCombatController>();

            SerializedObject runtimeSerialized =
                new SerializedObject(runtime);
            runtimeSerialized.FindProperty(
                    "combatController")
                .objectReferenceValue = controller;
            runtimeSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject fighterPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    FighterPrefabPath);

            SerializedObject spawnerSerialized =
                new SerializedObject(spawner);
            spawnerSerialized.FindProperty(
                    "fighterPrefab")
                .objectReferenceValue = fighterPrefab;
            spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();

            if (FindSceneObject(
                    "EnemyCombatBackpack") == null)
            {
                GameObject enemyPrefab =
                    AssetDatabase.LoadAssetAtPath<
                        GameObject>(EnemyPrefabPath);

                GameObject enemy =
                    (GameObject)PrefabUtility
                        .InstantiatePrefab(
                            enemyPrefab,
                            scene);
                enemy.name = "EnemyCombatBackpack";
                enemy.transform.position =
                    new Vector3(3.5f, 4f, 0f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetPlacement(
            SerializedProperty placement,
            ItemData data,
            Vector2Int cell)
        {
            placement.FindPropertyRelative("data")
                .objectReferenceValue = data;
            placement.FindPropertyRelative(
                    "anchorCell")
                .vector2IntValue = cell;
        }

        private static GameObject FindSceneObject(
            string objectName)
        {
            foreach (Transform transform in
                     Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include))
            {
                if (transform.name == objectName)
                {
                    return transform.gameObject;
                }
            }

            return null;
        }
    }
}
