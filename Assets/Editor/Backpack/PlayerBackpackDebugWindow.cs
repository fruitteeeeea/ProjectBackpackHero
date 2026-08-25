using BackpackHero.Battle;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    internal sealed class PlayerBackpackDebugWindow :
        ScriptableObject
    {
        private Vector2 scrollPosition;
        private GUIStyle itemStyle;
        private int selectedTarget;
        private EnemyBackpackData enemyDataToApply;
        internal void DrawTab()
        {
            DrawToolbar();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "尚未进入Play Mode。运行SampleScene后，" +
                    "窗口会在调试桥启动时自动连接。",
                    MessageType.Info);
                return;
            }

            PlayerBackpackDebugBridge bridge =
                PlayerBackpackDebugBridge.Active;

            if (bridge == null)
            {
                EditorGUILayout.HelpBox(
                    "没有活动的PlayerBackpackDebugBridge。" +
                    "场景可能正在切换，或Runtime实例已销毁。",
                    MessageType.Warning);
                return;
            }

            DrawTargetSelector(bridge);

            DrawDragCellVisualizationToggle(bridge);

            DrawRandomProjectileToggle();

            DrawRandomFlightCurveControls(bridge);

            PlayerBackpackDebugSnapshot snapshot =
                selectedTarget == 0
                    ? bridge.Snapshot
                    : bridge.EnemySnapshot;

            if (!snapshot.HasTarget)
            {
                EditorGUILayout.HelpBox(
                    snapshot.StatusMessage,
                    MessageType.Warning);
                return;
            }

            DrawActions(bridge, snapshot);
            DrawSummary(snapshot);
            DrawItems(snapshot);
        }

        private static void DrawRandomProjectileToggle()
        {
            BattleRandomProjectilePool pool =
                BattleRandomProjectilePool.Instance;

            if (pool == null)
            {
                EditorGUILayout.HelpBox(
                    "场景中没有BattleRandomProjectilePool，" +
                    "飞机将使用默认子弹。",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "随机子弹",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "可用子弹",
                    pool.ProjectilePrefabCount.ToString());

                string label = pool.RandomProjectilesEnabled
                    ? "随机子弹：开启"
                    : "随机子弹：关闭";

                if (GUILayout.Button(label,
                        GUILayout.Height(28f)))
                {
                    pool.SetRandomProjectilesEnabled(
                        !pool.RandomProjectilesEnabled);
                }

                if (pool.RandomProjectilesEnabled &&
                    pool.ProjectilePrefabCount == 0)
                {
                    EditorGUILayout.HelpBox(
                        "随机池为空，发射时会回退到默认子弹。",
                        MessageType.Warning);
                }
            }
        }

        private static void DrawRandomFlightCurveControls(
            PlayerBackpackDebugBridge bridge)
        {
            PlayerRandomFlightCurveController controller =
                bridge.GetComponent<PlayerRandomFlightCurveController>();
            if (controller == null)
            {
                EditorGUILayout.HelpBox(
                    "随机航线控制器尚未初始化。",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "随机航线",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "当前模式",
                    GetRandomFlightCurveModeLabel(controller.Mode));

                EditorGUILayout.BeginHorizontal();
                DrawRandomFlightCurveModeButton(
                    controller,
                    RandomFlightCurveMode.Off,
                    "关闭随机航线");
                DrawRandomFlightCurveModeButton(
                    controller,
                    RandomFlightCurveMode.Peaceful,
                    "随机航线 - 平和");
                DrawRandomFlightCurveModeButton(
                    controller,
                    RandomFlightCurveMode.Intense,
                    "随机航线 - 激烈");
                EditorGUILayout.EndHorizontal();

                float speed = EditorGUILayout.Slider(
                    "基础自动调整速度",
                    controller.BaseAdjustmentSpeed,
                    0f,
                    4f);
                if (!Mathf.Approximately(
                        speed,
                        controller.BaseAdjustmentSpeed))
                {
                    controller.BaseAdjustmentSpeed = speed;
                }

                EditorGUILayout.LabelField(
                    "平和实际速度",
                    PlayerRandomFlightCurveController
                        .GetAdjustmentSpeed(
                            RandomFlightCurveMode.Peaceful,
                            controller.BaseAdjustmentSpeed)
                        .ToString("0.##"));
                EditorGUILayout.LabelField(
                    "激烈实际速度",
                    PlayerRandomFlightCurveController
                        .GetAdjustmentSpeed(
                            RandomFlightCurveMode.Intense,
                            controller.BaseAdjustmentSpeed)
                        .ToString("0.##"));
            }
        }

        private static void DrawRandomFlightCurveModeButton(
            PlayerRandomFlightCurveController controller,
            RandomFlightCurveMode mode,
            string label)
        {
            GUIStyle style = controller.Mode == mode
                ? EditorStyles.miniButtonMid
                : EditorStyles.miniButton;
            if (GUILayout.Button(label, style, GUILayout.Height(26f)))
            {
                controller.SetMode(mode);
            }
        }

        private static string GetRandomFlightCurveModeLabel(
            RandomFlightCurveMode mode)
        {
            return mode == RandomFlightCurveMode.Peaceful
                ? "平和"
                : mode == RandomFlightCurveMode.Intense
                    ? "激烈"
                    : "关闭（手动控制）";
        }

        private static void DrawDragCellVisualizationToggle(
            PlayerBackpackDebugBridge bridge)
        {
            bool enabled = EditorGUILayout.Toggle(
                "拖拽格子可视化",
                bridge.DragCellVisualizationEnabled);

            if (enabled != bridge.DragCellVisualizationEnabled)
            {
                bridge.SetDragCellVisualizationEnabled(enabled);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(
                EditorStyles.toolbar);
            GUILayout.Label(
                "Backpack Debug",
                EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                    "刷新",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(52f)))
            {
                PlayerBackpackDebugBridge.Active
                    ?.RefreshSnapshot();
            }

            if (GUILayout.Button(
                    "关闭调试",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(72f)))
            {
                PlayerBackpackDebugBridge.Active
                    ?.SetDebugEnabled(false);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawActions(
            PlayerBackpackDebugBridge bridge,
            PlayerBackpackDebugSnapshot snapshot)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "阶段控制",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("准备阶段"))
            {
                bridge.EnterPreparation();
            }

            if (GUILayout.Button("战斗阶段"))
            {
                bridge.EnterCombat();
            }

            if (GUILayout.Button("切换阶段"))
            {
                bridge.TogglePhase();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(
                       !bridge.CanSwapRuntimeDecks))
            {
                if (GUILayout.Button("交换玩家和敌人背包"))
                {
                    bridge.TrySwapRuntimeDecksAndResetMatch();
                }
            }

            if (!bridge.CanSwapRuntimeDecks)
            {
                EditorGUILayout.HelpBox(
                    "需要双方背包系统就绪，且敌方当前使用有效 DeckPreset 才能交换。",
                    MessageType.Info);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "背包血量",
                EditorStyles.boldLabel);

            bool canLockBackpackHealth =
                bridge.CanLockBackpackHealth;
            bool isBackpackHealthLocked =
                bridge.BackpacksHealthLocked;
            using (new EditorGUI.DisabledScope(
                       !canLockBackpackHealth &&
                       !isBackpackHealthLocked))
            {
                bool requestedLock = EditorGUILayout.Toggle(
                    "锁定双方背包血量",
                    isBackpackHealthLocked);
                if (requestedLock != isBackpackHealthLocked)
                {
                    bridge.SetBackpacksHealthLocked(requestedLock);
                }
            }

            if (!canLockBackpackHealth &&
                !isBackpackHealthLocked)
            {
                EditorGUILayout.HelpBox(
                    "仅可在战斗阶段且双方背包均存活时开启。",
                    MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(
                       bridge.BackpacksHealthLocked ||
                       !CanDamageEnemyBackpack(bridge)))
            {
                if (GUILayout.Button("敌人背包血量 - 25%"))
                {
                    bridge.DamageEnemyBackpackByMaximumHealthFraction(
                        0.25f);
                }
            }

            using (new EditorGUI.DisabledScope(
                       bridge.BackpacksHealthLocked ||
                       !CanDamagePlayerBackpack(bridge)))
            {
                if (GUILayout.Button("玩家背包血量 - 25%"))
                {
                    bridge.DamagePlayerBackpackByMaximumHealthFraction(
                        0.25f);
                }
            }

            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(
                       snapshot.Phase !=
                       BattlePhase.Preparation ||
                       !snapshot.SystemReady))
            {
                EditorGUILayout.BeginHorizontal();

                if (selectedTarget == 0 &&
                    GUILayout.Button("刷新商店"))
                {
                    bridge.RefreshShop();
                }

                if (selectedTarget == 0 &&
                    GUILayout.Button("刷新Roll次数"))
                {
                    bridge.ResetRolls();
                }

                if (selectedTarget == 0 &&
                    GUILayout.Button("恢复默认布局"))
                {
                    bridge.RestoreDefaultLayout();
                }

                if (selectedTarget == 1 &&
                    GUILayout.Button("恢复默认敌人数据"))
                {
                    bridge.RestoreEnemyDefaultData();
                }

                EditorGUILayout.EndHorizontal();
            }

            using (new EditorGUI.DisabledScope(
                       snapshot.Phase !=
                       BattlePhase.Combat))
            {
                if (selectedTarget == 0 &&
                    GUILayout.Button("重新开始全部冷却"))
                {
                    bridge.BeginAllCooldowns();
                }

                if (selectedTarget == 0 &&
                    GUILayout.Button("随机化装备冷却时间"))
                {
                    bridge.RandomizeAllEquipmentCooldowns();
                }
            }

            if (selectedTarget == 0)
            {
                float curve =
                    EditorGUILayout.Slider(
                        "手动飞行曲线",
                        snapshot.CurveValue,
                        -1f,
                        1f);

                if (!Mathf.Approximately(
                        curve,
                        snapshot.CurveValue))
                {
                    bridge.SetCurveValue(curve);
                }
            }
            else
            {
                DrawEnemyDataActions(
                    bridge,
                    snapshot);
            }
        }

        private void DrawTargetSelector(
            PlayerBackpackDebugBridge bridge)
        {
            string[] targets =
            {
                "Player [Player]",
                "Enemy [Enemy]",
            };

            using (new EditorGUI.DisabledScope(
                       bridge.EnemyTarget == null))
            {
                selectedTarget =
                    EditorGUILayout.Popup(
                        "目标背包",
                        selectedTarget,
                        targets);
            }

            if (bridge.EnemyTarget == null)
            {
                selectedTarget = 0;
            }
        }

        private static bool CanDamagePlayerBackpack(
            PlayerBackpackDebugBridge bridge)
        {
            return bridge.Target != null &&
                   bridge.Target.TryGetComponent(
                       out BattleBackpackTarget2D target) &&
                   target.Health != null &&
                   !target.Health.IsDead;
        }

        private static bool CanDamageEnemyBackpack(
            PlayerBackpackDebugBridge bridge)
        {
            return bridge.EnemyTarget != null &&
                   bridge.EnemyTarget.TryGetComponent(
                       out BattleBackpackTarget2D target) &&
                   target.Health != null &&
                   !target.Health.IsDead;
        }

        private void DrawEnemyDataActions(
            PlayerBackpackDebugBridge bridge,
            PlayerBackpackDebugSnapshot snapshot)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "敌人背包数据",
                EditorStyles.boldLabel);
            EditorGUILayout.ObjectField(
                "当前数据",
                bridge.EnemyTarget?.CurrentData,
                typeof(EnemyBackpackData),
                false);
            enemyDataToApply =
                (EnemyBackpackData)
                EditorGUILayout.ObjectField(
                    "待应用数据",
                    enemyDataToApply,
                    typeof(EnemyBackpackData),
                    false);

            using (new EditorGUI.DisabledScope(
                       snapshot.Phase !=
                       BattlePhase.Preparation ||
                       !snapshot.SystemReady ||
                       enemyDataToApply == null))
            {
                if (GUILayout.Button("应用敌人背包数据"))
                {
                    bridge.ApplyEnemyData(
                        enemyDataToApply);
                }
            }
        }

        private static void DrawSummary(
            PlayerBackpackDebugSnapshot snapshot)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "运行时状态",
                EditorStyles.boldLabel);

            using (new EditorGUILayout
                       .VerticalScope(
                           EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "当前阶段",
                    snapshot.Phase ==
                    BattlePhase.Combat
                        ? "战斗"
                        : "准备");
                EditorGUILayout.LabelField(
                    "系统就绪",
                    snapshot.SystemReady
                        ? "是"
                        : "否");
                EditorGUILayout.LabelField(
                    "背包尺寸",
                    $"{snapshot.Width} × " +
                    $"{snapshot.Height}");
                EditorGUILayout.LabelField(
                    "物品数量",
                    snapshot.Items.Count.ToString());
                EditorGUILayout.LabelField(
                    "生成队列",
                    snapshot.PendingSpawnCount
                        .ToString());
                EditorGUILayout.LabelField(
                    "曲线值",
                    snapshot.CurveValue.ToString(
                        "+0.00;-0.00;0.00"));
            }
        }

        private void DrawItems(
            PlayerBackpackDebugSnapshot snapshot)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "背包物品与飞机增益",
                EditorStyles.boldLabel);

            scrollPosition =
                EditorGUILayout.BeginScrollView(
                    scrollPosition);

            if (snapshot.Items.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "背包为空。",
                    MessageType.Info);
            }

            itemStyle ??=
                new GUIStyle(EditorStyles.helpBox)
                {
                    alignment =
                        TextAnchor.UpperLeft,
                    wordWrap = true,
                    padding =
                        new RectOffset(8, 8, 7, 7),
                };

            foreach (PlayerBackpackDebugItemSnapshot item
                     in snapshot.Items)
            {
                EditorGUILayout.SelectableLabel(
                    item.Description,
                    itemStyle,
                    GUILayout.MinHeight(
                        item.ItemType ==
                        ItemType.Aircraft
                            ? 58f
                            : 38f));
                EditorGUILayout.Space(2f);
            }

            EditorGUILayout.EndScrollView();
        }

    }
}
