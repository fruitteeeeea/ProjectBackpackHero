using BackpackHero.Background;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    public sealed class BackgroundPhysicsDebugWindow : EditorWindow
    {
        private double nextRepaintTime;

        [MenuItem("Tools/Debug/Constellation Physics")]
        public static void ShowFromMenu() => OpenWindow();

        internal static BackgroundPhysicsDebugWindow OpenWindow()
        {
            var window = GetWindow<BackgroundPhysicsDebugWindow>();
            window.titleContent = new GUIContent("Constellation Physics");
            window.minSize = new Vector2(360f, 410f);
            window.Show();
            return window;
        }

        internal static void CloseAllWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<BackgroundPhysicsDebugWindow>())
            {
                window.Close();
            }
        }

        private void Update()
        {
            if (EditorApplication.timeSinceStartup < nextRepaintTime)
            {
                return;
            }

            nextRepaintTime = EditorApplication.timeSinceStartup + .1d;
            Repaint();
        }

        private void OnGUI()
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
            EditorGUILayout.LabelField("划动反馈", EditorStyles.boldLabel);
            var settings = runtime.TouchSettings;
            settings.InfluenceRadius = EditorGUILayout.Slider("影响半径", settings.InfluenceRadius, .1f, 5f);
            settings.FollowStrength = EditorGUILayout.Slider("跟随强度", settings.FollowStrength, 0f, 2f);
            settings.ImpulseStrength = EditorGUILayout.Slider("惯性冲量", settings.ImpulseStrength, 0f, 8f);
            settings.MaxAppliedDelta = EditorGUILayout.Slider("单帧最大位移", settings.MaxAppliedDelta, .01f, 2f);
            runtime.SetTouchSettings(settings);
        }
    }
}
