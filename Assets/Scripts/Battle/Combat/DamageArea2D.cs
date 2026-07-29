using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 可被范围攻击复用的2D伤害查询形状。
    /// </summary>
    public abstract class DamageArea2D : MonoBehaviour
    {
        public abstract Vector2 WorldCenter { get; }
        public abstract Vector2 WorldSize { get; }
        public abstract float WorldAngle { get; }

        public int CollectOverlaps(
            int layerMask,
            List<Collider2D> results)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();

            Collider2D[] overlaps =
                Physics2D.OverlapBoxAll(
                    WorldCenter,
                    WorldSize,
                    WorldAngle,
                    layerMask);

            results.AddRange(overlaps);
            return overlaps.Length;
        }
    }
}
