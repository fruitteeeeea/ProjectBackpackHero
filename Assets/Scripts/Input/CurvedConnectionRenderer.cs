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
        [SerializeField] private Color lineColor = new(0.451f, 0.851f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float lineOpacity = 0.38f;
        [SerializeField, Min(0.01f)] private float textureTiling = 1f;
        [SerializeField, Min(0f)] private float flowSpeed = 0.7f;
        [SerializeField] private int sortingOrder;

        private float currentCurveValue;
        private float flowOffset;
        private float runtimeVisibility = 1f;
        private MaterialPropertyBlock materialProperties;

        private static readonly int FlowOffsetId =
            Shader.PropertyToID("_FlowOffset");

        public float CurrentCurveValue => currentCurveValue;
        public Transform PlayerEndpoint => player;
        public Transform EnemyEndpoint => enemy;
        public bool HasBattleWorldEndpoints { get; private set; }

        public void SetRuntimeVisibility(float visibility)
        {
            runtimeVisibility = Mathf.Clamp01(visibility);
            RefreshCurve();
        }

        public float LineWidth
        {
            get => lineWidth;
            set
            {
                lineWidth = Mathf.Max(0.001f, value);
                RefreshCurve();
            }
        }

        public float LineOpacity
        {
            get => lineOpacity;
            set
            {
                lineOpacity = Mathf.Clamp01(value);
                RefreshCurve();
            }
        }

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

        /// <summary>
        /// Sets endpoints supplied after both backpacks have completed their
        /// transition into battle world space. This is the only operation that
        /// permits the curve to render during combat.
        /// </summary>
        public void SetBattleWorldEndpoints(
            Transform playerEndpoint,
            Transform enemyEndpoint)
        {
            SetEndpoints(playerEndpoint, enemyEndpoint);
            HasBattleWorldEndpoints =
                playerEndpoint != null && enemyEndpoint != null;
            RefreshLineRendererVisibility();
            RefreshCurve();
        }

        public void SetBattleWorldEndpointReadiness(bool isReady)
        {
            HasBattleWorldEndpoints = isReady &&
                player != null && enemy != null;
            RefreshLineRendererVisibility();
            RefreshCurve();
        }

        public void SetInput(HorizontalSwipeCurveInput curveInput)
        {
            UnsubscribeFromInput();
            input = curveInput;
            SubscribeToInput();
        }

        public void ConfigureVisual(
            Material material,
            float width,
            Color color,
            float opacity,
            float tiling,
            float speed)
        {
            lineMaterial = material;
            lineWidth = Mathf.Max(0.001f, width);
            lineColor = color;
            lineOpacity = Mathf.Clamp01(opacity);
            textureTiling = Mathf.Max(0.01f, tiling);
            flowSpeed = Mathf.Max(0f, speed);
            EnsureLineRenderer();
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

        public bool TryEvaluatePoint(
            float normalizedTime,
            out Vector3 point)
        {
            point = default;

            if (player == null || enemy == null)
            {
                return false;
            }

            point = CalculatePoint(
                player.position,
                enemy.position,
                currentCurveValue,
                maxBendDistance,
                normalizedTime);
            return true;
        }

        public bool TryFindClosestNormalizedTime(
            Camera camera,
            Vector2 screenPosition,
            int sampleCount,
            out float normalizedTime)
        {
            normalizedTime = 0f;

            if (camera == null || player == null || enemy == null)
            {
                return false;
            }

            int safeSampleCount = Mathf.Clamp(
                sampleCount,
                MinSegmentCount,
                MaxSegmentCount);
            var projectedPoints = new Vector2[safeSampleCount + 1];

            for (int index = 0; index <= safeSampleCount; index++)
            {
                Vector3 worldPoint = CalculatePoint(
                    player.position,
                    enemy.position,
                    currentCurveValue,
                    maxBendDistance,
                    (float)index / safeSampleCount);
                Vector3 projectedPoint =
                    camera.WorldToScreenPoint(worldPoint);
                projectedPoints[index] = new Vector2(
                    projectedPoint.x,
                    projectedPoint.y);
            }

            normalizedTime = BattleCurve2D.FindClosestNormalizedTime(
                screenPosition,
                projectedPoints);
            return true;
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
            lineOpacity = Mathf.Clamp01(lineOpacity);
            textureTiling = Mathf.Max(0.01f, textureTiling);
            flowSpeed = Mathf.Max(0f, flowSpeed);
            EnsureLineRenderer();
            RefreshCurve();
        }

        private void LateUpdate()
        {
            if (Application.isPlaying && flowSpeed > 0f)
            {
                flowOffset = Mathf.Repeat(
                    flowOffset + Time.deltaTime * flowSpeed,
                    1f);
            }

            RefreshCurve();
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            EnsureLineRenderer();

            if (phase != BattlePhase.Combat)
            {
                HasBattleWorldEndpoints = false;
            }

            RefreshLineRendererVisibility();
        }

        private void RefreshLineRendererVisibility()
        {
            if (!Application.isPlaying || lineRenderer == null)
            {
                return;
            }

            lineRenderer.enabled = BattleFlowController.IsCombatPhase &&
                HasBattleWorldEndpoints;
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
            lineRenderer.sortingOrder = sortingOrder;
            lineRenderer.textureMode = LineTextureMode.Tile;

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
            var visibleColor = lineColor;
            visibleColor.a = lineOpacity * runtimeVisibility;
            lineRenderer.startColor = visibleColor;
            lineRenderer.endColor = visibleColor;
            lineRenderer.textureScale = new Vector2(textureTiling, 1f);
            ApplyMaterialProperties();
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

        private void ApplyMaterialProperties()
        {
            if (lineRenderer == null || lineRenderer.sharedMaterial == null)
            {
                return;
            }

            materialProperties ??= new MaterialPropertyBlock();
            lineRenderer.GetPropertyBlock(materialProperties);
            materialProperties.SetFloat(FlowOffsetId, flowOffset);
            lineRenderer.SetPropertyBlock(materialProperties);
        }
    }
}
