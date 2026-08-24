using BackpackHero.Battle;
using UnityEngine;

namespace BackpackHero.Input
{
    public sealed class CurveAdjustmentStateModel
    {
        public bool IsAdjusting { get; private set; }
        public bool IsWaitingForPulseCompletion { get; private set; }
        public float LastActivityTime { get; private set; }

        public void RegisterActivity(float time)
        {
            IsAdjusting = true;
            IsWaitingForPulseCompletion = false;
            LastActivityTime = time;
        }

        public void Update(float time, bool inputEnabled, float idleDelay)
        {
            Update(time, inputEnabled, idleDelay, false);
        }

        public void Update(
            float time,
            bool inputEnabled,
            float idleDelay,
            bool isPulseActive)
        {
            if (!inputEnabled)
            {
                IsAdjusting = false;
                IsWaitingForPulseCompletion = false;
                return;
            }

            if (!IsAdjusting || IsWaitingForPulseCompletion ||
                time < LastActivityTime + idleDelay)
            {
                return;
            }

            if (isPulseActive)
            {
                IsWaitingForPulseCompletion = true;
                return;
            }

            IsAdjusting = false;
        }

        public void CompletePulseWait()
        {
            if (!IsWaitingForPulseCompletion)
            {
                return;
            }

            IsWaitingForPulseCompletion = false;
            IsAdjusting = false;
        }
    }

    [DisallowMultipleComponent]
    public sealed class CurveAdjustmentVisualController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HorizontalSwipeCurveInput input;
        [SerializeField] private CurvedConnectionRenderer adjustmentCurve;
        [SerializeField] private CurvedConnectionRenderer battleCurve;
        [SerializeField] private BattleCurvePulseRenderer adjustmentHighlight;
        [SerializeField] private CurveBeaconController beacon;

        [Header("Adjustment State")]
        [SerializeField, Min(0f)] private float idleDelay = 1f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.25f;
        [SerializeField, Range(0f, 1f)] private float idleBeaconOpacity = 0.5f;

        private readonly CurveAdjustmentStateModel state = new();
        private float adjustmentVisibility;

        private void OnEnable()
        {
            SubscribeToInput();
            SubscribeToAdjustmentHighlight();
            BattleFlowController.PhaseChanged += HandlePhaseChanged;
            ResetVisuals();
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
            UnsubscribeFromAdjustmentHighlight();
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
        }

        private void OnValidate()
        {
            idleDelay = Mathf.Max(0f, idleDelay);
            fadeDuration = Mathf.Max(0.01f, fadeDuration);
            idleBeaconOpacity = Mathf.Clamp01(idleBeaconOpacity);
        }

        private void LateUpdate()
        {
            SyncBattleCurve();

            bool canAdjust = BattleFlowController.IsCombatPhase &&
                input != null && input.IsInputEnabled;
            state.Update(
                Time.unscaledTime,
                canAdjust,
                idleDelay,
                adjustmentHighlight != null && adjustmentHighlight.IsPulseActive);

            float targetVisibility = state.IsAdjusting ? 1f : 0f;
            adjustmentVisibility = Mathf.MoveTowards(
                adjustmentVisibility,
                targetVisibility,
                Time.unscaledDeltaTime / fadeDuration);
            ApplyVisuals();
        }

        private void SubscribeToInput()
        {
            if (input == null)
            {
                return;
            }

            input.PointerBegan -= HandlePointerActivity;
            input.PointerBegan += HandlePointerActivity;
            input.PointerMoved -= HandlePointerActivity;
            input.PointerMoved += HandlePointerActivity;
        }

        private void UnsubscribeFromInput()
        {
            if (input != null)
            {
                input.PointerBegan -= HandlePointerActivity;
                input.PointerMoved -= HandlePointerActivity;
            }
        }

        private void SubscribeToAdjustmentHighlight()
        {
            if (adjustmentHighlight == null)
            {
                return;
            }

            adjustmentHighlight.PulseCompleted -= HandlePulseCompleted;
            adjustmentHighlight.PulseCompleted += HandlePulseCompleted;
        }

        private void UnsubscribeFromAdjustmentHighlight()
        {
            if (adjustmentHighlight != null)
            {
                adjustmentHighlight.PulseCompleted -= HandlePulseCompleted;
            }
        }

        private void HandlePointerActivity(Vector2 _)
        {
            if (BattleFlowController.IsCombatPhase)
            {
                state.RegisterActivity(Time.unscaledTime);
            }
        }

        private void HandlePulseCompleted()
        {
            state.CompletePulseWait();
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase != BattlePhase.Combat)
            {
                state.Update(Time.unscaledTime, false, idleDelay);
                adjustmentVisibility = 0f;
            }

            ApplyVisuals();
        }

        private void SyncBattleCurve()
        {
            if (adjustmentCurve == null || battleCurve == null)
            {
                return;
            }

            if (battleCurve.PlayerEndpoint != adjustmentCurve.PlayerEndpoint ||
                battleCurve.EnemyEndpoint != adjustmentCurve.EnemyEndpoint)
            {
                battleCurve.SetEndpoints(
                    adjustmentCurve.PlayerEndpoint,
                    adjustmentCurve.EnemyEndpoint);
            }

            battleCurve.SetBattleWorldEndpointReadiness(
                adjustmentCurve.HasBattleWorldEndpoints);

            battleCurve.SetCurveValue(adjustmentCurve.CurrentCurveValue);
        }

        private void ResetVisuals()
        {
            adjustmentVisibility = 0f;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            adjustmentCurve?.SetRuntimeVisibility(adjustmentVisibility);
            battleCurve?.SetRuntimeVisibility(1f - adjustmentVisibility);
            adjustmentHighlight?.SetRuntimeVisibility(adjustmentVisibility);
            beacon?.SetVisualOpacity(Mathf.Lerp(
                idleBeaconOpacity,
                1f,
                adjustmentVisibility));
        }
    }
}
