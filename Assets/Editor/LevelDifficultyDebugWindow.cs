using BackpackHero.Battle;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>玩法设计页：编辑关卡倍率并显式应用或持久化。</summary>
    internal sealed class LevelDifficultyDebugWindow : ScriptableObject
    {
        private LevelDifficultySettings target;
        private LevelDifficultySettings draft;
        private LevelDifficultyRuntime lastRuntime;
        private string validationMessage;

        internal void DrawTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("关卡配置", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后可编辑、应用和保存关卡配置。", MessageType.Info);
                return;
            }

            LevelDifficultyRuntime runtime = LevelDifficultyRuntime.Instance;
            if (runtime == null || runtime.Settings == null)
            {
                EditorGUILayout.HelpBox("未找到 Resources/LevelDifficultySettings 配置资产。", MessageType.Warning);
                return;
            }

            Sync(runtime);
            LevelDebugWindow.DrawInfo(LevelFlowController.EnsureInstance());
            DrawTarget(runtime);
            DrawPlayerMultipliers();
            DrawBackpackRoundHealthMultipliers();
            DrawEnemyTable();
            DrawValidation();
            DrawPersistence(runtime);
        }

        private void Sync(LevelDifficultyRuntime runtime)
        {
            if (runtime == lastRuntime) return;
            lastRuntime = runtime;
            target = runtime.Settings;
            CreateOrLoadDraft(target);
        }

        private void DrawTarget(LevelDifficultyRuntime runtime)
        {
            EditorGUILayout.Space(8f);
            LevelDifficultySettings next = (LevelDifficultySettings)EditorGUILayout.ObjectField("保存目标", target, typeof(LevelDifficultySettings), false);
            if (next != target)
            {
                target = next;
                CreateOrLoadDraft(target != null ? target : runtime.Settings);
                validationMessage = null;
            }
            EditorGUILayout.LabelField("资产路径", target != null ? AssetDatabase.GetAssetPath(target) : "未选择", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("未保存修改", HasUnsavedChanges ? "是" : "否", EditorStyles.miniLabel);
        }

        private void DrawPlayerMultipliers()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("玩家养成倍率（全局）", EditorStyles.boldLabel);
            float health = Mathf.Max(.01f, EditorGUILayout.FloatField("玩家飞机生命", draft.PlayerAircraftHealthMultiplier));
            float damage = Mathf.Max(.01f, EditorGUILayout.FloatField("玩家飞机伤害", draft.PlayerAircraftDamageMultiplier));
            draft.SetPlayerMultipliers(health, damage);
        }

        private void DrawEnemyTable()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("敌人强度（每关前三个阶段档位）", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("关卡阶段", GUILayout.Width(150));
                GUILayout.Label("敌人生命倍率", GUILayout.Width(115));
                GUILayout.Label("敌人伤害倍率", GUILayout.Width(115));
            }

            string[] stageLabels = { "阶段1", "阶段2", "阶段3～5" };
            for (int level = 1; level <= LevelDifficultySettings.LevelCount; level++)
            {
                for (int stage = 0; stage < LevelDifficultySettings.StageCount; stage++)
                {
                    bool currentLevel = level == LevelManager.CurrentLevel;
                    bool activeStage = currentLevel && stage == LevelDifficultySettings.GetStageIndex(LevelFlowController.CurrentRound);
                    Color old = GUI.backgroundColor;
                    if (activeStage) GUI.backgroundColor = new Color(1f, .92f, .42f);
                    else if (currentLevel) GUI.backgroundColor = new Color(.78f, .9f, 1f);
                    EnemyStrengthMultipliers value = draft.GetEnemyStrength(level, stage + 1);
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label($"关卡{level} {stageLabels[stage]}", GUILayout.Width(150));
                        float health = Mathf.Max(.01f, EditorGUILayout.FloatField(value.Health, GUILayout.Width(115)));
                        float damage = Mathf.Max(.01f, EditorGUILayout.FloatField(value.Damage, GUILayout.Width(115)));
                        draft.SetEnemyStrength(level, stage, health, damage);
                    }
                    GUI.backgroundColor = old;
                }
            }
            EditorGUILayout.HelpBox("蓝色表示当前关卡；黄色表示当前回合实际使用的强度档位。", MessageType.None);
        }

        private void DrawBackpackRoundHealthMultipliers()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "回合背包血量倍率（双方）",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "最终背包生命 = 基础生命 × 现有阵营倍率 × 回合倍率。",
                MessageType.None);

            for (int round = 1;
                 round <=
                 LevelDifficultySettings
                     .BackpackRoundMultiplierCount;
                 round++)
            {
                float value = Mathf.Max(
                    .01f,
                    EditorGUILayout.FloatField(
                        $"回合 {round}",
                        draft.GetBackpackRoundHealthMultiplier(
                            round)));
                draft.SetBackpackRoundHealthMultiplier(
                    round,
                    value);
            }
        }

        private void DrawValidation()
        {
            if (!string.IsNullOrEmpty(validationMessage)) EditorGUILayout.HelpBox(validationMessage, MessageType.Error);
        }

        private void DrawPersistence(LevelDifficultyRuntime runtime)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("配置操作", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用到运行时") && ValidateDraft())
                {
                    if (target != null) runtime.SetSettings(target);
                    runtime.ApplyValues(draft);
                }
                using (new EditorGUI.DisabledScope(target == null || !HasUnsavedChanges))
                    if (GUILayout.Button("保存")) SaveToTarget(runtime);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("另存为")) SaveAs(runtime);
                if (GUILayout.Button("还原")) { CreateOrLoadDraft(target != null ? target : runtime.Settings); validationMessage = null; }
            }
        }

        private bool HasUnsavedChanges => draft != null && target != null && !draft.ContentEquals(target);
        private bool ValidateDraft()
        {
            if (draft == null) { validationMessage = "配置草稿不可用。"; return false; }
            validationMessage = null;
            return true;
        }

        private void SaveToTarget(LevelDifficultyRuntime runtime)
        {
            if (target == null || !ValidateDraft()) return;
            Undo.RecordObject(target, "保存关卡配置");
            target.CopyFrom(draft);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            runtime.SetSettings(target);
            CreateOrLoadDraft(target);
        }

        private void SaveAs(LevelDifficultyRuntime runtime)
        {
            if (!ValidateDraft()) return;
            string path = EditorUtility.SaveFilePanelInProject("另存为关卡配置", "LevelDifficultySettings", "asset", "选择关卡配置位置");
            if (string.IsNullOrEmpty(path)) return;
            LevelDifficultySettings saved = CreateInstance<LevelDifficultySettings>();
            saved.CopyFrom(draft);
            AssetDatabase.CreateAsset(saved, path);
            AssetDatabase.SaveAssets();
            target = saved;
            runtime.SetSettings(saved);
            CreateOrLoadDraft(saved);
        }

        private void CreateOrLoadDraft(LevelDifficultySettings source)
        {
            if (draft == null) draft = CreateInstance<LevelDifficultySettings>();
            draft.CopyFrom(source);
        }

        private void OnDisable()
        {
            if (draft != null) DestroyImmediate(draft);
        }
    }
}
