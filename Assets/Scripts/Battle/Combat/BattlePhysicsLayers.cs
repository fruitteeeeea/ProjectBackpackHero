using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 集中管理战斗碰撞Layer名称。
    /// 避免在多个脚本中重复写字符串。
    /// </summary>
    public static class BattlePhysicsLayers
    {
        public const string PlayerHurtBox =
            "PlayerHurtBox";

        public const string EnemyHurtBox =
            "EnemyHurtBox";

        public const string PlayerHitBox =
            "PlayerHitBox";

        public const string EnemyHitBox =
            "EnemyHitBox";

        public static int GetHurtBoxLayer(
            BattleFaction faction)
        {
            string layerName =
                faction == BattleFaction.Player
                    ? PlayerHurtBox
                    : EnemyHurtBox;

            return GetRequiredLayer(layerName);
        }

        public static int GetHitBoxLayer(
            BattleFaction faction)
        {
            string layerName =
                faction == BattleFaction.Player
                    ? PlayerHitBox
                    : EnemyHitBox;

            return GetRequiredLayer(layerName);
        }

        private static int GetRequiredLayer(
            string layerName)
        {
            int layer =
                LayerMask.NameToLayer(layerName);

            if (layer < 0)
            {
                Debug.LogError(
                    $"找不到Physics Layer：{layerName}。" +
                    "请检查Tags and Layers设置。");
            }

            return layer;
        }
    }
}