using DG.Tweening;
using UnityEngine;

/// <summary>Source-compatible local Y-axis floating animation used by migrated card-pack UI.</summary>
public sealed class TweenUpAndDown : MonoBehaviour
{
    public float distance = 15f;
    public float duration = 1f;
    public Ease ease = Ease.InOutSine;

    Vector3 startPosition;
    Tween tween;

    void OnEnable()
    {
        startPosition = transform.localPosition;
        tween?.Kill();
        tween = transform.DOLocalMoveY(startPosition.y + distance, duration)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void OnDisable()
    {
        tween?.Kill();
        transform.localPosition = startPosition;
    }
}
