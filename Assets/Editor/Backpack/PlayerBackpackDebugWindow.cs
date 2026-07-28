using BackpackHero.Battle;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    public sealed class PlayerBackpackDebugWindow :
        EditorWindow
    {
        private Vector2 scrollPosition;
        private GUIStyle itemStyle;
        private bool closingFromLifecycle;
        private int selectedTarget;
        private EnemyBackpackData enemyDataToApply;

        [MenuItem(
            "Tools/Backpack/Backpack Debug")]
        public static void OpenBackpackDebug()
        {
            OpenManually();
        }

        [MenuItem(
            "Tools/Backpack/Player Backpack Debug")]
        public static void OpenManually()
        {
            PlayerBackpackDebugBridge bridge =
                PlayerBackpackDebugBridge.Active;

            if (bridge != null)
            {
                bridge.SetDebugEnabled(true);
            }

            ShowSingleWindow();
        }

        internal static PlayerBackpackDebugWindow
            ShowSingleWindow()
        {
            PlayerBackpackDebugWindow[] windows =
                Resources.FindObjectsOfTypeAll<
                    PlayerBackpackDebugWindow>();
            bool created = windows.Length == 0;
            PlayerBackpackDebugWindow window =
                !created
                    ? windows[0]
                    : GetWindow<
                        PlayerBackpackDebugWindow>();

            for (int index = 1;
                 index < windows.Length;
                 index++)
            {
                windows[index].CloseFromLifecycle();
            }

            window.titleContent =
                new GUIContent("Backpack Debug");
            window.minSize = new Vector2(440f, 360f);

            if (created)
            {
                window.Show();
            }

            return window;
        }

        internal static void CloseAllFromLifecycle()
        {
            foreach (PlayerBackpackDebugWindow window in
                     Resources.FindObjectsOfTypeAll<
                         PlayerBackpackDebugWindow>())
            {
                window.CloseFromLifecycle();
            }
        }

        private void OnGUI()
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
                Repaint();
            }

            if (GUILayout.Button(
                    "关闭调试",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(72f)))
            {
                PlayerBackpackDebugBridge.Active
                    ?.SetDebugEnabled(false);
                Close();
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

            using (new EditorGUI.DisabledScope(
                       snapshot.Phase !=
                       BattlePhase.Preparation ||
                       !snapshot.SystemReady ||
                       bridge.Target == null ||
                       !bridge.Target.IsReady ||
                       bridge.EnemyTarget == null ||
                       !bridge.EnemyTarget.IsReady))
            {
                if (GUILayout.Button(
                        "敌人复制当前玩家背包",
                        GUILayout.Height(28f)))
                {
                    bridge.CopyPlayerLayoutToEnemy();
                }
            }

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

        private void CloseFromLifecycle()
        {
            closingFromLifecycle = true;
            Close();
        }

        private void OnDisable()
        {
            if (!closingFromLifecycle &&
                EditorApplication.isPlaying)
            {
                PlayerBackpackDebugBridge.Active
                    ?.SetDebugEnabled(false);
            }
        }
    }

    [InitializeOnLoad]
    internal static class
        PlayerBackpackDebugWindowLifecycle
    {
        private static bool wasPlaying;

        static PlayerBackpackDebugWindowLifecycle()
        {
            wasPlaying = EditorApplication.isPlaying;
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            bool isPlaying =
                EditorApplication.isPlaying;
            PlayerBackpackDebugBridge bridge =
                PlayerBackpackDebugBridge.Active;

            if (isPlaying &&
                bridge != null &&
                bridge.DebugEnabled)
            {
                PlayerBackpackDebugWindow
                    .ShowSingleWindow()
                    .Repaint();
            }
            else if ((wasPlaying && !isPlaying) ||
                     (isPlaying &&
                      (bridge == null ||
                       !bridge.DebugEnabled)))
            {
                PlayerBackpackDebugWindow
                    .CloseAllFromLifecycle();
            }

            wasPlaying = isPlaying;
        }
    }
}
