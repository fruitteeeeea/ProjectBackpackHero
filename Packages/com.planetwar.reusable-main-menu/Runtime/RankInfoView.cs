using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    public sealed class RankInfoView : MonoBehaviour
    {
        public RankInfoScrollView scroll;
        public GameObject itemRankMain;
        public GameObject itemRankReward;
        public GameObject btnOffset;
        public RankInfoEntry[] entries;
        public int currentMissionId = 1003;
        public int currentPlayerRankPoint = 220;

        public event Action CloseRequested;

        private bool isScrollToIndex;
        private bool needsRebind;
        private int scrollToIndex = -1;
        private RankInfoSlider tempSliderRank;

        /// <summary>Replaces prefab sample values with the active progression catalog while retaining its artwork.</summary>
        public void ApplyProgression(RankInfoEntry[] progressionEntries, int missionId, int playerPoints)
        {
            bool changed = missionId != currentMissionId || playerPoints != currentPlayerRankPoint || !SameEntries(entries, progressionEntries);
            if (progressionEntries != null && progressionEntries.Length > 0) entries = progressionEntries;
            currentMissionId = missionId;
            currentPlayerRankPoint = playerPoints;
            if (changed && isActiveAndEnabled) needsRebind = true;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            needsRebind = false;
            Bind();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnClickClose()
        {
            Hide();
            CloseRequested?.Invoke();
        }

        public void OnClickOffset()
        {
            if (scroll == null || scrollToIndex < 0) return;
            scroll.ScrollToIndex(scrollToIndex);
        }

        private void Bind()
        {
            scrollToIndex = -1;
            tempSliderRank = null;
            isScrollToIndex = false;
            if (btnOffset != null) btnOffset.SetActive(false);

            if (scroll == null || entries == null || entries.Length == 0)
            {
                Debug.LogWarning("[RankInfoView] Scroll or entries are not configured.");
                return;
            }

            if (itemRankMain != null) itemRankMain.SetActive(false);
            if (itemRankReward != null) itemRankReward.SetActive(false);

            var configs = new List<RankInfoScrollView.ItemCreateConfig>();
            for (int i = entries.Length - 1; i >= 0; i--)
            {
                configs.Add(new RankInfoScrollView.ItemCreateConfig
                {
                    itemTemplate = entries[i].type == 0 ? itemRankMain : itemRankReward,
                    createCount = 1
                });
            }
            scroll.InitScrollView(configs);

            int j = 0;
            for (int i = entries.Length - 1; i >= 0; i--)
            {
                var entry = entries[i];
                var item = scroll.GetItemByIndex(j);
                if (item == null) continue;

                var previous = i > 0 ? entries[i - 1] : null;
                var next = i + 1 < entries.Length ? entries[i + 1] : null;
                bool isCurrent = false;

                if (entry.type == 0)
                {
                    var view = item.GetComponent<RankInfoItemMainView>();
                    if (view != null) isCurrent = view.SetItem(entry, previous, next, currentPlayerRankPoint, currentMissionId);
                    if (isCurrent)
                    {
                        scrollToIndex = j;
                        if (tempSliderRank != null) tempSliderRank.SetValue(0f, currentPlayerRankPoint);
                        tempSliderRank = view != null ? view.sliderRank : null;
                    }
                }
                else
                {
                    var view = item.GetComponent<RankInfoItemRewardView>();
                    if (view != null) isCurrent = view.SetItem(entry, previous, next, currentPlayerRankPoint);
                    if (isCurrent)
                    {
                        scrollToIndex = j;
                        if (tempSliderRank != null) tempSliderRank.SetValue(0f, currentPlayerRankPoint);
                        tempSliderRank = view != null ? view.sliderRank : null;
                    }
                }

                j++;
            }

            if (scrollToIndex == -1) scrollToIndex = configs.Count - 1;
            isScrollToIndex = true;
            StartCoroutine(CenterAfterLayout());
        }

        private static bool SameEntries(RankInfoEntry[] current, RankInfoEntry[] incoming)
        {
            if (ReferenceEquals(current, incoming)) return true;
            if (current == null || incoming == null || current.Length != incoming.Length) return false;
            for (int i = 0; i < current.Length; i++)
            {
                RankInfoEntry a = current[i], b = incoming[i];
                if (a == null || b == null) { if (a != b) return false; continue; }
                if (a.id != b.id || a.score != b.score || a.type != b.type || a.level != b.level) return false;
            }
            return true;
        }

        private IEnumerator CenterAfterLayout()
        {
            while (scroll != null && scroll.IsLayoutRebuilding)
            {
                yield return null;
            }
            if (scroll != null && scrollToIndex >= 0)
            {
                scroll.ScrollToIndex(scrollToIndex, false);
            }
            isScrollToIndex = false;
        }

        private void Update()
        {
            if (needsRebind)
            {
                needsRebind = false;
                Bind();
                return;
            }
            if (scroll == null || btnOffset == null || scrollToIndex < 0) return;
            int current = scroll.GetCurrentScrollIndex();
            btnOffset.SetActive(Mathf.Abs(current - scrollToIndex) > 1);
        }
    }
}
