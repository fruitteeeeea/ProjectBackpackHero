using PlanetWar.ReusableMainMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainMenuDemoBootstrap : MonoBehaviour
{
    private void Awake()
    {
        var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.matchWidthOrHeight = 0f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var menu = new GameObject("MainMenu", typeof(RectTransform), typeof(MainMenuView));
        menu.transform.SetParent(canvas.transform, false);
    }
}
