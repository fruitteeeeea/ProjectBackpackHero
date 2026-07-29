using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 可在运行时指定中心与半径的圆形范围查询。
    /// </summary>
    public sealed class CircleDamageArea2D : DamageArea2D
    {
        [SerializeField, Min(0f)]
        private float radius = 1.5f;

        private Vector2 worldCenter;

        public float Radius => radius;
        public override Vector2 WorldCenter => worldCenter;
        public override Vector2 WorldSize => Vector2.one * radius * 2f;
        public override float WorldAngle => 0f;

        public void Configure(Vector2 center, float newRadius)
        {
            worldCenter = center;
            radius = Mathf.Max(0f, newRadius);
        }

        public override int CollectOverlaps(
            int layerMask,
            List<Collider2D> results)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();

            Collider2D[] overlaps =
                Physics2D.OverlapCircleAll(
                    worldCenter,
                    radius,
                    layerMask);

            results.AddRange(overlaps);
            return overlaps.Length;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            radius = Mathf.Max(0f, radius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(worldCenter, radius);
        }
#endif
    }
}
