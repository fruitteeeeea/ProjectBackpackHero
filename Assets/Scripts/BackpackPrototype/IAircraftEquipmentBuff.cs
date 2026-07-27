using BackpackHero.Battle;

namespace BackpackPrototype
{
    /// <summary>
    /// 装备效果Prefab可以通过实现此接口，在生成飞机时应用自己的效果。
    /// 表现组件和实际数值组件可以独立实现并组合在同一个Prefab中。
    /// </summary>
    public interface IAircraftEquipmentBuff
    {
        void Apply(Fighter2D fighter, ItemInstance sourceItem);
    }
}
