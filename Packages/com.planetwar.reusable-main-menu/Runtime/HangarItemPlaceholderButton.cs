using UnityEngine;
using UnityEngine.EventSystems;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>Turns a pre-authored visual item into the temporary, data-free Hangar interaction.</summary>
    public sealed class HangarItemPlaceholderButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private HangarView hangar;
        public void SetHangar(HangarView value) => hangar = value;
        public void OnPointerClick(PointerEventData eventData) => hangar?.ShowPlaceholder();
    }
}
