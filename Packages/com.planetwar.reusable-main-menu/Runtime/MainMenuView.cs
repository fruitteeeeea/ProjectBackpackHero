using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>
    /// Hosts the statically serialized menu hierarchy. It does not create UI at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private MainMenuTheme theme;
        [SerializeField] private bool buildOnAwake = false;
        public event Action<MainMenuAction> ActionInvoked;

        public MainMenuTheme Theme => theme;
        public void SetTheme(MainMenuTheme value) { theme = value; }
        public void Refresh() { }

        private void Awake()
        {
            var relay = GetComponent<MainMenuActionRelay>();
            if (relay != null) relay.ActionInvoked += ForwardAction;

            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                var capturedButton = button;
                capturedButton.onClick.AddListener(() => LogButtonPressed(capturedButton));
            }
        }

        private void OnDestroy()
        {
            var relay = GetComponent<MainMenuActionRelay>();
            if (relay != null) relay.ActionInvoked -= ForwardAction;
        }

        private void ForwardAction(MainMenuAction action) => ActionInvoked?.Invoke(action);

        private static void LogButtonPressed(Button button)
        {
            Debug.Log($"[MainMenu Debug] Button pressed: {GetHierarchyPath(button.transform)}", button);
        }

        private static string GetHierarchyPath(Transform current)
        {
            var path = current.name;
            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
            return path;
        }
    }
}
