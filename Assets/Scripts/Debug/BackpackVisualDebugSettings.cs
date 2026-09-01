using TMPro;
using UnityEngine;

namespace BackpackHero.Debugging
{
    [CreateAssetMenu(fileName = "BackpackVisualDebugSettings",
        menuName = "Debug/Backpack Visual Settings")]
    public sealed class BackpackVisualDebugSettings : ScriptableObject
    {
        [SerializeField] private bool overridesEnabled = true;
        [SerializeField, Range(0f, 1f)] private float dragOpacity = .5f;
        [SerializeField] private Color legalPreviewColor = new(.95f, .78f, .18f, 1f);
        [SerializeField] private Color illegalPreviewColor = new(.86f, .22f, .18f, 1f);
        [SerializeField, Range(0f, 1f)] private float mergeFlashMinimum = .18f;
        [SerializeField, Range(0f, 1f)] private float mergeFlashMaximum = .62f;
        [SerializeField, Min(BackpackVisualSettings.MinimumFlashDuration)] private float mergeFlashCycleDuration = .7f;
        [SerializeField, Min(BackpackVisualSettings.MinimumPlacementScale)] private float placementScaleMultiplier = 1.17f;
        [SerializeField] private float placementPositiveRotationDegrees = 10f;
        [SerializeField] private float placementNegativeRotationDegrees = -6f;
        [SerializeField] private BackpackPlacementScaleEase placementScaleEase = BackpackPlacementScaleEase.OutElastic;
        [SerializeField, Min(BackpackVisualSettings.MinimumShopFlightDuration)] private float shopFlightDuration = .32f;
        [SerializeField] private Color levelFontColor = Color.white;
        [SerializeField, Min(BackpackVisualSettings.MinimumLevelFontSize)] private float levelFontSize = 16f;
        [SerializeField, Range(BackpackVisualSettings.MinimumItemIconScale,
            BackpackVisualSettings.MaximumItemIconScale)]
        private float itemIconScale = BackpackVisualSettings.DefaultItemIconScale;
        [SerializeField] private bool aircraftGlowEnabled = true;
        [SerializeField] private Color aircraftGlowColor = Color.white;
        [SerializeField, Range(0f, 1f)] private float aircraftGlowMinimumIntensity = .12f;
        [SerializeField, Range(0f, 1f)] private float aircraftGlowMaximumIntensity = .45f;
        [SerializeField, Min(BackpackVisualSettings.MinimumAircraftGlowCycleDuration)] private float aircraftGlowCycleDuration = 1.2f;
        [SerializeField, Min(BackpackVisualSettings.MinimumAircraftGlowEdgeWidth)] private float aircraftGlowEdgeWidth = 8f;
        [SerializeField] private bool equipmentBottomPlateGlowEnabled;
        [SerializeField] private bool colorQualityModeEnabled = true;
        [SerializeField] private ItemQualityPalette itemQualityPalette;
        [SerializeField] private TMP_FontAsset qualityLevelBadgeFont;
        [SerializeField, Min(BackpackVisualSettings.MinimumQualityLevelBadgeFontSize)] private float qualityLevelBadgeFontSize = 19f;
        [SerializeField, Range(BackpackVisualSettings.MinimumQualityLevelBadgeOutlineWidth, BackpackVisualSettings.MaximumQualityLevelBadgeOutlineWidth)] private float qualityLevelBadgeOutlineWidth = .36f;
        [SerializeField] private bool aircraftQualityPulseEnabled = true;
        [SerializeField, Min(BackpackVisualSettings.MinimumAircraftQualityPulseInterval)] private float aircraftQualityPulseInterval = .8f;
        [SerializeField, Min(BackpackVisualSettings.MinimumAircraftQualityPulseScale)] private float aircraftQualityPulseScaleMultiplier = 1.12f;
        [SerializeField, Min(BackpackVisualSettings.MinimumAircraftQualityPulseTweenDuration)] private float aircraftQualityPulseTweenDuration = .45f;

        public void SetValues(BackpackVisualSettings values)
        {
            overridesEnabled = values.OverridesEnabled;
            dragOpacity = values.DragOpacity;
            legalPreviewColor = values.LegalPreviewColor;
            illegalPreviewColor = values.IllegalPreviewColor;
            mergeFlashMinimum = values.MergeFlashMinimum;
            mergeFlashMaximum = values.MergeFlashMaximum;
            mergeFlashCycleDuration = values.MergeFlashCycleDuration;
            placementScaleMultiplier = values.PlacementScaleMultiplier;
            placementPositiveRotationDegrees = values.PlacementPositiveRotationDegrees;
            placementNegativeRotationDegrees = values.PlacementNegativeRotationDegrees;
            placementScaleEase = values.PlacementScaleEase;
            shopFlightDuration = values.ShopFlightDuration;
            levelFontColor = values.LevelFontColor;
            levelFontSize = values.LevelFontSize;
            itemIconScale = values.ItemIconScale;
            aircraftGlowEnabled = values.AircraftGlowEnabled;
            aircraftGlowColor = values.AircraftGlowColor;
            aircraftGlowMinimumIntensity = values.AircraftGlowMinimumIntensity;
            aircraftGlowMaximumIntensity = values.AircraftGlowMaximumIntensity;
            aircraftGlowCycleDuration = values.AircraftGlowCycleDuration;
            aircraftGlowEdgeWidth = values.AircraftGlowEdgeWidth;
            equipmentBottomPlateGlowEnabled = values.EquipmentBottomPlateGlowEnabled;
            colorQualityModeEnabled = values.ColorQualityModeEnabled;
            itemQualityPalette = values.ItemQualityPalette;
            qualityLevelBadgeFont = values.QualityLevelBadgeFont;
            qualityLevelBadgeFontSize = values.QualityLevelBadgeFontSize;
            qualityLevelBadgeOutlineWidth = values.QualityLevelBadgeOutlineWidth;
            aircraftQualityPulseEnabled = values.AircraftQualityPulseEnabled;
            aircraftQualityPulseInterval = values.AircraftQualityPulseInterval;
            aircraftQualityPulseScaleMultiplier =
                values.AircraftQualityPulseScaleMultiplier;
            aircraftQualityPulseTweenDuration =
                values.AircraftQualityPulseTweenDuration;
        }

        public BackpackVisualSettings GetValues() => new(
            overridesEnabled, dragOpacity, legalPreviewColor, illegalPreviewColor,
            mergeFlashMinimum, mergeFlashMaximum, mergeFlashCycleDuration,
            placementScaleMultiplier, placementPositiveRotationDegrees,
            placementNegativeRotationDegrees, placementScaleEase,
            shopFlightDuration, levelFontColor, levelFontSize,
            aircraftGlowEnabled, aircraftGlowColor,
            aircraftGlowMinimumIntensity, aircraftGlowMaximumIntensity,
            aircraftGlowCycleDuration, aircraftGlowEdgeWidth,
            equipmentBottomPlateGlowEnabled, colorQualityModeEnabled,
            itemQualityPalette, qualityLevelBadgeFont,
            qualityLevelBadgeFontSize, qualityLevelBadgeOutlineWidth,
            aircraftQualityPulseEnabled,
            aircraftQualityPulseInterval,
            aircraftQualityPulseScaleMultiplier,
            aircraftQualityPulseTweenDuration, itemIconScale);

#if UNITY_EDITOR
        private void OnValidate() => SetValues(GetValues());
#endif
    }
}
