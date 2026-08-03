using BackpackHero.Background;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    internal sealed class BackgroundPhysicsDebugWindow : ScriptableObject
    {
        internal void DrawTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("星球互动物理调试", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后，面板会自动连接当前星图背景。", MessageType.Info);
                return;
            }

            var runtime = BackgroundPhysicsDebugRuntime.Instance;
            if (runtime == null)
            {
                EditorGUILayout.HelpBox("正在等待 SampleScene 的星图运行时。", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("已连接星球", runtime.PlanetCount.ToString());
            DrawPlanetSettings(runtime);
            EditorGUILayout.Space(10f);
            DrawTouchSettings(runtime);
        }

        private static void DrawPlanetSettings(BackgroundPhysicsDebugRuntime runtime)
        {
            EditorGUILayout.LabelField("星球运动", EditorStyles.boldLabel);
            var settings = runtime.PlanetSettings;
            settings.DriftRadius = EditorGUILayout.Slider("漂浮半径", settings.DriftRadius, 0f, 1f);
            settings.DriftSpeed = EditorGUILayout.Slider("漂浮速度", settings.DriftSpeed, 0f, 1f);
            settings.SpringStrength = EditorGUILayout.Slider("回弹强度", settings.SpringStrength, 0f, 20f);
            settings.Damping = EditorGUILayout.Slider("阻尼", settings.Damping, 0f, 20f);
            settings.MaxDistance = EditorGUILayout.Slider("最大活动距离", settings.MaxDistance, .05f, 3f);
            settings.MaxSpeed = EditorGUILayout.Slider("最大速度", settings.MaxSpeed, .05f, 10f);
            runtime.SetPlanetSettings(settings);
        }

        private static void DrawTouchSettings(BackgroundPhysicsDebugRuntime runtime)
        {
            EditorGUILayout.LabelField("点击扰动", EditorStyles.boldLabel);
            var settings = runtime.TouchSettings;
            settings.InfluenceRadius = EditorGUILayout.Slider("影响半径", settings.InfluenceRadius, .1f, 5f);
            settings.MinimumRandomImpulse = EditorGUILayout.Slider("最小随机冲量", settings.MinimumRandomImpulse, 0f, 5f);
            settings.MaximumRandomImpulse = EditorGUILayout.Slider("最大随机冲量", settings.MaximumRandomImpulse, 0f, 5f);
            settings.InteractionCooldown = EditorGUILayout.Slider("触发冷却", settings.InteractionCooldown, 0f, 3f);
            settings.RearmSpeed = EditorGUILayout.Slider("再次触发速度阈值", settings.RearmSpeed, 0f, 1f);
            runtime.SetTouchSettings(settings);
        }
    }
}
