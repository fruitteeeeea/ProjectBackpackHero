using BackpackHero.Battle;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace BackpackPrototype
{
    public sealed class PreparationActionsPhaseMotion :
        MonoBehaviour
    {
        [SerializeField]
        private GameObject moduleRoot;

        [SerializeField]
        private MMF_Player showFeedbacks;

        [SerializeField]
        private MMF_Player hideFeedbacks;

        [SerializeField]
        private bool completesCombatTransition = true;

        private BattlePhase? requestedPhase;
        private PlayerBackpackSystem playerBackpackSystem;

        private void OnEnable()
        {
            BattleFlowController.PhaseChanged +=
                HandlePhaseChanged;
        }

        private void Start()
        {
            PlayForPhase(
                BattleFlowController.CurrentPhase);
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            PlayForPhase(phase);
        }

        private void PlayForPhase(BattlePhase phase)
        {
            if (requestedPhase == phase)
            {
                return;
            }

            requestedPhase = phase;

            if (phase == BattlePhase.Preparation)
            {
                hideFeedbacks?.StopFeedbacks();

                if (moduleRoot != null &&
                    !moduleRoot.activeSelf)
                {
                    moduleRoot.SetActive(true);
                }

                showFeedbacks?.PlayFeedbacks();
                return;
            }

            showFeedbacks?.StopFeedbacks();

            if (moduleRoot != null &&
                moduleRoot.activeSelf)
            {
                hideFeedbacks?.PlayFeedbacks();
            }
        }

        public void HandleHideCompleted()
        {
            if (requestedPhase !=
                BattlePhase.CombatTransition ||
                BattleFlowController.CurrentPhase !=
                BattlePhase.CombatTransition ||
                moduleRoot == null)
            {
                return;
            }

            moduleRoot.SetActive(false);

            if (!completesCombatTransition)
            {
                return;
            }

            playerBackpackSystem ??=
                GetComponentInParent<
                    PlayerBackpackSystem>(true);

            if (playerBackpackSystem == null)
            {
                Debug.LogError(
                    "背包FEEL动画完成，但找不到" +
                    "PlayerBackpackSystem，战斗不会启动。",
                    this);
                return;
            }

            playerBackpackSystem
                .CompleteCombatTransitionAfterMotion();
        }

        /// <summary>
        /// 由显示反馈的 OnComplete 调用，保证商店在背包缩放动画
        /// 已落到最终值后再计算其局部缩放。
        /// </summary>
        public void HandleShowCompleted()
        {
            if (requestedPhase != BattlePhase.Preparation ||
                BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation)
            {
                return;
            }

            playerBackpackSystem ??=
                GetComponentInParent<PlayerBackpackSystem>(true);
            playerBackpackSystem
                ?.RefreshShopLayoutAfterPreparationMotion();
        }

        private void OnDisable()
        {
            BattleFlowController.PhaseChanged -=
                HandlePhaseChanged;

            showFeedbacks?.StopFeedbacks();
            hideFeedbacks?.StopFeedbacks();
            requestedPhase = null;
        }

        private void OnDestroy()
        {
            BattleFlowController.PhaseChanged -=
                HandlePhaseChanged;
        }
    }
}
