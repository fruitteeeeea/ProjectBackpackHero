using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>Play Mode controls for exercising persistent item-progression states.</summary>
    internal sealed class ItemProgressionDebugWindow : ScriptableObject
    {
        internal void DrawTab()
        {
            EditorGUILayout.LabelField("物品养成调试", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "修改会立即保存，并刷新机库中的卡片数据。",
                EditorStyles.miniLabel);

            if (!EditorApplication.isPlaying || PlayerItemSystem.Instance == null)
            {
                EditorGUILayout.HelpBox(
                    "进入 Play Mode 后等待 PlayerItemSystem 初始化。",
                    MessageType.Info);
                return;
            }

            PlayerItemSystem system = PlayerItemSystem.Instance;
            DrawSummary(system);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("货币", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加 1000 金币", GUILayout.Height(24f)))
            {
                system.AddCurrency(1000);
            }
            if (GUILayout.Button("添加 1000 钻石", GUILayout.Height(24f)))
            {
                system.AddCurrency(0, 1000);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("养成状态", EditorStyles.boldLabel);
            if (GUILayout.Button("重置养成（已解锁物品 Lv.1，碎片清零）", GUILayout.Height(28f)))
            {
                system.ResetAllProgression();
            }

            if (GUILayout.Button("恢复初始存档（5 张 Lv.1 初始卡，10 分）", GUILayout.Height(28f)))
            {
                system.RestoreInitialProgression();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("为全部飞机添加对应碎片", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawAddFragmentsButton(system, 1);
            DrawAddFragmentsButton(system, 5);
            DrawAddFragmentsButton(system, 10);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("恢复默认养成（全解锁、Lv.10、50,000 分）", GUILayout.Height(28f)))
            {
                system.RestoreDefaultProgression();
            }
        }

        private static void DrawAddFragmentsButton(PlayerItemSystem system, int amount)
        {
            if (GUILayout.Button($"全部飞机 +{amount} 碎片", GUILayout.Height(24f)))
            {
                system.AddFragmentsToAllAircraft(amount);
            }
        }

        private static void DrawSummary(PlayerItemSystem system)
        {
            int aircraftCount = 0;
            int levelOneCount = 0;
            int maxLevelCount = 0;
            foreach (ItemData item in system.GetAllItems())
            {
                if (item == null || item.ItemType != ItemType.Aircraft) continue;
                aircraftCount++;
                if (system.GetLevel(item) == PlayerItemSystem.DefaultLevel) levelOneCount++;
                if (system.GetLevel(item) == PlayerItemSystem.MaximumLevel) maxLevelCount++;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("金币 / 钻石", $"{system.Gold} / {system.Diamond}");
                EditorGUILayout.LabelField("目录物品", system.GetAllItems().Count.ToString());
                EditorGUILayout.LabelField("飞机物品", aircraftCount.ToString());
                EditorGUILayout.LabelField("飞机 Lv.1 / Lv.10", $"{levelOneCount} / {maxLevelCount}");
            }
        }
    }
}
