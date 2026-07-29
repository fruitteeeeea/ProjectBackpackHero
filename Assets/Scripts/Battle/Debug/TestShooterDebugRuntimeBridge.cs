using System;

namespace BackpackHero.Battle
{
    /// <summary>
    /// Runtime安全的TestShooter调试入口。
    /// Editor工具只能通过此桥接层或TestShooter的公开接口修改配置。
    /// </summary>
    public static class TestShooterDebugRuntimeBridge
    {
        private static TestShooter2D current;

        public static event Action TargetAvailable;
        public static event Action TargetUnavailable;

        public static bool HasTarget => Current != null;

        public static TestShooter2D Current
        {
            get
            {
                if (current == null)
                {
                    current = null;
                }

                return current;
            }
        }

        public static bool Register(TestShooter2D shooter)
        {
            if (shooter == null)
            {
                return false;
            }

            if (Current == shooter)
            {
                return true;
            }

            if (Current != null)
            {
                UnityEngine.Debug.LogWarning(
                    "场景中存在多个活动的TestShooter。" +
                    $"调试面板继续连接{Current.name}，" +
                    $"忽略{shooter.name}。",
                    shooter);

                return false;
            }

            current = shooter;
            TargetAvailable?.Invoke();
            return true;
        }

        public static void Unregister(TestShooter2D shooter)
        {
            if (shooter == null || Current != shooter)
            {
                return;
            }

            current = null;
            TargetUnavailable?.Invoke();
        }

        public static void SetDefaultAttackPrefab(
            BattleAttack2D prefab)
        {
            Current?.SetDefaultAttackPrefab(prefab);
        }

        public static void SetFaction(
            BattleFaction faction)
        {
            Current?.SetFaction(faction);
        }

        public static void SetProjectileDamage(float damage)
        {
            Current?.SetProjectileDamage(damage);
        }

        public static void SetProjectileSpeed(float speed)
        {
            Current?.SetProjectileSpeed(speed);
        }

        public static void SetProjectileLifetime(float lifetime)
        {
            Current?.SetProjectileLifetime(lifetime);
        }

        public static void SetManualTriggerInterval(
            float interval)
        {
            Current?.SetManualTriggerInterval(interval);
        }

        public static int AddManualFireMode(
            ProjectileFirePattern pattern)
        {
            return Current != null
                ? Current.AddManualFireMode(pattern)
                : -1;
        }

        public static int AddManualFireMode(
            BattleAttack2D attackPrefab,
            ProjectileFirePattern pattern)
        {
            return Current != null
                ? Current.AddManualFireMode(
                    attackPrefab,
                    pattern)
                : -1;
        }

        public static bool RemoveFireModeAt(int index)
        {
            return Current != null &&
                   Current.RemoveFireModeAt(index);
        }

        public static bool SetFireModePattern(
            int index,
            ProjectileFirePattern pattern)
        {
            return Current != null &&
                   Current.SetFireModePattern(
                       index,
                       pattern);
        }

        public static bool SetFireModeAttackPrefab(
            int index,
            BattleAttack2D attackPrefab)
        {
            return Current != null &&
                   Current.SetFireModeAttackPrefab(
                       index,
                       attackPrefab);
        }

        public static int TriggerAllFireModes()
        {
            return Current != null
                ? Current.TriggerAllFireModes()
                : 0;
        }
    }
}
