using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BackpackHero.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleResultPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image titleBackground;
        [SerializeField] private Sprite victoryTitleSprite;
        [SerializeField] private Sprite defeatTitleSprite;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI totalScoreLabel;
        [SerializeField] private TextMeshProUGUI scoreDeltaLabel;
        [SerializeField] private TextMeshProUGUI advisoryLabel;
        [SerializeField] private Transform rewardsRoot;
        [SerializeField] private List<TextMeshProUGUI> rewardLabels = new();
        [SerializeField] private GameObject victoryConfetti;
        [SerializeField] private Button continueButton;

        [Header("Defaults")]
        [SerializeField] private int defaultTotalScore;
        [SerializeField] private int defaultVictoryDelta = 30;
        [SerializeField] private int defaultDefeatDelta = -10;
        [SerializeField] private string mainMenuSceneName = "MainMenuDemo";

        private Sequence revealSequence;

        private void Awake()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(ReturnToMainMenu);
        }

        private void OnDisable()
        {
            revealSequence?.Kill();
            revealSequence = null;
            if (victoryConfetti != null)
                victoryConfetti.SetActive(false);
        }

        public void Show(BattleResultData data)
        {
            data ??= BattleResultData.CreateDefault(true);
            bool victory = data.IsVictory;
            int delta = data.ScoreDelta;
            int total = data.TotalScore != 0 ? data.TotalScore : defaultTotalScore;

            if (titleBackground != null)
                titleBackground.sprite = victory ? victoryTitleSprite : defeatTitleSprite;
            if (titleLabel != null)
                titleLabel.text = victory ? "Victory" : "Defeat";
            if (advisoryLabel != null)
            {
                advisoryLabel.text = data.AdvisoryText ?? string.Empty;
                advisoryLabel.gameObject.SetActive(!string.IsNullOrEmpty(data.AdvisoryText));
            }
            else if (!string.IsNullOrEmpty(data.AdvisoryText) && titleLabel != null)
                titleLabel.text += $"\n{data.AdvisoryText}";
            if (totalScoreLabel != null)
                totalScoreLabel.text = total.ToString();
            if (scoreDeltaLabel != null)
            {
                scoreDeltaLabel.text = delta >= 0 ? $" +{delta}" : delta.ToString();
                scoreDeltaLabel.color = victory ? Color.green : Color.red;
            }

            RefreshRewards(data.Rewards ?? BattleResultData.CreateDefault(victory).Rewards);
            gameObject.SetActive(true);
            if (victoryConfetti != null)
                victoryConfetti.SetActive(victory);
            PlayReveal();
        }

        public void ReturnToMainMenu()
        {
            revealSequence?.Kill();
            SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
        }

        private void RefreshRewards(IReadOnlyList<BattleResultReward> rewards)
        {
            for (int index = 0; index < rewardLabels.Count; index++)
                rewardLabels[index].gameObject.SetActive(false);
            if (rewards == null) return;

            int count = Mathf.Min(rewards.Count, rewardLabels.Count);
            for (int index = 0; index < count; index++)
            {
                TextMeshProUGUI label = rewardLabels[index];
                if (label == null) continue;
                label.text = $"{rewards[index].Label}  x{rewards[index].Amount}";
                label.gameObject.SetActive(true);
            }
        }

        private void PlayReveal()
        {
            revealSequence?.Kill();
            Transform panel = transform;
            panel.localScale = new Vector3(0.92f, 0.92f, 1f);
            revealSequence = DOTween.Sequence()
                .Append(panel.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
        }
    }
}
