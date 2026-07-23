using BackpackHero.Input;
using UnityEditor;

[InitializeOnLoad]
internal static class HorizontalSwipeCurveDebugWindowLifecycle
{
    private static bool openQueued;
    private static bool closeQueued;

    static HorizontalSwipeCurveDebugWindowLifecycle()
    {
        HorizontalSwipeCurveDebugBridge.TargetAvailable += QueueOpen;
        HorizontalSwipeCurveDebugBridge.TargetUnavailable += QueueClose;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        if (EditorApplication.isPlaying && HorizontalSwipeCurveDebugBridge.HasTarget)
        {
            QueueOpen();
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && HorizontalSwipeCurveDebugBridge.HasTarget)
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
        if (EditorApplication.isPlaying && HorizontalSwipeCurveDebugBridge.HasTarget)
        {
            HorizontalSwipeCurveDebugWindow.OpenWindow();
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
        HorizontalSwipeCurveDebugWindow.CloseWindow();
    }
}
