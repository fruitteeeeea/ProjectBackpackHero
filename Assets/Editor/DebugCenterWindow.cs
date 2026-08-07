using System;
using System.Collections.Generic;
using BackpackHero.Background;
using BackpackHero.Battle;
using BackpackHero.Debugging;
using BackpackHero.Input;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    internal enum DebugCenterKind
    {
        ProgramTest,
        GameplayDesign
    }

    internal enum DebugCenterTab
    {
        Runtime,
        Values,
        SwipeCurve,
        PlayerBackpack,
        Battle,
        TestShooter,
        BackgroundPhysics,
        GamePacing,
        Level,
        LevelDifficulty,
        StyleTendency
    }

    internal sealed class DebugCenterTabDefinition
    {
        public DebugCenterTabDefinition(
            DebugCenterTab tab,
            DebugCenterKind center,
            string label,
            Func<bool> isAvailable,
            Func<ScriptableObject> createContent,
            bool isDefault = false)
        {
            Tab = tab;
            Center = center;
            Label = label;
            IsAvailable = isAvailable;
            CreateContent = createContent;
            IsDefault = isDefault;
        }

        public DebugCenterTab Tab { get; }
        public DebugCenterKind Center { get; }
        public string Label { get; }
        public Func<bool> IsAvailable { get; }
        public Func<ScriptableObject> CreateContent { get; }
        public bool IsDefault { get; }
    }

    /// <summary>集中注册所有 Editor 调试页，业务代码不依赖具体窗口类型。</summary>
    internal static class DebugCenterRegistry
    {
        private static readonly DebugCenterTabDefinition[] Definitions =
        {
            new(DebugCenterTab.Runtime, DebugCenterKind.ProgramTest, "Runtime",
                () => EditorApplication.isPlaying,
                () => ScriptableObject.CreateInstance<RuntimeDebugWindow>()),
            new(DebugCenterTab.Values, DebugCenterKind.ProgramTest, "数值",
                () => DebugValueRuntimeBridge.HasTarget,
                () => ScriptableObject.CreateInstance<DebugValueWindow>()),
            new(DebugCenterTab.SwipeCurve, DebugCenterKind.ProgramTest, "滑动曲线",
                () => HorizontalSwipeCurveDebugBridge.HasTarget,
                () => ScriptableObject.CreateInstance<HorizontalSwipeCurveDebugWindow>()),
            new(DebugCenterTab.PlayerBackpack, DebugCenterKind.ProgramTest, "玩家背包",
                () => PlayerBackpackDebugBridge.Active != null && PlayerBackpackDebugBridge.Active.DebugEnabled,
                () => ScriptableObject.CreateInstance<PlayerBackpackDebugWindow>()),
            new(DebugCenterTab.Battle, DebugCenterKind.ProgramTest, "战斗",
                () => BattleDebugRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<BattleDebugWindow>()),
            new(DebugCenterTab.TestShooter, DebugCenterKind.ProgramTest, "Test Shooter",
                () => TestShooterDebugRuntimeBridge.HasTarget,
                () => ScriptableObject.CreateInstance<TestShooterDebugWindow>()),
            new(DebugCenterTab.BackgroundPhysics, DebugCenterKind.ProgramTest, "星图物理",
                () => BackgroundPhysicsDebugRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<BackgroundPhysicsDebugWindow>(), true),
            new(DebugCenterTab.Level, DebugCenterKind.ProgramTest, "关卡",
                () => LevelFlowController.Instance != null,
                () => ScriptableObject.CreateInstance<LevelDebugWindow>()),
            new(DebugCenterTab.GamePacing, DebugCenterKind.GameplayDesign, "游戏节奏",
                () => GamePacingDebugRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<GamePacingDebugWindow>(), true),
            new(DebugCenterTab.StyleTendency, DebugCenterKind.GameplayDesign, "风格倾向",
                () => StyleTendencyDebugRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<StyleTendencyDebugWindow>()),
            new(DebugCenterTab.LevelDifficulty, DebugCenterKind.GameplayDesign, "关卡",
                () => LevelDifficultyRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<LevelDifficultyDebugWindow>())
        };

        internal static IEnumerable<DebugCenterTabDefinition> GetTabs(DebugCenterKind center)
        {
            foreach (DebugCenterTabDefinition definition in Definitions)
            {
                if (definition.Center == center)
                {
                    yield return definition;
                }
            }
        }

        internal static DebugCenterTabDefinition Get(DebugCenterTab tab)
        {
            foreach (DebugCenterTabDefinition definition in Definitions)
            {
                if (definition.Tab == tab)
                {
                    return definition;
                }
            }

            return null;
        }

        internal static bool HasAvailableRuntime(DebugCenterKind center)
        {
            foreach (DebugCenterTabDefinition definition in GetTabs(center))
            {
                if (definition.IsAvailable())
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal abstract class DebugCenterWindowBase : EditorWindow
    {
        private readonly Dictionary<DebugCenterTab, ScriptableObject> contents = new();
        private DebugCenterTab selectedTab;
        private bool hasSelection;

        protected abstract DebugCenterKind Center { get; }
        protected abstract string PreferenceKey { get; }

        protected void Initialize(string title)
        {
            titleContent = new GUIContent(title);
            minSize = new Vector2(440f, 360f);
            RestoreOrSelectDefault();
        }

        internal void Select(DebugCenterTab tab)
        {
            DebugCenterTabDefinition definition = DebugCenterRegistry.Get(tab);
            if (definition == null || definition.Center != Center)
            {
                return;
            }

            selectedTab = tab;
            hasSelection = true;
            EditorPrefs.SetInt(PreferenceKey, (int)tab);
            Repaint();
        }

        protected virtual void OnEnable()
        {
            if (EditorPrefs.HasKey(PreferenceKey))
            {
                selectedTab = (DebugCenterTab)EditorPrefs.GetInt(PreferenceKey);
                DebugCenterTabDefinition definition = DebugCenterRegistry.Get(selectedTab);
                hasSelection = definition != null && definition.Center == Center;
            }
        }

        protected virtual void OnDisable()
        {
            foreach (ScriptableObject content in contents.Values)
            {
                if (content != null)
                {
                    DestroyImmediate(content);
                }
            }

            contents.Clear();
        }

        protected virtual void Update() => Repaint();

        protected virtual void OnGUI()
        {
            RestoreOrSelectDefault();
            DrawToolbar();
            EditorGUILayout.Space(6f);
            DrawSelectedContent();
        }

        private void RestoreOrSelectDefault()
        {
            DebugCenterTabDefinition selected = hasSelection
                ? DebugCenterRegistry.Get(selectedTab)
                : null;
            if (selected != null && selected.Center == Center && selected.IsAvailable())
            {
                return;
            }

            foreach (DebugCenterTabDefinition definition in DebugCenterRegistry.GetTabs(Center))
            {
                if (definition.IsDefault && definition.IsAvailable())
                {
                    Select(definition.Tab);
                    return;
                }
            }

            foreach (DebugCenterTabDefinition definition in DebugCenterRegistry.GetTabs(Center))
            {
                if (definition.IsAvailable())
                {
                    Select(definition.Tab);
                    return;
                }
            }

            foreach (DebugCenterTabDefinition definition in DebugCenterRegistry.GetTabs(Center))
            {
                if (definition.IsDefault)
                {
                    Select(definition.Tab);
                    return;
                }
            }
        }

        private void DrawToolbar()
        {
            List<DebugCenterTabDefinition> tabs = new(DebugCenterRegistry.GetTabs(Center));
            string[] labels = new string[tabs.Count];
            int currentIndex = 0;
            for (int index = 0; index < tabs.Count; index++)
            {
                DebugCenterTabDefinition definition = tabs[index];
                labels[index] = definition.IsAvailable() ? $"● {definition.Label}" : $"○ {definition.Label}";
                if (definition.Tab == selectedTab)
                {
                    currentIndex = index;
                }
            }

            int nextIndex = GUILayout.Toolbar(currentIndex, labels, EditorStyles.toolbarButton);
            if (nextIndex >= 0 && nextIndex < tabs.Count && nextIndex != currentIndex)
            {
                Select(tabs[nextIndex].Tab);
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            DebugCenterTabDefinition selected = DebugCenterRegistry.Get(selectedTab);
            EditorGUILayout.LabelField($"当前页：{selected?.Label ?? "-"}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                selected != null && selected.IsAvailable() ? "运行时已连接" : "运行时未连接",
                EditorStyles.miniLabel, GUILayout.Width(100f));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectedContent()
        {
            DebugCenterTabDefinition definition = DebugCenterRegistry.Get(selectedTab);
            if (definition == null || definition.Center != Center)
            {
                EditorGUILayout.HelpBox("没有可用的调试页。", MessageType.Info);
                return;
            }

            if (!contents.TryGetValue(definition.Tab, out ScriptableObject content) || content == null)
            {
                content = definition.CreateContent();
                contents[definition.Tab] = content;
            }

            switch (content)
            {
                case RuntimeDebugWindow runtime: runtime.DrawTab(); break;
                case DebugValueWindow values: values.DrawTab(); break;
                case HorizontalSwipeCurveDebugWindow swipe: swipe.DrawTab(); break;
                case PlayerBackpackDebugWindow backpack: backpack.DrawTab(); break;
                case BattleDebugWindow battle: battle.DrawTab(); break;
                case TestShooterDebugWindow shooter: shooter.DrawTab(); break;
                case BackgroundPhysicsDebugWindow background: background.DrawTab(); break;
                case GamePacingDebugWindow pacing: pacing.DrawTab(); break;
                case StyleTendencyDebugWindow styleTendency: styleTendency.DrawTab(); break;
                case LevelDebugWindow level: level.DrawTab(); break;
                case LevelDifficultyDebugWindow levelDifficulty: levelDifficulty.DrawTab(); break;
            }
        }
    }

    internal sealed class ProgramTestDebugCenterWindow : DebugCenterWindowBase
    {
        protected override DebugCenterKind Center => DebugCenterKind.ProgramTest;
        protected override string PreferenceKey => "BackpackHero.DebugCenter.ProgramTest.LastTab";

        [MenuItem("Tools/Debug/程序测试面板")]
        internal static void OpenFromMenu() => DebugCenterWorkspace.Open();

        internal static ProgramTestDebugCenterWindow Open(
            DebugCenterTab? preferredTab = null,
            bool focus = true)
        {
            ProgramTestDebugCenterWindow window = EditorWindow.GetWindow<ProgramTestDebugCenterWindow>(
                "程序测试面板", focus);
            window.Initialize("程序测试面板");
            if (preferredTab.HasValue)
            {
                window.Select(preferredTab.Value);
            }

            if (focus)
            {
                window.Focus();
            }

            return window;
        }
    }

    internal sealed class GameplayDesignDebugCenterWindow : DebugCenterWindowBase
    {
        protected override DebugCenterKind Center => DebugCenterKind.GameplayDesign;
        protected override string PreferenceKey => "BackpackHero.DebugCenter.GameplayDesign.LastTab";

        [MenuItem("Tools/Debug/玩法设计面板")]
        internal static void OpenFromMenu() => DebugCenterWorkspace.Open();

        internal static GameplayDesignDebugCenterWindow Open(
            DebugCenterTab? preferredTab = null,
            bool focus = true,
            bool dockNextToProgramPanel = false)
        {
            GameplayDesignDebugCenterWindow window = dockNextToProgramPanel
                ? EditorWindow.GetWindow<GameplayDesignDebugCenterWindow>(
                    "玩法设计面板",
                    focus,
                    typeof(ProgramTestDebugCenterWindow))
                : EditorWindow.GetWindow<GameplayDesignDebugCenterWindow>(
                    "玩法设计面板", focus);
            window.Initialize("玩法设计面板");
            if (preferredTab.HasValue)
            {
                window.Select(preferredTab.Value);
            }

            if (focus)
            {
                window.Focus();
            }

            return window;
        }
    }

    /// <summary>按固定顺序创建双 Center，以便使用公开 API 尝试将其并列停靠。</summary>
    internal static class DebugCenterWorkspace
    {
        internal static void Open(DebugCenterTab? preferredTab = null)
        {
            ProgramTestDebugCenterWindow programPanel =
                ProgramTestDebugCenterWindow.Open(focus: false);
            GameplayDesignDebugCenterWindow designPanel =
                GameplayDesignDebugCenterWindow.Open(
                    focus: false,
                    dockNextToProgramPanel: true);

            if (!preferredTab.HasValue)
            {
                return;
            }

            DebugCenterTabDefinition definition =
                DebugCenterRegistry.Get(preferredTab.Value);
            if (definition?.Center == DebugCenterKind.ProgramTest)
            {
                programPanel.Select(preferredTab.Value);
                programPanel.Focus();
            }
            else if (definition?.Center == DebugCenterKind.GameplayDesign)
            {
                designPanel.Select(preferredTab.Value);
                designPanel.Focus();
            }
        }
    }

    [InitializeOnLoad]
    internal static class DebugCenterWindowLifecycle
    {
        private static readonly Dictionary<DebugCenterTab, bool> WasAvailable = new();

        static DebugCenterWindowLifecycle()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update += MonitorRuntime;
        }

        private static void MonitorRuntime()
        {
            foreach (DebugCenterTabDefinition definition in GetDefinitions())
            {
                bool available = EditorApplication.isPlaying && definition.IsAvailable();
                WasAvailable.TryGetValue(definition.Tab, out bool wasAvailable);
                if (available && !wasAvailable)
                {
                    OpenFor(definition);
                }

                WasAvailable[definition.Tab] = available;
            }
        }

        private static IEnumerable<DebugCenterTabDefinition> GetDefinitions()
        {
            foreach (DebugCenterTabDefinition definition in DebugCenterRegistry.GetTabs(DebugCenterKind.ProgramTest))
            {
                yield return definition;
            }

            foreach (DebugCenterTabDefinition definition in DebugCenterRegistry.GetTabs(DebugCenterKind.GameplayDesign))
            {
                yield return definition;
            }
        }

        private static void OpenFor(DebugCenterTabDefinition definition)
        {
            DebugCenterWorkspace.Open(definition.Tab);
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                WasAvailable.Clear();
                CloseAll<ProgramTestDebugCenterWindow>();
                CloseAll<GameplayDesignDebugCenterWindow>();
                return;
            }

            if (state != PlayModeStateChange.ExitingPlayMode && state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            WasAvailable.Clear();
            GamePacingDebugRuntime.Instance?.ResetGameSpeed();
            CloseAll<ProgramTestDebugCenterWindow>();
            CloseAll<GameplayDesignDebugCenterWindow>();
        }

        private static void CloseAll<T>() where T : EditorWindow
        {
            foreach (T window in Resources.FindObjectsOfTypeAll<T>())
            {
                window.Close();
            }
        }
    }
}
