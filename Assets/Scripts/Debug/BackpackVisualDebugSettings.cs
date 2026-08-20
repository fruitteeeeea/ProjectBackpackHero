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
        }

        public BackpackVisualSettings GetValues() => new(
            overridesEnabled, dragOpacity, legalPreviewColor, illegalPreviewColor,
            mergeFlashMinimum, mergeFlashMaximum, mergeFlashCycleDuration,
            placementScaleMultiplier, placementPositiveRotationDegrees,
            placementNegativeRotationDegrees, placementScaleEase,
            shopFlightDuration, levelFontColor, levelFontSize);

#if UNITY_EDITOR
        private void OnValidate() => SetValues(GetValues());
#endif
    }
}
