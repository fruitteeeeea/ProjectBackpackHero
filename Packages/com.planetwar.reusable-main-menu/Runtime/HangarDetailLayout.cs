using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>
    /// Applies the original UICardInfo/UICardSpell bottom-action visibility contract to the
    /// data-free preview: an unlocked card without enough fragments shows Equip + progress,
    /// never Upgrade and progress at the same time.
    /// </summary>
    public sealed class HangarDetailLayout : MonoBehaviour
    {
        private static TMP_FontAsset cjkFallbackFont;
        private static bool resolvedCjkFallbackFont;
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
        [SerializeField] private TMP_Text[] attributeLabelTexts;
        [SerializeField] private Slider progressionSlider;
        [SerializeField] private TMP_Text progressionText;

        public void Configure(GameObject upgrade, GameObject equip, GameObject progress, GameObject actions,
            TMP_Text name, TMP_Text description, TMP_Text lockLabel, HangarCardItem preview,
            TMP_Text[] attributeValues = null, TMP_Text[] attributeLabels = null)
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
            attributeLabelTexts = attributeLabels;
        }

        public void ShowPreview(HangarCardItem card)
        {
            if (card == null) return;
            ResolveTextReferencesIfMissing();
            HangarItemSnapshot snapshot = card.Snapshot;
            // Dynamic cards retain their authoritative values in the snapshot. This avoids a
            // stale serialized CardName/CardDescription from blanking the detail header.
            string displayName = !string.IsNullOrEmpty(snapshot.Name) ? snapshot.Name : card.CardName;
            string description = !string.IsNullOrEmpty(snapshot.Description) ? snapshot.Description : card.CardDescription;
            if (nameText != null)
            {
                nameText.gameObject.SetActive(true);
                ApplyCjkFallbackIfNeeded(nameText, displayName);
                nameText.text = displayName;
            }
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                ApplyCjkFallbackIfNeeded(descriptionText, description);
                descriptionText.text = description;
            }
            if (lockText != null)
            {
                lockText.gameObject.SetActive(!card.IsUnlocked);
                lockText.text = $"Unlocked at Rank {card.UnlockRank}";
            }
            if (actionGroup != null) actionGroup.SetActive(card.IsUnlocked);
            if (previewItem != null)
            {
                // Match the source UICardInfo -> ItemCard.Init path: the detail preview uses
                // the same card presentation configuration as the card that opened it.
                previewItem.Configure(card.Owner, card.Kind, card.CardId, card.CardName, card.CardDescription,
                    card.Icon, card.LockedIcon, card.IsUnlocked, card.IsEquipped, card.UnlockRank, card.Level);
            }

            if (snapshot.Name != null)
            {
                ApplyDetailAttributes(snapshot);
                if (lockText != null) lockText.text = snapshot.Unlocked ? string.Empty : string.IsNullOrEmpty(snapshot.UnlockRequirementText) ? "Locked" : snapshot.UnlockRequirementText;
                if (upgradeButton != null) upgradeButton.SetActive(snapshot.Unlocked && !snapshot.IsMaxLevel && snapshot.Fragments >= snapshot.RequiredFragments);
                if (progressGroup != null) progressGroup.SetActive(snapshot.Unlocked && !snapshot.IsMaxLevel);
                ApplyProgression(snapshot);
                if (equipButton != null) equipButton.SetActive(false);
                return;
            }
            // Original Refresh: btnUpgrade = isEnoughDebris; objSlider = !isEnoughDebris;
            // btnBattle = !IsCardEquipped(cardId).  The fixed original initial state has no debris.
            if (upgradeButton != null) upgradeButton.SetActive(false);
            if (progressGroup != null) progressGroup.SetActive(card.IsUnlocked);
            if (equipButton != null) equipButton.SetActive(card.IsUnlocked && !card.IsEquipped);
        }

        private void ApplyProgression(HangarItemSnapshot snapshot)
        {
            if (progressionSlider == null && progressGroup != null)
                progressionSlider = progressGroup.GetComponentInChildren<Slider>(true);
            if (progressionText == null && progressGroup != null)
                progressionText = progressGroup.GetComponentInChildren<TMP_Text>(true);
            if (progressionSlider != null)
                progressionSlider.value = snapshot.RequiredFragments <= 0 ? 0f :
                    Mathf.Clamp01(snapshot.Fragments / (float)snapshot.RequiredFragments);
            if (progressionText != null)
                progressionText.text = snapshot.IsMaxLevel ? "Max" :
                    $"{snapshot.Fragments}/{snapshot.RequiredFragments}";
        }

        private void ApplyDetailAttributes(HangarItemSnapshot snapshot)
        {
            if (snapshot.DetailAttributes == null || snapshot.DetailAttributes.Length == 0) return;
            if (attributeValueTexts == null) return;
            for (int index = 0; index < attributeValueTexts.Length; index++)
            {
                TMP_Text text = attributeValueTexts[index];
                if (text == null) continue;
                bool visible = index < snapshot.DetailAttributes.Length && snapshot.DetailAttributes[index].Visible;
                if (text.transform.parent != null) text.transform.parent.gameObject.SetActive(visible);
                if (!visible) continue;
                HangarDetailAttribute attribute = snapshot.DetailAttributes[index];
                text.text = attribute.Value;
                TMP_Text label = attributeLabelTexts != null && index < attributeLabelTexts.Length
                    ? attributeLabelTexts[index]
                    : null;
                if (label == null) label = FindSiblingLabel(text);
                if (label != null) label.text = attribute.Label;
            }
        }

        // The imported UICardInfo/UICardSpell each contain a preview ItemCard, whose child
        // labels use the same names as the panel header. Resolve only missing references and
        // never select text inside that preview (or the skill-list template).
        private void ResolveTextReferencesIfMissing()
        {
            if (nameText == null) nameText = FindDetailText("name");
            if (descriptionText == null) descriptionText = FindDetailText("desc");
        }

        private TMP_Text FindDetailText(string nodeName)
        {
            foreach (TMP_Text candidate in GetComponentsInChildren<TMP_Text>(true))
            {
                if (candidate == null || candidate.gameObject.name != nodeName ||
                    IsUnder(candidate.transform, "ItemCard") ||
                    IsUnder(candidate.transform, "SkillScroll")) continue;
                return candidate;
            }
            return null;
        }

        private static bool IsUnder(Transform item, string ancestorName)
        {
            for (Transform current = item.parent; current != null; current = current.parent)
                if (current.name == ancestorName || current.name == ancestorName + " (1)") return true;
            return false;
        }

        // The imported Hangar font was authored for PlanetWar's English EnName/EnDesc fields.
        // BackpackHero supplies Chinese ItemData values, so use a Windows CJK font in the editor
        // and Windows builds when the current TMP asset cannot render the content.
        private static void ApplyCjkFallbackIfNeeded(TMP_Text text, string content)
        {
            if (text == null || string.IsNullOrEmpty(content) || !ContainsCjk(content) ||
                text.font == null || text.font.HasCharacter(content[0])) return;
            TMP_FontAsset fallback = GetCjkFallbackFont();
            if (fallback != null) text.font = fallback;
        }

        private static TMP_FontAsset GetCjkFallbackFont()
        {
            if (resolvedCjkFallbackFont) return cjkFallbackFont;
            resolvedCjkFallbackFont = true;
            Font bundledFont = Resources.Load<Font>("Fonts/NotoSansSC-VF");
            if (bundledFont != null)
            {
                cjkFallbackFont = TMP_FontAsset.CreateFontAsset(bundledFont);
                return cjkFallbackFont;
            }
            foreach (string fontName in Font.GetOSInstalledFontNames())
            {
                if (!fontName.Contains("Noto Sans SC") && !fontName.Contains("Microsoft YaHei") &&
                    !fontName.Contains("SimHei")) continue;
                Font font = Font.CreateDynamicFontFromOSFont(fontName, 32);
                if (font != null)
                {
                    cjkFallbackFont = TMP_FontAsset.CreateFontAsset(font);
                    break;
                }
            }
            return cjkFallbackFont;
        }

        private static bool ContainsCjk(string value)
        {
            foreach (char character in value)
                if (character >= '\u4e00' && character <= '\u9fff') return true;
            return false;
        }

        // Older baked MainMenu prefabs did not serialize attributeLabelTexts. Their label is
        // a sibling of an ancestor (not necessarily the value's direct parent), so recover the
        // closest pre-authored text label without requiring existing prefabs to be rebaked.
        private static TMP_Text FindSiblingLabel(TMP_Text value)
        {
            for (Transform row = value.transform.parent; row != null; row = row.parent)
            {
                TMP_Text closest = null;
                foreach (TMP_Text candidate in row.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (candidate == value || candidate.name == "val" ||
                        string.IsNullOrWhiteSpace(candidate.text)) continue;
                    closest = candidate;
                    break;
                }
                if (closest != null) return closest;
            }
            return null;
        }
    }
}
