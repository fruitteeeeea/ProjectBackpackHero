using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 常驻显示关卡阶段和本关比分的场景级 HUD。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleStatusHUDView : MonoBehaviour
    {
        [Header("Status")]
        [SerializeField] private Text statusLabel;
        [SerializeField] private Image statusPanel;
        [SerializeField] private Color preparationPanelColor = new Color32(0x7C, 0xB3, 0x42, 0xFF);
        [SerializeField] private Color combatPanelColor = new Color32(0xFD, 0xD8, 0x35, 0xFF);

        [Header("Status Flicker")]
        [SerializeField, Min(0.01f)] private float phaseFlickerInterval = 0.05f;
        [SerializeField, Min(1)] private int phaseFlickerCount = 3;

        [Header("Score")]
        [SerializeField] private Text scoreLabel;
        [SerializeField] private Color playerScoreColor = new Color(0.45f, 0.85f, 1f, 1f);
        [SerializeField] private Color enemyScoreColor = new Color(1f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color scoreSeparatorColor = Color.white;
        [SerializeField] private string scoreFormat = "{0}{1}{2}";
        [SerializeField] private string scoreSeparator = " - ";

        private Coroutine phaseFlickerRoutine;
        private bool hasVisualPhase;
        private bool isCombatVisual;

        private void OnEnable()
        {
            LevelManager.LevelChanged += HandleLevelChanged;
            BattleFlowController.PhaseChanged += HandlePhaseChanged;
            LevelFlowController.MatchStateChanged += RefreshScore;
            ApplyPhase(BattleFlowController.CurrentPhase, false);
            RefreshScore();
        }

        private void OnDisable()
        {
            LevelManager.LevelChanged -= HandleLevelChanged;
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
            LevelFlowController.MatchStateChanged -= RefreshScore;
            if (phaseFlickerRoutine != null)
            {
                StopCoroutine(phaseFlickerRoutine);
                phaseFlickerRoutine = null;
            }
        }

        private void HandleLevelChanged(int _)
        {
            RefreshStatus();
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            ApplyPhase(phase, true);
        }

        private void ApplyPhase(BattlePhase phase, bool allowFlicker)
        {
            bool isCombat = phase == BattlePhase.Combat;
            bool changed = hasVisualPhase && isCombatVisual != isCombat;
            isCombatVisual = isCombat;
            hasVisualPhase = true;

            if (statusPanel != null)
                statusPanel.color = isCombat ? combatPanelColor : preparationPanelColor;

            RefreshStatus();
            if (allowFlicker && changed)
                StartPhaseFlicker();
        }

        private void RefreshStatus()
        {
            if (statusLabel == null) return;
            string phaseText = isCombatVisual ? "COMBAT" : "PREPARATION";
            statusLabel.text = $"LEVEL {LevelManager.CurrentLevel} | {phaseText}";
        }

        private void RefreshScore()
        {
            if (scoreLabel == null) return;
            int playerWins = LevelFlowController.Instance != null
                ? LevelFlowController.Instance.PlayerWins : 0;
            int enemyWins = LevelFlowController.Instance != null
                ? LevelFlowController.Instance.EnemyWins : 0;

            string player = Colorize(playerWins.ToString(), playerScoreColor);
            string separator = Colorize(scoreSeparator, scoreSeparatorColor);
            string enemy = Colorize(enemyWins.ToString(), enemyScoreColor);
            scoreLabel.text = string.Format(scoreFormat, player, separator, enemy);
        }

        private void StartPhaseFlicker()
        {
            if (statusPanel == null) return;
            if (phaseFlickerRoutine != null) StopCoroutine(phaseFlickerRoutine);
            phaseFlickerRoutine = StartCoroutine(PlayPhaseFlicker());
        }

        private IEnumerator PlayPhaseFlicker()
        {
            GameObject panelObject = statusPanel.gameObject;
            for (int index = 0; index < phaseFlickerCount; index++)
            {
                panelObject.SetActive(false);
                yield return new WaitForSecondsRealtime(phaseFlickerInterval);
                panelObject.SetActive(true);
                yield return new WaitForSecondsRealtime(phaseFlickerInterval);
            }
            panelObject.SetActive(true);
            phaseFlickerRoutine = null;
        }

        private static string Colorize(string value, Color color) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{value}</color>";

#if UNITY_EDITOR
        private void OnValidate()
        {
            phaseFlickerInterval = Mathf.Max(0.01f, phaseFlickerInterval);
            phaseFlickerCount = Mathf.Max(1, phaseFlickerCount);
        }
#endif
    }
}
