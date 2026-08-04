using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 集中保存飞机伤害飘字的 TMP 样式与 FEEL 运动参数。
    /// </summary>
    [CreateAssetMenu(
        fileName = "FloatingDamageTextSettings",
        menuName = "Backpack Hero/Battle/Floating Damage Text Settings")]
    public sealed class FloatingDamageTextSettings : ScriptableObject
    {
        [Header("Floating Text Prefabs")]
        [SerializeField] private MMFloatingText playerFloatingTextPrefab;
        [SerializeField] private MMFloatingText enemyFloatingTextPrefab;

        [Header("Pool")]
        [SerializeField, Min(1)] private int poolSize = 30;
        [SerializeField] private bool poolCanExpand = true;
        [SerializeField] private Vector2 lifetime = new(1f, 1.08f);

        [Header("Spawn Randomness")]
        [SerializeField] private Vector3 spawnOffsetMin = new(-0.12f, -0.05f, 0f);
        [SerializeField] private Vector3 spawnOffsetMax = new(0.12f, 0.08f, 0f);
        [SerializeField] private Vector2 horizontalEndOffset = new(-0.35f, 0.35f);

        [Header("Parabolic Motion")]
        [SerializeField] private Vector2 verticalPeakHeight = new(1.25f, 1.4f);
        [SerializeField] private AnimationCurve verticalMotion = new(
            new Keyframe(0f, 0f),
            new Keyframe(0.28f, 1f),
            new Keyframe(1f, -0.08f));

        [Header("Scale")]
        [SerializeField] private Vector2 scaleAtStart = new(0.75f, 0.75f);
        [SerializeField] private Vector2 scaleAtPeak = new(1.1f, 1.1f);
        [SerializeField] private AnimationCurve scaleMotion = new(
            new Keyframe(0f, 0.78f),
            new Keyframe(0.14f, 1f),
            new Keyframe(1f, 0.85f));

        [Header("Fade")]
        [SerializeField] private AnimationCurve opacityMotion = new(
            new Keyframe(0f, 1f),
            new Keyframe(0.7f, 1f),
            new Keyframe(1f, 0f));

        [Header("TMP Style")]
        [SerializeField] private TMP_FontAsset floatingTextFont;
        [SerializeField, Min(0.1f)] private float fontSize = 2.2f;
        [SerializeField, Range(0f, 1f)] private float outlineWidth = 0.18f;
        [SerializeField] private int sortingOrder = 200;
        [SerializeField] private Color playerFaceColor = new(0.28f, 0.78f, 1f, 1f);
        [SerializeField] private Color playerOutlineColor = new(0.015f, 0.04f, 0.09f, 0.9f);
        [SerializeField] private Color enemyFaceColor = new(1f, 0.34f, 0.12f, 1f);
        [SerializeField] private Color enemyOutlineColor = new(0.12f, 0.015f, 0.005f, 0.9f);

        public MMFloatingText GetPrefab(BattleFaction faction) =>
            faction == BattleFaction.Player
                ? playerFloatingTextPrefab
                : enemyFloatingTextPrefab;

        public float MaximumLifetime => lifetime.y;

        public float EvaluateOpacity(float normalizedLifetime) =>
            opacityMotion.Evaluate(
                Mathf.Clamp01(normalizedLifetime));

        public void ConfigureSpawner(
            MMFloatingTextSpawner spawner,
            BattleFaction faction)
        {
            spawner.PooledSimpleMMFloatingText = GetPrefab(faction);
            spawner.PoolSize = poolSize;
            spawner.PoolCanExpand = poolCanExpand;
            spawner.Lifetime = lifetime;
            spawner.SpawnOffsetMin = spawnOffsetMin;
            spawner.SpawnOffsetMax = spawnOffsetMax;
            spawner.AnimateX = true;
            spawner.RemapXOne = horizontalEndOffset;
            spawner.AnimateY = true;
            spawner.RemapYOne = verticalPeakHeight;
            spawner.AnimateYCurve = verticalMotion;
            spawner.AnimateScale = true;
            spawner.RemapScaleZero = scaleAtStart;
            spawner.RemapScaleOne = scaleAtPeak;
            spawner.AnimateScaleCurve = scaleMotion;
            spawner.AnimateOpacity = true;
            spawner.RemapOpacityZero = Vector2.one;
            spawner.RemapOpacityOne = Vector2.one;
            spawner.AnimateOpacityCurve = opacityMotion;
        }

        public void ApplyTextStyle(
            TMP_Text text,
            BattleFaction faction)
        {
            if (text == null)
            {
                return;
            }

            bool isPlayer = faction == BattleFaction.Player;
            if (floatingTextFont != null)
            {
                text.font = floatingTextFont;
            }

            text.color = isPlayer ? playerFaceColor : enemyFaceColor;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.outlineColor = isPlayer
                ? playerOutlineColor
                : enemyOutlineColor;
            text.outlineWidth = outlineWidth;

            if (text is TextMeshPro worldText)
            {
                worldText.sortingOrder = sortingOrder;
            }
        }
    }
}
