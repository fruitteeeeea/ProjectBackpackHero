using BackpackHero.Debugging;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>程序测试页：编辑、应用并持久化背包交互动效配置。</summary>
    internal sealed class BackpackVisualDebugWindow : ScriptableObject
    {
        private readonly DebugDraft<BackpackVisualSettings> draft = new();
        private BackpackVisualDebugSettings settingsTarget;
        private BackpackVisualDebugRuntime lastRuntime;
        private Vector2 scrollPosition;

        internal void DrawTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("全局背包动效", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后，面板会连接全局背包动效控制器。",
                    MessageType.Info);
                return;
            }

            BackpackVisualDebugRuntime runtime = BackpackVisualDebugRuntime.Instance;
            if (runtime == null)
            {
                EditorGUILayout.HelpBox("正在等待全局背包动效控制器启动。",
                    MessageType.Warning);
                return;
            }

            SyncRuntime(runtime);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawOverrideToggle();
            DrawSettingsTarget();
            DrawVisualSettings();
            DrawPersistence(runtime);
            EditorGUILayout.EndScrollView();
        }

        private void SyncRuntime(BackpackVisualDebugRuntime runtime)
        {
            if (runtime == lastRuntime) return;
            lastRuntime = runtime;
            settingsTarget = runtime.DefaultSettings;
            draft.Load(settingsTarget != null
                ? settingsTarget.GetValues()
                : runtime.Settings);
        }

        private void DrawOverrideToggle()
        {
            EditorGUILayout.Space(6f);
            draft.Value = draft.Value.WithOverridesEnabled(EditorGUILayout.Toggle(
                "启用背包动效调试覆写", draft.Value.OverridesEnabled));
        }

        private void DrawSettingsTarget()
        {
            EditorGUILayout.Space(6f);
            BackpackVisualDebugSettings nextTarget =
                (BackpackVisualDebugSettings)EditorGUILayout.ObjectField(
                    "保存目标", settingsTarget,
                    typeof(BackpackVisualDebugSettings), false);
            if (nextTarget != settingsTarget)
            {
                settingsTarget = nextTarget;
                draft.Load(settingsTarget != null
                    ? settingsTarget.GetValues()
                    : BackpackVisualSettings.Default);
            }

            EditorGUILayout.LabelField("资产路径", settingsTarget != null
                ? AssetDatabase.GetAssetPath(settingsTarget)
                : "未选择（请使用“另存为”创建配置）", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("未保存修改", draft.IsDirty ? "是" : "否",
                EditorStyles.miniLabel);
        }

        private void DrawVisualSettings()
        {
            BackpackVisualSettings current = draft.Value;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("拖起物品", EditorStyles.boldLabel);
            float dragOpacity = EditorGUILayout.Slider("拖动物品透明度",
                current.DragOpacity, 0f, 1f);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("拖动格子预览", EditorStyles.boldLabel);
            Color legalPreviewColor = EditorGUILayout.ColorField("可放置/合成颜色",
                current.LegalPreviewColor);
            Color illegalPreviewColor = EditorGUILayout.ColorField("不可放置颜色",
                current.IllegalPreviewColor);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("拖动同类可合成物品", EditorStyles.boldLabel);
            float mergeFlashMinimum = EditorGUILayout.Slider("闪烁最小强度",
                current.MergeFlashMinimum, 0f, 1f);
            float mergeFlashMaximum = EditorGUILayout.Slider("闪烁最大强度",
                current.MergeFlashMaximum, 0f, 1f);
            float mergeFlashCycleDuration = Mathf.Max(
                BackpackVisualSettings.MinimumFlashDuration,
                EditorGUILayout.FloatField("正弦往返循环时间", current.MergeFlashCycleDuration));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("放置成功/移动成功", EditorStyles.boldLabel);
            float placementScaleMultiplier = Mathf.Max(
                BackpackVisualSettings.MinimumPlacementScale,
                EditorGUILayout.FloatField("放大尺寸", current.PlacementScaleMultiplier));
            float placementPositiveRotationDegrees = EditorGUILayout.FloatField(
                "正向旋转角度", current.PlacementPositiveRotationDegrees);
            float placementNegativeRotationDegrees = EditorGUILayout.FloatField(
                "反向旋转角度", current.PlacementNegativeRotationDegrees);
            BackpackPlacementScaleEase placementScaleEase =
                (BackpackPlacementScaleEase)EditorGUILayout.EnumPopup(
                    "缩放 Tween 插值类型", current.PlacementScaleEase);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("从背包拖回商店", EditorStyles.boldLabel);
            float shopFlightDuration = Mathf.Max(
                BackpackVisualSettings.MinimumShopFlightDuration,
                EditorGUILayout.FloatField("飞行时间", current.ShopFlightDuration));

            draft.Value = new BackpackVisualSettings(
                current.OverridesEnabled, dragOpacity, legalPreviewColor,
                illegalPreviewColor, mergeFlashMinimum, mergeFlashMaximum,
                mergeFlashCycleDuration, placementScaleMultiplier,
                placementPositiveRotationDegrees,
                placementNegativeRotationDegrees, placementScaleEase,
                shopFlightDuration);
        }

        private void DrawPersistence(BackpackVisualDebugRuntime runtime)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("配置操作", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用到运行时"))
                {
                    if (settingsTarget != null) runtime.SetDefaultSettings(settingsTarget);
                    runtime.SetSettings(draft.Value);
                }

                using (new EditorGUI.DisabledScope(settingsTarget == null ||
                    !draft.IsDirty))
                {
                    if (GUILayout.Button("保存")) SaveToTarget(settingsTarget);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("另存为")) SaveAs(runtime);
                if (GUILayout.Button("还原")) draft.Load(settingsTarget != null
                    ? settingsTarget.GetValues() : runtime.Settings);
            }
        }

        private void SaveAs(BackpackVisualDebugRuntime runtime)
        {
            string path = EditorUtility.SaveFilePanelInProject("另存为背包动效配置",
                "BackpackVisualDebugSettings", "asset", "选择背包动效配置位置");
            if (string.IsNullOrEmpty(path)) return;
            BackpackVisualDebugSettings newSettings =
                CreateInstance<BackpackVisualDebugSettings>();
            newSettings.SetValues(draft.Value);
            AssetDatabase.CreateAsset(newSettings, path);
            AssetDatabase.SaveAssets();
            settingsTarget = newSettings;
            draft.Load(newSettings.GetValues());
            runtime.SetDefaultSettings(newSettings);
            runtime.SetSettings(draft.Value);
        }

        private void SaveToTarget(BackpackVisualDebugSettings target)
        {
            Undo.RecordObject(target, "保存背包动效配置");
            target.SetValues(draft.Value);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            draft.Load(target.GetValues());
        }
    }
}
