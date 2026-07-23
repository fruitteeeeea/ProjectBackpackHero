using UnityEditor;
using UnityEngine;

public class RuntimeDebugWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private bool autoRepaint = true;

    [MenuItem("Tools/Runtime Debug Window")]
    private static void Open()
    {
        var window = GetWindow<RuntimeDebugWindow>();
        window.titleContent = new GUIContent("Runtime Debug");
        window.minSize = new Vector2(320, 300);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (autoRepaint && EditorApplication.isPlaying)
        {
            Repaint();
        }
    }

    private void OnGUI()
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
}