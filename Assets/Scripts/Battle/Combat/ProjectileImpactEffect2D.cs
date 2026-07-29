using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 由Projectile2D在首次有效命中时调用的特殊命中效果。
    /// 返回true代表已接管伤害结算。
    /// </summary>
    public abstract class ProjectileImpactEffect2D : MonoBehaviour
    {
        public abstract bool ResolveImpact(
            HurtBox2D initialTarget,
            Vector2 impactPosition,
            BattleFaction attackerFaction,
            float damage);
    }
}
