using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>Displays read-only strength scores for the active battle backpacks.</summary>
    internal sealed class BackpackStrengthDebugWindow : ScriptableObject
    {
        internal void DrawTab()
        {
            EditorGUILayout.LabelField("场上背包强度", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "评分会随当前背包物品、等级和相邻关系实时更新。",
                EditorStyles.miniLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "进入 SampleScene 的 Play Mode 后读取场上背包强度。",
                    MessageType.Info);
                return;
            }

            PlayerBackpackDebugBridge bridge = PlayerBackpackDebugBridge.Active;
            if (bridge == null)
            {
                EditorGUILayout.HelpBox(
                    "未找到 PlayerBackpackDebugBridge，正在等待运行时背包。",
                    MessageType.Warning);
                return;
            }

            DrawBackpack("玩家背包", bridge.Target?.Backpack, bridge.Target?.IsReady ?? false);
            DrawBackpack("敌人背包", bridge.EnemyTarget?.Backpack, bridge.EnemyTarget?.IsReady ?? false);
        }

        private static void DrawBackpack(
            string title,
            BackpackController backpack,
            bool systemReady)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (!systemReady || backpack == null)
            {
                EditorGUILayout.HelpBox(
                    $"{title}尚未初始化，暂无可读取的评分。",
                    MessageType.Info);
                return;
            }

            BackpackStrengthScore score =
                BackpackStrengthCalculator.Calculate(backpack);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "背包总分",
                    score.TotalScore.ToString("0.##"),
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField("飞机物品数量", score.AircraftCount.ToString());
                EditorGUILayout.LabelField("装备物品数量", score.EquipmentCount.ToString());
                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("飞机总分", score.AircraftScore.ToString("0.##"));
                EditorGUILayout.LabelField(
                    "  Lv.1 / Lv.2 / Lv.3",
                    $"{score.AircraftLevel1Count} / {score.AircraftLevel2Count} / {score.AircraftLevel3Count}",
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField("物品总分", score.EquipmentScore.ToString("0.##"));
                EditorGUILayout.LabelField(
                    "  Lv.1 / Lv.2 / Lv.3",
                    $"{score.EquipmentLevel1Count} / {score.EquipmentLevel2Count} / {score.EquipmentLevel3Count}",
                    EditorStyles.miniLabel);
            }
        }
    }
}
