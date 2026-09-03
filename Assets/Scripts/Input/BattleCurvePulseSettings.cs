using System;
using UnityEngine;

namespace BackpackHero.Input
{
    /// <summary>战斗曲线脉冲的可编辑显示参数。</summary>
    public readonly struct BattleCurvePulseSettings :
        IEquatable<BattleCurvePulseSettings>
    {
        public const float MinimumTravelDuration = .01f;
        public const float MinimumEndFadeDuration = .01f;
        public const float MinimumCooldownDuration = 0f;
        public const float MinimumPulseLength = .01f;
        public const float MaximumPulseLength = 1f;
        public const float MinimumPulseWidth = .001f;

        public BattleCurvePulseSettings(
            float travelDuration,
            float endFadeDuration,
            float cooldownDuration,
            float pulseLength,
            int sampleCount,
            float pulseWidth)
        {
            TravelDuration = Mathf.Max(MinimumTravelDuration, travelDuration);
            EndFadeDuration = Mathf.Max(MinimumEndFadeDuration,
                endFadeDuration);
            CooldownDuration = Mathf.Max(MinimumCooldownDuration,
                cooldownDuration);
            PulseLength = Mathf.Clamp(pulseLength, MinimumPulseLength,
                MaximumPulseLength);
            SampleCount = Mathf.Clamp(sampleCount,
                BattleCurvePulseRenderer.MinimumSampleCount,
                BattleCurvePulseRenderer.MaximumSampleCount);
            PulseWidth = Mathf.Max(MinimumPulseWidth, pulseWidth);
        }

        public float TravelDuration { get; }
        public float EndFadeDuration { get; }
        public float CooldownDuration { get; }
        public float PulseLength { get; }
        public int SampleCount { get; }
        public float PulseWidth { get; }

        public bool Equals(BattleCurvePulseSettings other) =>
            Mathf.Approximately(TravelDuration, other.TravelDuration) &&
            Mathf.Approximately(EndFadeDuration, other.EndFadeDuration) &&
            Mathf.Approximately(CooldownDuration, other.CooldownDuration) &&
            Mathf.Approximately(PulseLength, other.PulseLength) &&
            SampleCount == other.SampleCount &&
            Mathf.Approximately(PulseWidth, other.PulseWidth);

        public override bool Equals(object obj) =>
            obj is BattleCurvePulseSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = TravelDuration.GetHashCode();
                hash = (hash * 31) + EndFadeDuration.GetHashCode();
                hash = (hash * 31) + CooldownDuration.GetHashCode();
                hash = (hash * 31) + PulseLength.GetHashCode();
                hash = (hash * 31) + SampleCount;
                return (hash * 31) + PulseWidth.GetHashCode();
            }
        }
    }
}
