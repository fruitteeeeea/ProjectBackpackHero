using System;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    public sealed class MainMenuActionRelay : MonoBehaviour
    {
        private Action<MainMenuAction> invoke;
        /// <summary>Optional game-level policy checked before a visual menu action is dispatched.</summary>
        public static Func<MainMenuAction, bool> ActionFilter;
        public event Action<MainMenuAction> ActionInvoked;
        public void Initialize(Action<MainMenuAction> callback) => invoke = callback;
        public void Invoke(MainMenuAction action)
        {
            if (ActionFilter != null && !ActionFilter(action)) return;
            invoke?.Invoke(action);
            ActionInvoked?.Invoke(action);
        }
    }
}
