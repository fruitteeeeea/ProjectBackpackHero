using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Battle
{
    [RequireComponent(typeof(AreaDamageResolver2D))]
    [RequireComponent(typeof(CircleDamageArea2D))]
    public sealed class ChainLightningProjectileImpact2D : ProjectileImpactEffect2D
    {
        [SerializeField, Min(0f)] private float jumpRadius = 2.5f;
        [SerializeField, Min(0)] private int maximumExtraTargets = 3;
        [SerializeField, Min(0f)] private float chainDamageMultiplier = 1f;
        [SerializeField] private LightningLinkVfx2D lightningLinkVfxPrefab;
        [SerializeField] private AreaDamageResolver2D damageResolver;
        [SerializeField] private CircleDamageArea2D damageArea;

        private readonly List<HurtBox2D> candidates = new();
        private readonly HashSet<Health> hitHealth = new();

        public float JumpRadius => jumpRadius;
        public int MaximumExtraTargets => maximumExtraTargets;

        private void Awake() => FindReferences();

        public override bool ResolveImpact(
            HurtBox2D initialTarget,
            Vector2 impactPosition,
            BattleFaction attackerFaction,
            float damage)
        {
            FindReferences();
            if (initialTarget == null || damageResolver == null || damageArea == null)
            {
                return false;
            }

            Health initialHealth = initialTarget.TargetHealth;
            if (initialHealth == null)
            {
                return false;
            }

            Vector2 currentPosition = GetTargetPosition(initialTarget);
            hitHealth.Clear();
            hitHealth.Add(initialHealth);

            // 初始目标按普通子弹的基础伤害结算一次。
            initialTarget.ReceiveHit(damage, attackerFaction);

            for (int index = 0; index < maximumExtraTargets; index++)
            {
                HurtBox2D nextTarget = FindNearestTarget(
                    currentPosition,
                    attackerFaction);

                if (nextTarget == null)
                {
                    break;
                }

                Vector2 nextPosition = GetTargetPosition(nextTarget);
                Health nextHealth = nextTarget.TargetHealth;

                if (!nextTarget.ReceiveHit(
                        damage * chainDamageMultiplier,
                        attackerFaction))
                {
                    hitHealth.Add(nextHealth);
                    continue;
                }

                hitHealth.Add(nextHealth);
                SpawnLink(currentPosition, nextPosition);
                currentPosition = nextPosition;
            }

            return true;
        }

        private HurtBox2D FindNearestTarget(
            Vector2 center,
            BattleFaction attackerFaction)
        {
            damageArea.Configure(center, jumpRadius);
            damageResolver.CollectTargets(
                damageArea,
                attackerFaction,
                candidates);

            HurtBox2D nearest = null;
            float nearestDistanceSquared = float.PositiveInfinity;
            string nearestName = null;

            foreach (HurtBox2D candidate in candidates)
            {
                Health health = candidate.TargetHealth;
                if (health == null || hitHealth.Contains(health))
                {
                    continue;
                }

                float distanceSquared =
                    (GetTargetPosition(candidate) - center).sqrMagnitude;
                string targetName = health.name;

                if (distanceSquared < nearestDistanceSquared ||
                    (Mathf.Approximately(distanceSquared, nearestDistanceSquared) &&
                     (nearestName == null ||
                      string.CompareOrdinal(
                          targetName,
                          nearestName) < 0)))
                {
                    nearest = candidate;
                    nearestDistanceSquared = distanceSquared;
                    nearestName = targetName;
                }
            }

            return nearest;
        }

        private void SpawnLink(Vector2 start, Vector2 end)
        {
            if (lightningLinkVfxPrefab == null)
            {
                return;
            }

            LightningLinkVfx2D effect = Instantiate(lightningLinkVfxPrefab);
            effect.Configure(start, end);
        }

        private static Vector2 GetTargetPosition(HurtBox2D target)
        {
            return target.TargetHealth != null
                ? target.TargetHealth.transform.position
                : target.transform.position;
        }

        private void FindReferences()
        {
            damageResolver ??= GetComponent<AreaDamageResolver2D>();
            damageArea ??= GetComponent<CircleDamageArea2D>();
        }
    }
}
