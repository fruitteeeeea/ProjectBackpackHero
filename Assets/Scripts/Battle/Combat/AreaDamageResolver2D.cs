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
            float damage)
        {
            if (damageArea == null ||
                damage <= 0f)
            {
                return 0;
            }

            int hurtBoxLayer =
                BattlePhysicsLayers.GetHurtBoxLayer(
                    GetEnemyFaction(
                        attackerFaction));

            if (hurtBoxLayer < 0)
            {
                return 0;
            }

            overlapBuffer.Clear();
            damagedHealth.Clear();

            damageArea.CollectOverlaps(
                1 << hurtBoxLayer,
                overlapBuffer);

            int damagedUnitCount = 0;

            foreach (Collider2D candidate
                     in overlapBuffer)
            {
                if (candidate == null)
                {
                    continue;
                }

                HurtBox2D hurtBox =
                    candidate.GetComponent<
                        HurtBox2D>();

                if (hurtBox == null ||
                    hurtBox.TargetHealth == null ||
                    damagedHealth.Contains(
                        hurtBox.TargetHealth))
                {
                    continue;
                }

                if (!hurtBox.ReceiveHit(
                        damage,
                        attackerFaction))
                {
                    continue;
                }

                damagedHealth.Add(
                    hurtBox.TargetHealth);

                damagedUnitCount++;
            }

            return damagedUnitCount;
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
