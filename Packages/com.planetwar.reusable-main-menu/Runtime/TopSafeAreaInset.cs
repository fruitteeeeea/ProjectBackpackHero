using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>
    /// Moves only the top HUD by the minimum amount needed to clear a notch.
    /// It intentionally does not resize the main content area.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class TopSafeAreaInset : MonoBehaviour
    {
        private RectTransform target;
        private Rect lastSafeArea;
        private Vector2 originalPosition;
        private bool initialized;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea) Apply();
        }

        private void Apply()
        {
            target = target == null ? transform as RectTransform : target;
            if (target == null) return;
            if (!initialized)
            {
                originalPosition = target.anchoredPosition;
                initialized = true;
            }

            target.anchoredPosition = originalPosition;
            Canvas.ForceUpdateCanvases();
            var canvas = target.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var highestGraphicY = float.NegativeInfinity;
            foreach (var graphic in target.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.gameObject.activeInHierarchy) continue;
                var corners = new Vector3[4];
                graphic.rectTransform.GetWorldCorners(corners);
                for (var i = 0; i < corners.Length; i++)
                    highestGraphicY = Mathf.Max(highestGraphicY, RectTransformUtility.WorldToScreenPoint(camera, corners[i]).y);
            }

            if (!float.IsNegativeInfinity(highestGraphicY) && highestGraphicY > Screen.safeArea.yMax)
            {
                var scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
                target.anchoredPosition = originalPosition - new Vector2(0f, (highestGraphicY - Screen.safeArea.yMax) / scale);
            }
            lastSafeArea = Screen.safeArea;
        }
    }
}
