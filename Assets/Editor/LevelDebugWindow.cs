using BackpackHero.Battle;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>程序测试页：快速重置并进入任一关卡。</summary>
    internal sealed class LevelDebugWindow : ScriptableObject
    {
        internal void DrawTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("关卡测试", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后可选择并重置关卡。", MessageType.Info);
                return;
            }

            LevelFlowController flow = LevelFlowController.EnsureInstance();
            EditorGUILayout.LabelField("选择关卡", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int level = 1; level <= LevelManager.MaximumLevel; level++)
                {
                    int selectedLevel = level;
                    GUI.backgroundColor = LevelManager.CurrentLevel == level ? new Color(.65f, .9f, 1f) : Color.white;
                    if (GUILayout.Button($"关卡{level}")) flow.ResetForLevel(selectedLevel);
                }
                GUI.backgroundColor = Color.white;
            }

            DrawEnemyStrengthToggle();

            DrawInfo(flow);
        }

        private static void DrawEnemyStrengthToggle()
        {
            LevelDifficultyRuntime runtime = LevelDifficultyRuntime.Instance;
            if (runtime == null)
            {
                EditorGUILayout.HelpBox("关卡强度运行时未初始化。", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("敌人关卡强度", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool enabled = runtime.EnemyStrengthEnabled;
                GUI.backgroundColor = enabled
                    ? new Color(.65f, .9f, 1f)
                    : Color.white;
                if (GUILayout.Button("开启"))
                    runtime.SetEnemyStrengthEnabled(true);

                GUI.backgroundColor = !enabled
                    ? new Color(.65f, .9f, 1f)
                    : Color.white;
                if (GUILayout.Button("关闭"))
                    runtime.SetEnemyStrengthEnabled(false);

                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.LabelField(
                "当前状态",
                runtime.EnemyStrengthEnabled ? "开启（仅影响新生成敌人）" : "关闭（新生成敌人不应用关卡倍率）");
        }

        internal static void DrawInfo(LevelFlowController flow)
        {
            LevelDifficultySettings settings = LevelDifficultyRuntime.Instance?.Settings;
            bool enemyStrengthEnabled =
                LevelDifficultyRuntime.Instance?.EnemyStrengthEnabled == true;
            EnemyStrengthMultipliers enemy = new(
                enemyStrengthEnabled
                    ? LevelDifficultyRuntime.GetAircraftHealthMultiplier(BattleFaction.Enemy)
                    : 1f,
                enemyStrengthEnabled
                    ? LevelDifficultyRuntime.GetProjectileDamageMultiplier(BattleFaction.Enemy)
                    : 1f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("当前关卡信息", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("关卡", $"关卡 {LevelManager.CurrentLevel}");
                EditorGUILayout.LabelField("阶段", BattleFlowController.CurrentPhase.ToString());
                EditorGUILayout.LabelField("回合 / 比分", $"第 {LevelFlowController.CurrentRound} 回合 | 玩家 {flow.PlayerWins} : {flow.EnemyWins} 敌人");
                EditorGUILayout.LabelField("对局状态", flow.IsMatchComplete ? "已结算" : "进行中");
                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField("新敌人强度", $"生命 × {enemy.Health:0.##}  |  伤害 × {enemy.Damage:0.##}");
                EditorGUILayout.LabelField("玩家养成强度", settings == null ? "配置未加载" : $"生命 × {LevelDifficultyRuntime.GetAircraftHealthMultiplier(BattleFaction.Player):0.##}  |  伤害 × {LevelDifficultyRuntime.GetProjectileDamageMultiplier(BattleFaction.Player):0.##}");
            }
        }
    }
}
