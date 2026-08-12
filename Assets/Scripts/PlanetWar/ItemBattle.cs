using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Data and entrance-animation adapter for the original match-card layout.</summary>
public sealed class ItemBattle : MonoBehaviour
{
    public Image imgHead;
    public TextMeshProUGUI textName;
    public TextMeshProUGUI textRankName;
    public TextMeshProUGUI textScore;
    public TextMeshProUGUI textWinCount;

    private Vector3 initialLocalPosition;
    private Tween moveTween;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
    }

    public void SetData(MatchParticipant participant, bool isPlayer)
    {
        SetStartPosition(isPlayer);
        if (participant.Avatar != null)
            imgHead.sprite = participant.Avatar;
        textName.text = participant.DisplayName;
        textRankName.text = participant.Rank;
        textWinCount.text = participant.Score;
    }

    public void RunAnimation()
    {
        moveTween?.Kill(false);
        moveTween = transform.DOLocalMove(initialLocalPosition, .5f);
    }

    private void SetStartPosition(bool isPlayer)
    {
        float width = 0f;
        Renderer rendererComponent = GetComponent<Renderer>();
        if (rendererComponent != null)
            width = rendererComponent.bounds.size.x;

        const float margin = 100f;
        Vector3 position = initialLocalPosition;
        position.x += (width + margin) * (isPlayer ? 1f : -1f);
        transform.localPosition = new Vector3(position.x, position.y, 0f);
    }

    private void OnDisable()
    {
        moveTween?.Kill(false);
        moveTween = null;
    }
}
