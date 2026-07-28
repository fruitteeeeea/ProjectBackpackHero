using System.Collections.Generic;
using BackpackHero.Battle;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    public sealed class TestShooterDebugWindow : EditorWindow
    {
        private const double RepaintInterval = 0.1;

        private Vector2 scrollPosition;
        private double nextRepaintTime;
        private ProjectileFirePattern patternToAdd;

        [MenuItem("Tools/Battle/Test Shooter Debug")]
        public static void ShowFromMenu()
        {
            OpenWindow();
        }

        internal static TestShooterDebugWindow OpenWindow()
        {
            TestShooterDebugWindow window =
                GetWindow<TestShooterDebugWindow>();

            window.titleContent =
                new GUIContent("Test Shooter Debug");

            window.minSize =
                new Vector2(360f, 420f);

            window.Show();
            return window;
        }

        internal static void CloseAllWindows()
        {
            foreach (TestShooterDebugWindow window in
                     Resources.FindObjectsOfTypeAll<
                         TestShooterDebugWindow>())
            {
                window.Close();
            }
        }

        private void Update()
        {
            if (EditorApplication.timeSinceStartup <
                nextRepaintTime)
            {
                return;
            }

            nextRepaintTime =
                EditorApplication.timeSinceStartup +
                RepaintInterval;

            Repaint();
        }

        private void OnGUI()
        {
            scrollPosition =
                EditorGUILayout.BeginScrollView(
                    scrollPosition);

            DrawContent();

            EditorGUILayout.EndScrollView();
        }

        private void DrawContent()
        {
            EditorGUILayout.LabelField(
                "Test Shooter",
                EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "进入Play Mode后，窗口会自动连接" +
                    "BattlePrototype中的TestShooter。",
                    MessageType.Info);

                return;
            }

            TestShooter2D shooter =
                TestShooterDebugRuntimeBridge.Current;

            if (shooter == null)
            {
                EditorGUILayout.HelpBox(
                    "没有找到活动的TestShooter2D实例。" +
                    "切换场景或实例销毁后窗口会自动关闭。",
                    MessageType.Warning);

                return;
            }

            DrawRuntimeStatus(shooter);
            DrawProjectileSettings(shooter);
            DrawFireModes(shooter);
            DrawActions();
        }

        private static void DrawRuntimeStatus(
            TestShooter2D shooter)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Runtime Status",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Connected Object",
                    shooter.name);

                EditorGUILayout.LabelField(
                    "Is Firing",
                    shooter.IsFiring.ToString());

                EditorGUILayout.Vector2Field(
                    "Aim Direction",
                    shooter.AimDirection);
            }
        }

        private static void DrawProjectileSettings(
            TestShooter2D shooter)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Projectile Settings",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                Projectile2D prefab =
                    (Projectile2D)EditorGUILayout.ObjectField(
                        "Projectile Prefab",
                        shooter.ProjectilePrefab,
                        typeof(Projectile2D),
                        false);

                if (prefab != shooter.ProjectilePrefab)
                {
                    TestShooterDebugRuntimeBridge
                        .SetProjectilePrefab(prefab);
                }

                BattleFaction faction =
                    (BattleFaction)EditorGUILayout.EnumPopup(
                        "Faction",
                        shooter.Faction);

                if (faction != shooter.Faction)
                {
                    TestShooterDebugRuntimeBridge
                        .SetFaction(faction);
                }

                float damage =
                    EditorGUILayout.FloatField(
                        "Damage",
                        shooter.ProjectileDamage);

                if (!Mathf.Approximately(
                        damage,
                        shooter.ProjectileDamage))
                {
                    TestShooterDebugRuntimeBridge
                        .SetProjectileDamage(damage);
                }

                float speed =
                    EditorGUILayout.FloatField(
                        "Speed",
                        shooter.ProjectileSpeed);

                if (!Mathf.Approximately(
                        speed,
                        shooter.ProjectileSpeed))
                {
                    TestShooterDebugRuntimeBridge
                        .SetProjectileSpeed(speed);
                }

                float lifetime =
                    EditorGUILayout.FloatField(
                        "Lifetime",
                        shooter.ProjectileLifetime);

                if (!Mathf.Approximately(
                        lifetime,
                        shooter.ProjectileLifetime))
                {
                    TestShooterDebugRuntimeBridge
                        .SetProjectileLifetime(lifetime);
                }

                float triggerInterval =
                    EditorGUILayout.FloatField(
                        "Hold Trigger Interval",
                        shooter.ManualTriggerInterval);

                if (!Mathf.Approximately(
                        triggerInterval,
                        shooter.ManualTriggerInterval))
                {
                    TestShooterDebugRuntimeBridge
                        .SetManualTriggerInterval(
                            triggerInterval);
                }
            }
        }

        private void DrawFireModes(
            TestShooter2D shooter)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Fire Modes",
                EditorStyles.boldLabel);

            ProjectileFireModeController2D controller =
                shooter.FireModeController;

            if (controller == null)
            {
                EditorGUILayout.HelpBox(
                    "TestShooter缺少发射模式控制器。",
                    MessageType.Error);

                return;
            }

            IReadOnlyList<ProjectileFireMode> modes =
                controller.FireModes;

            int removeIndex = -1;

            for (int index = 0;
                 index < modes.Count;
                 index++)
            {
                ProjectileFireMode mode = modes[index];

                using (new EditorGUILayout.VerticalScope(
                           EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        $"Mode {index + 1}",
                        EditorStyles.boldLabel);

                    ProjectileFirePattern pattern =
                        (ProjectileFirePattern)
                        EditorGUILayout.ObjectField(
                            "Pattern",
                            mode?.Pattern,
                            typeof(ProjectileFirePattern),
                            false);

                    if (mode != null &&
                        pattern != mode.Pattern)
                    {
                        TestShooterDebugRuntimeBridge
                            .SetFireModePattern(
                                index,
                                pattern);
                    }

                    EditorGUILayout.LabelField(
                        "Interval",
                        "-1 (Manual)");

                    if (GUILayout.Button("删除此模式"))
                    {
                        removeIndex = index;
                    }
                }
            }

            if (removeIndex >= 0)
            {
                TestShooterDebugRuntimeBridge
                    .RemoveFireModeAt(removeIndex);
            }

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                patternToAdd =
                    (ProjectileFirePattern)
                    EditorGUILayout.ObjectField(
                        "New Pattern",
                        patternToAdd,
                        typeof(ProjectileFirePattern),
                        false);

                using (new EditorGUI.DisabledScope(
                           patternToAdd == null))
                {
                    if (GUILayout.Button("添加发射模式"))
                    {
                        TestShooterDebugRuntimeBridge
                            .AddManualFireMode(
                                patternToAdd);
                    }
                }
            }
        }

        private static void DrawActions()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Actions",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(
                       !TestShooterDebugRuntimeBridge.HasTarget))
            {
                if (GUILayout.Button(
                        "触发所有发射模式"))
                {
                    TestShooterDebugRuntimeBridge
                        .TriggerAllFireModes();
                }
            }
        }
    }
}
