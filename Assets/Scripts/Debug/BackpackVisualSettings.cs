using System;
using DG.Tweening;
using TMPro;
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
        public const float DefaultItemIconScale = .75f;
        public const float MinimumItemIconScale = .1f;
        public const float MaximumItemIconScale = 2f;
        public const float MinimumQualityLevelBadgeFontSize = 0.01f;
        public const float MinimumQualityLevelBadgeOutlineWidth = 0f;
        public const float MaximumQualityLevelBadgeOutlineWidth = 1f;
        public const float MinimumAircraftGlowCycleDuration = 0.01f;
        public const float MinimumAircraftGlowEdgeWidth = 0f;
        public const float MinimumAircraftQualityPulseInterval = 0f;
        public const float MinimumAircraftQualityPulseScale = 1f;
        public const float MinimumAircraftQualityPulseTweenDuration = 0.01f;
        public const float MinimumBackpackHealthBarVerticalOffset = 0f;
        public const float DefaultBackpackHealthBarVerticalOffset = 1.15f;

        public static BackpackVisualSettings Default => new(
            true, .5f,
            new Color(.95f, .78f, .18f, 1f),
            new Color(.86f, .22f, .18f, 1f),
            .18f, .62f, .7f,
            1.17f, 10f, -6f, BackpackPlacementScaleEase.OutElastic,
            .32f, Color.white, 16f,
            true, Color.white, .12f, .45f, 1.2f, 8f, false, true, null,
            null, 19f, .36f, true,
            .8f, 1.12f, .45f, DefaultItemIconScale,
            DefaultBackpackHealthBarVerticalOffset);

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
            float levelFontSize,
            bool aircraftGlowEnabled,
            Color aircraftGlowColor,
            float aircraftGlowMinimumIntensity,
            float aircraftGlowMaximumIntensity,
            float aircraftGlowCycleDuration,
            float aircraftGlowEdgeWidth,
            bool equipmentBottomPlateGlowEnabled,
            bool colorQualityModeEnabled,
            ItemQualityPalette itemQualityPalette,
            TMP_FontAsset qualityLevelBadgeFont,
            float qualityLevelBadgeFontSize,
            float qualityLevelBadgeOutlineWidth,
            bool aircraftQualityPulseEnabled,
            float aircraftQualityPulseInterval,
            float aircraftQualityPulseScaleMultiplier,
            float aircraftQualityPulseTweenDuration,
            float itemIconScale = DefaultItemIconScale,
            float backpackHealthBarVerticalOffset =
                DefaultBackpackHealthBarVerticalOffset)
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
            AircraftGlowEnabled = aircraftGlowEnabled;
            AircraftGlowColor = aircraftGlowColor;
            AircraftGlowMinimumIntensity = Mathf.Clamp01(
                aircraftGlowMinimumIntensity);
            AircraftGlowMaximumIntensity = Mathf.Clamp01(
                aircraftGlowMaximumIntensity);
            AircraftGlowCycleDuration = Mathf.Max(
                MinimumAircraftGlowCycleDuration, aircraftGlowCycleDuration);
            AircraftGlowEdgeWidth = Mathf.Max(MinimumAircraftGlowEdgeWidth,
                aircraftGlowEdgeWidth);
            EquipmentBottomPlateGlowEnabled = equipmentBottomPlateGlowEnabled;
            ColorQualityModeEnabled = colorQualityModeEnabled;
            ItemQualityPalette = itemQualityPalette;
            QualityLevelBadgeFont = qualityLevelBadgeFont;
            QualityLevelBadgeFontSize = Mathf.Max(
                MinimumQualityLevelBadgeFontSize, qualityLevelBadgeFontSize);
            QualityLevelBadgeOutlineWidth = Mathf.Clamp(
                qualityLevelBadgeOutlineWidth, MinimumQualityLevelBadgeOutlineWidth,
                MaximumQualityLevelBadgeOutlineWidth);
            AircraftQualityPulseEnabled = aircraftQualityPulseEnabled;
            AircraftQualityPulseInterval = Mathf.Max(
                MinimumAircraftQualityPulseInterval,
                aircraftQualityPulseInterval);
            AircraftQualityPulseScaleMultiplier = Mathf.Max(
                MinimumAircraftQualityPulseScale,
                aircraftQualityPulseScaleMultiplier);
            AircraftQualityPulseTweenDuration = Mathf.Max(
                MinimumAircraftQualityPulseTweenDuration,
                aircraftQualityPulseTweenDuration);
            ItemIconScale = Mathf.Clamp(itemIconScale, MinimumItemIconScale,
                MaximumItemIconScale);
            BackpackHealthBarVerticalOffset = Mathf.Max(
                MinimumBackpackHealthBarVerticalOffset,
                backpackHealthBarVerticalOffset);
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
        public float ItemIconScale { get; }
        public bool AircraftGlowEnabled { get; }
        public Color AircraftGlowColor { get; }
        public float AircraftGlowMinimumIntensity { get; }
        public float AircraftGlowMaximumIntensity { get; }
        public float AircraftGlowCycleDuration { get; }
        public float AircraftGlowEdgeWidth { get; }
        public bool EquipmentBottomPlateGlowEnabled { get; }
        public bool ColorQualityModeEnabled { get; }
        public ItemQualityPalette ItemQualityPalette { get; }
        public TMP_FontAsset QualityLevelBadgeFont { get; }
        public float QualityLevelBadgeFontSize { get; }
        public float QualityLevelBadgeOutlineWidth { get; }
        public bool AircraftQualityPulseEnabled { get; }
        public float AircraftQualityPulseInterval { get; }
        public float AircraftQualityPulseScaleMultiplier { get; }
        public float AircraftQualityPulseTweenDuration { get; }
        public float BackpackHealthBarVerticalOffset { get; }
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
            ShopFlightDuration, LevelFontColor, LevelFontSize,
            AircraftGlowEnabled, AircraftGlowColor,
            AircraftGlowMinimumIntensity, AircraftGlowMaximumIntensity,
            AircraftGlowCycleDuration, AircraftGlowEdgeWidth,
            EquipmentBottomPlateGlowEnabled, ColorQualityModeEnabled,
            ItemQualityPalette, QualityLevelBadgeFont,
            QualityLevelBadgeFontSize, QualityLevelBadgeOutlineWidth,
            AircraftQualityPulseEnabled,
            AircraftQualityPulseInterval,
            AircraftQualityPulseScaleMultiplier,
            AircraftQualityPulseTweenDuration, ItemIconScale,
            BackpackHealthBarVerticalOffset);

        public BackpackVisualSettings WithBackpackHealthBarVerticalOffset(
            float value) => new(
            OverridesEnabled, DragOpacity, LegalPreviewColor, IllegalPreviewColor,
            MergeFlashMinimum, MergeFlashMaximum, MergeFlashCycleDuration,
            PlacementScaleMultiplier, PlacementPositiveRotationDegrees,
            PlacementNegativeRotationDegrees, PlacementScaleEase,
            ShopFlightDuration, LevelFontColor, LevelFontSize,
            AircraftGlowEnabled, AircraftGlowColor,
            AircraftGlowMinimumIntensity, AircraftGlowMaximumIntensity,
            AircraftGlowCycleDuration, AircraftGlowEdgeWidth,
            EquipmentBottomPlateGlowEnabled, ColorQualityModeEnabled,
            ItemQualityPalette, QualityLevelBadgeFont,
            QualityLevelBadgeFontSize, QualityLevelBadgeOutlineWidth,
            AircraftQualityPulseEnabled,
            AircraftQualityPulseInterval,
            AircraftQualityPulseScaleMultiplier,
            AircraftQualityPulseTweenDuration, ItemIconScale, value);

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
            Mathf.Approximately(LevelFontSize, other.LevelFontSize) &&
            Mathf.Approximately(ItemIconScale, other.ItemIconScale) &&
            AircraftGlowEnabled == other.AircraftGlowEnabled &&
            AircraftGlowColor.Equals(other.AircraftGlowColor) &&
            Mathf.Approximately(AircraftGlowMinimumIntensity,
                other.AircraftGlowMinimumIntensity) &&
            Mathf.Approximately(AircraftGlowMaximumIntensity,
                other.AircraftGlowMaximumIntensity) &&
            Mathf.Approximately(AircraftGlowCycleDuration,
                other.AircraftGlowCycleDuration) &&
            Mathf.Approximately(AircraftGlowEdgeWidth,
                other.AircraftGlowEdgeWidth) &&
            EquipmentBottomPlateGlowEnabled ==
                other.EquipmentBottomPlateGlowEnabled &&
            ColorQualityModeEnabled == other.ColorQualityModeEnabled &&
            ItemQualityPalette == other.ItemQualityPalette &&
            QualityLevelBadgeFont == other.QualityLevelBadgeFont &&
            Mathf.Approximately(QualityLevelBadgeFontSize,
                other.QualityLevelBadgeFontSize) &&
            Mathf.Approximately(QualityLevelBadgeOutlineWidth,
                other.QualityLevelBadgeOutlineWidth) &&
            AircraftQualityPulseEnabled == other.AircraftQualityPulseEnabled &&
            Mathf.Approximately(AircraftQualityPulseInterval,
                other.AircraftQualityPulseInterval) &&
            Mathf.Approximately(AircraftQualityPulseScaleMultiplier,
                other.AircraftQualityPulseScaleMultiplier) &&
            Mathf.Approximately(AircraftQualityPulseTweenDuration,
                other.AircraftQualityPulseTweenDuration) &&
            Mathf.Approximately(BackpackHealthBarVerticalOffset,
                other.BackpackHealthBarVerticalOffset);

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
                hash = (hash * 31) + LevelFontSize.GetHashCode();
                hash = (hash * 31) + ItemIconScale.GetHashCode();
                hash = (hash * 31) + (AircraftGlowEnabled ? 1 : 0);
                hash = (hash * 31) + AircraftGlowColor.GetHashCode();
                hash = (hash * 31) + AircraftGlowMinimumIntensity.GetHashCode();
                hash = (hash * 31) + AircraftGlowMaximumIntensity.GetHashCode();
                hash = (hash * 31) + AircraftGlowCycleDuration.GetHashCode();
                hash = (hash * 31) + AircraftGlowEdgeWidth.GetHashCode();
                hash = (hash * 31) +
                    (EquipmentBottomPlateGlowEnabled ? 1 : 0);
                hash = (hash * 31) + (ColorQualityModeEnabled ? 1 : 0);
                hash = (hash * 31) +
                    (ItemQualityPalette != null ? ItemQualityPalette.GetHashCode() : 0);
                hash = (hash * 31) + (QualityLevelBadgeFont != null
                    ? QualityLevelBadgeFont.GetHashCode() : 0);
                hash = (hash * 31) + QualityLevelBadgeFontSize.GetHashCode();
                hash = (hash * 31) + QualityLevelBadgeOutlineWidth.GetHashCode();
                hash = (hash * 31) + (AircraftQualityPulseEnabled ? 1 : 0);
                hash = (hash * 31) + AircraftQualityPulseInterval.GetHashCode();
                hash = (hash * 31) +
                    AircraftQualityPulseScaleMultiplier.GetHashCode();
                hash = (hash * 31) +
                    AircraftQualityPulseTweenDuration.GetHashCode();
                return (hash * 31) +
                    BackpackHealthBarVerticalOffset.GetHashCode();
            }
        }
    }
}
