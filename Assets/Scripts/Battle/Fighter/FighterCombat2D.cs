using UnityEngine;
using UnityEngine.Serialization;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 负责飞机的索敌、停止移动和瞄准行为。
    /// </summary>
    [RequireComponent(typeof(Fighter2D))]
    [RequireComponent(typeof(DirectionalMover2D))]
    [RequireComponent(typeof(BattleCurveFollower2D))]
    [RequireComponent(
        typeof(ProjectileFireModeController2D))]
    public sealed class FighterCombat2D :
        MonoBehaviour
    {
        [Header("Target Search")]
        [Tooltip("重新搜索目标的时间间隔。")]
        [SerializeField, Min(0.02f)]
        private float targetScanInterval = 0.1f;

        [Tooltip("当前飞机实例的索敌距离偏移。由生成器在生成时设置。")]
        [SerializeField]
        private float attackRangeOffset;
        
        [Header("Shooting")]
        [SerializeField]
        [FormerlySerializedAs("projectilePrefab")]
        private BattleAttack2D defaultAttackPrefab;

        [SerializeField]
        private Transform firePoint;

        [SerializeField]
        private ProjectileFireModeController2D
            fireModeController;

        [SerializeField]
        private ProjectileFirePattern defaultFirePattern;

        [Header("Runtime Debug")]
        [SerializeField]
        private HurtBox2D currentTarget;

        [SerializeField]
        private bool isInCombat;

        private Fighter2D fighter;
        private DirectionalMover2D mover;
        private BattleCurveFollower2D curveFollower;

        private float targetScanTimer;
        private Vector2 directionBeforeCombat =
            Vector2.up;

        private bool hasReportedMissingShootingConfiguration;
        
        public HurtBox2D CurrentTarget =>
            currentTarget;

        public bool HasTarget =>
            currentTarget != null &&
            currentTarget.IsAlive;

        public bool IsInCombat =>
            isInCombat;

        public float AttackRangeOffset =>
            attackRangeOffset;

        public ProjectileFireModeController2D
            FireModeController => fireModeController;

        public float EffectiveAttackRange =>
            fighter != null &&
            fighter.Definition != null
                ? Mathf.Max(
                    0.1f,
                    fighter.Definition.AttackRange +
                    attackRangeOffset)
                : 0.1f;

        private void Awake()
        {
            fighter =
                GetComponent<Fighter2D>();

            mover =
                GetComponent<DirectionalMover2D>();

            curveFollower =
                GetComponent<BattleCurveFollower2D>();

            fireModeController =
                GetComponent<
                    ProjectileFireModeController2D>();
        }

        private void OnEnable()
        {
            targetScanTimer = 0f;
            currentTarget = null;
            isInCombat = false;
            hasReportedMissingShootingConfiguration = false;

            if (fireModeController != null)
            {
                fireModeController.ShotRequested -=
                    HandleShotRequested;

                fireModeController.ShotRequested +=
                    HandleShotRequested;

                fireModeController
                    .AssignMissingAttackPrefabs(
                        defaultAttackPrefab);

                fireModeController.ResetCooldowns();
            }
        }

        private void OnDisable()
        {
            if (fireModeController != null)
            {
                fireModeController.ShotRequested -=
                    HandleShotRequested;
            }

            currentTarget = null;
            ExitCombat();
        }

        /// <summary>
        /// 设置当前飞机实例独立的索敌距离偏移。
        /// 不会修改所有飞机共享的FighterDefinition。
        /// </summary>
        public void SetAttackRangeOffset(float offset)
        {
            attackRangeOffset = offset;
        }

        /// <summary>
        /// 飞机生成后用自身Definition的射击间隔配置默认朝前单发模式。
        /// </summary>
        public void ConfigureDefaultFireMode(
            float attackInterval)
        {
            if (fireModeController == null)
            {
                fireModeController =
                    GetComponent<
                        ProjectileFireModeController2D>();
            }

            if (fireModeController == null ||
                defaultFirePattern == null)
            {
                ReportMissingShootingConfiguration();
                return;
            }

            fireModeController.ReplaceWithSingleMode(
                defaultAttackPrefab,
                defaultFirePattern,
                attackInterval);
        }
        
        private void Update()
        {
            if (!BattleFlowController.IsCombatPhase)
            {
                currentTarget = null;
                ExitCombat();
                return;
            }

            if (fighter == null ||
                !fighter.IsAlive ||
                fighter.Definition == null)
            {
                currentTarget = null;
                ExitCombat();
                return;
            }

            ValidateCurrentTarget();

            UpdateTargetSearch(
                Time.deltaTime);

            UpdateCombatBehaviour(
                Time.deltaTime);
        }

        private void ValidateCurrentTarget()
        {
            if (currentTarget == null)
            {
                return;
            }

            if (!IsValidTarget(currentTarget))
            {
                ClearCurrentTarget();
                return;
            }

            Vector3 targetPosition =
                GetTargetPosition(currentTarget);

            float distanceSquared =
            (
                targetPosition -
                transform.position
            ).sqrMagnitude;

            float attackRange =
                EffectiveAttackRange;

            if (distanceSquared >
                attackRange * attackRange)
            {
                ClearCurrentTarget();
            }
        }

        private void ClearCurrentTarget()
        {
            currentTarget = null;

            // 下一帧不必继续等待扫描间隔，
            // 可以立即搜索其他目标。
            targetScanTimer = 0f;
        }
        
        private void UpdateTargetSearch(
            float deltaTime)
        {
            targetScanTimer -= deltaTime;

            if (targetScanTimer > 0f)
            {
                return;
            }

            targetScanTimer =
                targetScanInterval;

            FindBestTarget();
        }

        private void UpdateCombatBehaviour(
            float deltaTime)
        {
            if (!HasTarget)
            {
                ExitCombat();
                return;
            }

            EnterCombat();
            AimAtCurrentTarget();
            UpdateShooting(deltaTime);
        }

        private void EnterCombat()
        {
            if (isInCombat)
            {
                return;
            }

            isInCombat = true;

            fireModeController?.ResetCooldowns();

            if (mover != null)
            {
                directionBeforeCombat =
                    mover.Direction;

                mover.SetPaused(true);
            }

            if (curveFollower != null)
            {
                curveFollower.SetPaused(true);
            }
        }

        private void ExitCombat()
        {
            if (!isInCombat)
            {
                return;
            }

            isInCombat = false;

            fireModeController?.ResetCooldowns();

            if (mover != null)
            {
                mover.SetDirection(
                    directionBeforeCombat);

                mover.SetPaused(false);
            }

            if (curveFollower != null)
            {
                curveFollower.SetPaused(false);
            }
        }

        private void AimAtCurrentTarget()
        {
            if (currentTarget == null ||
                mover == null)
            {
                return;
            }

            Vector3 targetPosition =
                GetTargetPosition(
                    currentTarget);

            Vector2 aimDirection =
                targetPosition -
                transform.position;

            if (aimDirection.sqrMagnitude <=
                Mathf.Epsilon)
            {
                return;
            }

            // SetDirection即使在暂停状态下，
            // 仍会更新飞机的视觉朝向。
            mover.SetDirection(
                aimDirection);
        }
        
        private void UpdateShooting(
            float deltaTime)
        {
            if (fighter == null ||
                fighter.Definition == null ||
                currentTarget == null ||
                fireModeController == null)
            {
                return;
            }

            fireModeController.Tick(
                deltaTime,
                transform.up);
        }

        private void HandleShotRequested(
            BattleShotRequest request)
        {
            BattleAttack2D attackPrefab =
                request.AttackPrefab != null
                    ? request.AttackPrefab
                    : defaultAttackPrefab;

            if (fighter == null ||
                fighter.Definition == null ||
                attackPrefab == null ||
                firePoint == null ||
                currentTarget == null)
            {
                ReportMissingShootingConfiguration();
                return;
            }

            Vector2 fireDirection =
                request.Direction;

            if (fireDirection.sqrMagnitude <=
                Mathf.Epsilon)
            {
                return;
            }

            BattleAttack2D attack =
                Instantiate(
                    attackPrefab,
                    firePoint.position,
                    Quaternion.FromToRotation(
                        Vector2.up,
                        fireDirection));

            Vector2 targetPosition =
                GetTargetPosition(currentTarget);

            BattleAttackLaunchContext
                launchContext =
                    BattleAttackLaunchContext
                        .WithAimPoint(
                            fighter.Faction,
                            fighter.Definition
                                .ProjectileDamage,
                            fighter.Definition
                                .ProjectileSpeed,
                            -1f,
                            firePoint.position,
                            fireDirection,
                            transform.position,
                            transform.up,
                            targetPosition);

            attack.Initialize(launchContext);
        }

        private void ReportMissingShootingConfiguration()
        {
            if (hasReportedMissingShootingConfiguration)
            {
                return;
            }

            hasReportedMissingShootingConfiguration = true;

            Debug.LogWarning(
                $"{name}无法发射：请检查Attack Prefab、" +
                "Fire Point、Fire Mode Controller和" +
                "Default Fire Pattern。",
                this);
        }

        private static Vector3 GetTargetPosition(
            HurtBox2D target)
        {
            if (target.TargetHealth != null)
            {
                return target
                    .TargetHealth
                    .transform
                    .position;
            }

            return target.transform.position;
        }

        private void FindBestTarget()
        {
            float attackRange =
                EffectiveAttackRange;

            int enemyHurtBoxLayer =
                BattlePhysicsLayers.GetHurtBoxLayer(
                    GetEnemyFaction());

            if (enemyHurtBoxLayer < 0)
            {
                currentTarget = null;
                return;
            }

            int targetLayerMask =
                1 << enemyHurtBoxLayer;

            Collider2D[] colliders =
                Physics2D.OverlapCircleAll(
                    transform.position,
                    attackRange,
                    targetLayerMask);

            HurtBox2D nearestFighter = null;
            HurtBox2D nearestBackpack = null;

            float bestFighterAlignment =
                float.NegativeInfinity;

            float bestFighterDistanceSquared =
                float.PositiveInfinity;

            float nearestBackpackDistanceSquared =
                float.PositiveInfinity;

            foreach (Collider2D candidateCollider
                     in colliders)
            {
                if (candidateCollider == null)
                {
                    continue;
                }

                HurtBox2D candidate =
                    candidateCollider.GetComponent<
                        HurtBox2D>();

                if (!IsValidTarget(candidate))
                {
                    continue;
                }

                float distanceSquared =
                    (
                        GetTargetPosition(candidate) -
                        transform.position
                    ).sqrMagnitude;

                if (candidate.TargetType ==
                    BattleTargetType.Fighter)
                {
                    if (!TryGetFighterAlignment(
                            candidate,
                            out float alignment))
                    {
                        continue;
                    }

                    bool isBetterAligned =
                        alignment >
                        bestFighterAlignment + 0.0001f;

                    bool hasSameAlignment =
                        Mathf.Abs(
                            alignment -
                            bestFighterAlignment) <= 0.0001f;

                    bool isCloserAtSameAlignment =
                        hasSameAlignment &&
                        distanceSquared <
                        bestFighterDistanceSquared;

                    if (isBetterAligned ||
                        isCloserAtSameAlignment)
                    {
                        nearestFighter =
                            candidate;

                        bestFighterAlignment =
                            alignment;

                        bestFighterDistanceSquared =
                            distanceSquared;
                    }

                    continue;
                }

                if (candidate.TargetType ==
                    BattleTargetType.Backpack &&
                    distanceSquared <
                    nearestBackpackDistanceSquared)
                {
                    nearestBackpack =
                        candidate;

                    nearestBackpackDistanceSquared =
                        distanceSquared;
                }
            }

            currentTarget =
                nearestFighter != null
                    ? nearestFighter
                    : nearestBackpack;
        }

        private bool TryGetFighterAlignment(
            HurtBox2D candidate,
            out float alignment)
        {
            alignment = -1f;

            if (candidate == null ||
                mover == null ||
                fighter.Definition == null)
            {
                return false;
            }

            Vector2 forward =
                mover.Direction.normalized;

            Vector2 directionToTarget =
                GetTargetPosition(candidate) -
                transform.position;

            if (directionToTarget.sqrMagnitude <=
                Mathf.Epsilon)
            {
                alignment = 1f;
                return true;
            }

            directionToTarget.Normalize();

            alignment =
                Vector2.Dot(
                    forward,
                    directionToTarget);

            float halfAngle =
                fighter.Definition
                    .TargetingArcAngle * 0.5f;

            float minimumAlignment =
                Mathf.Cos(
                    halfAngle *
                    Mathf.Deg2Rad);

            return alignment >= minimumAlignment;
        }
        
        private bool IsValidTarget(
            HurtBox2D candidate)
        {
            if (candidate == null ||
                !candidate.IsAlive)
            {
                return false;
            }

            if (candidate.Faction ==
                fighter.Faction)
            {
                return false;
            }

            if (candidate.transform.IsChildOf(
                    transform))
            {
                return false;
            }

            return true;
        }

        private BattleFaction GetEnemyFaction()
        {
            return fighter.Faction ==
                   BattleFaction.Player
                ? BattleFaction.Enemy
                : BattleFaction.Player;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Fighter2D fighterComponent =
                fighter != null
                    ? fighter
                    : GetComponent<Fighter2D>();

            if (fighterComponent == null ||
                fighterComponent.Definition == null)
            {
                return;
            }

            float attackRange =
                Application.isPlaying &&
                fighterComponent == fighter
                    ? EffectiveAttackRange
                    : Mathf.Max(
                        0.1f,
                        fighterComponent.Definition.AttackRange +
                        attackRangeOffset);

            Gizmos.color =
                new Color(1f, 0.8f, 0.1f, 0.8f);

            Gizmos.DrawWireSphere(
                transform.position,
                attackRange);

            Vector2 forward =
                fighterComponent.FactionMember != null
                    ? GetComponent<DirectionalMover2D>()
                        .Direction
                    : (Vector2)transform.up;

            float halfAngle =
                fighterComponent.Definition
                    .TargetingArcAngle * 0.5f;

            Vector2 leftBoundary =
                Quaternion.Euler(
                    0f,
                    0f,
                    halfAngle) * forward;

            Vector2 rightBoundary =
                Quaternion.Euler(
                    0f,
                    0f,
                    -halfAngle) * forward;

            Gizmos.color =
                new Color(0.2f, 1f, 0.4f, 0.9f);

            Gizmos.DrawLine(
                transform.position,
                transform.position +
                (Vector3)(
                    leftBoundary * attackRange));

            Gizmos.DrawLine(
                transform.position,
                transform.position +
                (Vector3)(
                    rightBoundary * attackRange));
            
            if (currentTarget == null)
            {
                return;
            }

            Gizmos.color = Color.red;

            Gizmos.DrawLine(
                transform.position,
                GetTargetPosition(currentTarget));
        }

        private void OnValidate()
        {
            targetScanInterval =
                Mathf.Max(
                    0.02f,
                    targetScanInterval);
        }
#endif
    }
}
