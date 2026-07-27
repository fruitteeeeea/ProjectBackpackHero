using BackpackHero.Battle;
using BackpackPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototypeEditor
{
    public static class BackpackBattleFlowSceneSetup
    {
        private const string ScenePath =
            "Assets/Scenes/BackpackDebugScene.unity";

        [MenuItem(
            "Tools/Backpack/Setup Battle Flow Debug Scene")]
        public static void Setup()
        {
            var scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            BackpackDebugRuntime runtime =
                Object.FindAnyObjectByType<
                    BackpackDebugRuntime>();

            Canvas canvas =
                Object.FindAnyObjectByType<Canvas>();

            BackpackGridView gridView =
                Object.FindAnyObjectByType<BackpackGridView>();

            if (runtime == null ||
                canvas == null ||
                gridView == null)
            {
                Debug.LogError(
                    "BackpackDebugScene缺少Runtime或Canvas。");
                return;
            }

            BattleFlowController flow =
                Object.FindAnyObjectByType<
                    BattleFlowController>();

            if (flow == null)
            {
                GameObject flowObject =
                    new GameObject("BattleFlowController");
                flow = flowObject.AddComponent<
                    BattleFlowController>();
            }

            Text phaseLabel =
                GetOrCreatePhaseLabel(
                    canvas.transform,
                    gridView.transform);

            Transform spawnPoint =
                GetOrCreateSpawnPoint();

            SerializedObject runtimeObject =
                new SerializedObject(runtime);

            runtimeObject.FindProperty("phaseLabel")
                .objectReferenceValue = phaseLabel;
            

            runtimeObject.FindProperty(
                    "playerFighterSpawnPoint")
                .objectReferenceValue = spawnPoint;

            runtimeObject.FindProperty("fighterPrefab")
                .objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/Prefabs/Battle/Fighter.prefab");

            runtimeObject.ApplyModifiedPropertiesWithoutUndo();

            ConfigureItemDefaults();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "BackpackDebugScene战斗流程配置完成。");
        }

        private static Text GetOrCreatePhaseLabel(
            Transform canvas,
            Transform backpackGrid)
        {
            Transform existing =
                canvas.Find("BattlePhaseLabel");

            GameObject labelObject =
                existing != null
                    ? existing.gameObject
                    : new GameObject(
                        "BattlePhaseLabel",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Text));

            labelObject.transform.SetParent(
                backpackGrid != null
                    ? backpackGrid
                    : canvas,
                false);

            RectTransform rect =
                labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, 48f);
            rect.sizeDelta = new Vector2(420f, 52f);

            Text label = labelObject.GetComponent<Text>();
            label.text = "当前阶段：准备阶段";
            label.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
            label.fontSize = 30;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.raycastTarget = false;

            return label;
        }

        private static Transform GetOrCreateSpawnPoint()
        {
            GameObject point =
                FindSceneObject("PlayerFighterSpawnPoint");

            if (point == null)
            {
                point =
                    new GameObject(
                        "PlayerFighterSpawnPoint");
            }

            point.transform.position =
                new Vector3(-3.5f, -3f, 0f);
            point.transform.rotation =
                Quaternion.identity;

            SpriteRenderer renderer =
                point.GetComponent<SpriteRenderer>();

            if (renderer == null)
            {
                renderer =
                    point.AddComponent<SpriteRenderer>();
            }

            FighterDefinition definition =
                AssetDatabase.LoadAssetAtPath<
                    FighterDefinition>(
                    "Assets/Settings/Battle/Fighters/" +
                    "Fighter_01_Normal.asset");

            renderer.sprite =
                definition != null
                    ? definition.Sprite
                    : null;
            renderer.color =
                new Color(0.45f, 0.85f, 1f, 0.55f);
            renderer.sortingOrder = -1;
            point.transform.localScale =
                Vector3.one * 0.45f;

            return point.transform;
        }

        private static void ConfigureItemDefaults()
        {
            FighterDefinition fighter =
                AssetDatabase.LoadAssetAtPath<
                    FighterDefinition>(
                    "Assets/Settings/Battle/Fighters/" +
                    "Fighter_01_Normal.asset");

            GameObject equipmentEffect =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/VFX/Particles/" +
                    "VFX_Particles_Orange.prefab");

            string[] itemGuids =
                AssetDatabase.FindAssets(
                    "t:ItemData",
                    new[]
                    {
                        "Assets/Data/Backpack/Items"
                    });

            foreach (string guid in itemGuids)
            {
                ItemData data =
                    AssetDatabase.LoadAssetAtPath<ItemData>(
                        AssetDatabase.GUIDToAssetPath(guid));

                if (data == null)
                {
                    continue;
                }

                SerializedObject serialized =
                    new SerializedObject(data);

                if (data.ItemType == ItemType.Aircraft)
                {
                    serialized.FindProperty(
                            "fighterDefinition")
                        .objectReferenceValue = fighter;
                }
                else
                {
                    serialized.FindProperty(
                            "equipmentEffectPrefab")
                        .objectReferenceValue =
                            equipmentEffect;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
            }
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
