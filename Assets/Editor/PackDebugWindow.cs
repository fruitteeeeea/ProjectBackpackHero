using System.Collections.Generic;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>Play Mode controls for exercising the persistent four-slot pack queue.</summary>
    internal sealed class PackDebugWindow : ScriptableObject
    {
        string lastResult;

        internal void DrawTab()
        {
            EditorGUILayout.LabelField("卡包调试", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "操作会立即写入卡包存档；直接结算会按正式规则发放金币、钻石和碎片。",
                EditorStyles.miniLabel);

            if (!EditorApplication.isPlaying || PackSystem.Instance == null)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后等待 PackSystem 初始化。", MessageType.Info);
                return;
            }

            PackSystem packs = PackSystem.Instance;
            DrawSlots(packs);
            EditorGUILayout.Space(6f);
            DrawGrantControls(packs);
            EditorGUILayout.Space(6f);
            DrawQueueControls(packs);

            if (!string.IsNullOrEmpty(lastResult))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(lastResult, MessageType.None);
            }
        }

        void DrawSlots(PackSystem packs)
        {
            EditorGUILayout.LabelField("当前队列", EditorStyles.boldLabel);
            for (int index = 0; index < PackSystem.SlotCount; index++)
            {
                PackState state = packs.GetSlotState(index);
                PackSlotData slot = packs.GetSlots()[index];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"槽位 {index + 1}", EditorStyles.boldLabel);
                    if (state == PackState.Empty)
                    {
                        EditorGUILayout.LabelField("状态", PackState.Empty.ToString());
                        continue;
                    }

                    PackDefinition definition = packs.GetDefinition(slot.Id);
                    EditorGUILayout.LabelField("卡包", definition?.Name ?? slot.Id.ToString());
                    EditorGUILayout.LabelField("状态", state.ToString());
                    EditorGUILayout.LabelField("剩余时间", FormatTime(packs.GetRemainingSeconds(index)));
                    EditorGUILayout.LabelField("跳过钻石", packs.GetSkipDiamondCost(index).ToString());
                    DrawSlotActions(packs, index, state);
                }
            }
        }

        void DrawSlotActions(PackSystem packs, int index, PackState state)
        {
            EditorGUILayout.BeginHorizontal();
            if (state == PackState.Start && GUILayout.Button("开始"))
                Report(packs.TryStartPack(index), $"槽位 {index + 1} 已开始倒计时。", "当前槽位无法开始。");

            if (state == PackState.Opening)
            {
                if (GUILayout.Button("减时 1 分钟"))
                    Report(packs.ReduceOpenTime(index, 60), $"槽位 {index + 1} 已减时 1 分钟。", "减时失败。");
                if (GUILayout.Button("减时 5 分钟"))
                    Report(packs.ReduceOpenTime(index, 300), $"槽位 {index + 1} 已减时 5 分钟。", "减时失败。");
                if (GUILayout.Button("完成"))
                {
                    int remaining = packs.GetRemainingSeconds(index);
                    Report(remaining > 0 && packs.ReduceOpenTime(index, remaining), $"槽位 {index + 1} 已完成，可直接结算。", "当前槽位无法完成。");
                }
            }

            if ((state == PackState.Opening || state == PackState.Locked) && GUILayout.Button("钻石跳过"))
                Report(packs.TrySkipAndOpenPack(index), $"槽位 {index + 1} 已用钻石跳过。", "跳过失败：钻石不足或状态不允许。");

            if (state == PackState.Opened && GUILayout.Button("结算奖励"))
            {
                if (packs.TrySettleReward(index, out PackReward reward))
                    lastResult = FormatReward(index, reward);
                else
                    lastResult = "结算失败：当前槽位不满足结算条件。";
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawGrantControls(PackSystem packs)
        {
            EditorGUILayout.LabelField("发放卡包", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawGrantButton(packs, PackId.Green, "绿色");
            DrawGrantButton(packs, PackId.Blue, "蓝色");
            DrawGrantButton(packs, PackId.Purple, "紫色");
            DrawGrantButton(packs, PackId.Gold, "金色");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("随机卡包", GUILayout.Height(24f)))
                Report(packs.TryAddRandomPack(), "已添加随机卡包。", "队列已满，无法添加卡包。");
            if (GUILayout.Button("填满四种品质", GUILayout.Height(24f)))
            {
                int added = 0;
                foreach (PackId id in new[] { PackId.Green, PackId.Blue, PackId.Purple, PackId.Gold })
                    if (packs.TryAddPack(id)) added++;
                lastResult = added > 0 ? $"已按绿、蓝、紫、金顺序添加 {added} 个卡包。" : "队列已满，未添加卡包。";
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawQueueControls(PackSystem packs)
        {
            EditorGUILayout.LabelField("队列维护", EditorStyles.boldLabel);
            if (GUILayout.Button("清空全部卡包队列", GUILayout.Height(28f)) &&
                EditorUtility.DisplayDialog("清空卡包队列", "将移除全部 4 个卡包槽位，不会影响金币、钻石或碎片。是否继续？", "清空", "取消"))
            {
                packs.ClearAllPacks();
                lastResult = "已清空全部卡包队列；玩家货币与物品养成未改变。";
            }
        }

        void DrawGrantButton(PackSystem packs, PackId id, string label)
        {
            if (GUILayout.Button(label, GUILayout.Height(24f)))
                Report(packs.TryAddPack(id), $"已添加{label}卡包。", "队列已满，无法添加卡包。");
        }

        void Report(bool success, string successMessage, string failureMessage) => lastResult = success ? successMessage : failureMessage;

        static string FormatTime(int seconds) => System.TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");

        static string FormatReward(int index, PackReward reward)
        {
            List<string> fragments = new();
            foreach (KeyValuePair<ItemData, int> entry in reward.Fragments)
                fragments.Add($"{entry.Key.Name} x{entry.Value}");
            string fragmentText = fragments.Count == 0 ? "无碎片" : string.Join("、", fragments);
            return $"槽位 {index + 1} 奖励已结算：金币 {reward.Gold}，钻石 {reward.Diamond}，碎片：{fragmentText}";
        }
    }
}
