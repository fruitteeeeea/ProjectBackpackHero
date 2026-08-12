using DG.Tweening;
using UnityEngine;

public sealed class TweenScaleToScale : MonoBehaviour
{
    public Vector3 startScale = Vector3.one * .5f;
    public Vector3 peakScale = Vector3.one * 1.2f;
    public Vector3 endScale = Vector3.one;
    public float duration = .4f;
    public float endDuration = .1f;
    public int loops = 2;
    public Ease ease = Ease.OutBack;
    private Tween tween;

    private void OnEnable()
    {
        tween?.Kill(false);
        transform.localScale = startScale;
        tween = DOTween.Sequence()
            .Append(transform.DOScale(peakScale, duration).SetEase(ease))
            .Append(transform.DOScale(endScale, endDuration).SetEase(ease))
            .SetLoops(loops);
    }

    private void OnDisable()
    {
        tween?.Kill(false);
        tween = null;
    }
}
