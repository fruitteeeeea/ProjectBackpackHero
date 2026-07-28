using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Battle
{
    [CreateAssetMenu(
        fileName = "Forward Single Fire Pattern",
        menuName = "Battle/Projectile Fire Patterns/Forward Single")]
    public sealed class ForwardProjectileFirePattern :
        ProjectileFirePattern
    {
        public override int ProjectileCount => 1;

        public override void AppendDirections(
            Vector2 forward,
            List<Vector2> results)
        {
            if (results == null)
            {
                return;
            }

            results.Add(
                GetSafeForward(forward));
        }
    }
}
