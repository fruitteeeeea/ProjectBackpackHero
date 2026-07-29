using BackpackHero.Debugging;
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
        private float baseMaximumHealth;
        private FactionMember factionMember;
        private HurtBox2D[] hurtBoxes;

        private HealthBar[] healthBars;

        private WorldSpaceHealthBarFollower2D[]
            healthBarFollowers;

        private Renderer[] visualRenderers;
        
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
            baseMaximumHealth = health != null
                ? health.MaxHealth
                : 1f;

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

            Transform visualRoot =
                transform.Find("Visual");
            visualRenderers =
                visualRoot != null
                    ? visualRoot.GetComponentsInChildren<
                        Renderer>(true)
                    : System.Array.Empty<Renderer>();
            
            ConfigureHurtBoxLayers();
            
            ConfigureHealthBars();
            ApplyPacingHealth();
            ApplyCombatPresentation(
                BattleFlowController.IsCombatPhase);
        }

        private void OnEnable()
        {
            BattleFlowController.PhaseChanged +=
                HandlePhaseChanged;
            GamePacingDebugRuntime.MultipliersChanged +=
                HandlePacingChanged;
        }

        private void OnDisable()
        {
            BattleFlowController.PhaseChanged -=
                HandlePhaseChanged;
            GamePacingDebugRuntime.MultipliersChanged -=
                HandlePacingChanged;
        }

        private void Start()
        {
            ApplyCombatPresentation(
                BattleFlowController.IsCombatPhase);
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            ApplyCombatPresentation(
                phase == BattlePhase.Combat);
        }

        private void HandlePacingChanged(
            GamePacingMultipliers _)
        {
            ApplyPacingHealth();
        }

        private void ApplyPacingHealth()
        {
            if (health == null)
            {
                return;
            }

            health.SetMaximumHealthAndFill(
                baseMaximumHealth *
                GamePacingDebugRuntime.GetBackpackHealthMultiplier(
                    Faction));
        }

        public void ApplyCombatPresentation(bool active)
        {
            if (hurtBoxes != null)
            {
                foreach (HurtBox2D hurtBox in hurtBoxes)
                {
                    if (hurtBox != null &&
                        hurtBox.TryGetComponent(
                            out Collider2D hurtCollider))
                    {
                        hurtCollider.enabled = active;
                    }
                }
            }

            if (healthBarFollowers != null)
            {
                foreach (
                    WorldSpaceHealthBarFollower2D follower
                    in healthBarFollowers)
                {
                    if (follower != null)
                    {
                        follower.gameObject.SetActive(active);
                    }
                }
            }

            if (visualRenderers != null)
            {
                foreach (Renderer visual in visualRenderers)
                {
                    if (visual != null)
                    {
                        visual.enabled = active;
                    }
                }
            }
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
