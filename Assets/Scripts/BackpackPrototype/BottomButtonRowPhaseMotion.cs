using BackpackHero.Battle;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>
    /// Plays the BottomButtonRow FEEL motion when the battle phase changes.
    /// </summary>
    public sealed class BottomButtonRowPhaseMotion : MonoBehaviour
    {
        [SerializeField]
        private MMF_Player showFeedbacks;

        [SerializeField]
        private MMF_Player hideFeedbacks;

        private BattlePhase? requestedPhase;
        private bool hasStarted;

        private void OnEnable()
        {
            BattleFlowController.PhaseChanged += HandlePhaseChanged;

            if (hasStarted)
            {
                PlayForPhase(BattleFlowController.CurrentPhase);
            }
        }

        private void Start()
        {
            hasStarted = true;
            PlayForPhase(BattleFlowController.CurrentPhase);
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
                showFeedbacks?.PlayFeedbacks();
                return;
            }

            showFeedbacks?.StopFeedbacks();
            hideFeedbacks?.PlayFeedbacks();
        }

        private void OnDisable()
        {
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
            StopFeedbacks();
            requestedPhase = null;
        }

        private void OnDestroy()
        {
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
        }

        private void StopFeedbacks()
        {
            showFeedbacks?.StopFeedbacks();
            hideFeedbacks?.StopFeedbacks();
        }
    }
}
