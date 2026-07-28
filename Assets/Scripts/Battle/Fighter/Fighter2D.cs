using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 表示场景中的一架飞机。
    /// FighterDefinition保存静态配置，
    /// Health保存运行时生命值。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(DirectionalMover2D))]
    [RequireComponent(typeof(Health))]
    
    [RequireComponent(typeof(FactionMember))]
    
    public sealed class Fighter2D : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private DirectionalMover2D mover;
        private Health health;
        
        private FactionMember factionMember;
        
        private HurtBox2D[] hurtBoxes;

        private HealthBar[] healthBars;

        private WorldSpaceHealthBarFollower2D[]
            healthBarFollowers;
        
        private FighterDefinition definition;

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

        private void Awake()
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();

            mover =
                GetComponent<DirectionalMover2D>();

            health =
                GetComponent<Health>();
            
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
            
            health.Died += HandleDied;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        /// <summary>
        /// 生成飞机后调用。
        /// factionColor用于区分玩家和敌人阵营。
        /// </summary>
        public void Initialize(
            FighterDefinition fighterDefinition,
            BattleFaction faction,
            Color factionColor)
        {
            if (fighterDefinition == null)
            {
                Debug.LogError(
                    "无法初始化飞机：FighterDefinition为空。",
                    this);

                return;
            }

            definition = fighterDefinition;
            
            factionMember.SetFaction(faction);
            
            ConfigureHurtBoxes(faction);

            spriteRenderer.sprite =
                fighterDefinition.Sprite;

            spriteRenderer.color =
                fighterDefinition.BaseColor *
                factionColor;

            mover.SetBaseSpeed(
                fighterDefinition.BaseSpeed);

            health.Initialize(
                fighterDefinition.MaximumHealth);

            FighterCombat2D combat =
                GetComponent<FighterCombat2D>();

            if (combat != null)
            {
                combat.ConfigureDefaultFireMode(
                    fighterDefinition.AttackInterval);
            }

            ConfigureHealthBars();
            
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
            Destroy(gameObject);
        }
    }
}
