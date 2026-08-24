using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Spine.Unity;

namespace BackpackHero.Battle
{
    /// <summary>Compatibility controller for the unmodified migrated UIGameSettle visual prefab.</summary>
    public sealed class OriginalSettlementPanelAdapter : MonoBehaviour
    {
        [SerializeField] private Image titleBackground;
        [SerializeField] private Sprite victorySprite;
        [SerializeField] private Sprite defeatSprite;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI deltaText;
        [SerializeField] private Transform rewardRoot;
        [SerializeField] private ItemReward rewardPrefab;
        [SerializeField] private GameObject victoryAnimation;
        [SerializeField] private SkeletonGraphic victorySpine;
        [SerializeField] private Button continueButton;
        [SerializeField] private string mainMenuSceneName = "MainMenuDemo";

        private readonly List<ItemReward> rewardInstances = new();
        private void Awake()
        {
            // Keep the source-style prefab assignment in the inspector, while
            // recovering safely from old prefabs serialized before this field
            // changed from a GameObject to ItemReward.
            if (rewardPrefab == null)
            {
                rewardPrefab = Resources.Load<ItemReward>(
                    "PlanetWar/OriginalSettlement/Prefab/ItemReward");
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(ReturnToMainMenu);
            }
            if (victoryAnimation != null) victoryAnimation.SetActive(false);
        }

        public void Show(BattleResultData data)
        {
            data ??= BattleResultData.CreateDefault(true);
            bool victory = data.IsVictory;
            if (titleBackground != null)
                titleBackground.sprite = victory ? victorySprite : defeatSprite;
            if (titleText != null) titleText.text = victory ? "Victory" : "Defeat";
            if (scoreText != null) scoreText.text = data.TotalScore.ToString();
            if (deltaText != null)
            {
                deltaText.text = data.ScoreDelta >= 0
                    ? $" +{data.ScoreDelta}" : data.ScoreDelta.ToString();
                deltaText.color = victory ? Color.green : Color.red;
            }
            RefreshRewards(data.Rewards);
            if (rewardRoot is RectTransform rewardRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rewardRect);
            }
            gameObject.SetActive(true);
            if (victoryAnimation != null)
            {
                victoryAnimation.SetActive(victory);
                if (victory && victorySpine != null)
                    victorySpine.AnimationState.SetAnimation(0, "animation", true);
            }
        }

        private void RefreshRewards(IReadOnlyList<BattleResultReward> rewards)
        {
            for (int index = 0; index < rewardInstances.Count; index++)
            {
                rewardInstances[index].ResetForReuse();
                rewardInstances[index].gameObject.SetActive(false);
            }

            if (rewards == null || rewardPrefab == null || rewardRoot == null)
            {
                return;
            }

            for (int index = 0; index < rewards.Count; index++)
            {
                ItemReward item;
                if (index < rewardInstances.Count)
                {
                    item = rewardInstances[index];
                }
                else
                {
                    item = Instantiate(rewardPrefab, rewardRoot);
                    rewardInstances.Add(item);
                }

                item.gameObject.SetActive(true);
                item.BindCurrency(rewards[index].Label == "DIAMOND", rewards[index].Amount, false);
            }
        }

        private void ReturnToMainMenu()
        {
            // LevelFlowController persists across the single-scene return.
            // Reset its scoreboard and match lock before entering the menu so
            // the next SampleScene session always begins at round 1, 0 : 0.
            LevelFlowController.EnsureInstance()
                ?.ResetForLevel(LevelManager.CurrentLevel);
            SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
        }
    }
}
