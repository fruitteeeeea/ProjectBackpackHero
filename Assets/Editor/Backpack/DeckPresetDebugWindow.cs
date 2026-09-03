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
            EditorGUILayout.LabelField("流派说明", EditorStyles.boldLabel);
            string strategy = EditorGUILayout.TextArea(preset.StrategyHint, GUILayout.MinHeight(34f));
            string adjacency = EditorGUILayout.TextArea(preset.AdjacencyHint, GUILayout.MinHeight(34f));
            if (strategy != preset.StrategyHint || adjacency != preset.AdjacencyHint)
            {
                Undo.RecordObject(preset, "Edit deck preset guidance");
                preset.SetGuidance(strategy, adjacency);
                EditorUtility.SetDirty(preset);
                AssetDatabase.SaveAssets();
            }
            EditorGUILayout.LabelField("当前配置物品", EditorStyles.boldLabel);
            if (!preset.IsValid(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            string[] labels = { "飞机 1", "飞机 2", "飞机 3", "装备 1", "装备 2" };
            IReadOnlyList<ItemData> slots = preset.Slots;
            var editedSlots = new List<ItemData>(slots);
            while (editedSlots.Count < PlayerItemSystem.DeckSlotCount) editedSlots.Add(null);
            for (int index = 0; index < PlayerItemSystem.DeckSlotCount; index++)
            {
                ItemData item = editedSlots[index];
                ItemData next = (ItemData)EditorGUILayout.ObjectField(labels[index], item, typeof(ItemData), false);
                if (next == item) continue;
                if (next != null && !CanAssign(editedSlots, index, next))
                {
                    EditorUtility.DisplayDialog("不能设置预设槽位", "物品类型与槽位不匹配，或该物品已在此预设中使用。", "确定");
                    continue;
                }
                Undo.RecordObject(preset, "Edit deck preset");
                editedSlots[index] = next;
                preset.SetSlots(editedSlots);
                EditorUtility.SetDirty(preset);
                AssetDatabase.SaveAssets();
            }
            if (GUILayout.Button("选中资产")) Selection.activeObject = preset;
        }

        private static bool CanAssign(IReadOnlyList<ItemData> slots, int slot, ItemData item)
        {
            if (!PlayerItemSystem.IsDeckSlotType(item, slot)) return false;
            for (int index = 0; index < slots.Count; index++)
            {
                if (index != slot && slots[index] == item) return false;
            }
            return true;
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
            PlayerItemSystem playerItems = PlayerItemSystem.Instance;
            PlayerBackpackSystem playerBackpack = UnityEngine.Object.FindAnyObjectByType<PlayerBackpackSystem>(FindObjectsInactive.Include);
            bool isBattleScene = playerBackpack != null;
            bool canApplyPlayer = selectedPreset != null && playerItems != null &&
                (!isBattleScene || BattleFlowController.CurrentPhase == BattlePhase.Preparation);
            bool canApplyEnemy = selectedPreset != null && bridge != null &&
                bridge.EnemyTarget != null && BattleFlowController.CurrentPhase == BattlePhase.Preparation;

            if (playerItems == null) EditorGUILayout.HelpBox("未找到玩家 Deck 数据。", MessageType.Warning);
            else if (isBattleScene && !canApplyPlayer) EditorGUILayout.HelpBox("战斗阶段不能读取预设。", MessageType.Warning);
            else if (!isBattleScene) EditorGUILayout.HelpBox("当前为 Hanger 界面：读取会立即更新并保存玩家 Deck。", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!canApplyPlayer))
                {
                    if (GUILayout.Button("读取到玩家")) ApplyToPlayer(selectedPreset, playerItems, playerBackpack);
                }
                using (new EditorGUI.DisabledScope(!canApplyEnemy))
                {
                    if (GUILayout.Button("读取到敌人")) bridge.ApplyEnemyDeckPreset(selectedPreset);
                }
            }

            if (bridge?.Snapshot.HasTarget == true) DrawRuntimeItems("当前玩家背包", bridge.Snapshot);
            if (bridge?.EnemySnapshot.HasTarget == true) DrawRuntimeItems("当前敌人背包", bridge.EnemySnapshot);
        }

        private static void ApplyToPlayer(DeckPreset preset, PlayerItemSystem playerItems, PlayerBackpackSystem playerBackpack)
        {
            if (playerBackpack != null)
            {
                playerBackpack.ApplyDeckPreset(preset);
                return;
            }

            if (preset != null && preset.IsValid(out _) &&
                playerItems.TryApplyDeck(preset.Slots) == PlayerDeckResult.Success)
            {
                PlayerItemHangarPresenter presenter = UnityEngine.Object.FindAnyObjectByType<PlayerItemHangarPresenter>(FindObjectsInactive.Include);
                presenter?.Refresh();
            }
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
