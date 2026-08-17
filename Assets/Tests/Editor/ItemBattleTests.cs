using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ItemBattleTests
{
    [Test]
    public void SetData_UpdatesEveryVisibleParticipantField()
    {
        GameObject root = new GameObject("ItemBattle");
        try
        {
            ItemBattle view = root.AddComponent<ItemBattle>();
            view.imgHead = new GameObject("Avatar", typeof(Image)).GetComponent<Image>();
            view.imgHead.transform.SetParent(root.transform);
            view.textName = NewText(root.transform, "Name");
            view.textRankName = NewText(root.transform, "Rank");
            view.textScore = NewText(root.transform, "Score");
            view.textWinCount = NewText(root.transform, "Wins");

            view.SetData(new MatchParticipant("Pilot", "Bronze I", "42"), true);

            Assert.That(view.textName.text, Is.EqualTo("Pilot"));
            Assert.That(view.textRankName.text, Is.EqualTo("Bronze I"));
            Assert.That(view.textScore.text, Is.EqualTo("42"));
            Assert.That(view.textWinCount.text, Is.EqualTo("42"));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static TextMeshProUGUI NewText(Transform parent, string name)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent);
        return textObject.AddComponent<TextMeshProUGUI>();
    }
}
