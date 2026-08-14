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
        [SerializeField] private TMP_Text levelPrefixText;
        [SerializeField] private TMP_Text levelValueText;

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
        public HangarView Owner => hangar;
        public int DeckSlot { get; private set; } = -1;
        public bool IsDeckSlot => DeckSlot >= 0;
        public bool IsEmptyDeckSlot { get; private set; }
        public HangarItemSnapshot Snapshot { get; private set; }

        public void Configure(HangarView owner, HangarItemSnapshot snapshot, int deckSlot = -1)
        {
            Snapshot = snapshot;
            DeckSlot = deckSlot;
            IsEmptyDeckSlot = false;
            Configure(owner,
                snapshot.Kind == HangarItemKind.Equipment ? CardKind.Spell : CardKind.Entity,
                0, snapshot.Name, snapshot.Description, snapshot.Icon, snapshot.LockedIcon,
                snapshot.Unlocked, false, 0, snapshot.Level);
        }

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

        public void Configure(HangarItemSnapshot snapshot)
        {
            Configure(hangar, snapshot);
        }

        public void ConfigureEmptyDeckSlot(HangarView owner, int deckSlot, CardKind kind)
        {
            hangar = owner;
            DeckSlot = deckSlot;
            IsEmptyDeckSlot = true;
            Snapshot = default;
            cardKind = kind;
            cardId = 0;
            cardName = string.Empty;
            cardDescription = string.Empty;
            icon = null;
            lockedIcon = null;
            unlocked = true;
            equipped = false;
            level = 0;
            ApplyVisual();
        }

        public void SetSwapHighlight(bool enabled)
        {
            StopAllCoroutines();
            transform.localRotation = Quaternion.identity;
            if (enabled) StartCoroutine(SwapBreath());
        }

        private System.Collections.IEnumerator SwapBreath()
        {
            while (true)
            {
                yield return RotateTo(-5f);
                yield return RotateTo(5f);
            }
        }

        private System.Collections.IEnumerator RotateTo(float target)
        {
            float elapsed = 0f;
            float start = transform.localEulerAngles.z;
            if (start > 180f) start -= 360f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(start, target, elapsed / 0.5f));
                yield return null;
            }
        }

        public void ConfigureOriginalLabels(TMP_Text prefix, TMP_Text value)
        {
            levelPrefixText = prefix;
            levelValueText = value;
            ApplyVisual();
        }

        private void OnEnable() => ApplyVisual();

        public void ApplyVisual()
        {
            var normal = Find(transform, "icon")?.GetComponent<Image>();
            var grey = Find(transform, "icon (1)")?.GetComponent<Image>();
            if (normal != null) { normal.sprite = icon; normal.gameObject.SetActive(!IsEmptyDeckSlot); }
            if (grey != null) { grey.sprite = lockedIcon != null ? lockedIcon : icon; grey.gameObject.SetActive(!unlocked); }
            Image background = FindCardBackground();
            if (background != null)
            {
                ImageLoader backgroundLoader = background.GetComponent<ImageLoader>();
                int styleIndex = cardKind == CardKind.Entity ? 0 : 1;
                if (backgroundLoader != null && backgroundLoader.sprites != null &&
                    styleIndex < backgroundLoader.sprites.Length)
                {
                    // Match the source ItemCard.Refresh() contract: the card's own
                    // ImageLoader owns its blue / purple / orange card-art variants.
                    backgroundLoader.Select(styleIndex);
                    background.color = Color.white;
                }
                else if (Snapshot.Name != null) background.color = Snapshot.BackgroundColor;
            }

            // Original ItemCardEquip nests both the grey icon and its background under "Lock".
            // "Lockbg" is only one child, so it cannot be toggled on its own.
            var lockRoot = Find(transform, "Lock");
            if (lockRoot != null) lockRoot.gameObject.SetActive(!unlocked && !IsEmptyDeckSlot);
            var lockBackground = Find(transform, "Lockbg");
            if (lockBackground != null) lockBackground.gameObject.SetActive(!unlocked && !IsEmptyDeckSlot);
            SetText("name", cardName);
            // Original ItemCard uses two separate left-top labels: "lv" is the authored
            // prefix ("Lv"), while "level" is ItemCard.textLevel and receives the number.
            // These references are the direct equivalents of the original ItemCard's authored
            // "lv" prefix and textLevel fields. They are serialized by the Builder, rather than
            // selected from any similarly named nested text object.
            if (levelPrefixText != null) levelPrefixText.text = IsEmptyDeckSlot ? string.Empty : "Lv";
            if (levelValueText != null) levelValueText.text = IsEmptyDeckSlot ? string.Empty : level.ToString();
            var rank = lockRoot != null ? lockRoot.GetComponentInChildren<TMP_Text>(true) : null;
            if (rank != null) rank.text = $"Rank {unlockRank}";
            var slider = Find(transform, "Slider");
            if (slider != null) slider.gameObject.SetActive(unlocked && !IsEmptyDeckSlot);

            // Target ItemCard.Refresh() enables this only for a Spell before its first tutorial
            // mission starts. This project has no equivalent mission/onboarding state, so guide
            // must remain off; otherwise it covers every equipment card's art.
            var guide = Find(transform, "guide");
            if (guide != null) guide.gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) ApplyVisual();
        }

        // Bound through the original card button's serialized m_OnClick list.
        public void onClickItem()
        {
            hangar?.HandleCardClick(this);
        }

        private void SetText(string parentName, string value)
        {
            var parent = Find(transform, parentName);
            var text = parent != null ? parent.GetComponentInChildren<TMP_Text>(true) : null;
            if (text != null) text.text = value;
        }

        // Original ItemCard's background Image owns ImageLoader.sprites. The direct Image
        // child is that layer for Deck, Collection and detail-preview templates.
        private Image FindCardBackground()
        {
            Image background = transform.Find("Image")?.GetComponent<Image>();
            if (background != null) return background;
            return Find(transform, "bg")?.GetComponent<Image>();
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
