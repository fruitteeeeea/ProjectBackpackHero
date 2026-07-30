using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>
    /// 装备可提供给相邻飞机的静态效果配置。
    /// 新效果类型通过继承此类并由对应的运行时控制器解释。
    /// </summary>
    public abstract class EquipmentEffectDefinition :
        ScriptableObject
    {
    }
}
