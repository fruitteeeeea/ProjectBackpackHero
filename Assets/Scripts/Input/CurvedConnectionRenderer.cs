using UnityEngine;
using BackpackHero.Battle;

namespace BackpackHero.Input
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CurvedConnectionRenderer : MonoBehaviour
    {
        public const int MinSegmentCount = 2;
        public const int MaxSegmentCount = 128;

        [Header("Connections")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform enemy;
        [SerializeField] private HorizontalSwipeCurveInput input;
        [SerializeField] private LineRenderer lineRenderer;

        [Header("Curve Shape")]
        [SerializeField, Min(0f)] private float maxBendDistance = 2f;
        [SerializeField, Range(MinSegmentCount, MaxSegmentCount)] private int segmentCount = 32;

        [Header("Line Visual")]
        [SerializeField] private Material lineMaterial;
        [SerializeField, Min(0.001f)] private float lineWidth = 0.12f;
        [SerializeField] private Color lineColor = new(1f, 0.85f, 0.25f, 1f);

        private float currentCurveValue;

        public float CurrentCurveValue => currentCurveValue;

        public float MaxBendDistance
        {
            get => maxBendDistance;
            set
            {
                maxBendDistance = Mathf.Max(0f, value);
                RefreshCurve();
            }
        }

        public int SegmentCount
        {
            get => segmentCount;
            set
            {
                segmentCount = Mathf.Clamp(value, MinSegmentCount, MaxSegmentCount);
                RefreshCurve();
            }
        }

        public void SetEndpoints(
            Transform playerEndpoint,
            Transform enemyEndpoint)
        {
            player = playerEndpoint;
            enemy = enemyEndpoint;
            RefreshCurve();
        }

        public void SetCurveValue(float value)
        {
            var clampedValue = Mathf.Clamp(value, -1f, 1f);
            if (Mathf.Approximately(currentCurveValue, clampedValue))
            {
                return;
            }

            currentCurveValue = clampedValue;
            RefreshCurve();
        }

        public static Vector3 CalculatePoint(
            Vector3 playerPosition,
            Vector3 enemyPosition,
            float curveValue,
            float maximumBendDistance,
            float t)
        {
            return BattleCurve2D.Evaluate(
                playerPosition,
                enemyPosition,
                curveValue,
                maximumBendDistance,
                t);
        }

        private void OnEnable()
        {
            EnsureLineRenderer();
            SubscribeToInput();
            HorizontalSwipeCurveDebugBridge.RegisterCurve(this);
            if (Application.isPlaying)
            {
                BattleFlowController.PhaseChanged +=
                    HandlePhaseChanged;
                HandlePhaseChanged(
                    BattleFlowController.CurrentPhase);
            }
            RefreshCurve();
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
            HorizontalSwipeCurveDebugBridge.UnregisterCurve(this);
            if (Application.isPlaying)
            {
                BattleFlowController.PhaseChanged -=
                    HandlePhaseChanged;
            }
        }

        private void OnValidate()
        {
            maxBendDistance = Mathf.Max(0f, maxBendDistance);
            segmentCount = Mathf.Clamp(segmentCount, MinSegmentCount, MaxSegmentCount);
            lineWidth = Mathf.Max(0.001f, lineWidth);
            EnsureLineRenderer();
            RefreshCurve();
        }

        private void LateUpdate()
        {
            RefreshCurve();
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            EnsureLineRenderer();

            if (lineRenderer != null)
            {
                lineRenderer.enabled =
                    phase == BattlePhase.Combat;
            }
        }

        private void SubscribeToInput()
        {
            if (input == null)
            {
                return;
            }

            input.ValueChanged -= SetCurveValue;
            input.ValueChanged += SetCurveValue;
            SetCurveValue(input.CurrentValue);
        }

        private void UnsubscribeFromInput()
        {
            if (input != null)
            {
                input.ValueChanged -= SetCurveValue;
            }
        }

        private void EnsureLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.sortingOrder = 0;

            if (lineMaterial != null)
            {
                lineRenderer.sharedMaterial = lineMaterial;
            }
        }

        private void RefreshCurve()
        {
            if (lineRenderer == null || player == null || enemy == null)
            {
                return;
            }

            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.positionCount = segmentCount + 1;

            for (var index = 0; index <= segmentCount; index++)
            {
                var t = (float)index / segmentCount;
                lineRenderer.SetPosition(index, CalculatePoint(
                    player.position,
                    enemy.position,
                    currentCurveValue,
                    maxBendDistance,
                    t));
            }
        }
    }
}
