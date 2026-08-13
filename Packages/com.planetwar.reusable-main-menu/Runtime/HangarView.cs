using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>Hosts the serialized Hangar visual page and its intentionally data-free item placeholder.</summary>
    [DisallowMultipleComponent]
    public sealed class HangarView : MonoBehaviour
    {
        [SerializeField] private GameObject entityDetails;
        [SerializeField] private GameObject spellDetails;
        [SerializeField] private HangarDetailLayout entityLayout;
        [SerializeField] private HangarDetailLayout spellLayout;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text diamondText;
        [SerializeField] private TMP_Text battleGoldText;
        [SerializeField] private TMP_Text battleDiamondText;
        private GameObject detailMask;
        private Transform detailOverlayParent;
        private Transform entityDetailsParent;
        private Transform spellDetailsParent;
        private int entityDetailsSiblingIndex;
        private int spellDetailsSiblingIndex;
        private IReadOnlyList<HangarItemSnapshot> boundItems;
        private int boundGold;
        private int boundDiamond;
        public void Show()
        {
            gameObject.SetActive(true);
            if (boundItems != null)
            {
                Bind(boundItems, boundGold, boundDiamond);
                HideDetails();
                return;
            }
            // The original UICardView refreshes every visible ItemCard when opened.  Our cards
            // are serialized rather than Addressables-created, so make the same refresh explicit
            // after the inactive page becomes active. This guarantees its Lock group cannot keep
            // an authored prefab state from before the card configuration was applied.
            foreach (var card in GetComponentsInChildren<HangarCardItem>(true)) card.ApplyVisual();
            HideDetails();
            if (goldText != null && battleGoldText != null) goldText.text = battleGoldText.text;
            if (diamondText != null && battleDiamondText != null) diamondText.text = battleDiamondText.text;
        }

        public void Bind(IReadOnlyList<HangarItemSnapshot> items, int gold, int diamond)
        {
            boundItems = items;
            boundGold = gold;
            boundDiamond = diamond;
            var allCards = GetComponentsInChildren<HangarCardItem>(true);
            var cards = new List<HangarCardItem>();
            foreach (var card in allCards)
                if (!IsDetailPreview(card.transform)) cards.Add(card);
            for (var i = 0; i < cards.Count; i++)
            {
                bool active = items != null && i < items.Count;
                cards[i].gameObject.SetActive(active);
                if (active) cards[i].Configure(this, items[i]);
            }
            if (goldText != null) goldText.text = gold.ToString();
            if (diamondText != null) diamondText.text = diamond.ToString();
        }

        private static bool IsDetailPreview(Transform item)
        {
            for (var current = item; current != null; current = current.parent)
                if (current.name == "UICardInfo" || current.name == "UICardSpell") return true;
            return false;
        }

        public void ShowEntityDetails(HangarCardItem card)
        {
            HideDetails();
            MoveDetailToOverlay(entityDetails);
            if (entityDetails != null) entityDetails.SetActive(true);
            ShowDetailMask(entityDetails);
            ApplyOriginalPreviewLayout(entityDetails, entityLayout, card);
        }

        public void ShowSpellDetails(HangarCardItem card)
        {
            HideDetails();
            MoveDetailToOverlay(spellDetails);
            if (spellDetails != null) spellDetails.SetActive(true);
            ShowDetailMask(spellDetails);
            ApplyOriginalPreviewLayout(spellDetails, spellLayout, card);
        }

        public void HideDetails()
        {
            if (entityDetails != null) entityDetails.SetActive(false);
            if (spellDetails != null) spellDetails.SetActive(false);
            if (detailMask != null) detailMask.SetActive(false);
            RestoreDetailParent(entityDetails, ref entityDetailsParent, entityDetailsSiblingIndex);
            RestoreDetailParent(spellDetails, ref spellDetailsParent, spellDetailsSiblingIndex);
        }

        // The source project's UICardInfo/UICardSpell are modal views. UIManager puts a
        // black, 150/255-alpha mask behind them and clicking it closes the modal. The reusable
        // menu has no UIManager, so reproduce that contract locally for its two detail panels.
        private void ShowDetailMask(GameObject detailPanel)
        {
            if (detailMask == null) CreateDetailMask();
            if (detailMask == null || detailPanel == null) return;

            detailMask.SetActive(true);
            // UIManager in the source project keeps modal content in a layer above its mask.
            // These panels share a parent here, so make the selected detail the last sibling and
            // insert the mask immediately below it. This remains correct after opening either
            // detail type repeatedly.
            detailPanel.transform.SetAsLastSibling();
            detailMask.transform.SetSiblingIndex(
                Mathf.Max(0, detailPanel.transform.GetSiblingIndex() - 1));
        }

        private void CreateDetailMask()
        {
            // The original UIManager owns its mask at the canvas level, above the bottom tab
            // page.  The migrated panels live inside UICardView, so using their direct parent
            // would leave UIMainBottom undimmed.  Use the page's parent (MainMenu root) instead.
            detailOverlayParent = transform.parent;
            Transform parent = detailOverlayParent;
            if (parent == null) return;

            detailMask = new GameObject("HangarDetailMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            detailMask.transform.SetParent(parent, false);
            RectTransform rect = detailMask.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = detailMask.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 150f / 255f);
            detailMask.GetComponent<Button>().onClick.AddListener(HideDetails);
            detailMask.SetActive(false);
        }

        private void MoveDetailToOverlay(GameObject panel)
        {
            if (panel == null) return;
            if (detailOverlayParent == null) detailOverlayParent = transform.parent;
            if (detailOverlayParent == null || panel.transform.parent == detailOverlayParent) return;

            if (panel == entityDetails)
            {
                entityDetailsParent = panel.transform.parent;
                entityDetailsSiblingIndex = panel.transform.GetSiblingIndex();
            }
            else if (panel == spellDetails)
            {
                spellDetailsParent = panel.transform.parent;
                spellDetailsSiblingIndex = panel.transform.GetSiblingIndex();
            }
            panel.transform.SetParent(detailOverlayParent, false);
        }

        private static void RestoreDetailParent(GameObject panel, ref Transform originalParent, int siblingIndex)
        {
            if (panel == null || originalParent == null) return;
            panel.transform.SetParent(originalParent, false);
            panel.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, originalParent.childCount - 1));
            originalParent = null;
        }

        private static void ApplyOriginalPreviewLayout(GameObject panel, HangarDetailLayout layout, HangarCardItem card)
        {
            if (layout != null)
            {
                layout.ShowPreview(card);
                return;
            }

            // Supports already-generated prefabs while the editor rebakes the serialized layout.
            var upgrade = Find(panel.transform, "btnUpgrade");
            var equip = Find(panel.transform, "btnUpBattle");
            var slider = Find(panel.transform, "Slider");
            if (upgrade != null) upgrade.gameObject.SetActive(false);
            if (equip != null) equip.gameObject.SetActive(true);
            if (slider != null && slider.parent != null) slider.parent.gameObject.SetActive(true);
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var result = Find(child, name);
                if (result != null) return result;
            }
            return null;
        }

    }
}
