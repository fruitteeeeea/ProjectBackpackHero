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
        private int scrollToIndex = -1;
        private RankInfoSlider tempSliderRank;

        public void Show()
        {
            gameObject.SetActive(true);
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
                j++;
                if (item == null) continue;

                var previous = i > 0 ? entries[i - 1] : null;
                var next = i + 1 < entries.Length ? entries[i + 1] : null;
                bool isCurrent = false;

                if (entry.type == 0)
                {
                    var view = item.GetComponent<RankInfoItemMainView>();
                    if (view != null) isCurrent = view.SetItem(entry, previous, next, currentPlayerRankPoint, currentMissionId);
                    if (isCurrent) tempSliderRank = view != null ? view.sliderRank : null;
                }
                else
                {
                    var view = item.GetComponent<RankInfoItemRewardView>();
                    if (view != null) isCurrent = view.SetItem(entry, previous, next, currentPlayerRankPoint);
                    if (isCurrent) tempSliderRank = view != null ? view.sliderRank : null;
                }

                if (isCurrent)
                {
                    scrollToIndex = j - 1;
                    if (tempSliderRank != null) tempSliderRank.SetValue(0f, currentPlayerRankPoint);
                }
            }

            if (scrollToIndex < 0) scrollToIndex = entries.Length - 1;
            isScrollToIndex = true;
            StartCoroutine(CenterAfterLayout());
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
            if (scroll == null || btnOffset == null || scrollToIndex < 0) return;
            int current = scroll.GetCurrentScrollIndex();
            btnOffset.SetActive(Mathf.Abs(current - scrollToIndex) > 1);
        }
    }
}
