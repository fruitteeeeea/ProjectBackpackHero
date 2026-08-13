using TMPro;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>
    /// Applies the original UICardInfo/UICardSpell bottom-action visibility contract to the
    /// data-free preview: an unlocked card without enough fragments shows Equip + progress,
    /// never Upgrade and progress at the same time.
    /// </summary>
    public sealed class HangarDetailLayout : MonoBehaviour
    {
        [SerializeField] private GameObject upgradeButton;
        [SerializeField] private GameObject equipButton;
        [SerializeField] private GameObject progressGroup;
        [SerializeField] private GameObject actionGroup;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text lockText;
        [SerializeField] private HangarCardItem previewItem;
        // Mirrors the source UICardInfo.textAttList contract: each fixed UI slot is serialized
        // in display order, rather than inferred from hierarchy names.
        [SerializeField] private TMP_Text[] attributeValueTexts;

        public void Configure(GameObject upgrade, GameObject equip, GameObject progress, GameObject actions,
            TMP_Text name, TMP_Text description, TMP_Text lockLabel, HangarCardItem preview,
            TMP_Text[] attributeValues = null)
        {
            upgradeButton = upgrade;
            equipButton = equip;
            progressGroup = progress;
            actionGroup = actions;
            nameText = name;
            descriptionText = description;
            lockText = lockLabel;
            previewItem = preview;
            attributeValueTexts = attributeValues;
        }

        public void ShowPreview(HangarCardItem card)
        {
            if (card == null) return;
            if (nameText != null) nameText.text = card.CardName;
            if (descriptionText != null) descriptionText.text = card.CardDescription;
            if (lockText != null)
            {
                lockText.gameObject.SetActive(!card.IsUnlocked);
                lockText.text = $"Unlocked at Rank {card.UnlockRank}";
            }
            if (actionGroup != null) actionGroup.SetActive(card.IsUnlocked);
            if (previewItem != null)
            {
                previewItem.Configure(null, card.Kind, card.CardId, card.CardName, card.CardDescription,
                    card.Icon, card.LockedIcon, card.IsUnlocked, card.IsEquipped, card.UnlockRank, card.Level);
            }

            var snapshot = card.Snapshot;
            if (snapshot.Name != null)
            {
                ApplyDetailValues(snapshot);
                if (lockText != null) lockText.text = snapshot.Unlocked ? string.Empty : string.IsNullOrEmpty(snapshot.UnlockRequirementText) ? "Locked" : snapshot.UnlockRequirementText;
                if (upgradeButton != null) upgradeButton.SetActive(snapshot.Unlocked && !snapshot.IsMaxLevel && snapshot.Fragments >= snapshot.RequiredFragments);
                if (progressGroup != null) progressGroup.SetActive(snapshot.Unlocked && !snapshot.IsMaxLevel);
                if (equipButton != null) equipButton.SetActive(false);
                return;
            }
            // Original Refresh: btnUpgrade = isEnoughDebris; objSlider = !isEnoughDebris;
            // btnBattle = !IsCardEquipped(cardId).  The fixed original initial state has no debris.
            if (upgradeButton != null) upgradeButton.SetActive(false);
            if (progressGroup != null) progressGroup.SetActive(card.IsUnlocked);
            if (equipButton != null) equipButton.SetActive(card.IsUnlocked && !card.IsEquipped);
        }

        private void ApplyDetailValues(HangarItemSnapshot snapshot)
        {
            if (snapshot.DetailValues == null || snapshot.DetailValues.Length == 0) return;
            if (attributeValueTexts == null) return;
            for (int index = 0; index < attributeValueTexts.Length; index++)
            {
                TMP_Text text = attributeValueTexts[index];
                if (text == null || index >= snapshot.DetailValues.Length) continue;
                text.text = snapshot.DetailValues[index];
            }
        }
    }
}
