using BackpackHero.Input;
using UnityEditor;
using UnityEngine;

internal sealed class HorizontalSwipeCurveDebugWindow : ScriptableObject
{
    internal void DrawTab()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("左右滑动曲线调试", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("请进入 Play Mode。调试 Runtime 启动后，本窗口会自动打开。", MessageType.Info);
            return;
        }

        if (!HorizontalSwipeCurveDebugBridge.HasTarget)
        {
            EditorGUILayout.HelpBox("正在等待 HorizontalSwipeCurveDebugRuntime，场景切换或脚本重编译期间可能暂时不可用。", MessageType.Warning);
            return;
        }

        var target = HorizontalSwipeCurveDebugBridge.Target;
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Runtime Target", target, typeof(HorizontalSwipeCurveInput), true);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(8f);
        DrawCurrentValue(HorizontalSwipeCurveDebugBridge.CurrentValue);

        EditorGUILayout.Space(8f);
        var inputEnabled = EditorGUILayout.Toggle("启用滑动输入", HorizontalSwipeCurveDebugBridge.IsInputEnabled);
        if (inputEnabled != HorizontalSwipeCurveDebugBridge.IsInputEnabled)
        {
            HorizontalSwipeCurveDebugBridge.SetInputEnabled(inputEnabled);
        }

        var sensitivity = EditorGUILayout.Slider(
            "滑动灵敏度",
            HorizontalSwipeCurveDebugBridge.Sensitivity,
            0f,
            50f);
        if (!Mathf.Approximately(sensitivity, HorizontalSwipeCurveDebugBridge.Sensitivity))
        {
            HorizontalSwipeCurveDebugBridge.SetSensitivity(sensitivity);
        }

        EditorGUILayout.Space(10f);
        EditorGUI.BeginDisabledGroup(!HorizontalSwipeCurveDebugBridge.IsInputEnabled);
        if (GUILayout.Button("数值归零", GUILayout.Height(28f)))
        {
            HorizontalSwipeCurveDebugBridge.ResetValue();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(12f);
        DrawCurveShapeControls();

        EditorGUILayout.Space(10f);
        EditorGUILayout.HelpBox(
            "在 Game 窗口空白区域按住鼠标左右拖动。右滑增加，左滑减少；从 UI 上开始的拖动会被忽略。",
            MessageType.Info);
    }

    private static void DrawCurveShapeControls()
    {
        EditorGUILayout.LabelField("曲线形状", EditorStyles.boldLabel);

        if (!HorizontalSwipeCurveDebugBridge.HasCurveTarget)
        {
            EditorGUILayout.HelpBox("当前场景中没有可调试的曲线对象。", MessageType.Warning);
            return;
        }

        var maximumBend = EditorGUILayout.Slider(
            "最大弯曲距离",
            HorizontalSwipeCurveDebugBridge.MaxBendDistance,
            0f,
            10f);
        if (!Mathf.Approximately(maximumBend, HorizontalSwipeCurveDebugBridge.MaxBendDistance))
        {
            HorizontalSwipeCurveDebugBridge.SetMaxBendDistance(maximumBend);
        }

        var segmentCount = EditorGUILayout.IntSlider(
            "曲线采样数",
            HorizontalSwipeCurveDebugBridge.SegmentCount,
            CurvedConnectionRenderer.MinSegmentCount,
            CurvedConnectionRenderer.MaxSegmentCount);
        if (segmentCount != HorizontalSwipeCurveDebugBridge.SegmentCount)
        {
            HorizontalSwipeCurveDebugBridge.SetSegmentCount(segmentCount);
        }
    }

    private static void DrawCurrentValue(float value)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("当前曲线变量", value.ToString("0.000"));
        var rect = EditorGUILayout.GetControlRect(false, 22f);
        EditorGUI.ProgressBar(rect, (value + 1f) * 0.5f, $"-1    {value:0.000}    1");
        EditorGUILayout.EndVertical();
    }
}
