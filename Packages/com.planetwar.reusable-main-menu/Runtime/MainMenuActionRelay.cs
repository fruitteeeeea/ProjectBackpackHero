using System;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    public sealed class MainMenuActionRelay : MonoBehaviour
    {
        private Action<MainMenuAction> invoke;
        public event Action<MainMenuAction> ActionInvoked;
        public void Initialize(Action<MainMenuAction> callback) => invoke = callback;
        public void Invoke(MainMenuAction action)
        {
            invoke?.Invoke(action);
            ActionInvoked?.Invoke(action);
        }
    }
}
