using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>
    /// Vertical irregular scroll list adapted from the original IrregularScrollView.
    /// Items are positioned manually after layout rebuild, with smooth indexed scrolling.
    /// </summary>
    public sealed class RankInfoScrollView : MonoBehaviour
    {
        public ScrollRect scrollRect;
        public RectTransform content;
        public RectTransform viewport;

        public float scrollDuration = 0.3f;
        public bool ignoreLayoutRebuild = true;
        public bool enableVirtualization = true;
        public float visibilityMargin = 500f;
        public float itemSpacing = 0f;

        private readonly Dictionary<int, GameObject> allItems = new Dictionary<int, GameObject>();
        private readonly List<float> itemLengths = new List<float>();
        private readonly List<float> itemPositions = new List<float>();
        private int totalItemCount;
        private bool isLayoutRebuilding;
        private int lastVisibleStart = -1;
        private int lastVisibleEnd = -1;

        public bool IsLayoutRebuilding => isLayoutRebuilding;

        [Serializable]
        public struct ItemCreateConfig
        {
            public GameObject itemTemplate;
            public int createCount;
        }

        private void Awake()
        {
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
            if (content == null && scrollRect != null) content = scrollRect.content;
            if (viewport == null && scrollRect != null) viewport = scrollRect.viewport;

            if (ignoreLayoutRebuild && content != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(content);
            }

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollChanged);
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }
        }

        public void InitScrollView(List<ItemCreateConfig> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                Debug.LogWarning("[RankInfoScrollView] Item creation config is empty.");
                return;
            }

            ClearAllItems();

            int globalIndex = 0;
            foreach (var config in configs)
            {
                if (config.itemTemplate == null || config.createCount <= 0) continue;
                for (int i = 0; i < config.createCount; i++)
                {
                    CreateSingleItem(config.itemTemplate, globalIndex);
                    globalIndex++;
                }
            }

            totalItemCount = globalIndex;
            ForceRebuildLayout();
        }

        private void CreateSingleItem(GameObject template, int globalIndex)
        {
            GameObject item = Instantiate(template, content);
            item.name = $"{template.name}_Index_{globalIndex}";
            var rt = item.GetComponent<RectTransform>();
            if (rt == null) rt = item.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            item.SetActive(true);
            allItems.Add(globalIndex, item);
        }

        private void ForceRebuildLayout()
        {
            isLayoutRebuilding = true;
            DOVirtual.DelayedCall(0.01f, () =>
            {
                UpdateAllItemLayouts();
                isLayoutRebuilding = false;
                UpdateVisibleItems();
            });
        }

        private void UpdateAllItemLayouts()
        {
            itemLengths.Clear();
            itemPositions.Clear();
            float acc = 0f;
            for (int i = 0; i < totalItemCount; i++)
            {
                if (allItems.TryGetValue(i, out var go) && go != null)
                {
                    var rt = go.GetComponent<RectTransform>();
                    float len = rt != null ? rt.rect.height : 0f;
                    itemLengths.Add(len);
                    itemPositions.Add(acc);
                    acc += len;
                    if (i < totalItemCount - 1) acc += itemSpacing;
                }
                else
                {
                    itemLengths.Add(0f);
                    itemPositions.Add(acc);
                }
            }

            if (content != null && viewport != null)
            {
                var size = content.sizeDelta;
                size.y = acc;
                size.x = viewport.rect.width;
                content.sizeDelta = size;
            }

            for (int i = 0; i < totalItemCount; i++)
            {
                if (allItems.TryGetValue(i, out var go) && go != null)
                {
                    var rt = go.GetComponent<RectTransform>();
                    if (rt == null) continue;
                    float pos = i < itemPositions.Count ? itemPositions[i] : 0f;
                    rt.anchoredPosition = new Vector2(0f, -pos);
                }
            }
        }

        public void ClearAllItems()
        {
            foreach (var kvp in allItems)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            allItems.Clear();
            itemLengths.Clear();
            itemPositions.Clear();
            totalItemCount = 0;
            lastVisibleStart = -1;
            lastVisibleEnd = -1;
            if (content != null) content.anchoredPosition = Vector2.zero;
        }

        public void ScrollToIndex(int targetIndex, bool isSmooth = true)
        {
            if (targetIndex < 0 || targetIndex >= totalItemCount)
            {
                Debug.LogWarning($"[RankInfoScrollView] Index {targetIndex} is out of range ({totalItemCount}).");
                return;
            }
            if (isLayoutRebuilding) return;

            float target = targetIndex < itemPositions.Count ? itemPositions[targetIndex] : 0f;
            target -= viewport != null ? viewport.rect.height * 0.5f : 0f;
            float maxY = content != null && viewport != null ? Mathf.Max(0f, content.rect.height - viewport.rect.height) : 0f;
            target = Mathf.Clamp(target, 0f, maxY);
            Vector2 targetPos = new Vector2(content.anchoredPosition.x, target);

            if (isSmooth && scrollDuration > 0f && !DOTween.IsTweening(content))
            {
                DOTween.To(
                        () => content.anchoredPosition,
                        value => content.anchoredPosition = value,
                        targetPos,
                        scrollDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
            else
            {
                content.anchoredPosition = targetPos;
            }
        }

        public int GetCurrentScrollIndex()
        {
            if (itemPositions.Count == 0 || content == null) return -1;

            float currentPos = Mathf.Abs(content.anchoredPosition.y);
            if (viewport != null) currentPos += viewport.rect.height * 0.5f;

            int index = itemPositions.BinarySearch(currentPos);
            if (index < 0) index = ~index;
            if (index >= itemPositions.Count) index = itemPositions.Count - 1;
            if (index < 0) index = 0;
            return index;
        }

        public GameObject GetItemByIndex(int index)
        {
            if (index < 0 || index >= totalItemCount) return null;
            allItems.TryGetValue(index, out var item);
            return item;
        }

        public int GetTotalItemCount() => totalItemCount;

        private void OnScrollChanged(Vector2 pos)
        {
            if (!enableVirtualization || isLayoutRebuilding) return;
            UpdateVisibleItems();
        }

        private void UpdateVisibleItems()
        {
            if (!enableVirtualization || viewport == null || content == null) return;
            if (itemLengths == null || itemLengths.Count == 0) return;

            float offset = content.anchoredPosition.y;
            float viewMin = Mathf.Max(0f, offset - visibilityMargin);
            float viewMax = offset + viewport.rect.height + visibilityMargin;

            int startIndex = itemPositions.BinarySearch(viewMin);
            if (startIndex < 0) startIndex = ~startIndex;
            if (startIndex > 0) startIndex--;
            startIndex = Mathf.Clamp(startIndex, 0, totalItemCount - 1);
            while (startIndex > 0)
            {
                float start = itemPositions[startIndex];
                float end = start + (startIndex < itemLengths.Count ? itemLengths[startIndex] : 0f);
                if (end < viewMin) { startIndex++; break; }
                startIndex--;
            }

            int endIndex = itemPositions.BinarySearch(viewMax);
            if (endIndex < 0) endIndex = ~endIndex;
            endIndex = Mathf.Clamp(endIndex, 0, totalItemCount - 1);
            while (endIndex < totalItemCount - 1)
            {
                float start = itemPositions[endIndex];
                if (start > viewMax) { endIndex--; break; }
                endIndex++;
            }
            if (endIndex < startIndex) endIndex = startIndex;

            if (lastVisibleStart == -1)
            {
                for (int i = 0; i < totalItemCount; i++)
                {
                    bool visible = i >= startIndex && i <= endIndex;
                    if (allItems.TryGetValue(i, out var go) && go != null && go.activeSelf != visible)
                        go.SetActive(visible);
                }
                lastVisibleStart = startIndex;
                lastVisibleEnd = endIndex;
                return;
            }

            for (int i = lastVisibleStart; i <= lastVisibleEnd; i++)
            {
                if (i < startIndex || i > endIndex)
                {
                    if (allItems.TryGetValue(i, out var go) && go != null && go.activeSelf)
                        go.SetActive(false);
                }
            }
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (i < lastVisibleStart || i > lastVisibleEnd)
                {
                    if (allItems.TryGetValue(i, out var go) && go != null && !go.activeSelf)
                        go.SetActive(true);
                }
            }

            lastVisibleStart = startIndex;
            lastVisibleEnd = endIndex;
        }

        private void OnDisable()
        {
            if (content != null) DOTween.Kill(content);
        }

        private void OnDestroy()
        {
            ClearAllItems();
        }
    }
}
