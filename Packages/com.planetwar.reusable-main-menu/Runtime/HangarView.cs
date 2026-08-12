using TMPro;
using UnityEngine;

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
        public void Show()
        {
            gameObject.SetActive(true);
            // The original UICardView refreshes every visible ItemCard when opened.  Our cards
            // are serialized rather than Addressables-created, so make the same refresh explicit
            // after the inactive page becomes active. This guarantees its Lock group cannot keep
            // an authored prefab state from before the card configuration was applied.
            foreach (var card in GetComponentsInChildren<HangarCardItem>(true)) card.ApplyVisual();
            HideDetails();
            if (goldText != null && battleGoldText != null) goldText.text = battleGoldText.text;
            if (diamondText != null && battleDiamondText != null) diamondText.text = battleDiamondText.text;
        }

        public void ShowEntityDetails(HangarCardItem card)
        {
            HideDetails();
            if (entityDetails != null) entityDetails.SetActive(true);
            ApplyOriginalPreviewLayout(entityDetails, entityLayout, card);
        }

        public void ShowSpellDetails(HangarCardItem card)
        {
            HideDetails();
            if (spellDetails != null) spellDetails.SetActive(true);
            ApplyOriginalPreviewLayout(spellDetails, spellLayout, card);
        }

        public void HideDetails()
        {
            if (entityDetails != null) entityDetails.SetActive(false);
            if (spellDetails != null) spellDetails.SetActive(false);
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
