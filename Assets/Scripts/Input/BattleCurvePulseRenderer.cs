using System;
using BackpackHero.Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace BackpackHero.Input
{
    /// <summary>Tracks one pulse and the cooldown that follows it.</summary>
    public sealed class BattleCurvePulseStateModel
    {
        public bool IsPulseActive { get; private set; }
        public float CooldownEndsAt { get; private set; }

        public bool TryTrigger(float time)
        {
            if (IsPulseActive || time < CooldownEndsAt)
            {
                return false;
            }

            IsPulseActive = true;
            return true;
        }

        public bool Update(float time, float pulseStartedAt,
            float travelTime, float fadeTime, float cooldownTime)
        {
            float completedAt = pulseStartedAt + Mathf.Max(.01f, travelTime) +
                Mathf.Max(.01f, fadeTime);
            if (!IsPulseActive || time < completedAt)
            {
                return false;
            }

            IsPulseActive = false;
            CooldownEndsAt = completedAt + Mathf.Max(0f, cooldownTime);
            return true;
        }

        public void Reset()
        {
            IsPulseActive = false;
            CooldownEndsAt = 0f;
        }
    }

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
        [SerializeField, Min(0.01f)] private float travelDuration = .55f;
        [SerializeField, Min(0.01f)] private float endFadeDuration = .25f;
        [FormerlySerializedAs("intervalDuration")]
        [SerializeField, Min(0f)] private float cooldownDuration = 6f;

        [Header("Pulse Shape")]
        [SerializeField, Range(.01f, 1f)] private float pulseLength = .16f;
        [SerializeField, Range(MinimumSampleCount, MaximumSampleCount)]
        private int sampleCount = 12;
        [SerializeField, Min(.001f)] private float pulseWidth = .42f;

        private readonly BattleCurvePulseStateModel pulseState = new();
        private Vector3[] sampledPositions;
        private float pulseStartedAt;
        private float materialOpacity = 1f;
        private float runtimeVisibility = 1f;
        private MaterialPropertyBlock materialProperties;

        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

        public static BattleCurvePulseRenderer ActiveInstance { get; private set; }
        public bool IsPulseActive => pulseState.IsPulseActive;
        public BattleCurvePulseSettings Settings => new(travelDuration,
            endFadeDuration, cooldownDuration, pulseLength, sampleCount, pulseWidth);
        public event Action PulseCompleted;

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

        /// <summary>Starts one pulse if combat, visibility, and cooldown permit it.</summary>
        public bool TryTriggerPulse()
        {
            if (!Application.isPlaying || !BattleFlowController.IsCombatPhase ||
                curve == null || pulseLineRenderer == null || runtimeVisibility <= 0f)
            {
                return false;
            }

            float time = Time.unscaledTime;
            CompletePulseIfFinished(time);
            if (!pulseState.TryTrigger(time))
            {
                return false;
            }

            pulseStartedAt = time;
            return true;
        }

        public static void CalculatePulseRange(float progress, float length,
            out float tailTime, out float headTime)
        {
            headTime = Mathf.Clamp01(progress);
            tailTime = Mathf.Clamp01(headTime - Mathf.Clamp01(length));
        }

        public static float CalculateEndFadeOpacity(float elapsedTime,
            float travelTime, float fadeTime)
        {
            if (elapsedTime <= travelTime)
            {
                return 1f;
            }

            return 1f - Mathf.Clamp01((elapsedTime - travelTime) /
                Mathf.Max(.01f, fadeTime));
        }

        public static bool IsPulseActiveAtElapsedTime(float elapsedTime,
            float travelTime, float fadeTime) => elapsedTime >= 0f &&
            elapsedTime < Mathf.Max(.01f, travelTime) + Mathf.Max(.01f, fadeTime);

        public void SetRuntimeVisibility(float visibility)
        {
            runtimeVisibility = Mathf.Clamp01(visibility);
            if (runtimeVisibility <= 0f)
            {
                SetPulseVisible(false);
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
            pulseState.Reset();
            SetPulseVisible(false);
            if (ActiveInstance == this)
            {
                ActiveInstance = null;
            }
        }

        private void OnValidate() => SetSettings(Settings);

        private void LateUpdate()
        {
            if (!Application.isPlaying || !BattleFlowController.IsCombatPhase)
            {
                SetPulseVisible(false);
                pulseState.Reset();
                return;
            }

            RefreshPulse();
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            pulseState.Reset();
            if (phase != BattlePhase.Combat)
            {
                SetPulseVisible(false);
            }
        }

        private void RefreshPulse()
        {
            float time = Time.unscaledTime;
            if (CompletePulseIfFinished(time) || !IsPulseActive || curve == null ||
                pulseLineRenderer == null || runtimeVisibility <= 0f)
            {
                SetPulseVisible(false);
                return;
            }

            float elapsedTime = time - pulseStartedAt;
            CalculatePulseRange(Mathf.Min(elapsedTime / travelDuration, 1f),
                pulseLength, out float tailTime, out float headTime);
            SetPulseOpacity(CalculateEndFadeOpacity(elapsedTime, travelDuration,
                endFadeDuration) * runtimeVisibility);

            int positionCount = sampleCount + 1;
            EnsureSampleBuffer(positionCount);
            for (int index = 0; index < positionCount; index++)
            {
                float curveTime = Mathf.Lerp(tailTime, headTime,
                    (float)index / sampleCount);
                if (!curve.TryEvaluatePoint(curveTime, out sampledPositions[index]))
                {
                    SetPulseVisible(false);
                    return;
                }
            }

            pulseLineRenderer.positionCount = positionCount;
            pulseLineRenderer.SetPositions(sampledPositions);
            pulseLineRenderer.enabled = true;
        }

        private bool CompletePulseIfFinished(float time)
        {
            if (!pulseState.Update(time, pulseStartedAt, travelDuration,
                    endFadeDuration, cooldownDuration))
            {
                return false;
            }

            PulseCompleted?.Invoke();
            return true;
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
            gradient.SetKeys(new[] { new GradientColorKey(new Color(.35f, .8f, 1f), 0f),
                new GradientColorKey(Color.white, 1f) }, new[] {
                new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.35f, .35f),
                new GradientAlphaKey(1f, 1f) });
            pulseLineRenderer.colorGradient = gradient;
        }

        private void EnsureSampleBuffer(int positionCount)
        {
            if (sampledPositions == null || sampledPositions.Length != positionCount)
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

        private void SetPulseOpacity(float opacity)
        {
            if (pulseLineRenderer == null)
            {
                return;
            }

            materialProperties ??= new MaterialPropertyBlock();
            pulseLineRenderer.GetPropertyBlock(materialProperties);
            materialProperties.SetFloat(OpacityId,
                materialOpacity * Mathf.Clamp01(opacity));
            pulseLineRenderer.SetPropertyBlock(materialProperties);
        }
    }
}
