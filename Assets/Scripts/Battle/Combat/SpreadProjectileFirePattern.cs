using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Battle
{
    [CreateAssetMenu(
        fileName = "Spread Fire Pattern",
        menuName = "Battle/Projectile Fire Patterns/Spread")]
    public sealed class SpreadProjectileFirePattern :
        ProjectileFirePattern
    {
        [SerializeField, Min(1)]
        private int projectileCount = 3;

        [SerializeField, Range(0f, 360f)]
        private float spreadAngle = 30f;

        public override int ProjectileCount =>
            Mathf.Max(1, projectileCount);

        public float SpreadAngle =>
            Mathf.Clamp(spreadAngle, 0f, 360f);

        public override void AppendDirections(
            Vector2 forward,
            List<Vector2> results)
        {
            if (results == null)
            {
                return;
            }

            Vector2 safeForward =
                GetSafeForward(forward);

            int count = ProjectileCount;

            if (count == 1)
            {
                results.Add(safeForward);
                return;
            }

            float safeSpreadAngle = SpreadAngle;
            float angleStep =
                safeSpreadAngle / (count - 1);

            float startAngle =
                -safeSpreadAngle * 0.5f;

            for (int index = 0;
                 index < count;
                 index++)
            {
                float angle =
                    startAngle + angleStep * index;

                Vector2 direction =
                    Quaternion.Euler(
                        0f,
                        0f,
                        angle) * safeForward;

                results.Add(direction.normalized);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            projectileCount =
                Mathf.Max(1, projectileCount);

            spreadAngle =
                Mathf.Clamp(
                    spreadAngle,
                    0f,
                    360f);
        }
#endif
    }
}
