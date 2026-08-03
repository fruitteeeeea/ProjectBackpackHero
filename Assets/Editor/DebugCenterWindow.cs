using BackpackHero.Background;
using BackpackHero.Battle;
using BackpackHero.Debugging;
using BackpackHero.Input;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    internal enum DebugCenterTab
    {
        Runtime,
        Values,
        SwipeCurve,
        PlayerBackpack,
        Battle,
        TestShooter,
        GamePacing,
        BackgroundPhysics
    }

    /// <summary>唯一的 Play Mode 调试工具宿主窗口。</summary>
    public sealed class DebugCenterWindow : EditorWindow
    {
        private const string LastTabPreferenceKey =
            "BackpackHero.DebugCenter.LastTab";

        private static readonly string[] TabLabels =
        {
            "Runtime", "数值", "滑动曲线", "玩家背包",
            "战斗", "Test Shooter", "游戏节奏", "星图物理"
        };

        private DebugCenterTab selectedTab;
        private bool hasSavedSelection;
        private RuntimeDebugWindow runtimeTab;
        private DebugValueWindow valuesTab;
        private HorizontalSwipeCurveDebugWindow swipeCurveTab;
        private PlayerBackpackDebugWindow playerBackpackTab;
        private BattleDebugWindow battleTab;
        private TestShooterDebugWindow testShooterTab;
        private GamePacingDebugWindow gamePacingTab;
        private BackgroundPhysicsDebugWindow backgroundPhysicsTab;

        [MenuItem("Tools/Debug/Debug Center")]
        public static void OpenFromMenu() => Open();

        internal static DebugCenterWindow Open(DebugCenterTab? preferredTab = null)
        {
            DebugCenterWindow window = GetWindow<DebugCenterWindow>();
            window.titleContent = new GUIContent("Debug Center");
            window.minSize = new Vector2(440f, 360f);
            window.Show();

            if (preferredTab.HasValue)
            {
                window.SelectTab(preferredTab.Value);
            }
            else if (!window.hasSavedSelection ||
                     !window.IsTabAvailable(window.selectedTab))
            {
                window.SelectFirstAvailableTab();
            }

            return window;
        }

        internal static void CloseAllWindows()
        {
            foreach (DebugCenterWindow window in
                     Resources.FindObjectsOfTypeAll<DebugCenterWindow>())
            {
                window.Close();
            }
        }

        private void OnEnable()
        {
            hasSavedSelection = EditorPrefs.HasKey(LastTabPreferenceKey);
            selectedTab = (DebugCenterTab)Mathf.Clamp(
                EditorPrefs.GetInt(LastTabPreferenceKey, 0),
                0,
                TabLabels.Length - 1);

            runtimeTab = CreateInstance<RuntimeDebugWindow>();
            valuesTab = CreateInstance<DebugValueWindow>();
            swipeCurveTab = CreateInstance<HorizontalSwipeCurveDebugWindow>();
            playerBackpackTab = CreateInstance<PlayerBackpackDebugWindow>();
            battleTab = CreateInstance<BattleDebugWindow>();
            testShooterTab = CreateInstance<TestShooterDebugWindow>();
            gamePacingTab = CreateInstance<GamePacingDebugWindow>();
            backgroundPhysicsTab = CreateInstance<BackgroundPhysicsDebugWindow>();
        }

        private void OnDisable()
        {
            GamePacingDebugRuntime.Instance?.ResetGameSpeed();
            DestroyTab(runtimeTab);
            DestroyTab(valuesTab);
            DestroyTab(swipeCurveTab);
            DestroyTab(playerBackpackTab);
            DestroyTab(battleTab);
            DestroyTab(testShooterTab);
            DestroyTab(gamePacingTab);
            DestroyTab(backgroundPhysicsTab);
        }

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6f);
            DrawSelectedTab();
        }

        private void DrawToolbar()
        {
            int nextIndex = GUILayout.Toolbar(
                (int)selectedTab,
                GetToolbarLabels(),
                EditorStyles.toolbarButton);
            if (nextIndex != (int)selectedTab)
            {
                SelectTab((DebugCenterTab)nextIndex);
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                $"当前页：{TabLabels[(int)selectedTab]}",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                IsTabAvailable(selectedTab) ? "运行时已连接" : "运行时未连接",
                EditorStyles.miniLabel,
                GUILayout.Width(100f));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectedTab()
        {
            switch (selectedTab)
            {
                case DebugCenterTab.Runtime: runtimeTab.DrawTab(); break;
                case DebugCenterTab.Values: valuesTab.DrawTab(); break;
                case DebugCenterTab.SwipeCurve: swipeCurveTab.DrawTab(); break;
                case DebugCenterTab.PlayerBackpack: playerBackpackTab.DrawTab(); break;
                case DebugCenterTab.Battle: battleTab.DrawTab(); break;
                case DebugCenterTab.TestShooter: testShooterTab.DrawTab(); break;
                case DebugCenterTab.GamePacing: gamePacingTab.DrawTab(); break;
                case DebugCenterTab.BackgroundPhysics: backgroundPhysicsTab.DrawTab(); break;
            }
        }

        private void SelectFirstAvailableTab()
        {
            for (int index = 1; index < TabLabels.Length; index++)
            {
                DebugCenterTab tab = (DebugCenterTab)index;
                if (IsTabAvailable(tab))
                {
                    SelectTab(tab);
                    return;
                }
            }

            SelectTab(DebugCenterTab.Runtime);
        }

        private string[] GetToolbarLabels()
        {
            string[] labels = new string[TabLabels.Length];
            for (int index = 0; index < TabLabels.Length; index++)
            {
                labels[index] = IsTabAvailable((DebugCenterTab)index)
                    ? $"● {TabLabels[index]}"
                    : $"○ {TabLabels[index]}";
            }

            return labels;
        }

        private void SelectTab(DebugCenterTab tab)
        {
            selectedTab = tab;
            EditorPrefs.SetInt(LastTabPreferenceKey, (int)tab);
            Repaint();
        }

        internal static bool HasAvailableRuntime()
        {
            return DebugValueRuntimeBridge.HasTarget ||
                   HorizontalSwipeCurveDebugBridge.HasTarget ||
                   (PlayerBackpackDebugBridge.Active != null &&
                    PlayerBackpackDebugBridge.Active.DebugEnabled) ||
                   BattleDebugRuntime.Instance != null ||
                   TestShooterDebugRuntimeBridge.HasTarget ||
                   GamePacingDebugRuntime.Instance != null ||
                   BackgroundPhysicsDebugRuntime.Instance != null;
        }

        private bool IsTabAvailable(DebugCenterTab tab)
        {
            if (!EditorApplication.isPlaying)
            {
                return false;
            }

            return tab switch
            {
                DebugCenterTab.Runtime => true,
                DebugCenterTab.Values => DebugValueRuntimeBridge.HasTarget,
                DebugCenterTab.SwipeCurve => HorizontalSwipeCurveDebugBridge.HasTarget,
                DebugCenterTab.PlayerBackpack => PlayerBackpackDebugBridge.Active != null && PlayerBackpackDebugBridge.Active.DebugEnabled,
                DebugCenterTab.Battle => BattleDebugRuntime.Instance != null,
                DebugCenterTab.TestShooter => TestShooterDebugRuntimeBridge.HasTarget,
                DebugCenterTab.GamePacing => GamePacingDebugRuntime.Instance != null,
                DebugCenterTab.BackgroundPhysics => BackgroundPhysicsDebugRuntime.Instance != null,
                _ => false
            };
        }

        private static void DestroyTab(Object tab)
        {
            if (tab != null)
            {
                DestroyImmediate(tab);
            }
        }
    }

    [InitializeOnLoad]
    internal static class DebugCenterWindowLifecycle
    {
        private static bool runtimeWasAvailable;

        static DebugCenterWindowLifecycle()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update += MonitorRuntime;
        }

        private static void MonitorRuntime()
        {
            bool runtimeIsAvailable = EditorApplication.isPlaying &&
                DebugCenterWindow.HasAvailableRuntime();

            if (runtimeIsAvailable && !runtimeWasAvailable)
            {
                DebugCenterWindow.Open();
            }
            else if (!runtimeIsAvailable && runtimeWasAvailable)
            {
                DebugCenterWindow.CloseAllWindows();
            }

            runtimeWasAvailable = runtimeIsAvailable;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                runtimeWasAvailable = false;
                DebugCenterWindow.CloseAllWindows();
            }
        }
    }
}
