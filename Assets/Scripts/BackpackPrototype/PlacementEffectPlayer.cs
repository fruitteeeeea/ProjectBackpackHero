using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    public static class PlacementEffectPlayer
    {
        public static void Play(RectTransform target)
        {
            if (target == null || target.parent == null)
            {
                return;
            }

            var effectRect = new GameObject("PlacementEffect", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            effectRect.SetParent(target.parent, false);
            effectRect.anchorMin = target.anchorMin;
            effectRect.anchorMax = target.anchorMax;
            effectRect.pivot = target.pivot;
            effectRect.sizeDelta = target.sizeDelta;
            effectRect.anchoredPosition = target.anchoredPosition;
            effectRect.SetAsLastSibling();

            var image = effectRect.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = new Color(1f, 0.88f, 0.24f, 0.42f);

            effectRect.localScale = Vector3.one * 0.82f;
            var sequence = DOTween.Sequence();
            sequence.Join(effectRect.DOScale(1.18f, 0.28f).SetEase(Ease.OutQuad));
            sequence.Join(image.DOFade(0f, 0.28f).SetEase(Ease.OutQuad));
            sequence.OnComplete(() =>
            {
                if (effectRect != null)
                {
                    Object.Destroy(effectRect.gameObject);
                }
            });
        }
    }
}
