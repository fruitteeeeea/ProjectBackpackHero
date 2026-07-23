using UnityEditor;
using BackpackHero.Battle;

namespace BackpackHero.EditorTools
{
    /// <summary>
    /// 集中管理Battle Debug窗口的自动打开和关闭。
    /// Runtime业务代码不需要认识具体的EditorWindow类型。
    /// </summary>
    [InitializeOnLoad]
    internal static class BattleDebugWindowLifecycle
    {
        private static bool runtimeWasAvailable;

        static BattleDebugWindowLifecycle()
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
                BattleDebugRuntime.Instance != null;

            if (runtimeIsAvailable &&
                !runtimeWasAvailable)
            {
                BattleDebugWindow.OpenWindow();
            }
            else if (!runtimeIsAvailable &&
                     runtimeWasAvailable)
            {
                BattleDebugWindow.CloseAllWindows();
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
                    BattleDebugWindow.CloseAllWindows();
                    break;
            }
        }
    }
}