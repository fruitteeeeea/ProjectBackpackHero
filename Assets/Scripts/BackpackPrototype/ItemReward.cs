using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared source-compatible reward item for settlement and pack opening UI.</summary>
public sealed class ItemReward : MonoBehaviour
{
    const string RewardArtPath = "PlanetWar/OriginalSettlement/Art/Reward/";

    public ImageLoader loaderBg;
    public TextMeshProUGUI textCount;
    public TextMeshProUGUI textName;
    public ImageLoader resLoader;
    public Image imgIcon;
    public float preScale = .5f;
    public float upDuration = .1f;
    public float downDuration = .5f;
    public float fragmentIconScale = .75f;
    public float currencyIconScale = .6f;

    Tween tween;
    Image backgroundImage;
    Image currencyImage;
    static Sprite normalBackground;
    static Sprite highlightBackground;
    static Sprite goldIcon;
    static Sprite diamondIcon;

    public void BindCurrency(bool diamond, int amount, bool revealStyle)
    {
        ApplyBackground(revealStyle);
        if (textCount != null) textCount.text = "x" + amount;
        if (textName != null) textName.gameObject.SetActive(false);
        if (resLoader != null)
        {
            resLoader.gameObject.SetActive(false);
            resLoader.Select(diamond ? 1 : 0);
        }
        ApplyCurrencyIcon(diamond, true);
        PlayReveal();
    }

    public void BindItemFragment(BackpackPrototype.ItemData item, int amount, bool revealStyle)
    {
        ApplyBackground(revealStyle);
        if (textCount != null) textCount.text = "x" + amount;
        if (textName != null) textName.gameObject.SetActive(false);
        if (resLoader != null) resLoader.gameObject.SetActive(false);
        if (imgIcon != null)
        {
            imgIcon.gameObject.SetActive(true);
            imgIcon.sprite = item != null ? item.Icon : null;
            imgIcon.rectTransform.localScale = Vector3.one * fragmentIconScale;
        }
        PlayReveal();
    }

    public void ResetForReuse()
    {
        tween?.Kill(false);
        tween = null;
        transform.localScale = Vector3.one;
        if (imgIcon != null)
        {
            imgIcon.sprite = null;
            imgIcon.rectTransform.localScale = Vector3.one;
            imgIcon.gameObject.SetActive(false);
        }
        if (resLoader != null) resLoader.gameObject.SetActive(false);
        if (textName != null) textName.gameObject.SetActive(false);
        if (textCount != null) textCount.text = string.Empty;
    }

    void OnEnable() => PlayReveal();
    void OnDisable() => ResetForReuse();
    void OnDestroy() => tween?.Kill(false);

    void ApplyBackground(bool revealStyle)
    {
        if (loaderBg != null) loaderBg.Select(revealStyle ? 1 : 0);
        backgroundImage ??= GetComponent<Image>();
        if (backgroundImage == null) return;

        normalBackground ??= Resources.Load<Sprite>(RewardArtPath + "bg_com_daoju_1");
        highlightBackground ??= Resources.Load<Sprite>(RewardArtPath + "bg_com_daoju_2");
        Sprite background = revealStyle ? highlightBackground : normalBackground;
        if (background != null) backgroundImage.sprite = background;
        backgroundImage.color = Color.white;
    }

    void ApplyCurrencyIcon(bool diamond, bool useFallbackImage)
    {
        goldIcon ??= Resources.Load<Sprite>(RewardArtPath + "icon_com_jinbi");
        diamondIcon ??= Resources.Load<Sprite>(RewardArtPath + "icon_com_zuanshi");
        Sprite icon = diamond ? diamondIcon : goldIcon;
        if (resLoader != null)
        {
            currencyImage ??= resLoader.GetComponent<Image>();
            if (currencyImage != null)
            {
                if (icon != null) currencyImage.sprite = icon;
                currencyImage.color = Color.white;
            }
        }

        if (imgIcon == null) return;
        imgIcon.sprite = icon;
        imgIcon.color = Color.white;
        imgIcon.rectTransform.localScale = Vector3.one * currencyIconScale;
        imgIcon.gameObject.SetActive(useFallbackImage);
    }

    void PlayReveal()
    {
        tween?.Kill(false);
        transform.localScale = Vector3.one * preScale;
        tween = DOTween.Sequence()
            .Append(transform.DOScale(1.2f, upDuration).SetEase(Ease.OutBack))
            .Append(transform.DOScale(1f, downDuration).SetEase(Ease.OutBack));
    }
}
