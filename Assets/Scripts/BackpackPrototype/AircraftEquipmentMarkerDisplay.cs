using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    /// <summary>
    /// 在飞机血条右侧显示临近装备的调试颜色标识。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AircraftEquipmentMarkerDisplay :
        MonoBehaviour
    {
        private const float MarkerSize = 16f;
        private const float MarkerSpacing = 4f;
        private const float RightOffset = 102f;

        private readonly List<Image> markers = new();
        private RectTransform container;

        public int MarkerCount => markers.Count;

        public void SetColors(IReadOnlyList<Color> colors)
        {
            EnsureContainer();

            if (container == null)
            {
                return;
            }

            int colorCount = colors?.Count ?? 0;
            EnsureMarkerCount(colorCount);

            for (int index = 0; index < markers.Count; index++)
            {
                Image marker = markers[index];
                bool isActive = index < colorCount;

                marker.gameObject.SetActive(isActive);

                if (!isActive)
                {
                    continue;
                }

                marker.color = colors[index];
                ((RectTransform)marker.transform).anchoredPosition =
                    new Vector2(
                        index * (MarkerSize + MarkerSpacing),
                        0f);
            }

            container.gameObject.SetActive(colorCount > 0);
        }

        private void EnsureContainer()
        {
            if (container != null)
            {
                return;
            }

            Canvas healthBarCanvas =
                GetComponentInChildren<Canvas>(true);

            if (healthBarCanvas == null)
            {
                Debug.LogWarning(
                    "飞机没有血条 Canvas，无法显示装备颜色标识。",
                    this);
                return;
            }

            var containerObject = new GameObject(
                "Equipment Color Markers",
                typeof(RectTransform));
            container = containerObject.GetComponent<RectTransform>();
            container.SetParent(healthBarCanvas.transform, false);
            container.anchorMin = new Vector2(0.5f, 0.5f);
            container.anchorMax = new Vector2(0.5f, 0.5f);
            container.pivot = new Vector2(0f, 0.5f);
            container.anchoredPosition =
                new Vector2(RightOffset, 0f);
            container.sizeDelta =
                new Vector2(MarkerSize, MarkerSize);
        }

        private void EnsureMarkerCount(int requiredCount)
        {
            while (markers.Count < requiredCount)
            {
                var markerObject = new GameObject(
                    "Equipment Color Marker",
                    typeof(RectTransform),
                    typeof(Image));
                RectTransform markerTransform =
                    markerObject.GetComponent<RectTransform>();
                markerTransform.SetParent(container, false);
                markerTransform.anchorMin = new Vector2(0f, 0.5f);
                markerTransform.anchorMax = new Vector2(0f, 0.5f);
                markerTransform.pivot = new Vector2(0.5f, 0.5f);
                markerTransform.sizeDelta =
                    new Vector2(MarkerSize, MarkerSize);

                Image marker = markerObject.GetComponent<Image>();
                // Unity 6 no longer ships the legacy UI/Skin/UISprite.psd
                // resource. A sprite-less Image uses UGUI's white texture,
                // which is exactly what these runtime-tinted markers need.
                marker.sprite = null;
                marker.type = Image.Type.Simple;
                markers.Add(marker);
            }
        }
    }
}
