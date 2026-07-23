using System;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 背包中一个正在运行的机体物品。
    /// 负责保存飞机配置和冷却状态，不负责生成飞机。
    /// </summary>
    [Serializable]
    public sealed class BattleBackpackItem
    {
        [SerializeField]
        private FighterDefinition fighterDefinition;

        [SerializeField, Min(0.1f)]
        private float cooldownDuration = 3f;

        [SerializeField]
        private float remainingCooldown;

        public FighterDefinition FighterDefinition =>
            fighterDefinition;

        public float CooldownDuration =>
            cooldownDuration;

        public float RemainingCooldown =>
            remainingCooldown;

        public string DisplayName =>
            fighterDefinition != null
                ? fighterDefinition.DisplayName
                : "Missing Fighter";

        /// <summary>
        /// 0 表示刚开始充能，1 表示充能完成。
        /// 后面调试窗口的进度条会读取这个值。
        /// </summary>
        public float CooldownProgress
        {
            get
            {
                if (cooldownDuration <= 0f)
                {
                    return 1f;
                }

                return Mathf.Clamp01(
                    1f - remainingCooldown /
                    cooldownDuration);
            }
        }

        public BattleBackpackItem(
            FighterDefinition definition,
            float cooldown,
            float initialDelay)
        {
            fighterDefinition = definition;
            cooldownDuration =
                Mathf.Max(0.1f, cooldown);

            remainingCooldown =
                Mathf.Clamp(
                    initialDelay,
                    0f,
                    cooldownDuration);
        }

        /// <summary>
        /// 推进冷却。
        /// 返回 true 表示本帧冷却结束，应生成一架飞机。
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (fighterDefinition == null)
            {
                return false;
            }

            remainingCooldown -=
                Mathf.Max(0f, deltaTime);

            if (remainingCooldown > 0f)
            {
                return false;
            }

            remainingCooldown =
                cooldownDuration;

            return true;
        }

        public void RestartCooldown()
        {
            remainingCooldown =
                cooldownDuration;
        }
    }
}