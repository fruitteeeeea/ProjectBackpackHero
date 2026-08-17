using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    internal sealed class DeckPresetDebugWindow : ScriptableObject
    {
        private const string PresetFolder = "Assets/Data/Backpack/DeckPresets";
        private DeckPreset selectedPreset;
        private Vector2 scrollPosition;

        internal void DrawTab()
        {
            List<DeckPreset> presets = FindPresets();
            EditorGUILayout.LabelField("共用背包预设", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("预设保存 Hanger 的 3 飞机 + 2 装备槽。读取仅能在准备阶段进行，背包会按首个可放格规则重新排列。", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"已创建：{presets.Count}", GUILayout.Width(100f));
                if (GUILayout.Button("新建空白预设")) selectedPreset = CreatePreset(null);
                if (GUILayout.Button("刷新")) AssetDatabase.Refresh();
            }

            selectedPreset = (DeckPreset)EditorGUILayout.ObjectField("当前预设", selectedPreset, typeof(DeckPreset), false);
            if (selectedPreset == null && presets.Count > 0) selectedPreset = presets[0];
            if (selectedPreset != null && !presets.Contains(selectedPreset)) selectedPreset = null;

            if (selectedPreset != null)
            {
                DrawPresetItems(selectedPreset);
                DrawAssetActions();
            }
            else
            {
                EditorGUILayout.HelpBox("尚无预设。新建空白预设，或在 Play Mode 将当前玩家 Deck 另存为预设。", MessageType.Info);
            }

            DrawRuntimeActions();
        }

        private void DrawPresetItems(DeckPreset preset)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("当前配置物品", EditorStyles.boldLabel);
            if (!preset.IsValid(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            string[] labels = { "飞机 1", "飞机 2", "飞机 3", "装备 1", "装备 2" };
            IReadOnlyList<ItemData> slots = preset.Slots;
            for (int index = 0; index < PlayerItemSystem.DeckSlotCount; index++)
            {
                ItemData item = index < slots.Count ? slots[index] : null;
                EditorGUILayout.ObjectField(labels[index], item, typeof(ItemData), false);
            }
            if (GUILayout.Button("选中资产")) Selection.activeObject = preset;
        }

        private void DrawAssetActions()
        {
            if (!EditorApplication.isPlaying) return;
            PlayerItemSystem playerItems = PlayerItemSystem.Instance;
            using (new EditorGUI.DisabledScope(playerItems == null))
            {
                if (GUILayout.Button("保存当前玩家 Deck 到此预设"))
                {
                    Undo.RecordObject(selectedPreset, "Save player deck preset");
                    selectedPreset.SetSlots(playerItems.GetDeckItems());
                    EditorUtility.SetDirty(selectedPreset);
                    AssetDatabase.SaveAssets();
                }
            }
            using (new EditorGUI.DisabledScope(playerItems == null))
            {
                if (GUILayout.Button("当前玩家 Deck 另存为新预设")) selectedPreset = CreatePreset(playerItems.GetDeckItems());
            }
        }

        private void DrawRuntimeActions()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("运行时读取", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后可将预设读取到玩家或敌人。", MessageType.Info);
                return;
            }
            PlayerBackpackDebugBridge bridge = PlayerBackpackDebugBridge.Active;
            bool canApply = selectedPreset != null && bridge != null && BattleFlowController.CurrentPhase == BattlePhase.Preparation;
            if (bridge == null) EditorGUILayout.HelpBox("未连接玩家背包调试桥。", MessageType.Warning);
            else if (!canApply) EditorGUILayout.HelpBox("战斗阶段不能读取预设。", MessageType.Warning);

            using (new EditorGUI.DisabledScope(!canApply))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("读取到玩家")) bridge.ApplyPlayerDeckPreset(selectedPreset);
                if (GUILayout.Button("读取到敌人")) bridge.ApplyEnemyDeckPreset(selectedPreset);
            }

            if (bridge?.Snapshot.HasTarget == true) DrawRuntimeItems("当前玩家背包", bridge.Snapshot);
            if (bridge?.EnemySnapshot.HasTarget == true) DrawRuntimeItems("当前敌人背包", bridge.EnemySnapshot);
        }

        private void DrawRuntimeItems(string title, PlayerBackpackDebugSnapshot snapshot)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(120f));
            foreach (PlayerBackpackDebugItemSnapshot item in snapshot.Items) EditorGUILayout.LabelField(item.Description, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        private static List<DeckPreset> FindPresets()
        {
            var result = new List<DeckPreset>();
            foreach (string guid in AssetDatabase.FindAssets("t:DeckPreset", new[] { PresetFolder }))
            {
                DeckPreset preset = AssetDatabase.LoadAssetAtPath<DeckPreset>(AssetDatabase.GUIDToAssetPath(guid));
                if (preset != null) result.Add(preset);
            }
            result.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return result;
        }

        private static DeckPreset CreatePreset(IReadOnlyList<ItemData> items)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data/Backpack")) return null;
            if (!AssetDatabase.IsValidFolder(PresetFolder)) AssetDatabase.CreateFolder("Assets/Data/Backpack", "DeckPresets");
            var preset = CreateInstance<DeckPreset>();
            preset.SetSlots(items);
            string path = AssetDatabase.GenerateUniqueAssetPath(PresetFolder + "/DeckPreset.asset");
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = preset;
            return preset;
        }
    }
}
