using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Original UIGameSettle fade-in animation.</summary>
public class TweenFadeIn : MonoBehaviour
{
    public float duration = 0.4f;
    public Ease ease = Ease.OutBack;
    public float startAlpha = 0f;
    private Tween tween;
    private CanvasGroup canvasGroup;
    private Graphic graphic;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) graphic = GetComponent<Graphic>();
    }

    private void OnEnable() => PlayAnim();

    public void PlayAnim()
    {
        tween?.Kill(false);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = startAlpha;
            tween = canvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad);
        }
        else if (graphic != null)
        {
            var color = graphic.color;
            color.a = startAlpha;
            graphic.color = color;
            tween = graphic.DOFade(1f, duration).SetEase(Ease.OutQuad);
        }
    }

    private void OnDisable()
    {
        tween?.Kill(false);
        tween = null;
    }
}
