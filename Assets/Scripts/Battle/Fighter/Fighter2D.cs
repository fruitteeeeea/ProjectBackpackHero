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
        public ItemInstance DamageSourceItem => damageSourceItem;
        public float ProgressionDamageMultiplier => progressionDamageMultiplier;
        public void SetDamageSourceItem(ItemInstance item) => damageSourceItem = item;

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
            
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
                health.Died -= HandleDied;
            }
        }

        private void OnEnable()
        {
            GamePacingDebugRuntime.MultipliersChanged +=
                HandlePacingChanged;
            LevelDifficultyRuntime.Changed += HandleLevelDifficultyChanged;
        }

        private void OnDisable()
        {
            GamePacingDebugRuntime.MultipliersChanged -=
                HandlePacingChanged;
            LevelDifficultyRuntime.Changed -= HandleLevelDifficultyChanged;
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
            progressionHealthMultiplier = Mathf.Max(0.01f, healthMultiplier);
            progressionDamageMultiplier = Mathf.Max(0.01f, damageMultiplier);
            
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
            if (!IsAlive || damage <= 0f)
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
                LevelDifficultyRuntime.GetAircraftHealthMultiplier(
                    Faction) *
                (LevelFlowController.Instance?.IsOvertime == true
                    ? LevelFlowController.OvertimeAircraftHealthMultiplier
                    : 1f));
        }

        private void ApplyStyleLifetime()
        {
            LifetimeAndScreenBounds2D lifetime =
                GetComponent<LifetimeAndScreenBounds2D>();
            if (lifetime == null)
            {
                return;
            }

            lifetime.SetLifetime(
                lifetime.ConfiguredLifetime *
                StyleTendencyDebugRuntime
                    .GetAircraftLifetimeMultiplier());
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
