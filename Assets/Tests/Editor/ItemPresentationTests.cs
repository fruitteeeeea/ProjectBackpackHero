using NUnit.Framework;
using PlanetWar.ReusableMainMenu;
using TMPro;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BackpackPrototype;

public sealed class ItemPresentationTests
{
    [Test]
    public void HangarDetailLayout_UsesSnapshotNameAndDescriptionWithoutStats()
    {
        GameObject root = new GameObject("HangarDetail");
        GameObject cardRoot = new GameObject("Card");
        try
        {
            HangarDetailLayout layout = root.AddComponent<HangarDetailLayout>();
            TMP_Text name = NewText(root.transform, "Name");
            TMP_Text description = NewText(root.transform, "Description");
            HangarCardItem card = cardRoot.AddComponent<HangarCardItem>();
            var snapshot = new HangarItemSnapshot(
                HangarItemKind.Equipment,
                "Rapid Cannon",
                "Adds rapid straight shots to adjacent fighters.",
                null, null, true, 1, 0, 0, 0, 2.5f, 1, null,
                Color.white, "Unlocked by default");
            card.Configure(snapshot);
            layout.Configure(null, null, null, null, name, description, null, null);

            layout.ShowPreview(card);

            Assert.That(name.text, Is.EqualTo(snapshot.Name));
            Assert.That(description.text, Is.EqualTo(snapshot.Description));
            Assert.That(description.text, Does.Not.Contain("Type:"));
        }
        finally
        {
            Object.DestroyImmediate(cardRoot);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void HangarCard_OverridesImportedButtonCallbackWithItsOwnHandler()
    {
        GameObject hangarRoot = new GameObject("Hangar");
        GameObject cardRoot = new GameObject("Card");
        try
        {
            HangarView hangar = hangarRoot.AddComponent<HangarView>();
            HangarCardItem card = cardRoot.AddComponent<HangarCardItem>();
            GameObject clickable = new GameObject("Image", typeof(RectTransform), typeof(Image), typeof(Button));
            clickable.transform.SetParent(cardRoot.transform);
            Button button = clickable.GetComponent<Button>();
            bool staleCallbackWasCalled = false;
            button.onClick.AddListener(() => staleCallbackWasCalled = true);

            card.Configure(hangar, new HangarItemSnapshot(
                HangarItemKind.Aircraft, "Scout", "Reliable frontline damage.",
                null, null, true, 1, 0, 0, 0, 2f, 1, null,
                Color.white, "Unlocked", itemId: "scout"));
            button.onClick.Invoke();

            Assert.That(staleCallbackWasCalled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(cardRoot);
            Object.DestroyImmediate(hangarRoot);
        }
    }

    [Test]
    public void ItemInfoPanel_ResolvesLabelsAndOnlyShowsWhileDragging()
    {
        GameObject root = new GameObject("ItemInfoPanel");
        GameObject itemRoot = new GameObject("Item", typeof(RectTransform));
        try
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform);
            TMP_Text name = NewText(visual.transform, "ItemNameText");
            TMP_Text level = NewText(visual.transform, "LevelText");
            TMP_Text description = NewText(visual.transform, "DescriptionText");
            ItemInfoPanel panel = root.AddComponent<ItemInfoPanel>();
            GameObject staleReferenceRoot = new GameObject("StalePrefabReference");
            staleReferenceRoot.transform.SetParent(root.transform);
            TMP_Text staleName = NewText(staleReferenceRoot.transform, "ItemNameText");
            SetField(panel, "panelVisual", visual);
            SetField(panel, "itemNameLabel", staleName);
            Invoke(panel, "ResolveTextReferences");

            ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Data/Backpack/Items/Equipment_RapidCannon.asset");
            ItemView item = itemRoot.AddComponent<ItemView>();
            SetAutoProperty(item, "Instance", new ItemInstance(data.ItemId, data, Vector2Int.zero));
            SetField(item, "isDragging", true);

            panel.RefreshPresentation(item);

            Assert.That(panel.IsShowing, Is.True);
            Assert.That(name.text, Is.EqualTo(data.ItemName));
            Assert.That(staleName.text, Is.Not.EqualTo(data.ItemName));
            Assert.That(level.text, Is.EqualTo("Lv.1"));
            Assert.That(description.text, Is.EqualTo(data.Description));

            SetField(item, "isDragging", false);
            panel.RefreshPresentation(item);
            Assert.That(panel.IsShowing, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(itemRoot);
            Object.DestroyImmediate(root);
        }
    }

    private static TMP_Text NewText(Transform parent, string name)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent);
        return textObject.AddComponent<TextMeshProUGUI>();
    }

    private static void Invoke(object target, string methodName)
    {
        target.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static void SetAutoProperty(object target, string propertyName, object value)
    {
        target.GetType().GetField("<" + propertyName + ">k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
