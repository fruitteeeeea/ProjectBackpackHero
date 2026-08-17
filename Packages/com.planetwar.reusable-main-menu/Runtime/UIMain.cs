using UnityEngine;
using PlanetWar.ReusableMainMenu;
using TMPro;

public class UIMain : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text diamondText;
    [SerializeField] private TMP_Text playerNameText;

    public void ApplyProfile(string playerName, int gold, int diamond)
    {
        if (playerNameText != null) playerNameText.text = playerName;
        if (goldText != null) goldText.text = gold.ToString();
        if (diamondText != null) diamondText.text = diamond.ToString();
    }

    public void OnClickConfirm() => Relay(MainMenuAction.Start);
    public void OnClickSet() => Relay(MainMenuAction.Settings);
    public void OnClickHead() => Relay(MainMenuAction.Profile);
    public void OnClickRank() => Relay(MainMenuAction.Rank);
    private void Relay(MainMenuAction action) => GetComponentInParent<MainMenuActionRelay>()?.Invoke(action);
}
