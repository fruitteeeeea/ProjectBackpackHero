using DG.Tweening;
using UnityEngine;

/// <summary>Original UIGameSettle scale-in animation.</summary>
public class TweenScale : MonoBehaviour
{
    public Vector3 startScale = Vector3.one * 0.5f;
    public float duration = 0.4f;
    public Ease ease = Ease.OutBack;
    private Tween tween;
    private Vector3 endScale = Vector3.one;

    private void Awake() => endScale = transform.localScale;

    private void OnEnable()
    {
        tween?.Kill(false);
        transform.localScale = startScale;
        tween = transform.DOScale(endScale, duration).SetEase(ease);
    }

    private void OnDisable()
    {
        tween?.Kill(false);
        tween = null;
    }
}
