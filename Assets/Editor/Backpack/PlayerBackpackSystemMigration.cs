using System;
using System.Collections.Generic;
using System.Linq;
using BackpackHero.Battle;
using BackpackHero.Input;
using BackpackPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BackpackHero.EditorTools
{
    public static class PlayerBackpackSystemMigration
    {
        private const string SourceBackpackPath =
            "Assets/Prefabs/Backpacks/PlayerBackpack.prefab";
        private const string UiPrefabPath =
            "Assets/Prefabs/BackpackUI/" +
            "PlayerBackpackUIAnimated.prefab";
        private const string OutputPrefabPath =
            "Assets/Prefabs/Backpacks/PlayerBackpackSystem.prefab";
        private const string MainScenePath =
            "Assets/Scenes/SampleScene.unity";

        private const string ItemViewPrefabPath =
            "Assets/Prefabs/BackpackUI/Items/Item_1x1.prefab";

        [MenuItem(
            "Tools/Backpack/Build Complete Player Backpack")]
        public static void ExecuteMigration()
        {
            GameObject prefab = BuildPrefab();
            IntegrateMainScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "PlayerBackpackSystem预制体创建并已接入SampleScene。");
        }

        [MenuItem(
            "Tools/Backpack/Install Player Backpack Debug Panel")]
        public static void InstallDebugPanel()
        {
            var scene =
                EditorSceneManager.OpenScene(
                    MainScenePath,
                    OpenSceneMode.Single);
            PlayerBackpackSystem system =
                Object.FindAnyObjectByType<
                    PlayerBackpackSystem>(
                    FindObjectsInactive.Include);

            if (system == null)
            {
                throw new InvalidOperationException(
                    "SampleScene中没有PlayerBackpackSystem。");
            }

            EnsureDebugBridge(scene, system);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "PlayerBackpackDebugBridge已接入SampleScene。");
        }

        private static GameObject BuildPrefab()
        {
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    SourceBackpackPath);
            GameObject uiPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    UiPrefabPath);

            if (source == null || uiPrefab == null)
            {
                throw new InvalidOperationException(
                    "无法加载玩家战斗背包或背包UI Prefab。");
            }

            GameObject root =
                PrefabUtility.LoadPrefabContents(
                    SourceBackpackPath);

            try
            {
                root.name = "PlayerBackpackSystem";

                Transform existingCanvas =
                    root.transform.Find("BackpackCanvas");

                if (existingCanvas != null)
                {
                    Object.DestroyImmediate(
                        existingCanvas.gameObject);
                }

                GameObject canvasObject =
                    new GameObject(
                        "BackpackCanvas",
                        typeof(RectTransform),
                        typeof(Canvas),
                        typeof(CanvasScaler),
                        typeof(GraphicRaycaster));
                canvasObject.layer =
                    LayerMask.NameToLayer("UI");
                canvasObject.transform.SetParent(
                    root.transform,
                    false);

                Canvas canvas =
                    canvasObject.GetComponent<Canvas>();
                canvas.renderMode =
                    RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 20;

                CanvasScaler scaler =
                    canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution =
                    new Vector2(1080f, 1920f);
                scaler.screenMatchMode =
                    CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                GameObject uiRoot =
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        uiPrefab,
                        canvasObject.transform);
                uiRoot.name = "PlayerBackpackUI";

                RectTransform uiRect =
                    uiRoot.GetComponent<RectTransform>();
                uiRect.anchorMin = Vector2.zero;
                uiRect.anchorMax = Vector2.one;
                uiRect.offsetMin = Vector2.zero;
                uiRect.offsetMax = Vector2.zero;

                PlayerBackpackSystem system =
                    root.GetComponent<PlayerBackpackSystem>() ??
                    root.AddComponent<PlayerBackpackSystem>();

                ConfigureSystem(system, uiRoot);

                PreparationActionsUI actions =
                    uiRoot.GetComponentInChildren<
                        PreparationActionsUI>(true);

                if (actions != null)
                {
                    SerializedObject actionsObject =
                        new SerializedObject(actions);
                    actionsObject.FindProperty(
                            "playerBackpackSystem")
                        .objectReferenceValue = system;
                    actionsObject
                        .ApplyModifiedPropertiesWithoutUndo();
                }

                return PrefabUtility.SaveAsPrefabAsset(
                    root,
                    OutputPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureSystem(
            PlayerBackpackSystem system,
            GameObject uiRoot)
        {
            BackpackGridView grid =
                uiRoot.GetComponentInChildren<
                    BackpackGridView>(true);
            RectTransform itemLayer =
                FindRect(uiRoot, "ItemLayer");
            RectTransform dragLayer =
                FindRect(uiRoot, "DragLayer");
            RectTransform trashZone =
                FindRect(uiRoot, "TrashZone");

            List<RectTransform> shopSlots =
                uiRoot.GetComponentsInChildren<
                        RectTransform>(true)
                    .Where(
                        rect =>
                            rect.name == "ShopSlot" ||
                            rect.name.StartsWith(
                                "ShopSlot (",
                                StringComparison.Ordinal))
                    .OrderBy(rect => rect.name)
                    .Take(3)
                    .ToList();

            if (grid == null ||
                itemLayer == null ||
                dragLayer == null ||
                trashZone == null ||
                shopSlots.Count != 3)
            {
                throw new InvalidOperationException(
                    "正式背包UI缺少网格、层级、垃圾区或三个商店槽位。");
            }

            SerializedObject serialized =
                new SerializedObject(system);
            serialized.FindProperty("gridView")
                .objectReferenceValue = grid;
            serialized.FindProperty("itemLayer")
                .objectReferenceValue = itemLayer;
            serialized.FindProperty("dragLayer")
                .objectReferenceValue = dragLayer;
            serialized.FindProperty("trashZone")
                .objectReferenceValue = trashZone;

            SerializedProperty slots =
                serialized.FindProperty("shopSlots");
            slots.arraySize = shopSlots.Count;

            for (int index = 0;
                 index < shopSlots.Count;
                 index++)
            {
                slots.GetArrayElementAtIndex(index)
                    .objectReferenceValue =
                    shopSlots[index];
            }

            serialized.FindProperty("itemViewPrefab")
                .objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ItemView>(
                    ItemViewPrefabPath);
            SetItemViewPrefab(serialized, "itemView1x2Prefab", "Item_1x2.prefab");
            SetItemViewPrefab(serialized, "itemView2x1Prefab", "Item_2x1.prefab");
            SetItemViewPrefab(serialized, "itemViewLMissingBottomLeftPrefab", "Item_L_MissingBottomLeft.prefab");
            SetItemViewPrefab(serialized, "itemViewLMissingBottomRightPrefab", "Item_L_MissingBottomRight.prefab");
            SetItemViewPrefab(serialized, "itemViewLMissingTopLeftPrefab", "Item_L_MissingTopLeft.prefab");
            SetItemViewPrefab(serialized, "itemViewLMissingTopRightPrefab", "Item_L_MissingTopRight.prefab");

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetItemViewPrefab(SerializedObject target, string property, string fileName)
        {
            target.FindProperty(property).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ItemView>(
                    "Assets/Prefabs/BackpackUI/Items/" + fileName);
        }

        private static void IntegrateMainScene(
            GameObject systemPrefab)
        {
            if (systemPrefab == null)
            {
                throw new InvalidOperationException(
                    "PlayerBackpackSystem Prefab创建失败。");
            }

            var scene =
                EditorSceneManager.OpenScene(
                    MainScenePath,
                    OpenSceneMode.Single);

            PlayerBackpackSystem existingSystem =
                Object.FindAnyObjectByType<
                    PlayerBackpackSystem>(
                    FindObjectsInactive.Include);
            BackpackCombatController oldPlayer =
                existingSystem != null
                    ? existingSystem.GetComponent<
                        BackpackCombatController>()
                    : Object.FindObjectsByType<
                        BackpackCombatController>(
                        FindObjectsInactive.Include)
                    .FirstOrDefault(
                        controller =>
                            controller.Faction ==
                            BattleFaction.Player &&
                            controller.GetComponent<
                                PlayerBackpackSystem>() ==
                            null);

            Transform parent =
                oldPlayer != null
                    ? oldPlayer.transform.parent
                    : null;
            Vector3 position =
                oldPlayer != null
                    ? oldPlayer.transform.localPosition
                    : new Vector3(0f, -5.59f, 0f);
            Quaternion rotation =
                oldPlayer != null
                    ? oldPlayer.transform.localRotation
                    : Quaternion.identity;
            Vector3 scale =
                oldPlayer != null
                    ? oldPlayer.transform.localScale
                    : Vector3.one;

            if (oldPlayer != null)
            {
                Object.DestroyImmediate(
                    oldPlayer.gameObject);
            }

            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(
                    systemPrefab,
                    scene);
            instance.name = "PlayerBackpackSystem";
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = scale;

            PlayerBackpackSystem system =
                instance.GetComponent<
                    PlayerBackpackSystem>();
            HorizontalSwipeCurveInput input =
                Object.FindAnyObjectByType<
                    HorizontalSwipeCurveInput>(
                    FindObjectsInactive.Include);

            SerializedObject systemObject =
                new SerializedObject(system);
            systemObject.FindProperty("curveInput")
                .objectReferenceValue = input;
            systemObject.ApplyModifiedPropertiesWithoutUndo();

            Transform fighters =
                FindSceneTransform("Fighters");

            if (fighters != null &&
                instance.TryGetComponent(
                    out BackpackFighterSpawner spawner))
            {
                SerializedObject spawnerObject =
                    new SerializedObject(spawner);
                spawnerObject.FindProperty(
                        "fighterContainer")
                    .objectReferenceValue = fighters;
                spawnerObject
                    .ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (BattleDebugRuntime debugRuntime in
                     Object.FindObjectsByType<
                         BattleDebugRuntime>(
                         FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(
                    debugRuntime.gameObject);
            }

            EnsureDebugBridge(scene, system);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureDebugBridge(
            UnityEngine.SceneManagement.Scene scene,
            PlayerBackpackSystem system)
        {
            PlayerBackpackDebugBridge bridge =
                Object.FindAnyObjectByType<
                    PlayerBackpackDebugBridge>(
                    FindObjectsInactive.Include);

            if (bridge == null)
            {
                Transform legacyPanel =
                    FindSceneTransform(
                        "PlayerBackpackDebugPanel");

                if (legacyPanel != null)
                {
                    Object.DestroyImmediate(
                        legacyPanel.gameObject);
                }

                GameObject bridgeObject =
                    new GameObject(
                        "PlayerBackpackDebugBridge");
                UnityEngine.SceneManagement
                    .SceneManager
                    .MoveGameObjectToScene(
                        bridgeObject,
                        scene);
                bridge =
                    bridgeObject.AddComponent<
                        PlayerBackpackDebugBridge>();
            }

            SerializedObject bridgeObjectData =
                new SerializedObject(bridge);
            bridgeObjectData.FindProperty(
                    "playerBackpackSystem")
                .objectReferenceValue = system;
            bridgeObjectData
                .ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform FindRect(
            GameObject root,
            string objectName)
        {
            return root.GetComponentsInChildren<
                    RectTransform>(true)
                .FirstOrDefault(
                    rect => rect.name == objectName);
        }

        private static Transform FindSceneTransform(
            string objectName)
        {
            return Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include)
                .FirstOrDefault(
                    item => item.name == objectName);
        }
    }
}
