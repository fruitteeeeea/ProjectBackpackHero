using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 所有飞行子弹共用的阵营颜色和拖尾外观配置。
    /// </summary>
    [CreateAssetMenu(
        fileName = "ProjectileVisualSettings",
        menuName = "Backpack Hero/Battle/Projectile Visual Settings")]
    public sealed class ProjectileVisualSettings : ScriptableObject
    {
        [Header("Faction Colors")]
        [SerializeField] private Color playerFactionColor =
            new(0.55f, 0.85f, 1f, 1f);
        [SerializeField] private Color enemyFactionColor =
            new(1f, 0.55f, 0.55f, 1f);

        [Header("Trail Colors")]
        [SerializeField] private Color equipmentTipColor =
            new(0.8666667f, 0.7882353f, 0.23137255f, 1f);
        [SerializeField, Range(0f, 1f)] private float trailOpacity =
            0.49019608f;

        [Header("Trail Timing")]
        [SerializeField, Min(0f)] private float fighterTrailDuration =
            0.07f;
        [SerializeField, Min(0f)] private float equipmentTrailDuration =
            0.14f;
        [SerializeField, Range(0f, 1f)]
        private float equipmentTipTransition = 0.35f;

        [Header("Trail Shape")]
        [SerializeField] private AnimationCurve trailWidthCurve =
            new(
                new Keyframe(0f, 0.20588803f),
                new Keyframe(0.9967766f, 0.044116974f));

        public Color GetFactionColor(BattleFaction faction)
        {
            return faction == BattleFaction.Player
                ? playerFactionColor
                : enemyFactionColor;
        }

        public float GetTrailDuration(ProjectileVisualSource source)
        {
            return source == ProjectileVisualSource.Equipment
                ? equipmentTrailDuration
                : fighterTrailDuration;
        }

        public AnimationCurve TrailWidthCurve => trailWidthCurve;

        public Gradient CreateTrailGradient(
            BattleFaction faction,
            ProjectileVisualSource source)
        {
            Color factionColor = GetFactionColor(faction);
            Gradient gradient = new();

            if (source == ProjectileVisualSource.Equipment)
            {
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(equipmentTipColor, 0f),
                        new GradientColorKey(
                            factionColor,
                            equipmentTipTransition),
                        new GradientColorKey(factionColor, 1f),
                    },
                    CreateAlphaKeys());
            }
            else
            {
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(factionColor, 0f),
                        new GradientColorKey(factionColor, 1f),
                    },
                    CreateAlphaKeys());
            }

            return gradient;
        }

        private GradientAlphaKey[] CreateAlphaKeys()
        {
            return new[]
            {
                new GradientAlphaKey(trailOpacity, 0f),
                new GradientAlphaKey(0f, 1f),
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            trailOpacity = Mathf.Clamp01(trailOpacity);
            fighterTrailDuration = Mathf.Max(0f, fighterTrailDuration);
            equipmentTrailDuration = Mathf.Max(0f, equipmentTrailDuration);
            equipmentTipTransition = Mathf.Clamp01(equipmentTipTransition);
        }
#endif
    }
}
