using UnityEngine;
using PlanetWar.ReusableMainMenu;

public class UIMain : MonoBehaviour
{
    public void OnClickConfirm() => Relay(MainMenuAction.Start);
    public void OnClickSet() => Relay(MainMenuAction.Settings);
    public void OnClickHead() => Relay(MainMenuAction.Profile);
    public void OnClickRank() => Relay(MainMenuAction.Rank);
    private void Relay(MainMenuAction action) => GetComponentInParent<MainMenuActionRelay>()?.Invoke(action);
}
