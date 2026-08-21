using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 表示能够造成伤害的2D碰撞区域。
    /// 当前主要用于子弹。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(FactionMember))]
    public sealed class HitBox2D : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField, Min(0f)]
        private float damage = 1f;

        private FactionMember factionMember;
        private ProjectileImpactEffect2D impactEffect;
        private bool hasHitTarget;
        private bool canDamageBackpack = true;
        private BattleDamageSource damageSource;

        public float Damage =>
            damage;

        public BattleFaction Faction =>
            factionMember != null
                ? factionMember.Faction
                : BattleFaction.Player;

        private void Awake()
        {
            factionMember =
                GetComponent<FactionMember>();

            impactEffect =
                GetComponent<ProjectileImpactEffect2D>();
        }

        private void OnEnable()
        {
            hasHitTarget = false;
        }

        /// <summary>
        /// 子弹生成后调用，设置发射阵营和伤害。
        /// </summary>
        public void Initialize(
            BattleFaction faction,
            float newDamage,
            bool newCanDamageBackpack = true,
            BattleDamageSource newDamageSource = default)
        {
            if (factionMember == null)
            {
                factionMember =
                    GetComponent<FactionMember>();
            }

            factionMember.SetFaction(faction);
            damage = Mathf.Max(0f, newDamage);
            canDamageBackpack = newCanDamageBackpack;
            damageSource = newDamageSource;

            ConfigureLayer(faction);
            hasHitTarget = false;
        }

        private void ConfigureLayer(
            BattleFaction faction)
        {
            int layer =
                BattlePhysicsLayers.GetHitBoxLayer(
                    faction);

            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
        }

        private void OnTriggerEnter2D(
            Collider2D other)
        {
            if (hasHitTarget)
            {
                return;
            }

            HurtBox2D hurtBox =
                other.GetComponent<HurtBox2D>();

            if (hurtBox == null)
            {
                return;
            }

            FactionMember targetFaction =
                hurtBox.FactionMember;

            if (!hurtBox.IsAlive ||
                targetFaction == null ||
                !targetFaction.IsEnemyFaction(Faction))
            {
                return;
            }

            if (!canDamageBackpack &&
                hurtBox.TargetType == BattleTargetType.Backpack)
            {
                return;
            }

            // 初始无敌的飞机会挡住子弹，但不会触发爆炸、连锁等命中效果。
            if (hurtBox.IsDamageImmune)
            {
                hasHitTarget = true;
                Destroy(gameObject);
                return;
            }

            bool causedDamage;

            if (impactEffect != null)
            {
                causedDamage = impactEffect.ResolveImpact(
                    hurtBox,
                    transform.position,
                    Faction,
                    damage,
                    damageSource);
            }
            else
            {
                causedDamage = hurtBox.ReceiveHit(
                    damage,
                    Faction,
                    damageSource);
            }

            if (!causedDamage)
            {
                return;
            }

            hasHitTarget = true;
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);

            Collider2D hitCollider =
                GetComponent<Collider2D>();

            if (hitCollider != null)
            {
                hitCollider.isTrigger = true;
            }
        }
#endif
    }
}
