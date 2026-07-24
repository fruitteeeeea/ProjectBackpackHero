using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    public sealed class BattleDebugWindow : EditorWindow
    {
        private int selectedBackpackIndex;
        private ItemData modifierItem;
        private Vector2Int modifierCell;
        private Vector2 scrollPosition;
        private double nextRepaintTime;

        [MenuItem("Tools/Battle/Battle Debug")]
        public static void ShowFromMenu()
        {
            OpenWindow();
        }

        internal static BattleDebugWindow OpenWindow()
        {
            BattleDebugWindow window =
                GetWindow<BattleDebugWindow>();
            window.titleContent =
                new GUIContent("Battle Debug");
            window.minSize =
                new Vector2(520f, 620f);
            window.Show();
            return window;
        }

        internal static void CloseAllWindows()
        {
            foreach (BattleDebugWindow window in
                     Resources.FindObjectsOfTypeAll<
                         BattleDebugWindow>())
            {
                window.Close();
            }
        }

        private void Update()
        {
            if (EditorApplication.timeSinceStartup <
                nextRepaintTime)
            {
                return;
            }

            nextRepaintTime =
                EditorApplication.timeSinceStartup + 0.1;
            Repaint();
        }

        private void OnGUI()
        {
            scrollPosition =
                EditorGUILayout.BeginScrollView(
                    scrollPosition);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "进入Play Mode后连接场景中的" +
                    "BackpackCombatController。",
                    MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            BackpackCombatController[] controllers =
                FindObjectsByType<
                    BackpackCombatController>(
                    FindObjectsInactive.Exclude);

            if (controllers.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "没有找到正式背包战斗组件。",
                    MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            selectedBackpackIndex =
                Mathf.Clamp(
                    selectedBackpackIndex,
                    0,
                    controllers.Length - 1);

            string[] names =
                new string[controllers.Length];

            for (int index = 0;
                 index < controllers.Length;
                 index++)
            {
                names[index] =
                    $"{controllers[index].name} " +
                    $"[{controllers[index].Faction}]";
            }

            selectedBackpackIndex =
                EditorGUILayout.Popup(
                    "Target Backpack",
                    selectedBackpackIndex,
                    names);

            BackpackCombatController controller =
                controllers[selectedBackpackIndex];

            DrawStatus(controller);
            DrawActions(controller);
            DrawItems(controller);
            DrawModifier(controller);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawStatus(
            BackpackCombatController controller)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Runtime Status",
                EditorStyles.boldLabel);

            BackpackFighterSpawner spawner =
                controller.FighterSpawner;

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Faction",
                    controller.Faction.ToString());
                EditorGUILayout.LabelField(
                    "Phase",
                    BattleFlowController.CurrentPhase
                        .ToString());
                EditorGUILayout.LabelField(
                    "Items",
                    controller.Items.Count.ToString());
                EditorGUILayout.LabelField(
                    "Pending Spawns",
                    spawner != null
                        ? spawner.PendingCount.ToString()
                        : "-");
                EditorGUILayout.LabelField(
                    "Spawn Interval",
                    spawner != null
                        ? $"{spawner.SpawnInterval:0.00}s"
                        : "-");
                EditorGUILayout.LabelField(
                    "Last Spawn Time",
                    spawner != null &&
                    !float.IsNegativeInfinity(
                        spawner.LastSpawnTime)
                        ? $"{spawner.LastSpawnTime:0.00}s"
                        : "-");
            }
        }

        private static void DrawActions(
            BackpackCombatController controller)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Actions",
                EditorStyles.boldLabel);

            if (GUILayout.Button(
                    "切换阶段",
                    GUILayout.Height(30f)))
            {
                BattleFlowController.Instance
                    ?.TogglePhase();
            }

            if (GUILayout.Button(
                    "恢复当前背包默认布局",
                    GUILayout.Height(30f)))
            {
                controller.RestoreDefaultLayout();
            }
        }

        private static void DrawItems(
            BackpackCombatController controller)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Backpack Items",
                EditorStyles.boldLabel);

            ItemInstance removeTarget = null;

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                foreach (ItemInstance item
                         in controller.Items)
                {
                    using (new EditorGUILayout
                               .HorizontalScope())
                    {
                        EditorGUILayout.LabelField(
                            $"{item.Data.ItemName} " +
                            $"({item.AnchorCell.x}," +
                            $"{item.AnchorCell.y})",
                            GUILayout.Width(190f));

                        Rect progressRect =
                            GUILayoutUtility.GetRect(
                                100f,
                                20f,
                                GUILayout.ExpandWidth(true));

                        EditorGUI.ProgressBar(
                            progressRect,
                            item.CooldownProgress,
                            item.Data.CanEnterCooldown
                                ? $"{item.RemainingCooldown:0.0}s"
                                : "No Cooldown");

                        if (GUILayout.Button(
                                "Delete",
                                GUILayout.Width(65f)))
                        {
                            removeTarget = item;
                        }
                    }
                }
            }

            if (removeTarget != null)
            {
                controller.RemoveItem(removeTarget);
            }
        }

        private void DrawModifier(
            BackpackCombatController controller)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Backpack Modifier",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                modifierItem =
                    (ItemData)EditorGUILayout.ObjectField(
                        "Item Data",
                        modifierItem,
                        typeof(ItemData),
                        false);
                modifierCell =
                    EditorGUILayout.Vector2IntField(
                        "Anchor Cell",
                        modifierCell);

                using (new EditorGUI.DisabledScope(
                           modifierItem == null))
                {
                    if (GUILayout.Button(
                            "Add Item",
                            GUILayout.Height(30f)))
                    {
                        controller.AddItem(
                            modifierItem,
                            modifierCell);
                    }
                }
            }
        }
    }
}
