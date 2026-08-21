using BackpackHero.Battle;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>敌人自动背包的运行时控制页。</summary>
    internal sealed class EnemyBackpackDebugWindow : ScriptableObject
    {
        private DeckPreset selectedPreset;

        internal void DrawTab()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后可调试敌人自动背包。", MessageType.Info);
                return;
            }

            EnemyBackpackSystem enemy = PlayerBackpackDebugBridge.Active?.EnemyTarget;
            if (enemy == null)
            {
                EditorGUILayout.HelpBox("未找到 EnemyBackpackSystem。", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("敌人背包", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("当前背包预设", enemy.CurrentDeckPreset, typeof(DeckPreset), false);
            selectedPreset = (DeckPreset)EditorGUILayout.ObjectField("选取背包预设", selectedPreset, typeof(DeckPreset), false);
            bool canModify = enemy.IsReady && BattleFlowController.CurrentPhase == BattlePhase.Preparation && !enemy.IsOperationRunning;
            using (new EditorGUI.DisabledScope(!canModify))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("使用选中预设")) enemy.SetCurrentDeckPreset(selectedPreset);
                if (GUILayout.Button("随机选取预设")) enemy.SelectRandomPreset();
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("根据当前预设随机摆放初始物品", GUILayout.Height(26f))) enemy.RandomizeInitialPlacement();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("进行一次背包操作", GUILayout.Height(26f))) enemy.RequestOperation();
                if (GUILayout.Button("重置商店刷新机会", GUILayout.Height(26f))) enemy.ResetShopRollAllowance();
                EditorGUILayout.EndHorizontal();

                float interval = EditorGUILayout.FloatField("操作间隔（秒）", enemy.OperationInterval);
                if (!Mathf.Approximately(interval, enemy.OperationInterval)) enemy.SetOperationInterval(interval);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("当前阶段", BattleFlowController.CurrentPhase == BattlePhase.Preparation ? "准备" : "战斗");
                EditorGUILayout.LabelField("剩余自动操作次数", $"{enemy.RemainingOperations} / {enemy.MaximumOperations}");
                EditorGUILayout.LabelField("剩余商店刷新次数", $"{enemy.RemainingShopRolls} / 1");
                EditorGUILayout.LabelField("操作中", enemy.IsOperationRunning ? "是" : "否");
                string stock = "（空）";
                if (enemy.ShopItems.Count > 0)
                {
                    var names = new System.Collections.Generic.List<string>();
                    foreach (ItemData item in enemy.ShopItems)
                    {
                        names.Add(item != null ? item.ItemName : "<空>");
                    }

                    stock = string.Join("、", names);
                }

                EditorGUILayout.LabelField("隐藏商店库存", stock);
            }
        }
    }
}
