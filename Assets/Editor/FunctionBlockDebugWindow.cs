using BackpackHero.Debugging;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>Program-test controls for hiding incomplete main-menu features.</summary>
    internal sealed class FunctionBlockDebugWindow : ScriptableObject
    {
        private FunctionBlockSettings settingsTarget;
        private FunctionBlockRuntime lastRuntime;
        private bool blockMilestone;
        private bool blockPack;

        internal void DrawTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("功能屏蔽", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying || FunctionBlockRuntime.Instance == null)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后可修改主界面功能屏蔽状态。", MessageType.Info);
                return;
            }

            FunctionBlockRuntime runtime = FunctionBlockRuntime.Instance;
            SyncRuntime(runtime);
            DrawTarget(runtime);
            EditorGUILayout.Space(8f);
            bool nextMilestone = EditorGUILayout.Toggle("屏蔽里程碑", blockMilestone);
            bool nextPack = EditorGUILayout.Toggle("屏蔽卡包", blockPack);
            if (nextMilestone != blockMilestone || nextPack != blockPack)
            {
                blockMilestone = nextMilestone;
                blockPack = nextPack;
                runtime.SetValues(blockMilestone, blockPack);
            }

            EditorGUILayout.HelpBox("屏蔽里程碑会阻止主界面星球打开里程碑页；屏蔽卡包会隐藏主界面卡包和关闭已打开的卡包界面。", MessageType.None);
            DrawActions(runtime);
        }

        private void SyncRuntime(FunctionBlockRuntime runtime)
        {
            if (lastRuntime == runtime) return;
            lastRuntime = runtime;
            settingsTarget = runtime.DefaultSettings;
            Load(settingsTarget, runtime);
        }

        private void DrawTarget(FunctionBlockRuntime runtime)
        {
            FunctionBlockSettings next = (FunctionBlockSettings)EditorGUILayout.ObjectField(
                "保存目标", settingsTarget, typeof(FunctionBlockSettings), false);
            if (next != settingsTarget)
            {
                settingsTarget = next;
                Load(settingsTarget, runtime);
            }
            EditorGUILayout.LabelField("资产路径", settingsTarget != null ? AssetDatabase.GetAssetPath(settingsTarget) : "未选择（请使用“另存为”创建配置）", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("未保存修改", HasUnsavedChanges ? "是" : "否", EditorStyles.miniLabel);
        }

        private void DrawActions(FunctionBlockRuntime runtime)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("配置操作", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用到运行时"))
                {
                    if (settingsTarget != null) runtime.SetDefaultSettings(settingsTarget);
                    runtime.SetValues(blockMilestone, blockPack);
                }
                using (new EditorGUI.DisabledScope(settingsTarget == null || !HasUnsavedChanges))
                    if (GUILayout.Button("保存")) SaveToTarget();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("另存为")) SaveAs(runtime);
                if (GUILayout.Button("还原")) Load(settingsTarget, runtime);
            }
        }

        private bool HasUnsavedChanges => settingsTarget == null ||
            settingsTarget.BlockMilestone != blockMilestone || settingsTarget.BlockPack != blockPack;

        private void Load(FunctionBlockSettings settings, FunctionBlockRuntime runtime)
        {
            blockMilestone = settings != null ? settings.BlockMilestone : runtime.BlockMilestone;
            blockPack = settings != null ? settings.BlockPack : runtime.BlockPack;
        }

        private void SaveToTarget()
        {
            Undo.RecordObject(settingsTarget, "保存功能屏蔽配置");
            settingsTarget.SetValues(blockMilestone, blockPack);
            EditorUtility.SetDirty(settingsTarget);
            AssetDatabase.SaveAssets();
        }

        private void SaveAs(FunctionBlockRuntime runtime)
        {
            string path = EditorUtility.SaveFilePanelInProject("另存为功能屏蔽配置", "FunctionBlock", "asset", "选择玩法配置位置");
            if (string.IsNullOrEmpty(path)) return;
            FunctionBlockSettings settings = CreateInstance<FunctionBlockSettings>();
            settings.SetValues(blockMilestone, blockPack);
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            settingsTarget = settings;
            runtime.SetDefaultSettings(settings);
            runtime.SetValues(blockMilestone, blockPack);
        }
    }
}
