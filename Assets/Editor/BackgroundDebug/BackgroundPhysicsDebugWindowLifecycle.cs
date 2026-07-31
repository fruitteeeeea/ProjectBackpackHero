using BackpackHero.Background;
using UnityEditor;

namespace BackpackHero.EditorTools
{
    [InitializeOnLoad]
    internal static class BackgroundPhysicsDebugWindowLifecycle
    {
        private static bool runtimeWasAvailable;

        static BackgroundPhysicsDebugWindowLifecycle()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update += MonitorRuntime;
        }

        private static void MonitorRuntime()
        {
            var available = EditorApplication.isPlaying && BackgroundPhysicsDebugRuntime.Instance != null;
            if (available && !runtimeWasAvailable)
            {
                BackgroundPhysicsDebugWindow.OpenWindow();
            }
            else if (!available && runtimeWasAvailable)
            {
                BackgroundPhysicsDebugWindow.CloseAllWindows();
            }

            runtimeWasAvailable = available;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                runtimeWasAvailable = false;
                BackgroundPhysicsDebugWindow.CloseAllWindows();
            }
        }
    }
}
