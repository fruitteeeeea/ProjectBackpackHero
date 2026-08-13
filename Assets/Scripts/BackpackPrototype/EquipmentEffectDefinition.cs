using UnityEngine;

namespace BackpackPrototype
{
    public readonly struct EquipmentHangarStats
    {
        public EquipmentHangarStats(float range, float damage, float interval)
        { Range = range; Damage = damage; Interval = interval; }
        public float Range { get; }
        public float Damage { get; }
        public float Interval { get; }
    }

    /// <summary>
    /// 装备可提供给相邻飞机的静态效果配置。
    /// 新效果类型通过继承此类并由对应的运行时控制器解释。
    /// </summary>
    public abstract class EquipmentEffectDefinition :
        ScriptableObject
    {
        /// <summary>Optional Equipment-detail statistics. Effects without numeric values return false.</summary>
        public virtual bool TryGetHangarStats(out EquipmentHangarStats stats)
        {
            stats = default;
            return false;
        }
    }
}
