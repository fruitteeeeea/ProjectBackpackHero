using UnityEngine;
using PlanetWar.ReusableMainMenu;
public class UIMainBottom : MonoBehaviour
{
    public void OnChooseBottom(int index) => GetComponentInParent<MainMenuActionRelay>()?.Invoke((MainMenuAction)((int)MainMenuAction.BottomHome + Mathf.Clamp(index, 0, 4)));
}
