using UnityEditor;
using UnityEngine;

public class RunningProgressWindow : EditorWindow
{
    private const float Duration = 3f;

    private double startTime;

    [MenuItem("Tools/Running Progress Window")]
    private static void Open()
    {
        var window = GetWindow<RunningProgressWindow>();
        window.titleContent = new GUIContent("Progress");
        window.minSize = new Vector2(360f, 150f);
        window.Show();
    }

    private void OnEnable()
    {
        startTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        // 让窗口持续刷新
        Repaint();
    }

    private void OnGUI()
    {
        double elapsed = EditorApplication.timeSinceStartup - startTime;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(
            "3 秒循环进度",
            EditorStyles.boldLabel
        );
        EditorGUILayout.Space(5);

        DrawProgressBar(
            "任务 1",
            GetLoopProgress(elapsed, 0f)
        );

        EditorGUILayout.Space(8);

        DrawProgressBar(
            "任务 2",
            GetLoopProgress(elapsed, 1f)
        );

        EditorGUILayout.Space(8);

        DrawProgressBar(
            "任务 3",
            GetLoopProgress(elapsed, 2f)
        );
    }

    private static float GetLoopProgress(
        double elapsed,
        float phaseOffset
    )
    {
        // 每个进度条周期都是 3 秒。
        // phaseOffset 分别为 0、1、2 秒。
        return Mathf.Repeat(
            (float)elapsed + phaseOffset,
            Duration
        ) / Duration;
    }

    private static void DrawProgressBar(
        string label,
        float progress
    )
    {
        Rect rect = EditorGUILayout.GetControlRect(
            false,
            24f
        );

        EditorGUI.ProgressBar(
            rect,
            progress,
            $"{label}  {progress * 100f:0}%"
        );
    }
}