using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 对一个DamageArea2D内的所有敌方单位结算一次伤害。
    /// 同一个Health拥有多个HurtBox时只会结算一次。
    /// </summary>
    public sealed class AreaDamageResolver2D :
        MonoBehaviour
    {
        private readonly List<Collider2D>
            overlapBuffer = new();

        private readonly HashSet<Health>
            damagedHealth = new();

        public int Resolve(
            DamageArea2D damageArea,
            BattleFaction attackerFaction,
            float damage,
            BattleDamageSource damageSource = default)
        {
            if (damageArea == null ||
                damage <= 0f)
            {
                return 0;
            }

            damagedHealth.Clear();

            CollectTargets(
                damageArea,
                attackerFaction,
                candidateHurtBoxes);

            int damagedUnitCount = 0;

            foreach (HurtBox2D hurtBox in candidateHurtBoxes)
            {
                if (!hurtBox.ReceiveHit(
                        damage,
                        attackerFaction,
                        damageSource))
                {
                    continue;
                }

                damagedHealth.Add(
                    hurtBox.TargetHealth);

                damagedUnitCount++;
            }

            return damagedUnitCount;
        }

        private readonly List<HurtBox2D>
            candidateHurtBoxes = new();

        /// <summary>
        /// 收集范围内可受攻击的敌方单位，每个Health仅保留一个HurtBox。
        /// 供连锁等需要自行决定命中顺序的攻击复用。
        /// </summary>
        public int CollectTargets(
            DamageArea2D damageArea,
            BattleFaction attackerFaction,
            List<HurtBox2D> results)
        {
            if (damageArea == null || results == null)
            {
                return 0;
            }

            int hurtBoxLayer =
                BattlePhysicsLayers.GetHurtBoxLayer(
                    GetEnemyFaction(attackerFaction));

            if (hurtBoxLayer < 0)
            {
                results.Clear();
                return 0;
            }

            overlapBuffer.Clear();
            damagedHealth.Clear();
            results.Clear();

            damageArea.CollectOverlaps(
                1 << hurtBoxLayer,
                overlapBuffer);

            foreach (Collider2D candidate in overlapBuffer)
            {
                if (candidate == null)
                {
                    continue;
                }

                HurtBox2D hurtBox =
                    candidate.GetComponent<HurtBox2D>();

                Health health = hurtBox != null
                    ? hurtBox.TargetHealth
                    : null;

                FactionMember factionMember = hurtBox != null
                    ? hurtBox.FactionMember
                    : null;

                if (hurtBox == null || health == null ||
                    factionMember == null || health.IsDead ||
                    !factionMember.IsEnemyFaction(attackerFaction) ||
                    !damagedHealth.Add(health))
                {
                    continue;
                }

                results.Add(hurtBox);
            }

            return results.Count;
        }

        private static BattleFaction GetEnemyFaction(
            BattleFaction faction)
        {
            return faction == BattleFaction.Player
                ? BattleFaction.Enemy
                : BattleFaction.Player;
        }
    }
}
