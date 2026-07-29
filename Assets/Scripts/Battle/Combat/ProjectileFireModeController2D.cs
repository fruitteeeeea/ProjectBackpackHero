using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Battle
{
    public readonly struct BattleShotRequest
    {
        public BattleAttack2D AttackPrefab { get; }
        public Vector2 Direction { get; }
        public int ModeIndex { get; }

        public BattleShotRequest(
            BattleAttack2D attackPrefab,
            Vector2 direction,
            int modeIndex)
        {
            AttackPrefab = attackPrefab;
            Direction =
                direction.sqrMagnitude > Mathf.Epsilon
                    ? direction.normalized
                    : Vector2.up;
            ModeIndex = modeIndex;
        }
    }

    /// <summary>
    /// 一个发射模式及其独立冷却。
    /// interval小于0表示只能被主动触发。
    /// </summary>
    [Serializable]
    public sealed class ProjectileFireMode
    {
        [SerializeField]
        private BattleAttack2D attackPrefab;

        [SerializeField]
        private ProjectileFirePattern pattern;

        [SerializeField]
        private float interval = 0.5f;

        [NonSerialized]
        private float remainingCooldown;

        public BattleAttack2D AttackPrefab =>
            attackPrefab;

        public ProjectileFirePattern Pattern => pattern;
        public float Interval => interval;
        public float RemainingCooldown => remainingCooldown;
        public bool IsManual => interval < 0f;

        public ProjectileFireMode(
            BattleAttack2D newAttackPrefab,
            ProjectileFirePattern firePattern,
            float fireInterval)
        {
            attackPrefab = newAttackPrefab;
            pattern = firePattern;
            SetInterval(fireInterval);
            ResetCooldown();
        }

        public void SetAttackPrefab(
            BattleAttack2D newAttackPrefab)
        {
            attackPrefab = newAttackPrefab;
        }

        public void SetPattern(
            ProjectileFirePattern firePattern)
        {
            pattern = firePattern;
        }

        public void SetInterval(float fireInterval)
        {
            interval =
                fireInterval < 0f
                    ? ProjectileFireModeController2D
                        .ManualInterval
                    : Mathf.Max(
                        ProjectileFireModeController2D
                            .MinimumAutomaticInterval,
                        fireInterval);
        }

        public void ResetCooldown()
        {
            remainingCooldown = 0f;
        }

        internal bool Tick(float deltaTime)
        {
            if (IsManual || pattern == null)
            {
                return false;
            }

            remainingCooldown -=
                Mathf.Max(0f, deltaTime);

            if (remainingCooldown > 0f)
            {
                return false;
            }

            remainingCooldown = interval;
            return true;
        }
    }

    /// <summary>
    /// 可被飞机、测试射手和其他武器复用的发射模式控制器。
    /// 每个模式独立计时，冷却完成后请求对应方向的子弹。
    /// </summary>
    public sealed class ProjectileFireModeController2D :
        MonoBehaviour
    {
        public const float ManualInterval = -1f;
        public const float MinimumAutomaticInterval = 0.02f;

        [SerializeField]
        private List<ProjectileFireMode> fireModes = new();

        private readonly List<Vector2> directionBuffer = new();

        public event Action<BattleShotRequest>
            ShotRequested;

        public IReadOnlyList<ProjectileFireMode> FireModes
        {
            get
            {
                EnsureModeList();
                return fireModes;
            }
        }

        public int ModeCount =>
            fireModes != null
                ? fireModes.Count
                : 0;

        private void OnEnable()
        {
            ResetCooldowns();
        }

        public void Tick(
            float deltaTime,
            Vector2 forward)
        {
            if (fireModes == null)
            {
                return;
            }

            for (int index = 0;
                 index < fireModes.Count;
                 index++)
            {
                ProjectileFireMode mode =
                    fireModes[index];

                if (mode != null &&
                    mode.Tick(deltaTime))
                {
                    TriggerMode(
                        mode,
                        index,
                        forward);
                }
            }
        }

        public int TriggerAll(Vector2 forward)
        {
            if (fireModes == null)
            {
                return 0;
            }

            int shotCount = 0;

            for (int index = 0;
                 index < fireModes.Count;
                 index++)
            {
                ProjectileFireMode mode =
                    fireModes[index];

                shotCount +=
                    TriggerMode(
                        mode,
                        index,
                        forward);
            }

            return shotCount;
        }

        public int AddMode(
            BattleAttack2D attackPrefab,
            ProjectileFirePattern pattern,
            float interval)
        {
            EnsureModeList();

            fireModes.Add(
                new ProjectileFireMode(
                    attackPrefab,
                    pattern,
                    interval));

            return fireModes.Count - 1;
        }

        /// <summary>
        /// 兼容只验证Pattern与冷却的旧调用。
        /// 实际生成攻击时应使用带AttackPrefab的重载。
        /// </summary>
        public int AddMode(
            ProjectileFirePattern pattern,
            float interval)
        {
            return AddMode(
                null,
                pattern,
                interval);
        }

        public bool RemoveModeAt(int index)
        {
            if (fireModes == null ||
                index < 0 ||
                index >= fireModes.Count)
            {
                return false;
            }

            fireModes.RemoveAt(index);
            return true;
        }

        public bool SetModePattern(
            int index,
            ProjectileFirePattern pattern)
        {
            ProjectileFireMode mode =
                GetMode(index);

            if (mode == null)
            {
                return false;
            }

            mode.SetPattern(pattern);
            return true;
        }

        public bool SetModeAttackPrefab(
            int index,
            BattleAttack2D attackPrefab)
        {
            ProjectileFireMode mode =
                GetMode(index);

            if (mode == null)
            {
                return false;
            }

            mode.SetAttackPrefab(attackPrefab);
            return true;
        }

        public bool SetModeInterval(
            int index,
            float interval)
        {
            ProjectileFireMode mode =
                GetMode(index);

            if (mode == null)
            {
                return false;
            }

            mode.SetInterval(interval);
            mode.ResetCooldown();
            return true;
        }

        public void ReplaceWithSingleMode(
            BattleAttack2D attackPrefab,
            ProjectileFirePattern pattern,
            float interval)
        {
            EnsureModeList();
            fireModes.Clear();
            AddMode(
                attackPrefab,
                pattern,
                interval);
        }

        public void ReplaceWithSingleMode(
            ProjectileFirePattern pattern,
            float interval)
        {
            ReplaceWithSingleMode(
                null,
                pattern,
                interval);
        }

        public void AssignMissingAttackPrefabs(
            BattleAttack2D fallbackPrefab)
        {
            if (fallbackPrefab == null ||
                fireModes == null)
            {
                return;
            }

            foreach (ProjectileFireMode mode
                     in fireModes)
            {
                if (mode != null &&
                    mode.AttackPrefab == null)
                {
                    mode.SetAttackPrefab(
                        fallbackPrefab);
                }
            }
        }

        public void ResetCooldowns()
        {
            if (fireModes == null)
            {
                return;
            }

            foreach (ProjectileFireMode mode in fireModes)
            {
                mode?.ResetCooldown();
            }
        }

        private int TriggerMode(
            ProjectileFireMode mode,
            int modeIndex,
            Vector2 forward)
        {
            if (mode == null ||
                mode.Pattern == null)
            {
                return 0;
            }

            directionBuffer.Clear();

            mode.Pattern.AppendDirections(
                forward,
                directionBuffer);

            foreach (Vector2 direction in directionBuffer)
            {
                ShotRequested?.Invoke(
                    new BattleShotRequest(
                        mode.AttackPrefab,
                        direction,
                        modeIndex));
            }

            return directionBuffer.Count;
        }

        private ProjectileFireMode GetMode(int index)
        {
            if (fireModes == null ||
                index < 0 ||
                index >= fireModes.Count)
            {
                return null;
            }

            return fireModes[index];
        }

        private void EnsureModeList()
        {
            fireModes ??= new List<ProjectileFireMode>();
        }
    }
}
