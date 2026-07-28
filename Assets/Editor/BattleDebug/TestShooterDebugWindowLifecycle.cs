using BackpackHero.Battle;
using UnityEditor;

namespace BackpackHero.EditorTools
{
    /// <summary>
    /// 集中管理Test Shooter Debug窗口的自动打开和关闭。
    /// </summary>
    [InitializeOnLoad]
    internal static class TestShooterDebugWindowLifecycle
    {
        private static bool runtimeWasAvailable;

        static TestShooterDebugWindowLifecycle()
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
                TestShooterDebugRuntimeBridge.HasTarget;

            if (runtimeIsAvailable &&
                !runtimeWasAvailable)
            {
                TestShooterDebugWindow.OpenWindow();
            }
            else if (!runtimeIsAvailable &&
                     runtimeWasAvailable)
            {
                TestShooterDebugWindow.CloseAllWindows();
            }

            runtimeWasAvailable = runtimeIsAvailable;
        }

        private static void OnPlayModeStateChanged(
            PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    runtimeWasAvailable = false;
                    TestShooterDebugWindow
                        .CloseAllWindows();
                    break;
            }
        }
    }
}
