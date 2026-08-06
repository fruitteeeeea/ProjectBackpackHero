using UnityEngine;
using UnityEngine.UI;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 常驻显示当前回合和战斗阶段的场景级 HUD。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleStatusHUDView : MonoBehaviour
    {
        [SerializeField]
        private Text statusLabel;

        private void OnEnable()
        {
            LevelManager.LevelChanged += HandleLevelChanged;
            LevelFlowController.RoundStarted += HandleRoundStarted;
            Refresh();
        }

        private void OnDisable()
        {
            LevelManager.LevelChanged -= HandleLevelChanged;
            LevelFlowController.RoundStarted -= HandleRoundStarted;
        }

        private void HandleLevelChanged(int _)
        {
            Refresh();
        }

        private void HandleRoundStarted(int _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = $"关卡{LevelManager.CurrentLevel} | 第{ToChineseNumber(LevelFlowController.CurrentRound)}回合";
        }

        private static string ToChineseNumber(int value)
        {
            if (value <= 0)
            {
                return "零";
            }

            string[] digits =
            {
                "零", "一", "二", "三", "四",
                "五", "六", "七", "八", "九"
            };

            if (value < 10)
            {
                return digits[value];
            }

            if (value < 20)
            {
                return value == 10
                    ? "十"
                    : $"十{digits[value % 10]}";
            }

            if (value < 100)
            {
                int tens = value / 10;
                int ones = value % 10;
                return ones == 0
                    ? $"{digits[tens]}十"
                    : $"{digits[tens]}十{digits[ones]}";
            }

            return value.ToString();
        }
    }
}
