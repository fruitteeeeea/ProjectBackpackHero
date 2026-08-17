using BackpackHero.Battle;
using UnityEngine;

namespace BackpackHero.Input
{
    public sealed class CurveBeaconDragStateModel
    {
        public bool IsDraggingBeacon { get; private set; }

        public void Begin(bool beganOnBeacon)
        {
            IsDraggingBeacon = beganOnBeacon;
        }

        public void End()
        {
            IsDraggingBeacon = false;
        }

        public Vector3 ResolvePosition(
            Vector3 curvePoint,
            Vector3 currentPosition)
        {
            return IsDraggingBeacon
                ? curvePoint
                : new Vector3(
                    curvePoint.x,
                    currentPosition.y,
                    currentPosition.z);
        }
    }

    [DisallowMultipleComponent]
    public sealed class CurveBeaconController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CurvedConnectionRenderer curve;
        [SerializeField] private HorizontalSwipeCurveInput input;
        [SerializeField] private SpriteRenderer beaconRenderer;
        [SerializeField] private Camera worldCamera;

        [Header("Placement")]
        [SerializeField, Range(2, CurvedConnectionRenderer.MaxSegmentCount)]
        private int closestPointSampleCount = 64;
        [SerializeField, Range(0f, 1f)] private float normalizedTime = 0.5f;

        [Header("Visibility")]
        [SerializeField, Range(0f, 1f)] private float idleOpacity = 0.35f;
        [SerializeField, Min(0f)] private float idleFadeDelay = 1f;
        [SerializeField, Min(0.01f)] private float idleFadeDuration = 0.25f;

        private float originalAlpha = 1f;
        private float currentOpacity = 1f;
        private float lastPointerMotionTime;
        private readonly CurveBeaconDragStateModel dragState = new();

        private void Awake()
        {
            if (beaconRenderer == null)
            {
                beaconRenderer = GetComponent<SpriteRenderer>();
            }

            originalAlpha = beaconRenderer != null
                ? beaconRenderer.color.a
                : 1f;
        }

        private void OnEnable()
        {
            if (beaconRenderer == null)
            {
                beaconRenderer = GetComponent<SpriteRenderer>();
            }

            SetOpacity(idleOpacity);
            SubscribeToInput();
            BattleFlowController.PhaseChanged += HandlePhaseChanged;
            HandlePhaseChanged(BattleFlowController.CurrentPhase);
            RefreshPosition();
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
            dragState.End();
        }

        private void OnValidate()
        {
            closestPointSampleCount = Mathf.Clamp(
                closestPointSampleCount,
                CurvedConnectionRenderer.MinSegmentCount,
                CurvedConnectionRenderer.MaxSegmentCount);
            normalizedTime = Mathf.Clamp01(normalizedTime);
            idleOpacity = Mathf.Clamp01(idleOpacity);
            idleFadeDelay = Mathf.Max(0f, idleFadeDelay);
            idleFadeDuration = Mathf.Max(0.01f, idleFadeDuration);

            if (beaconRenderer == null)
            {
                beaconRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void LateUpdate()
        {
            RefreshPosition();
            UpdateOpacity();
        }

        private void SubscribeToInput()
        {
            if (input == null)
            {
                return;
            }

            input.PointerBegan -= HandlePointerBegan;
            input.PointerBegan += HandlePointerBegan;
            input.PointerMoved -= HandlePointerMoved;
            input.PointerMoved += HandlePointerMoved;
            input.PointerEnded -= HandlePointerEnded;
            input.PointerEnded += HandlePointerEnded;
            input.InputEnabledChanged -= HandleInputEnabledChanged;
            input.InputEnabledChanged += HandleInputEnabledChanged;
        }

        private void UnsubscribeFromInput()
        {
            if (input != null)
            {
                input.PointerBegan -= HandlePointerBegan;
                input.PointerMoved -= HandlePointerMoved;
                input.PointerEnded -= HandlePointerEnded;
                input.InputEnabledChanged -= HandleInputEnabledChanged;
            }
        }

        private void HandlePointerBegan(Vector2 screenPosition)
        {
            BeginPointerDrag(screenPosition);
        }

        private void HandlePointerMoved(Vector2 screenPosition)
        {
            UpdatePointerDrag(screenPosition);
        }

        private void BeginPointerDrag(Vector2 screenPosition)
        {
            lastPointerMotionTime = Time.unscaledTime;
            SetOpacity(1f);
            dragState.Begin(IsPointerOnBeacon(screenPosition));

            if (dragState.IsDraggingBeacon)
            {
                UpdateClosestPosition(screenPosition);
            }
        }

        private void UpdatePointerDrag(Vector2 screenPosition)
        {
            lastPointerMotionTime = Time.unscaledTime;
            SetOpacity(1f);

            if (dragState.IsDraggingBeacon)
            {
                UpdateClosestPosition(screenPosition);
            }
        }

        private void HandlePointerEnded()
        {
            dragState.End();
        }

        private void HandleInputEnabledChanged(bool isEnabled)
        {
            if (!isEnabled)
            {
                dragState.End();
            }
        }

        private bool IsPointerOnBeacon(Vector2 screenPosition)
        {
            if (beaconRenderer == null)
            {
                return false;
            }

            Camera targetCamera = worldCamera != null
                ? worldCamera
                : Camera.main;

            if (targetCamera == null)
            {
                return false;
            }

            Ray pointerRay = targetCamera.ScreenPointToRay(screenPosition);
            Plane beaconPlane = new(
                targetCamera.transform.forward,
                beaconRenderer.bounds.center);

            return beaconPlane.Raycast(pointerRay, out float distance) &&
                beaconRenderer.bounds.Contains(
                    pointerRay.GetPoint(distance));
        }

        private void UpdateClosestPosition(Vector2 screenPosition)
        {
            Camera targetCamera = worldCamera != null
                ? worldCamera
                : Camera.main;

            if (curve != null &&
                curve.TryFindClosestNormalizedTime(
                    targetCamera,
                    screenPosition,
                    closestPointSampleCount,
                    out float closestTime))
            {
                normalizedTime = closestTime;
                RefreshPosition();
            }
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (beaconRenderer != null)
            {
                beaconRenderer.enabled = phase == BattlePhase.Combat;
            }

            if (phase == BattlePhase.Combat)
            {
                SetOpacity(idleOpacity);
                return;
            }

            dragState.End();
        }

        private void RefreshPosition()
        {
            if (curve != null &&
                curve.TryEvaluatePoint(normalizedTime, out Vector3 point))
            {
                transform.position = dragState.ResolvePosition(
                    point,
                    transform.position);
            }
        }

        private void UpdateOpacity()
        {
            if (Time.unscaledTime < lastPointerMotionTime + idleFadeDelay)
            {
                return;
            }

            float fadeSpeed =
                (1f - idleOpacity) / idleFadeDuration;
            SetOpacity(Mathf.MoveTowards(
                currentOpacity,
                idleOpacity,
                fadeSpeed * Time.unscaledDeltaTime));
        }

        private void SetOpacity(float opacity)
        {
            currentOpacity = Mathf.Clamp01(opacity);

            if (beaconRenderer == null)
            {
                return;
            }

            Color color = beaconRenderer.color;
            color.a = originalAlpha * currentOpacity;
            beaconRenderer.color = color;
        }

        public void SetVisualOpacity(float opacity)
        {
            SetOpacity(opacity);
        }
    }
}
