using BackpackPrototype;
using UnityEditor;

namespace BackpackPrototypeEditor
{
    [InitializeOnLoad]
    internal static class BackpackDebugWindowLifecycle
    {
        private static bool runtimeWasAvailable;

        static BackpackDebugWindowLifecycle()
        {
            EditorApplication.playModeStateChanged -=
                OnPlayModeStateChanged;

            EditorApplication.playModeStateChanged +=
                OnPlayModeStateChanged;

            EditorApplication.update -=
                MonitorRuntime;

            EditorApplication.update +=
                MonitorRuntime;
        }

        private static void MonitorRuntime()
        {
            bool runtimeIsAvailable =
                EditorApplication.isPlaying &&
                BackpackDebugRuntime.Instance != null &&
                BackpackDebugRuntime.Instance.IsReady;

            if (runtimeIsAvailable &&
                !runtimeWasAvailable)
            {
                BackpackDebugWindow.OpenWindow();
            }
            else if (!runtimeIsAvailable &&
                     runtimeWasAvailable)
            {
                BackpackDebugWindow.CloseAllWindows();
            }

            runtimeWasAvailable =
                runtimeIsAvailable;
        }

        private static void OnPlayModeStateChanged(
            PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    runtimeWasAvailable = false;
                    BackpackDebugWindow.CloseAllWindows();
                    break;
            }
        }
    }
}