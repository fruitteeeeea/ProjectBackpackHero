using System.Collections.Generic;
using BackpackHero.Battle;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    internal sealed class TestShooterDebugWindow : ScriptableObject
    {
        private Vector2 scrollPosition;
        private ProjectileFirePattern patternToAdd;
        private BattleAttack2D attackToAdd;
        internal void DrawTab()
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
            DrawAttackSettings(shooter);
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

        private static void DrawAttackSettings(
            TestShooter2D shooter)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Attack Settings",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
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

                if (HasProjectileMode(shooter))
                {
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
                            .SetProjectileLifetime(
                                lifetime);
                    }
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

        private static bool HasProjectileMode(
            TestShooter2D shooter)
        {
            ProjectileFireModeController2D controller =
                shooter.FireModeController;

            if (controller == null)
            {
                return shooter.DefaultAttackPrefab
                       is Projectile2D;
            }

            foreach (ProjectileFireMode mode
                     in controller.FireModes)
            {
                if (mode?.AttackPrefab
                    is Projectile2D)
                {
                    return true;
                }
            }

            return false;
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

                    BattleAttack2D attackPrefab =
                        (BattleAttack2D)
                        EditorGUILayout.ObjectField(
                            "Attack Prefab",
                            mode?.AttackPrefab,
                            typeof(BattleAttack2D),
                            false);

                    if (mode != null &&
                        attackPrefab !=
                        mode.AttackPrefab)
                    {
                        TestShooterDebugRuntimeBridge
                            .SetFireModeAttackPrefab(
                                index,
                                attackPrefab);
                    }

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
                attackToAdd =
                    (BattleAttack2D)
                    EditorGUILayout.ObjectField(
                        "New Attack",
                        attackToAdd,
                        typeof(BattleAttack2D),
                        false);

                patternToAdd =
                    (ProjectileFirePattern)
                    EditorGUILayout.ObjectField(
                        "New Pattern",
                        patternToAdd,
                        typeof(ProjectileFirePattern),
                        false);

                using (new EditorGUI.DisabledScope(
                           attackToAdd == null ||
                           patternToAdd == null))
                {
                    if (GUILayout.Button("添加发射模式"))
                    {
                        TestShooterDebugRuntimeBridge
                            .AddManualFireMode(
                                attackToAdd,
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
