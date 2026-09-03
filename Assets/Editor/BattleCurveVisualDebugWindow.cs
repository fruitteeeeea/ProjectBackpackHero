using BackpackHero.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackHero.EditorTools
{
    /// <summary>编辑、预览并保存 SampleScene 战斗曲线脉冲效果。</summary>
    internal sealed class BattleCurveVisualDebugWindow : ScriptableObject
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private readonly DebugDraft<BattleCurvePulseSettings> draft = new();
        private BattleCurvePulseRenderer lastTarget;
        private BattleCurvePulseSettings savedSettings;

        internal void DrawTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("战斗曲线视觉效果", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 SampleScene 的 Play Mode 后可调整并保存战斗脉冲曲线。",
                    MessageType.Info);
                return;
            }

            BattleCurvePulseRenderer target =
                BattleCurvePulseRenderer.ActiveInstance;
            if (target == null)
            {
                EditorGUILayout.HelpBox("正在等待 Pulse Line 的战斗脉冲组件启动。",
                    MessageType.Warning);
                return;
            }

            SyncTarget(target);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("脉冲目标", target,
                typeof(BattleCurvePulseRenderer), true);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.LabelField("场景", target.gameObject.scene.path,
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField("未保存修改", draft.IsDirty ? "是" : "否",
                EditorStyles.miniLabel);

            DrawSettings(target);
            DrawPersistence(target);
        }

        private void SyncTarget(BattleCurvePulseRenderer target)
        {
            if (target == lastTarget)
            {
                return;
            }

            lastTarget = target;
            savedSettings = target.Settings;
            draft.Load(savedSettings);
        }

        private void DrawSettings(BattleCurvePulseRenderer target)
        {
            BattleCurvePulseSettings current = draft.Value;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("脉冲时序", EditorStyles.boldLabel);
            float travelDuration = Mathf.Max(
                BattleCurvePulseSettings.MinimumTravelDuration,
                EditorGUILayout.FloatField("移动时长（秒）",
                    current.TravelDuration));
            float endFadeDuration = Mathf.Max(
                BattleCurvePulseSettings.MinimumEndFadeDuration,
                EditorGUILayout.FloatField("末端淡出时长（秒）",
                    current.EndFadeDuration));
            float cooldownDuration = Mathf.Max(
                BattleCurvePulseSettings.MinimumCooldownDuration,
                EditorGUILayout.FloatField("冷却间隔（秒）",
                    current.CooldownDuration));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("脉冲形状", EditorStyles.boldLabel);
            float pulseLength = EditorGUILayout.Slider("脉冲长度",
                current.PulseLength,
                BattleCurvePulseSettings.MinimumPulseLength,
                BattleCurvePulseSettings.MaximumPulseLength);
            int sampleCount = EditorGUILayout.IntSlider("曲线采样数",
                current.SampleCount,
                BattleCurvePulseRenderer.MinimumSampleCount,
                BattleCurvePulseRenderer.MaximumSampleCount);
            float pulseWidth = Mathf.Max(
                BattleCurvePulseSettings.MinimumPulseWidth,
                EditorGUILayout.FloatField("线宽", current.PulseWidth));

            BattleCurvePulseSettings next = new(travelDuration,
                endFadeDuration, cooldownDuration, pulseLength, sampleCount,
                pulseWidth);
            if (!next.Equals(current))
            {
                draft.Value = next;
                target.SetSettings(next);
            }
        }

        private void DrawPersistence(BattleCurvePulseRenderer target)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("配置操作", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!draft.IsDirty))
                {
                    if (GUILayout.Button("保存"))
                    {
                        SaveToScene(target);
                    }
                }

                if (GUILayout.Button("还原"))
                {
                    target.SetSettings(savedSettings);
                    draft.Load(savedSettings);
                }
            }
        }

        private void SaveToScene(BattleCurvePulseRenderer target)
        {
            Scene scene = target.gameObject.scene;
            if (!scene.IsValid() || scene.path != SampleScenePath)
            {
                Debug.LogError("只能将战斗曲线脉冲参数保存到 SampleScene。");
                return;
            }

            Undo.RecordObject(target, "保存战斗曲线脉冲参数");
            target.SetSettings(draft.Value);
            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            savedSettings = draft.Value;
            draft.Load(savedSettings);
        }
    }
}
