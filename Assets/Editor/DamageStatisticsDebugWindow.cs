using System.Collections.Generic;
using System.Linq;
using BackpackHero.Battle;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    internal sealed class DamageStatisticsDebugWindow : ScriptableObject
    {
        private bool showRawDamage = true;
        private Vector2 scroll;
        internal void DrawTab()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("DPS 检测（本场累计）", EditorStyles.boldLabel);
            showRawDamage = EditorGUILayout.Toggle("显示原始伤害值", showRawDamage);
            if (!EditorApplication.isPlaying || DamageStatisticsRuntime.Instance == null) { EditorGUILayout.HelpBox("进入 Play Mode 后等待伤害统计运行时。", MessageType.Info); EditorGUILayout.EndScrollView(); return; }
            DrawFaction("玩家背包", BattleFaction.Player);
            DrawFaction("敌人背包", BattleFaction.Enemy);
            EditorGUILayout.EndScrollView();
        }
        private void DrawFaction(string title, BattleFaction faction)
        {
            IReadOnlyList<DamageStatisticsEntry> source = DamageStatisticsRuntime.Instance.GetEntries(faction);
            float total = source.Sum(x => showRawDamage ? x.RawDamage : x.ActualDamage);
            EditorGUILayout.Space(8); EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("当前总伤害", total.ToString("0.##"));
            EditorGUILayout.LabelField("击杀敌方飞机数量",
                DamageStatisticsRuntime.Instance
                    .GetEnemyAircraftKillCount(faction).ToString());
            EditorGUILayout.LabelField("我方飞机超时退场数量",
                DamageStatisticsRuntime.Instance
                    .GetOvertimeAircraftExitCount(faction).ToString());
            foreach (DamageStatisticsItemCategory category in new[] { DamageStatisticsItemCategory.Aircraft, DamageStatisticsItemCategory.Equipment })
            {
                EditorGUILayout.LabelField(category == DamageStatisticsItemCategory.Aircraft ? "飞机物品" : "装备物品", EditorStyles.miniBoldLabel);
                List<DamageStatisticsEntry> rows = source.Where(x => x.Category == category).OrderByDescending(x => showRawDamage ? x.RawDamage : x.ActualDamage).ThenBy(x => x.Data != null ? x.Data.Name : "").ThenBy(x => x.Level).ToList();
                if (rows.Count == 0) { EditorGUILayout.LabelField("暂无统计物品", EditorStyles.miniLabel); continue; }
                foreach (DamageStatisticsEntry row in rows)
                {
                    float value = showRawDamage ? row.RawDamage : row.ActualDamage;
                    float ratio = total > 0f ? value / total : 0f;
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField($"{row.Data?.Name ?? "未命名物品"}  Lv.{row.Level}    当前背包个数 {row.BackpackCount}    伤害 {value:0.##}    击杀飞机数 {row.KillCount}");
                        Rect rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true)); EditorGUI.ProgressBar(rect, ratio, $"{ratio:P1}");
                    }
                }
            }
        }
    }
}
