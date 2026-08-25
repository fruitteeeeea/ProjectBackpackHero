using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>
    /// Uses the original RankPage item template and creates only its leaderboard entries at runtime.
    /// The page chrome, title, currency bar, viewport and item template remain authored in the prefab.
    /// </summary>
    public sealed class RanksView : MonoBehaviour
    {
        [SerializeField] private TMP_Text arenaTitle;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RankRowView itemTemplate;
        [SerializeField] private RankEntry[] entries;
        [SerializeField] private float rowHeight = 100f;
        [Header("Shared top currency HUD")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text diamondText;
        [SerializeField] private TMP_Text battleGoldText;
        [SerializeField] private TMP_Text battleDiamondText;

        private readonly List<RankRowView> spawnedRows = new List<RankRowView>();
        private RankEntry[] displayEntries = System.Array.Empty<RankEntry>();

        /// <summary>Supplies the complete persistent leaderboard without changing the authored row template.</summary>
        public void SetEntries(IEnumerable<RankEntry> value)
        {
            displayEntries = value == null
                ? System.Array.Empty<RankEntry>()
                : System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(value, entry => entry != null));
        }

        /// <summary>Returns the six authored country flags used by the original Ranks page.</summary>
        public Sprite GetCountryFlag(int countryIndex)
        {
            if (entries == null || entries.Length == 0) return null;
            var flags = new List<Sprite>();
            foreach (var entry in entries)
                if (entry != null && entry.countryFlag != null && !flags.Contains(entry.countryFlag)) flags.Add(entry.countryFlag);
            return flags.Count == 0 ? null : flags[Mathf.Abs(countryIndex) % flags.Count];
        }

        public void Show()
        {
            gameObject.SetActive(true);
            SyncCurrencyHud();
            Bind();
            StartCoroutine(CenterCurrentPlayerAfterLayout());
        }

        public void Bind()
        {
            if (arenaTitle != null) arenaTitle.text = "Arena 3";
            ClearSpawnedRows();
            if (itemTemplate == null || scrollRect == null || scrollRect.content == null) return;

            itemTemplate.gameObject.SetActive(false);
            var content = scrollRect.content;
            // SetEntries is used by the persistent progression system. Fall back to the
            // original serialized list so this view remains usable in isolation.
            if (displayEntries == null || displayEntries.Length == 0)
                displayEntries = entries == null
                    ? System.Array.Empty<RankEntry>()
                    : System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(entries, entry => entry != null));
            displayEntries = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OrderByDescending(displayEntries, entry => entry.score));
            for (var index = 0; index < displayEntries.Length; index++)
            {
                var row = Instantiate(itemTemplate, content);
                row.name = $"RankRow_Runtime_{index + 1:00}";
                var rect = row.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(0f, -rowHeight * index - rowHeight * .5f);
                row.gameObject.SetActive(true);
                row.Bind(index + 1, displayEntries[index]);
                spawnedRows.Add(row);
            }
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(scrollRect.viewport.rect.height, displayEntries.Length * rowHeight));
            content.anchoredPosition = Vector2.zero;
        }

        private IEnumerator CenterCurrentPlayerAfterLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var selfIndex = -1;
            for (var i = 0; i < displayEntries.Length; i++)
                if (displayEntries[i].isCurrentPlayer) { selfIndex = i; break; }
            if (scrollRect == null || selfIndex < 0) yield break;

            var contentHeight = Mathf.Max(scrollRect.viewport.rect.height, displayEntries.Length * rowHeight);
            var maxOffset = Mathf.Max(0f, contentHeight - scrollRect.viewport.rect.height);
            var desiredOffset = Mathf.Clamp(selfIndex * rowHeight - scrollRect.viewport.rect.height * .5f + rowHeight * .5f, 0f, maxOffset);
            scrollRect.content.anchoredPosition = new Vector2(0f, desiredOffset);
        }

        private void ClearSpawnedRows()
        {
            foreach (var row in spawnedRows)
                if (row != null) Destroy(row.gameObject);
            spawnedRows.Clear();
        }

        private void SyncCurrencyHud()
        {
            if (goldText != null && battleGoldText != null) goldText.text = battleGoldText.text;
            if (diamondText != null && battleDiamondText != null) diamondText.text = battleDiamondText.text;
        }

        private void OnDestroy() => ClearSpawnedRows();
    }
}
