using UnityEngine;
using UnityEngine.UI;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 通过时间缩放暂停或继续本局游戏，并同步按钮文案。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GamePauseToggle : MonoBehaviour
    {
        [SerializeField]
        private Text buttonLabel;

        private bool pausedByThisControl;

        private void OnEnable()
        {
            UpdateLabel();
        }

        public void TogglePause()
        {
            if (pausedByThisControl)
            {
                Time.timeScale = 1f;
                pausedByThisControl = false;
            }
            else
            {
                Time.timeScale = 0f;
                pausedByThisControl = true;
            }

            UpdateLabel();
        }

        private void OnDisable()
        {
            ResumeIfPausedByThisControl();
        }

        private void OnDestroy()
        {
            ResumeIfPausedByThisControl();
        }

        private void ResumeIfPausedByThisControl()
        {
            if (!pausedByThisControl)
            {
                return;
            }

            Time.timeScale = 1f;
            pausedByThisControl = false;
        }

        private void UpdateLabel()
        {
            if (buttonLabel != null)
            {
                buttonLabel.text = pausedByThisControl ? "Resume" : "Pause";
            }
        }
    }
}
