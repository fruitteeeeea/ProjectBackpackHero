using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    internal sealed class BattleDebugWindow : ScriptableObject
    {
        private int selectedBackpackIndex;
        private ItemData modifierItem;
        private Vector2Int modifierCell;
        private Vector2 scrollPosition;
        internal void DrawTab()
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

            BattleDebugRuntime debugRuntime =
                BattleDebugRuntime.Instance;

            if (debugRuntime != null)
            {
                DrawDebugRuntimeVariation(
                    debugRuntime);
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
            DrawFighterVariation(controller);
            DrawItems(controller);
            DrawModifier(controller);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawDebugRuntimeVariation(
            BattleDebugRuntime runtime)
        {
            EditorGUILayout.LabelField(
                "调试场景飞机随机偏移",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                DrawRandomProjectileToggle();

                Vector2 directionRange =
                    EditorGUILayout.Vector2Field(
                        "方向角范围（度）",
                        runtime.SpawnDirectionOffsetRange);

                Vector2 attackRange =
                    EditorGUILayout.Vector2Field(
                        "索敌距离偏移范围",
                        runtime.AttackRangeOffsetRange);

                if (directionRange !=
                    runtime.SpawnDirectionOffsetRange)
                {
                    runtime
                        .SetSpawnDirectionOffsetRange(
                            directionRange);
                }

                if (attackRange !=
                    runtime.AttackRangeOffsetRange)
                {
                    runtime
                        .SetAttackRangeOffsetRange(
                            attackRange);
                }
            }
        }

        private static void DrawRandomProjectileToggle()
        {
            BattleRandomProjectilePool pool =
                BattleRandomProjectilePool.Instance;

            if (pool == null)
            {
                EditorGUILayout.HelpBox(
                    "场景中没有随机子弹池。",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(
                $"随机子弹池：{pool.ProjectilePrefabCount} 种");

            string label = pool.RandomProjectilesEnabled
                ? "随机子弹：开启"
                : "随机子弹：关闭";

            if (GUILayout.Button(label,
                    GUILayout.Height(28f)))
            {
                pool.SetRandomProjectilesEnabled(
                    !pool.RandomProjectilesEnabled);
            }
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

            PlayerBackpackSystem player =
                Object.FindAnyObjectByType<
                    PlayerBackpackSystem>(
                    FindObjectsInactive.Include);
            EnemyBackpackSystem enemy =
                Object.FindAnyObjectByType<
                    EnemyBackpackSystem>(
                    FindObjectsInactive.Include);

            using (new EditorGUI.DisabledScope(
                       BattleFlowController.CurrentPhase !=
                       BattlePhase.Preparation ||
                       player == null ||
                       !player.IsReady ||
                       enemy == null ||
                       !enemy.IsReady))
            {
                if (GUILayout.Button(
                        "敌人复制当前玩家背包",
                        GUILayout.Height(30f)))
                {
                    enemy.CopyLayoutFrom(
                        player.Backpack);
                }
            }
        }

        private static void DrawFighterVariation(
            BackpackCombatController controller)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "飞机随机偏移",
                EditorStyles.boldLabel);

            BackpackFighterSpawner spawner =
                controller.FighterSpawner;

            if (spawner == null)
            {
                EditorGUILayout.HelpBox(
                    "当前背包没有BackpackFighterSpawner。",
                    MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                Vector2 directionRange =
                    EditorGUILayout.Vector2Field(
                        "方向角范围（度）",
                        spawner
                            .SpawnDirectionOffsetRange);

                Vector2 attackRange =
                    EditorGUILayout.Vector2Field(
                        "索敌距离偏移范围",
                        spawner
                            .AttackRangeOffsetRange);

                if (directionRange !=
                    spawner.SpawnDirectionOffsetRange)
                {
                    spawner
                        .SetSpawnDirectionOffsetRange(
                            directionRange);
                }

                if (attackRange !=
                    spawner.AttackRangeOffsetRange)
                {
                    spawner
                        .SetAttackRangeOffsetRange(
                            attackRange);
                }

                EditorGUILayout.HelpBox(
                    "每架新飞机会分别从两个范围内抽样。" +
                    "已生成飞机保留生成时的偏移值。",
                    MessageType.Info);
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
