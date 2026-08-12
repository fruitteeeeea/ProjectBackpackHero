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
            for (var index = 0; index < entries.Length; index++)
            {
                var row = Instantiate(itemTemplate, content);
                row.name = $"RankRow_Runtime_{index + 1:00}";
                var rect = row.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(0f, -rowHeight * index - rowHeight * .5f);
                row.gameObject.SetActive(true);
                row.Bind(index + 1, entries[index]);
                spawnedRows.Add(row);
            }
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(scrollRect.viewport.rect.height, entries.Length * rowHeight));
            content.anchoredPosition = Vector2.zero;
        }

        private IEnumerator CenterCurrentPlayerAfterLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var selfIndex = -1;
            for (var i = 0; i < entries.Length; i++)
                if (entries[i] != null && entries[i].isCurrentPlayer) { selfIndex = i; break; }
            if (scrollRect == null || selfIndex < 0) yield break;

            var contentHeight = Mathf.Max(scrollRect.viewport.rect.height, entries.Length * rowHeight);
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
