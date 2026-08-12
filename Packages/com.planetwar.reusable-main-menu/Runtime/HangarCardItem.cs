using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>
    /// Serialized, data-free equivalent of the original ItemCard state.  The card shell and its
    /// UGUI Button are still the original prefabs; this component only supplies their fixed demo
    /// configuration until CardSystem is integrated.
    /// </summary>
    public sealed class HangarCardItem : MonoBehaviour
    {
        public enum CardKind { Entity, Spell }

        [SerializeField] private HangarView hangar;
        [SerializeField] private CardKind cardKind;
        [SerializeField] private int cardId;
        [SerializeField] private string cardName;
        [SerializeField, TextArea] private string cardDescription;
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite lockedIcon;
        [SerializeField] private int level = 1;
        [SerializeField] private int unlockRank;
        [SerializeField] private bool unlocked;
        [SerializeField] private bool equipped;

        public CardKind Kind => cardKind;
        public int CardId => cardId;
        public string CardName => cardName;
        public string CardDescription => cardDescription;
        public Sprite Icon => icon;
        public Sprite LockedIcon => lockedIcon;
        public int Level => level;
        public int UnlockRank => unlockRank;
        public bool IsUnlocked => unlocked;
        public bool IsEquipped => equipped;

        public void Configure(HangarView owner, CardKind kind, int id, string displayName,
            string description, Sprite normalIcon, Sprite greyIcon, bool isUnlocked,
            bool isEquipped, int requiredRank, int cardLevel = 1)
        {
            hangar = owner;
            cardKind = kind;
            cardId = id;
            cardName = displayName;
            cardDescription = description;
            icon = normalIcon;
            lockedIcon = greyIcon;
            unlocked = isUnlocked;
            equipped = isEquipped;
            unlockRank = requiredRank;
            level = cardLevel;
            ApplyVisual();
        }

        private void OnEnable() => ApplyVisual();

        public void ApplyVisual()
        {
            var normal = Find(transform, "icon")?.GetComponent<Image>();
            var grey = Find(transform, "icon (1)")?.GetComponent<Image>();
            if (normal != null) { normal.sprite = icon; normal.gameObject.SetActive(true); }
            if (grey != null) { grey.sprite = lockedIcon != null ? lockedIcon : icon; grey.gameObject.SetActive(!unlocked); }

            // Original ItemCardEquip nests both the grey icon and its background under "Lock".
            // "Lockbg" is only one child, so it cannot be toggled on its own.
            var lockRoot = Find(transform, "Lock");
            if (lockRoot != null) lockRoot.gameObject.SetActive(!unlocked);
            var lockBackground = Find(transform, "Lockbg");
            if (lockBackground != null) lockBackground.gameObject.SetActive(!unlocked);
            SetText("name", cardName);
            SetText("lv", level.ToString());
            var rank = lockRoot != null ? lockRoot.GetComponentInChildren<TMP_Text>(true) : null;
            if (rank != null) rank.text = $"Rank {unlockRank}";
            var slider = Find(transform, "Slider");
            if (slider != null) slider.gameObject.SetActive(unlocked);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) ApplyVisual();
        }

        // Bound through the original card button's serialized m_OnClick list.
        public void onClickItem()
        {
            if (cardKind == CardKind.Spell) hangar?.ShowSpellDetails(this);
            else hangar?.ShowEntityDetails(this);
        }

        private void SetText(string parentName, string value)
        {
            var parent = Find(transform, parentName);
            var text = parent != null ? parent.GetComponentInChildren<TMP_Text>(true) : null;
            if (text != null) text.text = value;
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
