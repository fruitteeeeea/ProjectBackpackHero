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
        // These are the direct equivalents of the source ItemCard.sliderUpgrade,
        // textUpDebris and objUpgrade fields. The prefab builder serializes them from the
        // original card hierarchy so card progression never depends on ambiguous name lookup.
        [SerializeField] private Slider progressionSlider;
        [SerializeField] private TMP_Text fragmentProgressText;
        [SerializeField] private GameObject upgradeIndicator;
        private Button cardButton;

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
                snapshot.Unlocked, deckSlot >= 0, 0, snapshot.Level);
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
            BindCardButton();
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
            BindCardButton();
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

        public void ConfigureProgressionPresentation(Slider slider, TMP_Text progressText,
            GameObject indicator)
        {
            progressionSlider = slider;
            fragmentProgressText = progressText;
            upgradeIndicator = indicator;
            ApplyVisual();
        }

        private void OnEnable()
        {
            BindCardButton();
            ApplyVisual();
        }

        private void BindCardButton()
        {
            // The imported original ItemCard prefabs retain serialized callbacks to their
            // source-card components. Those targets can be a detail-preview card instead of
            // the visible runtime card, which opens the panel with blank placeholder data.
            // Replace the event on the card's actual clickable Image with this instance.
            cardButton ??= transform.Find("Image")?.GetComponent<Button>();
            cardButton ??= GetComponentInChildren<Button>(true);
            if (cardButton == null) return;

            cardButton.onClick = new Button.ButtonClickedEvent();
            cardButton.onClick.AddListener(onClickItem);
        }

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
            ApplyProgressionPresentation();

            // Target ItemCard.Refresh() enables this only for a Spell before its first tutorial
            // mission starts. This project has no equivalent mission/onboarding state, so guide
            // must remain off; otherwise it covers every equipment card's art.
            var guide = Find(transform, "guide");
            if (guide != null) guide.gameObject.SetActive(false);
        }

        private void ApplyProgressionPresentation()
        {
            ResolveProgressionPresentationIfMissing();
            // Static builder placeholders do not carry a live item snapshot. Preserve their
            // authored slider/indicator state until HangarView binds the runtime item data.
            if (Snapshot.Name == null)
            {
                if (progressionSlider != null)
                    progressionSlider.gameObject.SetActive(unlocked && !IsEmptyDeckSlot);
                if (upgradeIndicator != null && IsEmptyDeckSlot)
                    upgradeIndicator.SetActive(false);
                return;
            }

            bool showProgress = Snapshot.Unlocked && !IsEmptyDeckSlot;
            if (progressionSlider != null)
            {
                progressionSlider.gameObject.SetActive(showProgress);
                if (showProgress)
                    progressionSlider.value = Snapshot.IsMaxLevel ? 1f :
                        Snapshot.RequiredFragments > 0
                            ? Mathf.Clamp01(Snapshot.Fragments / (float)Snapshot.RequiredFragments)
                            : 0f;
            }
            if (fragmentProgressText != null && showProgress)
                fragmentProgressText.text = Snapshot.IsMaxLevel ? "Max" :
                    $"{Snapshot.Fragments}/{Snapshot.RequiredFragments}";

            // Match ItemCard.Refresh(): this marker is a fragment-sufficiency cue only.
            // Detail-page Upgrade availability continues to evaluate its complete conditions.
            if (upgradeIndicator != null)
                upgradeIndicator.SetActive(showProgress && !Snapshot.IsMaxLevel &&
                    Snapshot.RequiredFragments > 0 && Snapshot.Fragments >= Snapshot.RequiredFragments);
        }

        // Old baked MainMenu prefabs predate the serialized fields above. Resolve only the
        // original ItemCard's direct Slider hierarchy as a compatibility path; newly rebuilt
        // cards receive all three references from RanksPrefabBuilder.
        private void ResolveProgressionPresentationIfMissing()
        {
            Transform sliderRoot = progressionSlider != null ? progressionSlider.transform :
                transform.Find("Slider");
            if (sliderRoot == null) return;
            progressionSlider ??= sliderRoot.GetComponent<Slider>();
            fragmentProgressText ??= sliderRoot.GetComponentInChildren<TMP_Text>(true);
            upgradeIndicator ??= sliderRoot.Find("Image (2)")?.gameObject;
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
