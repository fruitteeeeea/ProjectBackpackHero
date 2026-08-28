using BackpackHero.Debugging;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>视效调整页：编辑、应用并持久化美术资源选择。</summary>
    internal sealed class ArtAssetDebugWindow : ScriptableObject
    {
        private readonly DebugDraft<ArtAssetDebugSettingsValue> draft = new();
        private ArtAssetDebugSettings settingsTarget;
        private ArtAssetDebugRuntime lastRuntime;

        internal void DrawTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("美术资源调整", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后可实时切换飞船素材。", MessageType.Info);
                return;
            }

            ArtAssetDebugRuntime runtime = ArtAssetDebugRuntime.Instance;
            if (runtime == null)
            {
                EditorGUILayout.HelpBox("正在等待美术资源控制器启动。", MessageType.Warning);
                return;
            }

            SyncRuntime(runtime);
            DrawSettingsTarget();
            DrawShipStyleButtons(runtime);
            DrawPersistence(runtime);
        }

        private void SyncRuntime(ArtAssetDebugRuntime runtime)
        {
            if (runtime == lastRuntime) return;
            lastRuntime = runtime;
            settingsTarget = runtime.DefaultSettings;
            draft.Load(settingsTarget != null
                ? settingsTarget.GetValues()
                : runtime.Settings);
        }

        private void DrawSettingsTarget()
        {
            ArtAssetDebugSettings nextTarget =
                (ArtAssetDebugSettings)EditorGUILayout.ObjectField(
                    "保存目标", settingsTarget, typeof(ArtAssetDebugSettings), false);
            if (nextTarget != settingsTarget)
            {
                settingsTarget = nextTarget;
                draft.Load(settingsTarget != null
                    ? settingsTarget.GetValues()
                    : ArtAssetDebugSettingsValue.Default);
            }

            EditorGUILayout.LabelField("资产路径", settingsTarget != null
                ? AssetDatabase.GetAssetPath(settingsTarget)
                : "未选择（请使用“另存为”创建配置）", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("未保存修改", draft.IsDirty ? "是" : "否",
                EditorStyles.miniLabel);
        }

        private void DrawShipStyleButtons(ArtAssetDebugRuntime runtime)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("飞船素材调整", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("玩家和敌人背包会同时切换，且每个背包只启用一种飞船素材。",
                MessageType.None);
            BackpackShipStyle style = draft.Value.BackpackShipStyle;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(style == BackpackShipStyle.Style1, "样式1", "Button") &&
                    style != BackpackShipStyle.Style1)
                {
                    SelectShipStyle(runtime, BackpackShipStyle.Style1);
                }

                if (GUILayout.Toggle(style == BackpackShipStyle.Style2, "样式2", "Button") &&
                    style != BackpackShipStyle.Style2)
                {
                    SelectShipStyle(runtime, BackpackShipStyle.Style2);
                }
            }
        }

        private void SelectShipStyle(
            ArtAssetDebugRuntime runtime, BackpackShipStyle style)
        {
            draft.Value = draft.Value.WithBackpackShipStyle(style);
            runtime.SetSettings(draft.Value);
        }

        private void DrawPersistence(ArtAssetDebugRuntime runtime)
        {
            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用到运行时"))
                {
                    if (settingsTarget != null) runtime.SetDefaultSettings(settingsTarget);
                    runtime.SetSettings(draft.Value);
                }

                using (new EditorGUI.DisabledScope(settingsTarget == null || !draft.IsDirty))
                {
                    if (GUILayout.Button("保存")) SaveToTarget(settingsTarget);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("另存为")) SaveAs(runtime);
                if (GUILayout.Button("还原")) draft.Load(settingsTarget != null
                    ? settingsTarget.GetValues()
                    : runtime.Settings);
            }
        }

        private void SaveAs(ArtAssetDebugRuntime runtime)
        {
            string path = EditorUtility.SaveFilePanelInProject("另存为美术资源配置",
                "ArtAssetDebugSettings", "asset", "选择美术资源配置位置");
            if (string.IsNullOrEmpty(path)) return;
            ArtAssetDebugSettings target = CreateInstance<ArtAssetDebugSettings>();
            target.SetValues(draft.Value);
            AssetDatabase.CreateAsset(target, path);
            AssetDatabase.SaveAssets();
            settingsTarget = target;
            draft.Load(target.GetValues());
            runtime.SetDefaultSettings(target);
            runtime.SetSettings(draft.Value);
        }

        private void SaveToTarget(ArtAssetDebugSettings target)
        {
            Undo.RecordObject(target, "保存美术资源配置");
            target.SetValues(draft.Value);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            draft.Load(target.GetValues());
        }
    }
}
