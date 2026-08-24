using UnityEditor;
using UnityEngine;
using BackpackHero.Battle;

internal sealed class RuntimeDebugWindow : ScriptableObject
{
    private bool autoRepaint = true;
    internal void DrawTab()
    {
        EditorGUILayout.LabelField(
            "运行状态",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            "Playing",
            EditorApplication.isPlaying.ToString()
        );

        EditorGUILayout.LabelField(
            "Paused",
            EditorApplication.isPaused.ToString()
        );

        EditorGUILayout.LabelField(
            "Frame",
            Time.frameCount.ToString()
        );

        EditorGUILayout.LabelField(
            "Time",
            Time.time.ToString("0.00")
        );

        EditorGUILayout.LabelField(
            "Time Scale",
            Time.timeScale.ToString("0.00")
        );

        autoRepaint = EditorGUILayout.Toggle(
            "自动刷新",
            autoRepaint
        );

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "时间控制",
            EditorStyles.boldLabel
        );

        Time.timeScale = EditorGUILayout.Slider(
            "Time Scale",
            Time.timeScale,
            0f,
            2f
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("暂停"))
        {
            EditorApplication.isPaused = true;
        }

        if (GUILayout.Button("继续"))
        {
            EditorApplication.isPaused = false;
        }

        if (GUILayout.Button("单帧"))
        {
            EditorApplication.Step();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        DrawRoundTimerControls();

        EditorGUILayout.Space();

        if (GUILayout.Button("查找玩家"))
        {
            GameObject player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                Selection.activeGameObject = player;
                EditorGUIUtility.PingObject(player);
            }
            else
            {
                Debug.LogWarning("未找到带有 Player 标签的对象");
            }
        }
    }

    private static void DrawRoundTimerControls()
    {
        EditorGUILayout.LabelField("回合倒计时", EditorStyles.boldLabel);
        LevelFlowController flow = LevelFlowController.Instance;
        bool canAdjust = EditorApplication.isPlaying &&
            flow != null && flow.IsRoundTimerRunning;

        string state = !EditorApplication.isPlaying
            ? "请进入 Play Mode"
            : flow == null
                ? "等待关卡流程控制器"
                : !flow.IsRoundTimerRunning
                    ? "仅战斗阶段可调整"
                    : flow.IsOvertime
                        ? $"加时 {flow.RemainingRoundTime:0.0} 秒"
                        : $"常规 {flow.RemainingRoundTime:0.0} 秒";
        EditorGUILayout.LabelField("当前状态", state);

        using (new EditorGUI.DisabledScope(!canAdjust))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("-5 秒"))
            {
                flow.AdjustRoundTimerForDebug(-5f);
            }

            if (GUILayout.Button("+5 秒"))
            {
                flow.AdjustRoundTimerForDebug(5f);
            }
        }
    }
}
