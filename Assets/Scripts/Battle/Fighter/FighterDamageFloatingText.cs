using MoreMountains.Feedbacks;
using BackpackHero.Debugging;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 将飞机受击数值发送给场景中的 FEEL 飘字对象池。
    /// 两个通道分别对应玩家与敌机的视觉样式。
    /// </summary>
    public sealed class FighterDamageFloatingText : MonoBehaviour
    {
        [Header("FEEL Floating Text Channels")]
        [SerializeField]
        private int playerChannel = 41;

        [SerializeField]
        private int enemyChannel = 42;

        public void Play(float damage, BattleFaction faction)
        {
            if (damage <= 0f)
            {
                return;
            }

            int channel = faction == BattleFaction.Player
                ? playerChannel
                : enemyChannel;

            float displayedDamage = damage *
                GamePacingDebugRuntime
                    .GetDamageFloatingTextMagicNumber();

            MMFloatingTextSpawnEvent.Trigger(
                new MMChannelData(
                    MMChannelModes.Int,
                    channel,
                    null),
                transform.position,
                displayedDamage.ToString("0.##"),
                Vector3.up,
                1f);
        }
    }
}
