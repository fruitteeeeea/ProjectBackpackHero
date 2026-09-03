using System;
using BackpackHero.Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace BackpackHero.Input
{
    [DisallowMultipleComponent]
    public sealed class BattleCurvePulseRenderer : MonoBehaviour
    {
        public const int MinimumSampleCount = 2;
        public const int MaximumSampleCount = 32;

        [Header("References")]
        [SerializeField] private CurvedConnectionRenderer curve;
        [SerializeField] private LineRenderer pulseLineRenderer;
        [SerializeField] private Material pulseMaterial;

        [Header("Pulse Timing")]
        [SerializeField, Min(0.01f)] private float travelDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float endFadeDuration = 0.25f;
        [FormerlySerializedAs("intervalDuration")]
        [SerializeField, Min(0f)] private float cooldownDuration = 2.5f;

        [Header("Pulse Shape")]
        [SerializeField, Range(0.01f, 1f)] private float pulseLength = 0.16f;
        [SerializeField, Range(MinimumSampleCount, MaximumSampleCount)]
        private int sampleCount = 12;
        [SerializeField, Min(0.001f)] private float pulseWidth = 0.42f;

        private Vector3[] sampledPositions;
        private float combatStartTime;
        private float materialOpacity = 1f;
        private float runtimeVisibility = 1f;
        private MaterialPropertyBlock materialProperties;

        private static readonly int OpacityId =
            Shader.PropertyToID("_Opacity");

        public static BattleCurvePulseRenderer ActiveInstance { get; private set; }

        /// <summary>
        /// True while the current pulse is travelling or fading at the curve end.
        /// </summary>
        public bool IsPulseActive { get; private set; }

        public BattleCurvePulseSettings Settings => new(
            travelDuration,
            endFadeDuration,
            cooldownDuration,
            pulseLength,
            sampleCount,
            pulseWidth);

        /// <summary>
        /// Raised when a visible pulse completes its travel and end fade naturally.
        /// </summary>
        public event Action PulseCompleted;

        /// <summary>即时应用调试面板编辑的脉冲显示参数。</summary>
        public void SetSettings(BattleCurvePulseSettings settings)
        {
            travelDuration = settings.TravelDuration;
            endFadeDuration = settings.EndFadeDuration;
            cooldownDuration = settings.CooldownDuration;
            pulseLength = settings.PulseLength;
            sampleCount = settings.SampleCount;
            pulseWidth = settings.PulseWidth;
            EnsureLineRenderer();
        }

        public static void CalculatePulseRange(
            float progress,
            float length,
            out float tailTime,
            out float headTime)
        {
            headTime = Mathf.Clamp01(progress);
            tailTime = Mathf.Clamp01(
                headTime - Mathf.Clamp01(length));
        }

        public static float CalculateEndFadeOpacity(
            float elapsedTime,
            float travelTime,
            float fadeTime)
        {
            if (elapsedTime <= travelTime)
            {
                return 1f;
            }

            return 1f - Mathf.Clamp01(
                (elapsedTime - travelTime) /
                Mathf.Max(0.01f, fadeTime));
        }

        public static bool IsPulseActiveAtElapsedTime(
            float elapsedTime,
            float travelTime,
            float fadeTime)
        {
            return elapsedTime >= 0f &&
                elapsedTime < Mathf.Max(0.01f, travelTime) +
                Mathf.Max(0.01f, fadeTime);
        }

        public static bool IsPulseVisibleAtCycleTime(
            float elapsedTime,
            float travelTime,
            float fadeTime,
            float cooldownTime)
        {
            float cycleDuration = Mathf.Max(.01f, travelTime) +
                Mathf.Max(.01f, fadeTime) + Mathf.Max(0f, cooldownTime);
            float cycleTime = Mathf.Repeat(elapsedTime, cycleDuration);
            return IsPulseActiveAtElapsedTime(cycleTime, travelTime, fadeTime);
        }

        public void SetRuntimeVisibility(float visibility)
        {
            runtimeVisibility = Mathf.Clamp01(visibility);
            if (runtimeVisibility <= 0f)
            {
                SetPulseVisible(false);
                SetPulseActive(false, false);
            }
        }

        private void OnEnable()
        {
            ActiveInstance = this;
            EnsureLineRenderer();
            BattleFlowController.PhaseChanged += HandlePhaseChanged;
            HandlePhaseChanged(BattleFlowController.CurrentPhase);
        }

        private void OnDisable()
        {
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
            SetPulseActive(false, false);
            if (ActiveInstance == this)
            {
                ActiveInstance = null;
            }
        }

        private void OnValidate()
        {
            SetSettings(Settings);
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying ||
                !BattleFlowController.IsCombatPhase)
            {
                SetPulseVisible(false);
                SetPulseActive(false, false);
                return;
            }

            RefreshPulse();
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.Combat)
            {
                combatStartTime = Time.time;
                SetPulseActive(false, false);
                return;
            }

            SetPulseVisible(false);
            SetPulseActive(false, false);
        }

        private void RefreshPulse()
        {
            if (curve == null || pulseLineRenderer == null)
            {
                SetPulseVisible(false);
                SetPulseActive(false, false);
                return;
            }

            if (runtimeVisibility <= 0f)
            {
                SetPulseVisible(false);
                SetPulseActive(false, false);
                return;
            }

            float elapsedTime = Time.time - combatStartTime;
            float cycleDuration = travelDuration + endFadeDuration +
                cooldownDuration;
            float cycleTime = Mathf.Repeat(elapsedTime, cycleDuration);
            if (!IsPulseVisibleAtCycleTime(
                    elapsedTime,
                    travelDuration,
                    endFadeDuration,
                    cooldownDuration))
            {
                SetPulseVisible(false);
                SetPulseActive(false, true);
                return;
            }

            CalculatePulseRange(
                Mathf.Min(cycleTime / travelDuration, 1f),
                pulseLength,
                out float tailTime,
                out float headTime);
            SetPulseOpacity(CalculateEndFadeOpacity(
                cycleTime,
                travelDuration,
                endFadeDuration) * runtimeVisibility);
            int positionCount = sampleCount + 1;
            EnsureSampleBuffer(positionCount);

            for (int index = 0; index < positionCount; index++)
            {
                float normalizedPosition = (float)index / sampleCount;
                float curveTime = Mathf.Lerp(
                    tailTime,
                    headTime,
                    normalizedPosition);
                if (!curve.TryEvaluatePoint(
                        curveTime,
                        out sampledPositions[index]))
                {
                    SetPulseVisible(false);
                    SetPulseActive(false, false);
                    return;
                }
            }

            pulseLineRenderer.positionCount = positionCount;
            pulseLineRenderer.SetPositions(sampledPositions);
            pulseLineRenderer.enabled = true;
            SetPulseActive(true, false);
        }

        private void EnsureLineRenderer()
        {
            if (pulseLineRenderer == null)
            {
                pulseLineRenderer = GetComponent<LineRenderer>();
            }

            if (pulseLineRenderer == null)
            {
                pulseLineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            pulseLineRenderer.useWorldSpace = true;
            pulseLineRenderer.loop = false;
            pulseLineRenderer.alignment = LineAlignment.View;
            pulseLineRenderer.textureMode = LineTextureMode.Stretch;
            pulseLineRenderer.numCapVertices = 8;
            pulseLineRenderer.numCornerVertices = 4;
            pulseLineRenderer.startWidth = pulseWidth;
            pulseLineRenderer.endWidth = pulseWidth;
            pulseLineRenderer.sortingOrder = 2;

            if (pulseMaterial != null)
            {
                pulseLineRenderer.sharedMaterial = pulseMaterial;
                materialOpacity = pulseMaterial.GetFloat(OpacityId);
            }

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(0.35f, 0.8f, 1f), 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.35f, 0.35f),
                    new GradientAlphaKey(1f, 1f),
                });
            pulseLineRenderer.colorGradient = gradient;
        }

        private void EnsureSampleBuffer(int positionCount)
        {
            if (sampledPositions == null ||
                sampledPositions.Length != positionCount)
            {
                sampledPositions = new Vector3[positionCount];
            }
        }

        private void SetPulseVisible(bool visible)
        {
            if (pulseLineRenderer != null)
            {
                pulseLineRenderer.enabled = visible;
            }
        }

        private void SetPulseActive(
            bool isActive,
            bool notifyCompletion)
        {
            bool completed = IsPulseActive && !isActive && notifyCompletion;
            IsPulseActive = isActive;

            if (completed)
            {
                PulseCompleted?.Invoke();
            }
        }

        private void SetPulseOpacity(float opacity)
        {
            if (pulseLineRenderer == null)
            {
                return;
            }

            materialProperties ??= new MaterialPropertyBlock();
            pulseLineRenderer.GetPropertyBlock(materialProperties);
            materialProperties.SetFloat(
                OpacityId,
                materialOpacity * Mathf.Clamp01(opacity));
            pulseLineRenderer.SetPropertyBlock(materialProperties);
        }
    }
}
