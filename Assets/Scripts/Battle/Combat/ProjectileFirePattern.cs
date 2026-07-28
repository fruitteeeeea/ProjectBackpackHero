using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 描述一次发射产生哪些子弹方向。
    /// 冷却与子弹生成由使用方负责。
    /// </summary>
    public abstract class ProjectileFirePattern :
        ScriptableObject
    {
        public abstract int ProjectileCount
        {
            get;
        }

        public abstract void AppendDirections(
            Vector2 forward,
            List<Vector2> results);

        protected static Vector2 GetSafeForward(
            Vector2 forward)
        {
            return forward.sqrMagnitude >
                   Mathf.Epsilon
                ? forward.normalized
                : Vector2.up;
        }
    }
}
