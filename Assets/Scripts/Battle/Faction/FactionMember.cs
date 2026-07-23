using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 表示一个战斗对象所属的阵营。
    /// 飞机、背包和子弹都会使用这个组件。
    /// </summary>
    public sealed class FactionMember : MonoBehaviour
    {
        [Header("Faction")]
        [SerializeField]
        private BattleFaction faction =
            BattleFaction.Player;

        public BattleFaction Faction =>
            faction;

        /// <summary>
        /// 在生成飞机或子弹时设置阵营。
        /// </summary>
        public void SetFaction(
            BattleFaction newFaction)
        {
            faction = newFaction;
        }

        /// <summary>
        /// 判断另一个阵营成员是否是敌人。
        /// </summary>
        public bool IsEnemyOf(
            FactionMember other)
        {
            return other != null &&
                   faction != other.faction;
        }

        /// <summary>
        /// 不需要另一个组件时，可以直接比较阵营。
        /// </summary>
        public bool IsEnemyFaction(
            BattleFaction otherFaction)
        {
            return faction != otherFaction;
        }
    }
}