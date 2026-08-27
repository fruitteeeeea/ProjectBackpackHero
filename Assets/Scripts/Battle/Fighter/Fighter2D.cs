using BackpackHero.Debugging;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 表示场景中的一架飞机。
    /// FighterDefinition保存静态配置，
    /// Health保存运行时生命值。
    /// </summary>
    [RequireComponent(typeof(DirectionalMover2D))]
    [RequireComponent(typeof(Health))]
    
    [RequireComponent(typeof(FactionMember))]
    
    public sealed class Fighter2D : MonoBehaviour
    {
        private const float OvertimePenaltyRetryInterval = 1f;
        private const float OvertimeDamageMultiplier = 0.1f;
        private const float InitialInvulnerabilityDuration = 0.1f;

        [Header("Overtime Penalty")]
        [SerializeField]
        private BattleAttack2D overtimePenaltyProjectilePrefab;

        private SpriteRenderer spriteRenderer;
        private DirectionalMover2D mover;
        private Health health;
        private FighterFeedbacks feedbacks;
        
        private FactionMember factionMember;
        
        private HurtBox2D[] hurtBoxes;

        private HealthBar[] healthBars;

        private WorldSpaceHealthBarFollower2D[]
            healthBarFollowers;
        
        private FighterDefinition definition;
        private bool isDying;
        private ItemInstance damageSourceItem;
        private float progressionHealthMultiplier = 1f;
        private float progressionDamageMultiplier = 1f;
        private bool appliesEnemyLevelStrength;
        private LifetimeAndScreenBounds2D lifetime;
        private bool overtimePenaltyActive;
        private float overtimePenaltyRetryTimer;
        private float initialInvulnerabilityEndsAt;

        public FighterDefinition Definition =>
            definition;

        public Health Health =>
            health;

        public FactionMember FactionMember =>
            factionMember;

        public BattleFaction Faction =>
            factionMember != null
                ? factionMember.Faction
                : BattleFaction.Player;
        
        public float CurrentHealth =>
            health != null
                ? health.CurrentHealth
                : 0f;

        public float MaximumHealth =>
            health != null
                ? health.MaxHealth
                : 0f;

        public bool IsAlive =>
            health != null &&
            !health.IsDead;
        /// <summary>
        /// 背包飞机生成后的短暂无敌状态。
        /// </summary>
        public bool IsInitialInvulnerable =>
            Time.time < initialInvulnerabilityEndsAt;
        public ItemInstance DamageSourceItem => damageSourceItem;
        public float ProgressionDamageMultiplier => progressionDamageMultiplier;
        /// <summary>
        /// 生成时确定的关卡伤害倍率。调试开关后不会改变既有敌机。
        /// </summary>
        public float LevelDifficultyDamageMultiplier =>
            appliesEnemyLevelStrength
                ? LevelDifficultyRuntime.GetProjectileDamageMultiplier(
                    Faction)
                : 1f;

        /// <summary>
        /// 本架飞机生成时截取的战斗曲线值。曲线输入之后变化不会影响它。
        /// </summary>
        public float BattleCurveValue { get; private set; }
        /// <summary>
        /// 飞机到达自身寿命上限后进入超时状态，后续造成的伤害降为原本的10%。
        /// </summary>
        public float EffectiveDamageMultiplier =>
            progressionDamageMultiplier *
            (lifetime != null && lifetime.HasLifetimeExpired
                ? OvertimeDamageMultiplier
                : 1f);
        public void SetDamageSourceItem(ItemInstance item) => damageSourceItem = item;

        public void SetBattleCurveValue(float value)
        {
            BattleCurveValue = Mathf.Clamp(value, -1f, 1f);
        }

        private void Awake()
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>(
                    true);

            mover =
                GetComponent<DirectionalMover2D>();

            health =
                GetComponent<Health>();

            feedbacks =
                GetComponent<FighterFeedbacks>();
            
            factionMember =
                GetComponent<FactionMember>();

            hurtBoxes =
                GetComponentsInChildren<HurtBox2D>(
                    true);
            
            healthBars =
                GetComponentsInChildren<HealthBar>(
                    true);

            healthBarFollowers =
                GetComponentsInChildren<
                    WorldSpaceHealthBarFollower2D>(
                    true);

            lifetime = GetComponent<LifetimeAndScreenBounds2D>();

            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
            if (lifetime != null)
            {
                lifetime.LifetimeExpired += HandleLifetimeExpired;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
                health.Died -= HandleDied;
            }

            if (lifetime != null)
            {
                lifetime.LifetimeExpired -= HandleLifetimeExpired;
            }
        }

        private void OnEnable()
        {
            GamePacingDebugRuntime.MultipliersChanged +=
                HandlePacingChanged;
            LevelDifficultyRuntime.Changed += HandleLevelDifficultyChanged;
            AircraftVisualDebugRuntime.SettingsChanged +=
                HandleAircraftVisualSettingsChanged;
            ApplyStyleLifetime();
        }

        private void OnDisable()
        {
            GamePacingDebugRuntime.MultipliersChanged -=
                HandlePacingChanged;
            LevelDifficultyRuntime.Changed -= HandleLevelDifficultyChanged;
            AircraftVisualDebugRuntime.SettingsChanged -=
                HandleAircraftVisualSettingsChanged;
            StopOvertimePenalty();
        }

        private void Update()
        {
            if (!overtimePenaltyActive || !IsAlive)
            {
                return;
            }

            overtimePenaltyRetryTimer -= Time.deltaTime;
            if (overtimePenaltyRetryTimer <= 0f)
            {
                TryFireOvertimePenalty();
            }
        }

        /// <summary>
        /// 生成飞机后调用。
        /// factionColor用于区分玩家和敌人阵营。
        /// </summary>
        public void Initialize(
            FighterDefinition fighterDefinition,
            BattleFaction faction,
            Color factionColor,
            float healthMultiplier = 1f,
            float damageMultiplier = 1f)
        {
            if (fighterDefinition == null)
            {
                Debug.LogError(
                    "无法初始化飞机：FighterDefinition为空。",
                    this);

                return;
            }

            definition = fighterDefinition;
            isDying = false;
            initialInvulnerabilityEndsAt =
                Time.time + InitialInvulnerabilityDuration;
            progressionHealthMultiplier = Mathf.Max(0.01f, healthMultiplier);
            progressionDamageMultiplier = Mathf.Max(0.01f, damageMultiplier);
            appliesEnemyLevelStrength = faction != BattleFaction.Enemy ||
                LevelDifficultyRuntime.Instance?.EnemyStrengthEnabled == true;
            
            factionMember.SetFaction(faction);
            
            ConfigureHurtBoxes(faction);

            spriteRenderer.sprite =
                fighterDefinition.Sprite;

            spriteRenderer.color =
                fighterDefinition.BaseColor *
                factionColor;

            mover.SetBaseSpeed(
                fighterDefinition.BaseSpeed);

            ApplyPacingHealth();
            ApplyStyleLifetime();
            FighterCombat2D combat =
                GetComponent<FighterCombat2D>();

            if (combat != null)
            {
                combat.ConfigureDefaultFireMode(
                    fighterDefinition.AttackInterval,
                    fighterDefinition.DefaultAttackPrefab,
                    fighterDefinition.DefaultFirePattern);
            }

            FighterDeathExplosion2D deathExplosion =
                GetComponent<FighterDeathExplosion2D>() ??
                gameObject.AddComponent<FighterDeathExplosion2D>();
            deathExplosion.Configure(
                fighterDefinition.DeathExplosionAttackPrefab);

            if (GetComponent<FighterEquipmentLaserLink2D>() == null)
            {
                gameObject.AddComponent<FighterEquipmentLaserLink2D>();
            }

            ConfigureHealthBars();
            SetHealthBarsVisible(false);
            
            gameObject.name =
                fighterDefinition.DisplayName;
        }

        /// <summary>
        /// 临时保留伤害入口。
        /// 后续HurtBox会直接调用Health。
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsAlive || IsInitialInvulnerable || damage <= 0f)
            {
                return;
            }

            health.DecreaseHealth(damage);
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            health.IncreaseHealth(amount);
        }

        private void HandlePacingChanged(
            GamePacingMultipliers _)
        {
            ApplyPacingHealth();
        }

        private void HandleLevelDifficultyChanged()
        {
            ApplyPacingHealth();
        }

        private void HandleAircraftVisualSettingsChanged(
            AircraftVisualSettings _)
        {
            ApplyStyleLifetime();
        }

        private void ApplyPacingHealth()
        {
            if (health == null || definition == null)
            {
                return;
            }

            health.SetMaximumHealthAndFill(
                definition.MaximumHealth *
                progressionHealthMultiplier *
                GamePacingDebugRuntime.GetAircraftHealthMultiplier(
                    Faction) *
                (appliesEnemyLevelStrength
                    ? LevelDifficultyRuntime.GetAircraftHealthMultiplier(
                        Faction)
                    : 1f) *
                (LevelFlowController.Instance?.IsOvertime == true
                    ? LevelFlowController.OvertimeAircraftHealthMultiplier
                    : 1f));
        }

        private void ApplyStyleLifetime()
        {
            if (lifetime == null)
            {
                return;
            }

            AircraftVisualSettings visualSettings =
                AircraftVisualDebugRuntime.CurrentSettings;
            lifetime.SetLifetime(
                lifetime.ConfiguredLifetime *
                StyleTendencyDebugRuntime
                    .GetAircraftLifetimeMultiplier());
            lifetime.SetDestroyWhenLifetimeExpires(
                !visualSettings.AircraftVisualOverridesEnabled ||
                visualSettings.AircraftLifetimeEnabled);
        }

        private void HandleLifetimeExpired()
        {
            DamageStatisticsRuntime.RecordOvertimeAircraftExit(Faction);

            if (lifetime == null ||
                lifetime.DestroyWhenLifetimeExpires ||
                !IsAlive)
            {
                return;
            }

            overtimePenaltyActive = true;
            TryFireOvertimePenalty();
        }

        private void TryFireOvertimePenalty()
        {
            overtimePenaltyRetryTimer = OvertimePenaltyRetryInterval;

            if (overtimePenaltyProjectilePrefab == null)
            {
                Debug.LogWarning(
                    $"{name}超时惩罚未发射：缺少贝塞尔子弹Prefab。",
                    this);
                return;
            }

            Fighter2D nearestEnemy = FindNearestLivingEnemy();
            AircraftVisualSettings visualSettings =
                AircraftVisualDebugRuntime.CurrentSettings;
            ProjectileVisualSource visualSource =
                visualSettings.AircraftVisualOverridesEnabled &&
                visualSettings.HighlightOvertimePenaltyProjectile
                    ? ProjectileVisualSource.OvertimePenalty
                    : ProjectileVisualSource.Equipment;

            if (nearestEnemy != null &&
                nearestEnemy.TryGetComponent(out FighterCombat2D combat) &&
                combat.IsPositionWithinTargetingArc(
                    transform.position,
                    2f) &&
                combat.FireAttackAtPoint(
                    overtimePenaltyProjectilePrefab,
                    transform.position,
                    visualSource,
                    countsForDamageStatistics: false))
            {
                return;
            }

            FindEnemyFighterSpawner()?.FireOvertimePenalty(
                overtimePenaltyProjectilePrefab,
                this,
                visualSource);
        }

        private Fighter2D FindNearestLivingEnemy()
        {
            Fighter2D nearest = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            foreach (Fighter2D candidate in
                     FindObjectsByType<Fighter2D>(
                         FindObjectsInactive.Exclude))
            {
                if (candidate == null ||
                    candidate == this ||
                    !candidate.IsAlive ||
                    candidate.Faction == Faction)
                {
                    continue;
                }

                float distanceSquared =
                    ((Vector2)(candidate.transform.position -
                        transform.position)).sqrMagnitude;
                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistanceSquared = distanceSquared;
            }

            return nearest;
        }

        private BackpackFighterSpawner FindEnemyFighterSpawner()
        {
            foreach (BackpackCombatController controller in
                     BackpackCombatController.ActiveControllers)
            {
                BackpackFighterSpawner spawner =
                    controller != null
                        ? controller.FighterSpawner
                        : null;
                if (controller != null && controller.Faction != Faction &&
                    spawner != null && spawner.isActiveAndEnabled)
                {
                    return spawner;
                }
            }

            return null;
        }

        private void StopOvertimePenalty()
        {
            overtimePenaltyActive = false;
            overtimePenaltyRetryTimer = 0f;
        }

        private void ConfigureHealthBars()
        {
            if (healthBars != null)
            {
                foreach (HealthBar healthBar in healthBars)
                {
                    if (healthBar == null)
                    {
                        continue;
                    }

                    healthBar.SetTarget(health);
                }
            }

            if (healthBarFollowers != null)
            {
                foreach (
                    WorldSpaceHealthBarFollower2D follower
                    in healthBarFollowers)
                {
                    if (follower == null)
                    {
                        continue;
                    }

                    follower.SetTarget(transform);
                }
            }
        }
        
        private void ConfigureHurtBoxes(
            BattleFaction faction)
        {
            if (hurtBoxes == null)
            {
                return;
            }

            foreach (HurtBox2D hurtBox in hurtBoxes)
            {
                if (hurtBox == null)
                {
                    continue;
                }

                hurtBox.ConfigureLayer(faction);
            }
        }
        
        private void HandleDied()
        {
            if (isDying)
            {
                return;
            }

            isDying = true;
            StopOvertimePenalty();

            DisableCombatInteractions();
            feedbacks?.PlayDeath(Faction);

            float cleanupDelay =
                feedbacks != null
                    ? feedbacks.DeathCleanupDelay
                    : 0f;

            Destroy(gameObject, cleanupDelay);
        }

        private void HandleDamaged(float damage)
        {
            SetHealthBarsVisible(true);
            feedbacks?.PlayHit(damage, Faction);
        }

        private void SetHealthBarsVisible(bool visible)
        {
            if (healthBars == null)
            {
                return;
            }

            foreach (HealthBar healthBar in healthBars)
            {
                if (healthBar != null)
                {
                    healthBar.gameObject.SetActive(visible);
                }
            }
        }

        private void DisableCombatInteractions()
        {
            mover?.SetPaused(true);

            FighterCombat2D combat =
                GetComponent<FighterCombat2D>();

            if (combat != null)
            {
                combat.enabled = false;
            }

            BattleCurveFollower2D curveFollower =
                GetComponent<BattleCurveFollower2D>();

            if (curveFollower != null)
            {
                curveFollower.enabled = false;
            }

            if (hurtBoxes == null)
            {
                return;
            }

            foreach (HurtBox2D hurtBox in hurtBoxes)
            {
                if (hurtBox != null)
                {
                    hurtBox.gameObject.SetActive(false);
                }
            }
        }
    }
}
