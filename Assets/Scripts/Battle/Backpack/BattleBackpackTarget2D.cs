using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 场景中可被攻击的背包实体。
    /// 当前只负责阵营、生命和HurtBox配置。
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(FactionMember))]
    public sealed class BattleBackpackTarget2D :
        MonoBehaviour
    {
        private Health health;
        private FactionMember factionMember;
        private HurtBox2D[] hurtBoxes;

        private HealthBar[] healthBars;

        private WorldSpaceHealthBarFollower2D[]
            healthBarFollowers;
        
        public Health Health =>
            health;

        public FactionMember FactionMember =>
            factionMember;

        public BattleFaction Faction =>
            factionMember != null
                ? factionMember.Faction
                : BattleFaction.Player;

        public bool IsAlive =>
            health != null &&
            !health.IsDead;

        private void Awake()
        {
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
            
            ConfigureHurtBoxLayers();
            
            ConfigureHealthBars();
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
        
        /// <summary>
        /// 根据背包阵营配置所有HurtBox的Layer。
        /// </summary>
        private void ConfigureHurtBoxLayers()
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

                hurtBox.ConfigureLayer(
                    factionMember.Faction);
            }
        }
    }
}