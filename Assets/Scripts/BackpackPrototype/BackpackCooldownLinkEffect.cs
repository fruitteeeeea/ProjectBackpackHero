using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    /// <summary>
    /// 装备冷却结束时，将其图标沿 UI 贝塞尔曲线送往相邻飞机。
    /// 每一条曲线独立计时，抵达时才通知生成器请求对应飞机。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BackpackCooldownLinkEffect : MonoBehaviour
    {
        private const float MinimumDuration = 0.12f;

        [SerializeField, Min(MinimumDuration)]
        private float flightDuration = 0.27f;

        [SerializeField, Min(0f)]
        private float durationVariation = 0.045f;

        [SerializeField, Min(0f)]
        private float curveBend = 78f;

        [SerializeField, Min(0f)]
        private float trailSpacing = 0.035f;

        private readonly Dictionary<ItemInstance, ItemView> views = new();
        private RectTransform effectLayer;

        public void Play(
            ItemInstance source,
            IReadOnlyList<ItemInstance> targets,
            Action<ItemInstance> onImpact)
        {
            if (source == null || targets == null || targets.Count == 0)
            {
                return;
            }

            RefreshViews();
            if (!views.TryGetValue(source, out ItemView sourceView) ||
                sourceView == null || !EnsureEffectLayer(sourceView))
            {
                // UI 尚未创建时，不能让战斗流程被视觉层卡住。
                foreach (ItemInstance target in targets)
                {
                    onImpact?.Invoke(target);
                }
                return;
            }

            foreach (ItemInstance target in targets)
            {
                if (target != null && views.TryGetValue(target, out ItemView targetView))
                {
                    StartCoroutine(PlayFlight(
                        sourceView,
                        targetView,
                        onImpact));
                }
                else
                {
                    onImpact?.Invoke(target);
                }
            }
        }

        private IEnumerator PlayFlight(
            ItemView source,
            ItemView target,
            Action<ItemInstance> onImpact)
        {
            Image projectile = CreateProjectile(source.IconSprite);
            if (projectile == null)
            {
                onImpact?.Invoke(target.Instance);
                yield break;
            }

            Vector3 start = source.GetImageGeometricCenterWorldPosition();
            Vector3 end = target.GetImageGeometricCenterWorldPosition();
            // 所有装备触发弹道统一向上拱起，避免左右随机造成视觉噪声。
            Vector3 control = (start + end) * .5f +
                              Vector3.up * curveBend;
            float duration = Mathf.Max(
                MinimumDuration,
                flightDuration + UnityEngine.Random.Range(
                    -durationVariation,
                    durationVariation));
            float elapsed = 0f;
            float nextTrailTime = 0f;

            while (elapsed < duration && projectile != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 position = EvaluateQuadratic(
                    start,
                    control,
                    end,
                    DOVirtual.EasedValue(0f, 1f, t, Ease.OutQuad));
                projectile.rectTransform.position = position;
                projectile.rectTransform.localScale = Vector3.one *
                    Mathf.Lerp(0.75f, 0.48f, t);

                if (elapsed >= nextTrailTime)
                {
                    CreateTrail(source.IconSprite, position);
                    nextTrailTime = elapsed + trailSpacing;
                }
                yield return null;
            }

            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }

            if (target != null && target.isActiveAndEnabled)
            {
                target.PlayTriggeredFeedback();
                PlayImpactParticles(target);
            }

            onImpact?.Invoke(target != null ? target.Instance : null);
        }

        private Image CreateProjectile(Sprite sprite)
        {
            if (effectLayer == null)
            {
                return null;
            }

            GameObject gameObject = new("Cooldown Link Projectile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(effectLayer, false);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(1f, 1f, 1f, .98f);
            image.raycastTarget = false;
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            rect.sizeDelta = new Vector2(78f, 78f);
            rect.position = Vector3.zero;
            return image;
        }

        private void CreateTrail(Sprite sprite, Vector3 position)
        {
            Image trail = CreateProjectile(sprite);
            if (trail == null)
            {
                return;
            }

            trail.rectTransform.position = position;
            trail.rectTransform.localScale = Vector3.one * .34f;
            trail.color = new Color(1f, 1f, 1f, .35f);
            trail.DOFade(0f, .18f);
            trail.rectTransform.DOScale(.1f, .18f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (trail != null)
                    {
                        Destroy(trail.gameObject);
                    }
                });
        }

        // 与飞机受击同样采用短促、向外扩散的白色闪光粒子语言。
        private void PlayImpactParticles(ItemView target)
        {
            Sprite sprite = target.IconSprite;
            for (int index = 0; index < 6; index++)
            {
                Image particle = CreateProjectile(sprite);
                if (particle == null)
                {
                    continue;
                }

                RectTransform rect = particle.rectTransform;
                rect.position =
                    target.GetImageGeometricCenterWorldPosition();
                rect.sizeDelta = new Vector2(10f, 10f);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 42f;
                particle.color = new Color(1f, 1f, 1f, .8f);
                particle.DOFade(0f, .24f);
                rect.DOMove(rect.position + (Vector3)offset, .24f)
                    .SetEase(Ease.OutQuad);
                rect.DOScale(UnityEngine.Random.Range(.2f, .55f), .24f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if (particle != null)
                        {
                            Destroy(particle.gameObject);
                        }
                    });
            }
        }

        private void RefreshViews()
        {
            views.Clear();
            foreach (ItemView view in FindObjectsByType<ItemView>(FindObjectsSortMode.None))
            {
                if (view != null && view.CombatController == GetComponent<BackpackCombatController>() && view.Instance != null)
                {
                    views[view.Instance] = view;
                }
            }
        }

        private bool EnsureEffectLayer(ItemView source)
        {
            if (effectLayer != null)
            {
                return true;
            }

            Canvas canvas = source.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return false;
            }

            GameObject layer = new("Cooldown Link Effects", typeof(RectTransform));
            layer.transform.SetParent(canvas.transform, false);
            effectLayer = layer.GetComponent<RectTransform>();
            effectLayer.anchorMin = Vector2.zero;
            effectLayer.anchorMax = Vector2.one;
            effectLayer.offsetMin = Vector2.zero;
            effectLayer.offsetMax = Vector2.zero;
            effectLayer.SetAsLastSibling();
            return true;
        }

        private static Vector3 EvaluateQuadratic(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            flightDuration = Mathf.Max(MinimumDuration, flightDuration);
            durationVariation = Mathf.Max(0f, durationVariation);
            curveBend = Mathf.Max(0f, curveBend);
            trailSpacing = Mathf.Max(0f, trailSpacing);
        }
#endif
    }
}
