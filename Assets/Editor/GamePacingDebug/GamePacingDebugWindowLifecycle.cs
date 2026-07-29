using BackpackHero.Debugging;
using UnityEditor;

namespace BackpackHero.EditorTools
{
    /// <summary>集中处理全局节奏调试窗口的自动开关。</summary>
    [InitializeOnLoad]
    internal static class GamePacingDebugWindowLifecycle
    {
        private static bool runtimeWasAvailable;

        static GamePacingDebugWindowLifecycle()
        {
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
            EditorApplication.update += MonitorRuntime;
        }

        private static void MonitorRuntime()
        {
            bool isAvailable = EditorApplication.isPlaying &&
                GamePacingDebugRuntime.Instance != null;

            if (isAvailable && !runtimeWasAvailable)
            {
                GamePacingDebugWindow.OpenWindow();
            }
            else if (!isAvailable && runtimeWasAvailable)
            {
                GamePacingDebugWindow.CloseAllWindows();
            }

            runtimeWasAvailable = isAvailable;
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                runtimeWasAvailable = false;
                GamePacingDebugWindow.CloseAllWindows();
            }
        }
    }
}
