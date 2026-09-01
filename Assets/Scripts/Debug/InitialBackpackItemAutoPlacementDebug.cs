using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>
    /// 平衡调试用的开局背包配置物品开关。
    /// 该状态只影响下一次新对局的初始布局，不写入正式 GameData。
    /// </summary>
    public static class InitialBackpackItemAutoPlacementDebug
    {
        public static bool IsEnabled { get; private set; }

        public static void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            IsEnabled = false;
        }
    }
}
