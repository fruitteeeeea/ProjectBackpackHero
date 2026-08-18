using DG.Tweening;
using BackpackHero.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    /// <summary>
    /// Keeps the backpack ship image in sync with the battle phase.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BackpackSpaceShipPhaseFade : MonoBehaviour
    {
        private const float FadeDuration = 0.4f;

        [SerializeField]
        private Image targetImage;

        private float visibleAlpha;
        private Tween fadeTween;

        private void Awake()
        {
            targetImage ??= GetComponent<Image>();
            visibleAlpha = targetImage != null
                ? targetImage.color.a
                : 1f;
        }

        private void OnEnable()
        {
            BattleFlowController.PhaseChanged += HandlePhaseChanged;
        }

        private void Start()
        {
            ApplyPhaseImmediately(BattleFlowController.CurrentPhase);
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            FadeTo(phase == BattlePhase.Preparation ? 0f : visibleAlpha);
        }

        private void ApplyPhaseImmediately(BattlePhase phase)
        {
            fadeTween?.Kill();
            fadeTween = null;
            SetAlpha(phase == BattlePhase.Preparation ? 0f : visibleAlpha);
        }

        private void FadeTo(float alpha)
        {
            if (targetImage == null)
            {
                return;
            }

            fadeTween?.Kill();
            fadeTween = targetImage
                .DOFade(alpha, FadeDuration)
                .SetEase(Ease.OutQuad);
        }

        private void SetAlpha(float alpha)
        {
            if (targetImage == null)
            {
                return;
            }

            Color color = targetImage.color;
            color.a = alpha;
            targetImage.color = color;
        }

        private void OnDisable()
        {
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
            fadeTween?.Kill();
            fadeTween = null;
        }
    }
}
