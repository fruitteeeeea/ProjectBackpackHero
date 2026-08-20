using System;
using DG.Tweening;
using UnityEngine;

namespace BackpackHero.Debugging
{
    public enum BackpackPlacementScaleEase
    {
        OutElastic,
        OutCubic
    }

    /// <summary>背包交互动效的全局运行时快照。</summary>
    public readonly struct BackpackVisualSettings :
        IEquatable<BackpackVisualSettings>
    {
        public const float MinimumDragOpacity = 0f;
        public const float MaximumDragOpacity = 1f;
        public const float MinimumFlashDuration = 0.01f;
        public const float MinimumPlacementScale = 1f;
        public const float MinimumShopFlightDuration = 0.01f;
        public const float MinimumLevelFontSize = 0.01f;

        public static BackpackVisualSettings Default => new(
            true, .5f,
            new Color(.95f, .78f, .18f, 1f),
            new Color(.86f, .22f, .18f, 1f),
            .18f, .62f, .7f,
            1.17f, 10f, -6f, BackpackPlacementScaleEase.OutElastic,
            .32f, Color.white, 16f);

        public BackpackVisualSettings(
            bool overridesEnabled,
            float dragOpacity,
            Color legalPreviewColor,
            Color illegalPreviewColor,
            float mergeFlashMinimum,
            float mergeFlashMaximum,
            float mergeFlashCycleDuration,
            float placementScaleMultiplier,
            float placementPositiveRotationDegrees,
            float placementNegativeRotationDegrees,
            BackpackPlacementScaleEase placementScaleEase,
            float shopFlightDuration,
            Color levelFontColor,
            float levelFontSize)
        {
            OverridesEnabled = overridesEnabled;
            DragOpacity = Mathf.Clamp(dragOpacity, MinimumDragOpacity,
                MaximumDragOpacity);
            LegalPreviewColor = legalPreviewColor;
            IllegalPreviewColor = illegalPreviewColor;
            MergeFlashMinimum = Mathf.Clamp01(mergeFlashMinimum);
            MergeFlashMaximum = Mathf.Clamp01(mergeFlashMaximum);
            MergeFlashCycleDuration = Mathf.Max(MinimumFlashDuration,
                mergeFlashCycleDuration);
            PlacementScaleMultiplier = Mathf.Max(MinimumPlacementScale,
                placementScaleMultiplier);
            PlacementPositiveRotationDegrees = placementPositiveRotationDegrees;
            PlacementNegativeRotationDegrees = placementNegativeRotationDegrees;
            PlacementScaleEase = placementScaleEase;
            ShopFlightDuration = Mathf.Max(MinimumShopFlightDuration,
                shopFlightDuration);
            LevelFontColor = levelFontColor;
            LevelFontSize = Mathf.Max(MinimumLevelFontSize, levelFontSize);
        }

        public bool OverridesEnabled { get; }
        public float DragOpacity { get; }
        public Color LegalPreviewColor { get; }
        public Color IllegalPreviewColor { get; }
        public float MergeFlashMinimum { get; }
        public float MergeFlashMaximum { get; }
        public float MergeFlashCycleDuration { get; }
        public float PlacementScaleMultiplier { get; }
        public float PlacementPositiveRotationDegrees { get; }
        public float PlacementNegativeRotationDegrees { get; }
        public BackpackPlacementScaleEase PlacementScaleEase { get; }
        public float ShopFlightDuration { get; }
        public Color LevelFontColor { get; }
        public float LevelFontSize { get; }
        public bool UsesFactionLevelColor => !OverridesEnabled ||
            IsWhiteRgb(LevelFontColor);

        public Ease GetPlacementScaleDotweenEase() =>
            PlacementScaleEase == BackpackPlacementScaleEase.OutCubic
                ? Ease.OutCubic
                : Ease.OutElastic;

        public BackpackVisualSettings WithOverridesEnabled(bool value) => new(
            value, DragOpacity, LegalPreviewColor, IllegalPreviewColor,
            MergeFlashMinimum, MergeFlashMaximum, MergeFlashCycleDuration,
            PlacementScaleMultiplier, PlacementPositiveRotationDegrees,
            PlacementNegativeRotationDegrees, PlacementScaleEase,
            ShopFlightDuration, LevelFontColor, LevelFontSize);

        private static bool IsWhiteRgb(Color color) =>
            Mathf.Approximately(color.r, 1f) &&
            Mathf.Approximately(color.g, 1f) &&
            Mathf.Approximately(color.b, 1f);

        public bool Equals(BackpackVisualSettings other) =>
            OverridesEnabled == other.OverridesEnabled &&
            Mathf.Approximately(DragOpacity, other.DragOpacity) &&
            LegalPreviewColor.Equals(other.LegalPreviewColor) &&
            IllegalPreviewColor.Equals(other.IllegalPreviewColor) &&
            Mathf.Approximately(MergeFlashMinimum, other.MergeFlashMinimum) &&
            Mathf.Approximately(MergeFlashMaximum, other.MergeFlashMaximum) &&
            Mathf.Approximately(MergeFlashCycleDuration,
                other.MergeFlashCycleDuration) &&
            Mathf.Approximately(PlacementScaleMultiplier,
                other.PlacementScaleMultiplier) &&
            Mathf.Approximately(PlacementPositiveRotationDegrees,
                other.PlacementPositiveRotationDegrees) &&
            Mathf.Approximately(PlacementNegativeRotationDegrees,
                other.PlacementNegativeRotationDegrees) &&
            PlacementScaleEase == other.PlacementScaleEase &&
            Mathf.Approximately(ShopFlightDuration, other.ShopFlightDuration) &&
            LevelFontColor.Equals(other.LevelFontColor) &&
            Mathf.Approximately(LevelFontSize, other.LevelFontSize);

        public override bool Equals(object obj) =>
            obj is BackpackVisualSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = OverridesEnabled ? 1 : 0;
                hash = (hash * 31) + DragOpacity.GetHashCode();
                hash = (hash * 31) + LegalPreviewColor.GetHashCode();
                hash = (hash * 31) + IllegalPreviewColor.GetHashCode();
                hash = (hash * 31) + MergeFlashMinimum.GetHashCode();
                hash = (hash * 31) + MergeFlashMaximum.GetHashCode();
                hash = (hash * 31) + MergeFlashCycleDuration.GetHashCode();
                hash = (hash * 31) + PlacementScaleMultiplier.GetHashCode();
                hash = (hash * 31) + PlacementPositiveRotationDegrees.GetHashCode();
                hash = (hash * 31) + PlacementNegativeRotationDegrees.GetHashCode();
                hash = (hash * 31) + (int)PlacementScaleEase;
                hash = (hash * 31) + ShopFlightDuration.GetHashCode();
                hash = (hash * 31) + LevelFontColor.GetHashCode();
                return (hash * 31) + LevelFontSize.GetHashCode();
            }
        }
    }
}
