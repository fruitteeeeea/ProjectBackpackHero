using DG.Tweening;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 在发射点与Pattern方向上瞬间生成一束激光。
    /// 激光长度由发射点到AimPoint的距离决定。
    /// </summary>
    [RequireComponent(typeof(AreaDamageResolver2D))]
    [RequireComponent(
        typeof(SegmentBoxDamageArea2D))]
    public sealed class LaserBeamAttack2D :
        BattleAttack2D
    {
        private const float MinimumBeamLength =
            0.001f;

        [Header("References")]
        [SerializeField]
        private SpriteRenderer beamRenderer;

        [SerializeField]
        private AreaDamageResolver2D
            damageResolver;

        [SerializeField]
        private SegmentBoxDamageArea2D
            damageArea;

        [Header("Beam Shape")]
        [SerializeField, Min(0.001f)]
        private float visualWidth = 0.2f;

        [SerializeField, Min(0.001f)]
        private float damageWidth = 0.16f;

        [SerializeField, Min(0f)]
        private float startPadding;

        [SerializeField, Min(0f)]
        private float endPadding;

        [Header("Appearance")]
        [SerializeField]
        private Color tint = Color.white;

        [SerializeField, Range(0f, 1f)]
        private float peakAlpha = 1f;

        [SerializeField]
        private string sortingLayerName =
            "Default";

        [SerializeField]
        private int sortingOrder = 30;

        [Header("Timing")]
        [SerializeField, Min(0f)]
        private float fadeInDuration = 0.1f;

        [SerializeField, Min(0f)]
        private float holdDuration = 0.2f;

        [SerializeField, Min(0f)]
        private float fadeOutDuration = 0.3f;

        [SerializeField]
        private Ease fadeInEase = Ease.Linear;

        [SerializeField]
        private Ease fadeOutEase = Ease.Linear;

        private Sequence sequence;
        private BattleAttackLaunchContext launchContext;
        private bool damageApplied;
        private bool hasValidBeam;
        private Vector2 beamStart;
        private Vector2 beamEnd;

        public Vector2 BeamStart => beamStart;
        public Vector2 BeamEnd => beamEnd;
        public float BeamLength =>
            Vector2.Distance(
                beamStart,
                beamEnd);
        public bool HasValidBeam => hasValidBeam;
        public bool DamageApplied => damageApplied;

        private void Awake()
        {
            FindReferences();
        }

        private void OnDisable()
        {
            sequence?.Kill(false);
            sequence = null;
        }

        public override void Initialize(
            BattleAttackLaunchContext context)
        {
            FindReferences();
            launchContext = context;
            damageApplied = false;
            hasValidBeam = false;

            if (beamRenderer == null ||
                damageResolver == null ||
                damageArea == null)
            {
                Debug.LogError(
                    $"{name}无法初始化：激光Prefab引用不完整。",
                    this);

                Destroy(gameObject);
                return;
            }

            if (!context.HasAimPoint)
            {
                Debug.LogWarning(
                    $"{name}没有AimPoint，无法生成激光。",
                    this);

                Destroy(gameObject);
                return;
            }

            float length =
                Vector2.Distance(
                    context.Origin,
                    context.AimPoint);

            if (length <= MinimumBeamLength)
            {
                Debug.LogWarning(
                    $"{name}的激光起点与瞄准点重合。",
                    this);

                Destroy(gameObject);
                return;
            }

            Vector2 direction =
                context.FireDirection.sqrMagnitude >
                Mathf.Epsilon
                    ? context
                        .FireDirection
                        .normalized
                    : Vector2.up;

            beamStart = context.Origin;
            beamEnd =
                beamStart +
                direction * length;

            damageArea.Configure(
                beamStart,
                beamEnd,
                damageWidth,
                startPadding,
                endPadding);

            LayoutVisual(
                direction,
                length);

            hasValidBeam = true;
            gameObject.name =
                $"{context.Faction} Laser Beam";

            PlaySequence();
        }

        private void LayoutVisual(
            Vector2 direction,
            float length)
        {
            transform.position =
                (beamStart + beamEnd) * 0.5f;

            transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(
                        direction.y,
                        direction.x) *
                    Mathf.Rad2Deg);

            Transform visualTransform =
                beamRenderer.transform;

            visualTransform.localPosition =
                Vector3.zero;
            visualTransform.localRotation =
                Quaternion.identity;

            Vector2 spriteSize =
                beamRenderer.sprite != null
                    ? beamRenderer
                        .sprite
                        .bounds
                        .size
                    : Vector2.one;

            visualTransform.localScale =
                new Vector3(
                    length /
                    Mathf.Max(
                        0.001f,
                        spriteSize.x),
                    visualWidth /
                    Mathf.Max(
                        0.001f,
                        spriteSize.y),
                    1f);

            beamRenderer.sortingLayerName =
                sortingLayerName;
            beamRenderer.sortingOrder =
                sortingOrder;

            Color initialColor = tint;
            initialColor.a = 0f;
            beamRenderer.color = initialColor;
        }

        private void PlaySequence()
        {
            sequence?.Kill(false);

            sequence = DOTween.Sequence()
                .SetTarget(this)
                .Append(
                    beamRenderer
                        .DOFade(
                            peakAlpha,
                            fadeInDuration)
                        .SetEase(fadeInEase))
                .AppendCallback(ApplyDamageOnce)
                .AppendInterval(holdDuration)
                .Append(
                    beamRenderer
                        .DOFade(
                            0f,
                            fadeOutDuration)
                        .SetEase(fadeOutEase))
                .OnComplete(
                    () =>
                    {
                        sequence = null;
                        Destroy(gameObject);
                    });
        }

        private void ApplyDamageOnce()
        {
            if (damageApplied ||
                !hasValidBeam)
            {
                return;
            }

            damageApplied = true;

            damageResolver.Resolve(
                damageArea,
                launchContext.Faction,
                launchContext.Damage);
        }

        private void FindReferences()
        {
            if (beamRenderer == null)
            {
                beamRenderer =
                    GetComponentInChildren<
                        SpriteRenderer>(true);
            }

            if (damageResolver == null)
            {
                damageResolver =
                    GetComponent<
                        AreaDamageResolver2D>();
            }

            if (damageArea == null)
            {
                damageArea =
                    GetComponent<
                        SegmentBoxDamageArea2D>();
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            FindReferences();
        }

        private void OnValidate()
        {
            visualWidth =
                Mathf.Max(
                    0.001f,
                    visualWidth);
            damageWidth =
                Mathf.Max(
                    0.001f,
                    damageWidth);
            startPadding =
                Mathf.Max(
                    0f,
                    startPadding);
            endPadding =
                Mathf.Max(
                    0f,
                    endPadding);
            fadeInDuration =
                Mathf.Max(
                    0f,
                    fadeInDuration);
            holdDuration =
                Mathf.Max(
                    0f,
                    holdDuration);
            fadeOutDuration =
                Mathf.Max(
                    0f,
                    fadeOutDuration);
            peakAlpha =
                Mathf.Clamp01(
                    peakAlpha);

            FindReferences();
        }
#endif
    }
}
