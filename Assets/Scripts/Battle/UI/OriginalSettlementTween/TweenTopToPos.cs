using DG.Tweening;
using UnityEngine;

/// <summary>Original UIGameSettle title drop-in animation.</summary>
public class TweenTopToPos : MonoBehaviour
{
    public float duration = 0.6f;
    public float delay;
    public Ease ease = Ease.OutBounce;
    public float margin = 50f;
    private RectTransform rectTransform;
    private Vector2 initialAnchoredPosition;
    private Vector3 initialLocalPosition;
    private Tween tween;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) initialAnchoredPosition = rectTransform.anchoredPosition;
        else initialLocalPosition = transform.localPosition;
    }

    private void OnEnable() => Play();

    public void Play()
    {
        tween?.Kill(false);
        if (rectTransform != null)
        {
            var canvas = GetComponentInParent<Canvas>();
            var height = canvas != null ? canvas.GetComponent<RectTransform>().rect.height : Screen.height;
            rectTransform.anchoredPosition = new Vector2(initialAnchoredPosition.x, initialAnchoredPosition.y + height + margin);
            tween = rectTransform.DOAnchorPos(initialAnchoredPosition, duration).SetDelay(delay).SetEase(ease);
        }
        else
        {
            transform.localPosition = new Vector3(initialLocalPosition.x, initialLocalPosition.y + Screen.height + margin, initialLocalPosition.z);
            tween = transform.DOLocalMove(initialLocalPosition, duration).SetDelay(delay).SetEase(ease);
        }
    }

    private void OnDisable()
    {
        tween?.Kill(false);
        if (rectTransform != null) rectTransform.anchoredPosition = initialAnchoredPosition;
        else transform.localPosition = initialLocalPosition;
        tween = null;
    }
}
