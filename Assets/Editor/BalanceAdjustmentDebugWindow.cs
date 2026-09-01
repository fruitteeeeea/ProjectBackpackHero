using System.Collections.Generic;
using System.Linq;
using BackpackHero.Battle;
using BackpackHero.Debugging;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>对局内的只读汇总与临时平衡控制入口。</summary>
    internal sealed class BalanceAdjustmentDebugWindow : ScriptableObject
    {
        private Vector2 scroll;
        private readonly BalanceAdjustmentPersistence persistence =
            new BalanceAdjustmentPersistence();

        internal static bool IsAvailable()
        {
            PlayerBackpackDebugBridge bridge = PlayerBackpackDebugBridge.Active;
            return EditorApplication.isPlaying && bridge?.Target?.IsReady == true &&
                bridge.EnemyTarget?.IsReady == true &&
                (BattleFlowController.CurrentPhase == BattlePhase.Preparation ||
                 BattleFlowController.CurrentPhase == BattlePhase.Combat);
        }

        internal void DrawTab()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("平衡调整", EditorStyles.boldLabel);
            persistence.DrawTestToolbar();
            persistence.DrawPersistentEnemyStrength();
            bool runtimeReady = IsAvailable();
            if (!runtimeReady)
            {
                EditorGUILayout.HelpBox("等待 Play Mode 对局运行时：页面保持可打开，功能将在双方背包就绪且位于准备或战斗阶段时启用。", MessageType.Info);
            }

            PlayerBackpackDebugBridge bridge = PlayerBackpackDebugBridge.Active;
            using (new EditorGUI.DisabledScope(!runtimeReady))
            {
                if (runtimeReady)
                {
                    DrawRuntimeInformation(bridge);
                    EditorGUILayout.Space(12f);
                    DrawAdjustments(bridge);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawRuntimeInformation(PlayerBackpackDebugBridge bridge)
        {
            EditorGUILayout.LabelField("运行时信息显示", EditorStyles.boldLabel);
            DrawFactionInformation("玩家", BattleFaction.Player, bridge.Target, bridge.EnemyTarget, true);
            EditorGUILayout.Space(6f);
            DrawFactionInformation("敌人", BattleFaction.Enemy, bridge.Target, bridge.EnemyTarget, false);
        }

        private static void DrawFactionInformation(string title, BattleFaction faction,
            PlayerBackpackSystem player, EnemyBackpackSystem enemy, bool isPlayer)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                int playerLevel = PlayerItemSystem.DefaultLevel;
                bool hasUniformLevel = !isPlayer ||
                    player.TryGetUniformProgressionLevel(out playerLevel);
                int level = isPlayer ? playerLevel : enemy.ActiveProgressionLevel;
                EditorGUILayout.LabelField("养成等级", hasUniformLevel
                    ? $"Lv.{level}"
                    : "混合等级（未启用临时统一覆写）");
                EditorGUILayout.LabelField("背包配置", isPlayer
                    ? BalanceAdjustmentDeckDisplay.DescribePlayer(player.ActiveDeckItems)
                    : BalanceAdjustmentDeckDisplay.DescribeEnemy(
                        enemy.CurrentDeckPreset?.Slots,
                        enemy.CurrentDeckPreset?.name ?? "未选择预设"),
                    EditorStyles.wordWrappedLabel);
                BackpackController backpack = isPlayer ? player.Backpack : enemy.Backpack;
                EditorGUILayout.LabelField("背包强度", backpack != null ? BackpackStrengthCalculator.Calculate(backpack).TotalScore.ToString("0.##") : "-");
                DrawDamageSummary(faction);
            }
        }

        private static void DrawDamageSummary(BattleFaction faction)
        {
            DamageStatisticsRuntime statistics = DamageStatisticsRuntime.Instance;
            IReadOnlyList<DamageStatisticsEntry> entries = statistics?.GetEntries(faction) ?? new List<DamageStatisticsEntry>();
            float total = entries.Sum(entry => entry.ActualDamage);
            float elapsed = statistics?.CombatElapsedSeconds ?? 0f;
            EditorGUILayout.LabelField("当前总伤害", total.ToString("0.##"));
            EditorGUILayout.LabelField("DPS", elapsed > 0f ? (total / elapsed).ToString("0.##") : "0");
            int kills = statistics?.GetEnemyAircraftKillCount(faction) ?? 0;
            int exits = statistics?.GetOvertimeAircraftExitCount(faction) ?? 0;
            EditorGUILayout.LabelField("击杀敌机（己方强制退场）", $"{kills}（{exits}）");
            DrawDamageCategory("飞机子弹", DamageStatisticsItemCategory.Aircraft, entries, total);
            DrawDamageCategory("装备伤害", DamageStatisticsItemCategory.Equipment, entries, total);
        }

        private static void DrawDamageCategory(string label, DamageStatisticsItemCategory category,
            IReadOnlyList<DamageStatisticsEntry> entries, float total)
        {
            float damage = entries.Where(entry => entry.Category == category).Sum(entry => entry.ActualDamage);
            EditorGUILayout.LabelField(label, $"{damage:0.##}（{(total > 0f ? damage / total : 0f):P1}）", EditorStyles.miniLabel);
        }

        private void DrawAdjustments(PlayerBackpackDebugBridge bridge)
        {
            EditorGUILayout.LabelField("操作调整", EditorStyles.boldLabel);
            DrawGameSpeed();
            DrawInitialBackpackItemPlacement();
            DrawFlightRoute(bridge);
            DrawProgressionLevels(bridge);
            DrawAutomatedPlacement(bridge);
            DrawMatchControls(bridge);
        }

        private static void DrawGameSpeed()
        {
            GamePacingDebugRuntime runtime = GamePacingDebugRuntime.Instance;
            if (runtime == null) return;
            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.Slider("游戏运行速度", runtime.GameSpeed <= 0f ? 1f : runtime.GameSpeed, .5f, 2f);
            value = Mathf.Round(value * 2f) * .5f;
            if (EditorGUI.EndChangeCheck()) runtime.SetGameSpeed(value);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("恢复正常游戏速度（1x）")) runtime.SetGameSpeed(1f);
                if (GUILayout.Button("暂停游戏（0x）")) runtime.SetGameSpeed(0f);
            }
        }

        private static void DrawInitialBackpackItemPlacement()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("开局背包", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle(
                "自动添加配置物品",
                InitialBackpackItemAutoPlacementDebug.IsEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                InitialBackpackItemAutoPlacementDebug.SetEnabled(enabled);
            }

            EditorGUILayout.HelpBox(
                "仅在下一次新对局或点击“重启对局”后生效；关闭时双方背包开局为空。",
                MessageType.None);
        }

        private static void DrawFlightRoute(PlayerBackpackDebugBridge bridge)
        {
            PlayerRandomFlightCurveController controller = bridge.GetComponent<PlayerRandomFlightCurveController>();
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("随机航线", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(BattleFlowController.CurrentPhase != BattlePhase.Combat || controller == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRouteButton(controller, RandomFlightCurveMode.Off, "关闭");
                DrawRouteButton(controller, RandomFlightCurveMode.Peaceful, "平和");
                DrawRouteButton(controller, RandomFlightCurveMode.Intense, "激烈");
            }
        }

        private static void DrawRouteButton(PlayerRandomFlightCurveController controller, RandomFlightCurveMode mode, string label)
        {
            Color old = GUI.backgroundColor;
            if (controller != null && controller.Mode == mode) GUI.backgroundColor = new Color(.65f, .9f, 1f);
            if (GUILayout.Button(label) && controller != null) controller.SetMode(mode);
            GUI.backgroundColor = old;
        }

        private void DrawProgressionLevels(PlayerBackpackDebugBridge bridge)
        {
            EditorGUILayout.Space(6f);
            bool hasUniformPlayerLevel = bridge.Target
                .TryGetUniformProgressionLevel(out int playerLevel);
            DrawLevelButtons("玩家临时养成", playerLevel,
                hasUniformPlayerLevel,
                level => bridge.Target.SetDebugProgressionLevel(level));
            EditorGUILayout.Space(6f);
            DrawLevelButtons("敌人临时养成", bridge.EnemyTarget.ActiveProgressionLevel,
                true,
                level => bridge.EnemyTarget.SetDebugProgressionLevel(level));
        }

        private static void DrawLevelButtons(string title, int activeLevel,
            bool hasHighlightedLevel, System.Func<int, bool> setLevel)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                if (!hasHighlightedLevel)
                {
                    EditorGUILayout.LabelField("当前物品养成等级不一致；点击任一按钮可临时统一为该等级。", EditorStyles.miniLabel);
                }
                for (int row = 0; row < 2; row++)
                using (new EditorGUILayout.HorizontalScope())
                for (int column = 1; column <= 5; column++)
                {
                    int level = row * 5 + column;
                    Color old = GUI.backgroundColor;
                    if (hasHighlightedLevel && activeLevel == level) GUI.backgroundColor = new Color(.65f, .9f, 1f);
                    if (GUILayout.Button($"Lv.{level}")) setLevel(level);
                    GUI.backgroundColor = old;
                }
            }
        }

        private static void DrawAutomatedPlacement(PlayerBackpackDebugBridge bridge)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("背包 AI 自动操作", EditorStyles.boldLabel);
            bool playerOperating = bridge.Target.IsDebugAutoOperationRunning ||
                bridge.EnemyTarget.IsDebugFastOperationRunning;
            bool canPlace = BattleFlowController.CurrentPhase == BattlePhase.Preparation &&
                bridge.Target.IsReady && bridge.EnemyTarget.IsReady && !playerOperating && !bridge.EnemyTarget.IsOperationRunning;
            using (new EditorGUI.DisabledScope(!canPlace))
            {
                if (GUILayout.Button("双方 AI 执行 15 次操作"))
                {
                    bridge.StartSynchronizedDebugAutoOperations();
                }
                if (GUILayout.Button("敌人 AI 快速放置（最多 15 次）"))
                    bridge.EnemyTarget.StartDebugFastOperations();
            }
            string status = string.IsNullOrEmpty(bridge.Target.DebugAutoOperationStatus)
                ? "未启动" : bridge.Target.DebugAutoOperationStatus;
            EditorGUILayout.LabelField("玩家 AI", $"{bridge.Target.DebugAutoOperationSuccessCount} / 15 · {status}");
            EditorGUILayout.LabelField("当前操作", bridge.Target.DebugAutoOperationName ?? "-");
            string enemyStatus = string.IsNullOrEmpty(bridge.EnemyTarget.DebugFastOperationStatus)
                ? "未启动" : bridge.EnemyTarget.DebugFastOperationStatus;
            EditorGUILayout.LabelField("敌人 AI", $"{bridge.EnemyTarget.DebugFastOperationSuccessCount} / 15 · {enemyStatus}");
            EditorGUILayout.LabelField("敌人当前操作", bridge.EnemyTarget.DebugFastOperationName ?? "-");
            EditorGUILayout.HelpBox("双方 AI 都从当前背包连续优化，不会重建初始布局。敌人快速放置期间暂停其常规自动操作。", MessageType.None);
        }

        private static void DrawMatchControls(PlayerBackpackDebugBridge bridge)
        {
            LevelFlowController flow = LevelFlowController.Instance;
            bool playerOperating = bridge.Target.IsDebugAutoOperationRunning ||
                bridge.EnemyTarget.IsDebugFastOperationRunning;
            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(playerOperating))
            {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(flow == null || !flow.IsRoundTimerRunning))
                {
                    if (GUILayout.Button("-5 秒")) flow.AdjustRoundTimerForDebug(-5f);
                    if (GUILayout.Button("+5 秒")) flow.AdjustRoundTimerForDebug(5f);
                }
                if (GUILayout.Button("准备阶段")) bridge.EnterPreparation();
                if (GUILayout.Button("战斗阶段")) bridge.EnterCombat();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!bridge.CanSwapRuntimeDecks))
                    if (GUILayout.Button("交换玩家和敌人背包")) bridge.TrySwapRuntimeDecksAndResetMatch();
                using (new EditorGUI.DisabledScope(bridge.BackpacksHealthLocked || !bridge.CanLockBackpackHealth))
                {
                    if (GUILayout.Button("敌人背包 -25%")) bridge.DamageEnemyBackpackByMaximumHealthFraction(.25f);
                    if (GUILayout.Button("玩家背包 -25%")) bridge.DamagePlayerBackpackByMaximumHealthFraction(.25f);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(flow == null || !BattleFlowController.IsCombatPhase))
                {
                    if (GUILayout.Button("战斗胜利")) flow.ForceDebugMatchResult(true);
                    if (GUILayout.Button("战斗失败")) flow.ForceDebugMatchResult(false);
                }
                if (GUILayout.Button("重启对局")) { DamageStatisticsRuntime.Instance?.Clear(); flow?.ResetForDebugMatch(); }
            }
            }
            if (playerOperating)
            {
                EditorGUILayout.HelpBox("玩家 AI 正在操作背包，完成或停止前不能切换阶段或重置对局。", MessageType.Info);
            }
        }

        private void OnDisable() => persistence.Dispose();
    }

    /// <summary>将当前运行时 Deck 映射为通用预设名；未命中时提供原有的显示文案。</summary>
    internal static class BalanceAdjustmentDeckDisplay
    {
        private const string SharedPresetFolder =
            "Assets/Data/Backpack/DeckPresets";

        internal static string DescribePlayer(IReadOnlyList<ItemData> deck)
        {
            string presetName = FindSharedPresetName(deck);
            return !string.IsNullOrEmpty(presetName)
                ? presetName
                : "当前玩家 Deck：" + DescribeSlots(deck);
        }

        internal static string DescribeEnemy(IReadOnlyList<ItemData> deck,
            string fallbackName)
        {
            string presetName = FindSharedPresetName(deck);
            return !string.IsNullOrEmpty(presetName)
                ? presetName
                : fallbackName;
        }

        internal static string FindSharedPresetName(
            IReadOnlyList<ItemData> deck)
        {
            if (deck == null)
            {
                return null;
            }

            foreach (string path in AssetDatabase.FindAssets(
                         "t:DeckPreset", new[] { SharedPresetFolder })
                     .Select(AssetDatabase.GUIDToAssetPath)
                     .OrderBy(path => path, System.StringComparer.Ordinal))
            {
                DeckPreset preset = AssetDatabase.LoadAssetAtPath<DeckPreset>(
                    path);
                if (preset != null && SlotsMatch(deck, preset.Slots))
                {
                    return preset.name;
                }
            }

            return null;
        }

        internal static bool SlotsMatch(IReadOnlyList<ItemData> first,
            IReadOnlyList<ItemData> second)
        {
            if (first == null || second == null ||
                first.Count != PlayerItemSystem.DeckSlotCount ||
                second.Count != PlayerItemSystem.DeckSlotCount)
            {
                return false;
            }

            for (int index = 0; index < PlayerItemSystem.DeckSlotCount;
                 index++)
            {
                if (first[index] != second[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static string DescribeSlots(IReadOnlyList<ItemData> deck)
        {
            return deck == null
                ? "-"
                : string.Join(" / ", deck.Select(item => item != null
                    ? item.ItemName
                    : "空"));
        }
    }
}
