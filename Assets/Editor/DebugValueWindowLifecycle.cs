using UnityEditor;

[InitializeOnLoad]
internal static class DebugValueWindowLifecycle
{
    private static bool openQueued;
    private static bool closeQueued;

    static DebugValueWindowLifecycle()
    {
        DebugValueRuntimeBridge.TargetAvailable += QueueOpen;
        DebugValueRuntimeBridge.TargetUnavailable += QueueClose;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        if (EditorApplication.isPlaying && DebugValueRuntimeBridge.HasTarget)
        {
            QueueOpen();
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && DebugValueRuntimeBridge.HasTarget)
        {
            QueueOpen();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode ||
                 state == PlayModeStateChange.EnteredEditMode)
        {
            QueueClose();
        }
    }

    private static void QueueOpen()
    {
        if (!EditorApplication.isPlaying || openQueued)
        {
            return;
        }

        openQueued = true;
        EditorApplication.delayCall += OpenIfValid;
    }

    private static void OpenIfValid()
    {
        openQueued = false;
        if (EditorApplication.isPlaying && DebugValueRuntimeBridge.HasTarget)
        {
            DebugValueWindow.OpenWindow();
        }
    }

    private static void QueueClose()
    {
        if (closeQueued)
        {
            return;
        }

        closeQueued = true;
        EditorApplication.delayCall += Close;
    }

    private static void Close()
    {
        closeQueued = false;
        DebugValueWindow.CloseWindow();
    }
}
