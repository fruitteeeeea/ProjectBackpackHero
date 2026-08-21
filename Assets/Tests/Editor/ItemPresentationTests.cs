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
    [TestCase(HangarItemKind.Aircraft, 2, 3, 5, 0.6f, "3/5", false)]
    [TestCase(HangarItemKind.Equipment, 4, 8, 5, 1f, "8/5", true)]
    [TestCase(HangarItemKind.Aircraft, 10, 0, 0, 1f, "Max", false)]
    public void HangarCard_UsesItsOwnSnapshotForFragmentProgress(
        HangarItemKind kind, int level, int fragments, int required, float expectedValue,
        string expectedText, bool expectedUpgradeIndicator)
    {
        GameObject root = new GameObject("Card");
        try
        {
            HangarCardItem card = root.AddComponent<HangarCardItem>();
            Slider slider = NewProgressSlider(root.transform, out TMP_Text progressText,
                out GameObject upgradeIndicator);
            card.ConfigureProgressionPresentation(slider, progressText, upgradeIndicator);
            card.Configure(new HangarItemSnapshot(kind, "Item", "Description", null, null, true,
                level, fragments, required, 0, 1f, 1, null, Color.white, "Unlocked",
                itemId: "item", maximumLevel: 10));

            Assert.That(slider.gameObject.activeSelf, Is.True);
            Assert.That(slider.value, Is.EqualTo(expectedValue).Within(0.0001f));
            Assert.That(progressText.text, Is.EqualTo(expectedText));
            Assert.That(upgradeIndicator.activeSelf, Is.EqualTo(expectedUpgradeIndicator));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(false, false)]
    [TestCase(true, true)]
    public void HangarCard_HidesFragmentProgressForLockedCardsAndEmptyDeckSlots(
        bool emptyDeckSlot, bool expectedEmpty)
    {
        GameObject root = new GameObject("Card");
        GameObject ownerRoot = new GameObject("Hangar");
        try
        {
            HangarCardItem card = root.AddComponent<HangarCardItem>();
            Slider slider = NewProgressSlider(root.transform, out TMP_Text progressText,
                out GameObject upgradeIndicator);
            card.ConfigureProgressionPresentation(slider, progressText, upgradeIndicator);
            if (emptyDeckSlot)
                card.ConfigureEmptyDeckSlot(ownerRoot.AddComponent<HangarView>(), 0,
                    HangarCardItem.CardKind.Entity);
            else
                card.Configure(new HangarItemSnapshot(HangarItemKind.Aircraft, "Locked", "", null,
                    null, false, 1, 5, 5, 0, 1f, 1, null, Color.white, "Locked", itemId: "locked"));

            Assert.That(card.IsEmptyDeckSlot, Is.EqualTo(expectedEmpty));
            Assert.That(slider.gameObject.activeSelf, Is.False);
            Assert.That(upgradeIndicator.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(ownerRoot);
            Object.DestroyImmediate(root);
        }
    }

    [TestCase(false, false, false, true, false)]
    [TestCase(false, true, true, false, false)]
    [TestCase(true, false, false, true, false)]
    [TestCase(true, true, true, false, false)]
    [TestCase(false, false, false, false, true)]
    public void HangarDetailLayout_BottomActionsMatchOriginalCardInfoContract(
        bool equipped, bool enoughFragments, bool expectedUpgrade,
        bool expectedProgress, bool maxLevel)
    {
        GameObject root = new GameObject("HangarDetail");
        GameObject ownerRoot = new GameObject("Hangar");
        GameObject cardRoot = new GameObject("Card");
        try
        {
            HangarDetailLayout layout = root.AddComponent<HangarDetailLayout>();
            GameObject upgrade = new GameObject("Upgrade");
            GameObject equip = new GameObject("Equip");
            GameObject progress = new GameObject("Progress");
            layout.Configure(upgrade, equip, progress, null, null, null, null, null);

            int level = maxLevel ? 10 : 1;
            int fragments = enoughFragments ? 5 : 4;
            var snapshot = new HangarItemSnapshot(
                HangarItemKind.Aircraft, "Scout", "Reliable frontline damage.",
                null, null, true, level, fragments, 5, 0, 2f, 1, null,
                Color.white, "Unlocked", itemId: "scout", maximumLevel: 10);
            HangarCardItem card = cardRoot.AddComponent<HangarCardItem>();
            card.Configure(ownerRoot.AddComponent<HangarView>(), snapshot, equipped ? 0 : -1);

            layout.ShowPreview(card);

            Assert.That(upgrade.activeSelf, Is.EqualTo(expectedUpgrade));
            Assert.That(progress.activeSelf, Is.EqualTo(expectedProgress));
            Assert.That(equip.activeSelf, Is.EqualTo(!equipped));
        }
        finally
        {
            Object.DestroyImmediate(cardRoot);
            Object.DestroyImmediate(ownerRoot);
            Object.DestroyImmediate(root);
        }
    }

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
    public void HangarDetailLayout_RecoversMissingHeaderReferencesWithoutUsingPreviewTexts()
    {
        GameObject root = new GameObject("HangarDetail");
        GameObject cardRoot = new GameObject("Card");
        try
        {
            HangarDetailLayout layout = root.AddComponent<HangarDetailLayout>();
            GameObject nameBackground = new GameObject("nameBg");
            nameBackground.transform.SetParent(root.transform);
            TMP_Text name = NewText(nameBackground.transform, "name");
            TMP_Text description = NewText(root.transform, "desc");
            name.gameObject.SetActive(false);
            description.gameObject.SetActive(false);
            GameObject preview = new GameObject("ItemCard (1)");
            preview.transform.SetParent(root.transform);
            TMP_Text staleName = NewText(preview.transform, "name");
            TMP_Text staleDescription = NewText(preview.transform, "desc");
            HangarCardItem card = cardRoot.AddComponent<HangarCardItem>();
            var snapshot = new HangarItemSnapshot(HangarItemKind.Aircraft,
                "初号飞机", "均衡的前线战机，提供稳定火力。", null, null, true,
                1, 0, 0, 0, 2f, 1, null, Color.white, "Unlocked");
            card.Configure(snapshot);

            layout.ShowPreview(card);

            Assert.That(name.gameObject.activeSelf, Is.True);
            Assert.That(description.gameObject.activeSelf, Is.True);
            Assert.That(name.text, Is.EqualTo(snapshot.Name));
            Assert.That(description.text, Is.EqualTo(snapshot.Description));
            Assert.That(staleName.text, Is.Not.EqualTo(snapshot.Name));
            Assert.That(staleDescription.text, Is.Not.EqualTo(snapshot.Description));
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

    private static Slider NewProgressSlider(Transform parent, out TMP_Text progressText,
        out GameObject upgradeIndicator)
    {
        GameObject sliderRoot = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderRoot.transform.SetParent(parent);
        progressText = NewText(sliderRoot.transform, "Text (TMP)");
        upgradeIndicator = new GameObject("Image (2)");
        upgradeIndicator.transform.SetParent(sliderRoot.transform);
        return sliderRoot.GetComponent<Slider>();
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
