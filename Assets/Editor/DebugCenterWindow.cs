using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
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
        GameplayDesign,
        VisualEffects
    }

    internal enum DebugCenterTab
    {
        Runtime,
        Values,
        SwipeCurve,
        PlayerBackpack,
        EnemyBackpack,
        Battle,
        TestShooter,
        BackgroundPhysics,
        GamePacing,
        Level,
        LevelDifficulty,
        StyleTendency,
        AircraftVisual,
        BackpackVisual,
        DeckPresets,
        ItemProgression,
        Pack,
        DamageStatistics,
        BackpackStrength,
        FloatingDamageText
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
            new(DebugCenterTab.EnemyBackpack, DebugCenterKind.ProgramTest, "敌人背包",
                () => PlayerBackpackDebugBridge.Active?.EnemyTarget != null,
                () => ScriptableObject.CreateInstance<EnemyBackpackDebugWindow>()),
            new(DebugCenterTab.Battle, DebugCenterKind.ProgramTest, "战斗",
                () => BattleDebugRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<BattleDebugWindow>()),
            new(DebugCenterTab.TestShooter, DebugCenterKind.ProgramTest, "Test Shooter",
                () => TestShooterDebugRuntimeBridge.HasTarget,
                () => ScriptableObject.CreateInstance<TestShooterDebugWindow>()),
            new(DebugCenterTab.BackgroundPhysics, DebugCenterKind.VisualEffects, "星图物理",
                () => BackgroundPhysicsDebugRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<BackgroundPhysicsDebugWindow>(), true),
            new(DebugCenterTab.Level, DebugCenterKind.ProgramTest, "关卡",
                () => LevelFlowController.Instance != null,
                () => ScriptableObject.CreateInstance<LevelDebugWindow>()),
            new(DebugCenterTab.AircraftVisual, DebugCenterKind.VisualEffects, "飞机视觉动效",
                () => AircraftVisualDebugRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<AircraftVisualDebugWindow>()),
            new(DebugCenterTab.BackpackVisual, DebugCenterKind.VisualEffects, "背包动效",
                () => BackpackVisualDebugRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<BackpackVisualDebugWindow>()),
            new(DebugCenterTab.FloatingDamageText, DebugCenterKind.VisualEffects, "伤害飘字",
                () => FloatingDamageTextDebugRuntime.Instance != null,
                VisualEffectsDebugCenterWindow.CreateFloatingDamageTextContent, true),
            new(DebugCenterTab.GamePacing, DebugCenterKind.GameplayDesign, "游戏节奏",
                () => GamePacingDebugRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<GamePacingDebugWindow>(), true),
            new(DebugCenterTab.StyleTendency, DebugCenterKind.GameplayDesign, "风格倾向",
                () => StyleTendencyDebugRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<StyleTendencyDebugWindow>()),
            new(DebugCenterTab.LevelDifficulty, DebugCenterKind.GameplayDesign, "关卡",
                () => LevelDifficultyRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<LevelDifficultyDebugWindow>()),
            new(DebugCenterTab.DeckPresets, DebugCenterKind.GameplayDesign, "背包预设",
                () => true,
                () => ScriptableObject.CreateInstance<DeckPresetDebugWindow>()),
            new(DebugCenterTab.ItemProgression, DebugCenterKind.ProgramTest, "物品养成",
                () => EditorApplication.isPlaying && PlayerItemSystem.Instance != null,
                () => ScriptableObject.CreateInstance<ItemProgressionDebugWindow>()),
            new(DebugCenterTab.Pack, DebugCenterKind.ProgramTest, "卡包",
                () => EditorApplication.isPlaying && PackSystem.Instance != null,
                () => ScriptableObject.CreateInstance<PackDebugWindow>()),
            new(DebugCenterTab.DamageStatistics, DebugCenterKind.GameplayDesign, "DPS 检测",
                () => EditorApplication.isPlaying && DamageStatisticsRuntime.Instance != null,
                () => ScriptableObject.CreateInstance<DamageStatisticsDebugWindow>()),
            new(DebugCenterTab.BackpackStrength, DebugCenterKind.GameplayDesign, "场上背包强度",
                () => EditorApplication.isPlaying && PlayerBackpackDebugBridge.Active != null,
                () => ScriptableObject.CreateInstance<BackpackStrengthDebugWindow>())
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
                case EnemyBackpackDebugWindow enemyBackpack: enemyBackpack.DrawTab(); break;
                case BattleDebugWindow battle: battle.DrawTab(); break;
                case TestShooterDebugWindow shooter: shooter.DrawTab(); break;
                case BackgroundPhysicsDebugWindow background: background.DrawTab(); break;
                case GamePacingDebugWindow pacing: pacing.DrawTab(); break;
                case StyleTendencyDebugWindow styleTendency: styleTendency.DrawTab(); break;
                case LevelDebugWindow level: level.DrawTab(); break;
                case AircraftVisualDebugWindow aircraftVisual: aircraftVisual.DrawTab(); break;
                case BackpackVisualDebugWindow backpackVisual: backpackVisual.DrawTab(); break;
                case LevelDifficultyDebugWindow levelDifficulty: levelDifficulty.DrawTab(); break;
                case DeckPresetDebugWindow deckPresets: deckPresets.DrawTab(); break;
                case ItemProgressionDebugWindow itemProgression: itemProgression.DrawTab(); break;
                case PackDebugWindow pack: pack.DrawTab(); break;
                case DamageStatisticsDebugWindow damageStatistics: damageStatistics.DrawTab(); break;
                case BackpackStrengthDebugWindow backpackStrength: backpackStrength.DrawTab(); break;
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

    internal sealed class VisualEffectsDebugCenterWindow : DebugCenterWindowBase
    {
        protected override DebugCenterKind Center => DebugCenterKind.VisualEffects;
        protected override string PreferenceKey => "BackpackHero.DebugCenter.VisualEffects.LastTab";

        [MenuItem("Tools/Debug/视效调整面板")]
        internal static void OpenFromMenu() => DebugCenterWorkspace.Open();

        internal static VisualEffectsDebugCenterWindow Open(bool focus = true)
        {
            VisualEffectsDebugCenterWindow window = EditorWindow.GetWindow<VisualEffectsDebugCenterWindow>(
                "视效调整面板", focus, typeof(ProgramTestDebugCenterWindow));
            window.Initialize("视效调整面板");
            if (focus) window.Focus();
            return window;
        }

        internal static ScriptableObject CreateFloatingDamageTextContent()
        {
            AircraftVisualDebugWindow content = ScriptableObject.CreateInstance<AircraftVisualDebugWindow>();
            content.FloatingTextOnly = true;
            return content;
        }
    }

    /// <summary>按固定顺序创建三个 Center，并在公开 API 不足时最小范围反射停靠。</summary>
    internal static class DebugCenterWorkspace
    {
        private const int DockRetryCount = 8;
        private static int pendingDockRetries;

        internal static void Open(DebugCenterTab? preferredTab = null)
        {
            ProgramTestDebugCenterWindow programPanel =
                ProgramTestDebugCenterWindow.Open(focus: false);
            GameplayDesignDebugCenterWindow designPanel =
                GameplayDesignDebugCenterWindow.Open(
                    focus: false,
                    dockNextToProgramPanel: true);
            VisualEffectsDebugCenterWindow visualPanel =
                VisualEffectsDebugCenterWindow.Open(focus: false);
            TryDockTogether(programPanel, designPanel, visualPanel);
            ScheduleDockRetry();

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
            else if (definition?.Center == DebugCenterKind.VisualEffects)
            {
                visualPanel.Select(preferredTab.Value);
                visualPanel.Focus();
            }
        }

        internal static void ResetDockRetries() => pendingDockRetries = 0;

        private static void TryDockTogether(EditorWindow anchor, params EditorWindow[] windows)
        {
            // DockArea.AddTab 是 Unity 内部 API；仅集中在这里作为公开 GetWindow 的兜底。
            object dockArea = typeof(EditorWindow).GetField("m_Parent",
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(anchor);
            MethodInfo addTab = dockArea?.GetType().GetMethod("AddTab",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(EditorWindow) }, null);
            if (addTab == null) return;
            foreach (EditorWindow window in windows)
            {
                if (window != null) addTab.Invoke(dockArea, new object[] { window });
            }
        }

        private static void ScheduleDockRetry()
        {
            if (pendingDockRetries >= DockRetryCount) return;
            pendingDockRetries++;
            EditorApplication.delayCall += () =>
            {
                ProgramTestDebugCenterWindow program = Resources.FindObjectsOfTypeAll<ProgramTestDebugCenterWindow>()
                    .FirstOrDefault();
                GameplayDesignDebugCenterWindow gameplay = Resources.FindObjectsOfTypeAll<GameplayDesignDebugCenterWindow>()
                    .FirstOrDefault();
                VisualEffectsDebugCenterWindow visual = Resources.FindObjectsOfTypeAll<VisualEffectsDebugCenterWindow>()
                    .FirstOrDefault();
                if (program != null && gameplay != null && visual != null)
                    TryDockTogether(program, gameplay, visual);
                ScheduleDockRetry();
            };
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
            foreach (DebugCenterTabDefinition definition in DebugCenterRegistry.GetTabs(DebugCenterKind.VisualEffects))
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
                DebugCenterWorkspace.ResetDockRetries();
                CloseAll<ProgramTestDebugCenterWindow>();
                CloseAll<GameplayDesignDebugCenterWindow>();
                CloseAll<VisualEffectsDebugCenterWindow>();
                return;
            }

            if (state != PlayModeStateChange.ExitingPlayMode && state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            WasAvailable.Clear();
            DebugCenterWorkspace.ResetDockRetries();
            GamePacingDebugRuntime.Instance?.ResetGameSpeed();
            CloseAll<ProgramTestDebugCenterWindow>();
            CloseAll<GameplayDesignDebugCenterWindow>();
            CloseAll<VisualEffectsDebugCenterWindow>();
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
