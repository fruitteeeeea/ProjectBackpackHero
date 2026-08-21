using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 表示一个能够受到攻击的2D碰撞区域。
    /// 实际生命值由所属对象的Health管理。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HurtBox2D : MonoBehaviour
    {
        [Header("Owner References")]
        [SerializeField]
        private Health targetHealth;

        [SerializeField]
        private FactionMember factionMember;

        [Header("Target")]
        [SerializeField]
        private BattleTargetType targetType =
            BattleTargetType.Fighter;

        public Health TargetHealth
        {
            get
            {
                FindOwnerReferences();
                return targetHealth;
            }
        }

        public FactionMember FactionMember
        {
            get
            {
                FindOwnerReferences();
                return factionMember;
            }
        }

        public BattleFaction Faction =>
            factionMember != null
                ? factionMember.Faction
                : BattleFaction.Player;

        public BattleTargetType TargetType =>
            targetType;

        public bool IsAlive =>
            targetHealth != null &&
            !targetHealth.IsDead;

        /// <summary>
        /// 当前目标是否会拦截伤害但不扣除生命值。
        /// </summary>
        public bool IsDamageImmune
        {
            get
            {
                FindOwnerReferences();

                Fighter2D fighter = targetHealth != null
                    ? targetHealth.GetComponent<Fighter2D>()
                    : null;

                BattleBackpackTarget2D backpack = targetHealth != null
                    ? targetHealth.GetComponent<BattleBackpackTarget2D>()
                    : null;

                return (fighter != null && fighter.IsInitialInvulnerable) ||
                       (backpack != null && backpack.IsHealthLocked);
            }
        }

        private void Awake()
        {
            RefreshOwnerConfiguration();
        }

        private void OnEnable()
        {
            RefreshOwnerConfiguration();
        }

        /// <summary>
        /// 根据所属阵营设置HurtBox的Physics Layer。
        /// </summary>
        public void ConfigureLayer(
            BattleFaction faction)
        {
            int layer =
                BattlePhysicsLayers.GetHurtBoxLayer(
                    faction);

            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
        }

        /// <summary>
        /// 从所属对象刷新Health、阵营引用和物理Layer。
        /// 让直接放在场景中的敌方Prefab无需经过Fighter.Initialize
        /// 也能被范围查询正确识别。
        /// </summary>
        public void RefreshOwnerConfiguration()
        {
            FindOwnerReferences();

            if (factionMember != null)
            {
                ConfigureLayer(
                    factionMember.Faction);
            }
        }
        
        /// <summary>
        /// 尝试让这个HurtBox受到一次攻击。
        /// 返回true代表确实对敌方目标造成了伤害。
        /// </summary>
        public bool ReceiveHit(
            float damage,
            BattleFaction attackerFaction,
            BattleDamageSource damageSource = default)
        {
            if (damage <= 0f)
            {
                return false;
            }

            FindOwnerReferences();

            if (targetHealth == null ||
                factionMember == null)
            {
                Debug.LogWarning(
                    $"{name}的HurtBox缺少Health或FactionMember。",
                    this);

                return false;
            }

            if (targetHealth.IsDead)
            {
                return false;
            }

            if (IsDamageImmune)
            {
                return false;
            }

            if (!factionMember.IsEnemyFaction(
                    attackerFaction))
            {
                return false;
            }

            float healthBeforeDamage = targetHealth.CurrentHealth;
            targetHealth.DecreaseHealth(damage);
            float actualDamage = healthBeforeDamage - targetHealth.CurrentHealth;
            if (actualDamage > 0f &&
                damageSource.CountsForDamageStatistics)
            {
                DamageStatisticsRuntime.RecordHit(
                    attackerFaction,
                    damageSource,
                    damage,
                    actualDamage,
                    targetHealth.IsDead &&
                    targetHealth.GetComponent<Fighter2D>() != null);
            }
            return true;
        }

        /// <summary>
        /// 自动从自己或父对象寻找所属组件。
        /// HurtBox通常会作为飞机或背包的子对象。
        /// </summary>
        private void FindOwnerReferences()
        {
            if (targetHealth == null)
            {
                targetHealth =
                    GetComponentInParent<Health>();
            }

            if (factionMember == null)
            {
                factionMember =
                    GetComponentInParent<FactionMember>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindOwnerReferences();

            Collider2D hurtCollider =
                GetComponent<Collider2D>();

            if (hurtCollider != null)
            {
                hurtCollider.isTrigger = true;
            }
        }
#endif
    }
}
