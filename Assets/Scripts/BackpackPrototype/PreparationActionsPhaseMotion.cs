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

        private BattlePhase? requestedPhase;

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
            if (requestedPhase != BattlePhase.Combat ||
                moduleRoot == null)
            {
                return;
            }

            moduleRoot.SetActive(false);
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