using System.IO;
using BackpackHero.Battle;
using BackpackHero.Debugging;
using BackpackHero.Config;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>平衡页的 Editor 侧保存逻辑；测试方案和正式关卡配置彼此独立。</summary>
    internal sealed class BalanceAdjustmentPersistence
    {
        private const string TestFolder = "Assets/TestPresets/BalanceAdjustment";
        private const string DefaultTestPath = TestFolder + "/BalanceAdjustmentDefault.asset";
        private BalanceAdjustmentTestPreset testTarget;
        private BalanceAdjustmentTestPreset testDraft;
        private LevelDifficultySettings difficultyTarget;
        private LevelDifficultySettings difficultyDraft;
        private LevelDifficultyRuntime lastDifficultyRuntime;
        private string message;

        public void DrawTestToolbar()
        {
            EnsureTestDraft();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("平衡测试方案（仅调试复现）", EditorStyles.boldLabel);
                BalanceAdjustmentTestPreset next = (BalanceAdjustmentTestPreset)EditorGUILayout.ObjectField("保存目标", testTarget, typeof(BalanceAdjustmentTestPreset), false);
                if (next != null && next != testTarget) { testTarget = next; testDraft.CopyFrom(next); message = null; }
                EditorGUILayout.LabelField("资产路径", AssetDatabase.GetAssetPath(testTarget), EditorStyles.miniLabel);
                EditorGUI.BeginChangeCheck();
                BalanceProfile profile = (BalanceProfile)EditorGUILayout.EnumPopup(
                    "数值 Profile", testDraft.BalanceProfile);
                if (EditorGUI.EndChangeCheck())
                {
                    testDraft.SetBalanceProfile(profile);
                    message = profile == BalanceProfile.Legacy
                        ? "Legacy 保留当前旧版数值；应用时会在准备阶段切换。"
                        : "Proposed 使用首轮商业化调优数值；应用时会在准备阶段切换。";
                }
                EditorGUILayout.LabelField("未保存修改", !testDraft.ContentEquals(testTarget) ? "是" : "否", EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!CanApplyTestPreset()))
                        if (GUILayout.Button("从当前对局读取")) message = PlayerBackpackDebugBridge.Active.TryCaptureBalanceTestPreset(testDraft, out string error) ? null : error;
                    using (new EditorGUI.DisabledScope(!CanApplyTestPreset()))
                        if (GUILayout.Button("应用到运行时")) message = PlayerBackpackDebugBridge.Active.TryApplyBalanceTestPreset(testDraft, out string error) ? null : error;
                    using (new EditorGUI.DisabledScope(testDraft.ContentEquals(testTarget)))
                        if (GUILayout.Button("保存")) SaveTest();
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("另存为")) SaveTestAs();
                    if (GUILayout.Button("还原")) { testDraft.CopyFrom(testTarget); message = null; }
                }
                if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Warning);
            }
        }

        public void DrawPersistentEnemyStrength()
        {
            LevelDifficultyRuntime runtime = LevelDifficultyRuntime.Instance;
            if (runtime == null || runtime.Settings == null)
            {
                EditorGUILayout.HelpBox("等待正式关卡强度配置运行时。", MessageType.Info);
                return;
            }
            if (runtime != lastDifficultyRuntime)
            {
                lastDifficultyRuntime = runtime;
                difficultyTarget = runtime.Settings;
                if (difficultyDraft == null) difficultyDraft = ScriptableObject.CreateInstance<LevelDifficultySettings>();
                difficultyDraft.CopyFrom(difficultyTarget);
            }

            int level = LevelManager.CurrentLevel;
            int round = LevelFlowController.CurrentRound;
            int stage = LevelDifficultySettings.GetStageIndex(round);
            EnemyStrengthMultipliers current = difficultyDraft.GetEnemyStrength(level, round);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("敌人整体强度（正式关卡配置）", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("当前目标", $"关卡 {level} / 阶段 {stage + 1}", EditorStyles.miniLabel);
                if (!Mathf.Approximately(current.Health, current.Damage)) EditorGUILayout.HelpBox("当前生命与伤害倍率不同；调整后将统一为同一值。", MessageType.Info);
                EditorGUI.BeginChangeCheck();
                float value = EditorGUILayout.Slider("生命与伤害倍率", Mathf.Clamp((current.Health + current.Damage) * .5f, .5f, 1f), .5f, 1f);
                if (EditorGUI.EndChangeCheck()) difficultyDraft.SetEnemyStrength(level, stage, value, value);
                DrawDifficultyToolbar(runtime);
            }
        }

        public void Dispose()
        {
            if (testDraft != null) Object.DestroyImmediate(testDraft);
            if (difficultyDraft != null) Object.DestroyImmediate(difficultyDraft);
        }

        private static bool CanApplyTestPreset() => EditorApplication.isPlaying &&
            PlayerBackpackDebugBridge.Active?.Target?.IsReady == true &&
            PlayerBackpackDebugBridge.Active.EnemyTarget?.IsReady == true &&
            BattleFlowController.CurrentPhase == BattlePhase.Preparation;

        private void DrawDifficultyToolbar(LevelDifficultyRuntime runtime)
        {
            LevelDifficultySettings next = (LevelDifficultySettings)EditorGUILayout.ObjectField("保存目标", difficultyTarget, typeof(LevelDifficultySettings), false);
            if (next != null && next != difficultyTarget) { difficultyTarget = next; difficultyDraft.CopyFrom(next); }
            EditorGUILayout.LabelField("未保存修改", !difficultyDraft.ContentEquals(difficultyTarget) ? "是" : "否", EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用到运行时")) runtime.ApplyValues(difficultyDraft);
                using (new EditorGUI.DisabledScope(difficultyDraft.ContentEquals(difficultyTarget)))
                    if (GUILayout.Button("保存")) { Undo.RecordObject(difficultyTarget, "保存敌人关卡强度"); difficultyTarget.CopyFrom(difficultyDraft); EditorUtility.SetDirty(difficultyTarget); AssetDatabase.SaveAssets(); runtime.SetSettings(difficultyTarget); difficultyDraft.CopyFrom(difficultyTarget); }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("另存为")) SaveDifficultyAs(runtime);
                if (GUILayout.Button("还原")) difficultyDraft.CopyFrom(difficultyTarget);
            }
        }

        private void EnsureTestDraft()
        {
            if (testTarget == null)
            {
                EnsureFolder(TestFolder);
                testTarget = AssetDatabase.LoadAssetAtPath<BalanceAdjustmentTestPreset>(DefaultTestPath);
                if (testTarget == null) { testTarget = ScriptableObject.CreateInstance<BalanceAdjustmentTestPreset>(); AssetDatabase.CreateAsset(testTarget, DefaultTestPath); AssetDatabase.SaveAssets(); }
            }
            if (testDraft == null) { testDraft = ScriptableObject.CreateInstance<BalanceAdjustmentTestPreset>(); testDraft.CopyFrom(testTarget); }
        }

        private void SaveTest()
        {
            Undo.RecordObject(testTarget, "保存平衡测试方案"); testTarget.CopyFrom(testDraft); EditorUtility.SetDirty(testTarget); AssetDatabase.SaveAssets(); message = null;
        }

        private void SaveTestAs()
        {
            string path = EditorUtility.SaveFilePanelInProject("另存为平衡测试方案", "BalanceAdjustmentTestPreset", "asset", "选择测试预设位置");
            if (string.IsNullOrEmpty(path)) return;
            BalanceAdjustmentTestPreset asset = ScriptableObject.CreateInstance<BalanceAdjustmentTestPreset>(); asset.CopyFrom(testDraft); AssetDatabase.CreateAsset(asset, path); AssetDatabase.SaveAssets(); testTarget = asset; testDraft.CopyFrom(asset); message = null;
        }

        private void SaveDifficultyAs(LevelDifficultyRuntime runtime)
        {
            string path = EditorUtility.SaveFilePanelInProject("另存为关卡难度配置", "LevelDifficultySettings", "asset", "选择正式 Gameplay GameData 位置");
            if (string.IsNullOrEmpty(path)) return;
            LevelDifficultySettings asset = ScriptableObject.CreateInstance<LevelDifficultySettings>(); asset.CopyFrom(difficultyDraft); AssetDatabase.CreateAsset(asset, path); AssetDatabase.SaveAssets(); difficultyTarget = asset; difficultyDraft.CopyFrom(asset); runtime.SetSettings(asset);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
