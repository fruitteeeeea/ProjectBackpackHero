using UnityEngine;
using UnityEngine.EventSystems;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>Consumes placeholder-mask clicks and returns to the static Hangar page.</summary>
    public sealed class HangarMaskDismiss : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private HangarView hangar;
        public void SetHangar(HangarView value) => hangar = value;
        public void OnPointerClick(PointerEventData eventData) => hangar?.HidePlaceholder();
    }
}
