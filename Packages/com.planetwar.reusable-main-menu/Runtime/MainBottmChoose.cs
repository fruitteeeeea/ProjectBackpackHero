using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class MainBottmChoose : MonoBehaviour
{
    [SerializeField] private ImageLoader imgLoader;
    [SerializeField] private TextMeshProUGUI txtChoose;
    private readonly List<Tween> tweens = new List<Tween>();
    private static readonly string[] Labels = { "", "Ranks", "Battle", "Hangar", "" };

    public virtual void OnChooseBottom(Transform target, int index)
    {
        if (target == null || index < 0 || index >= Labels.Length) return;
        foreach (var tween in tweens) tween?.Kill();
        tweens.Clear();
        if (txtChoose != null) txtChoose.text = Labels[index];
        if (imgLoader != null) imgLoader.Select(index);
        var position = transform.position;
        position.x = target.position.x;
        tweens.Add(transform.DOMove(position, .3f).SetEase(Ease.OutQuad));
        AddBounce(imgLoader != null ? imgLoader.transform : null);
        AddBounce(txtChoose != null ? txtChoose.transform : null);
    }

    private void AddBounce(Transform target)
    {
        if (target == null) return;
        target.localScale = Vector3.one * .5f;
        tweens.Add(DOTween.Sequence().Append(target.DOScale(1.2f, .15f).SetEase(Ease.OutQuad)).Append(target.DOScale(1f, .1f).SetEase(Ease.OutQuad)));
    }
}
