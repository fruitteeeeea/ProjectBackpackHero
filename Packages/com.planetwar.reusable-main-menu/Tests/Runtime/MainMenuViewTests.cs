using NUnit.Framework;
using PlanetWar.ReusableMainMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainMenuViewTests
{
    [Test]
    public void MenuBuildsAndEmitsSemanticAction()
    {
        var menu = new GameObject("Menu", typeof(RectTransform), typeof(MainMenuActionRelay), typeof(MainMenuView)).GetComponent<MainMenuView>();
        var view = new GameObject("UIMain", typeof(UIMain)).GetComponent<UIMain>();
        view.transform.SetParent(menu.transform, false);
        MainMenuAction received = default;
        menu.ActionInvoked += action => received = action;
        view.OnClickConfirm();
        Assert.AreEqual(MainMenuAction.Start, received);
        Object.DestroyImmediate(menu.gameObject);
    }
}
