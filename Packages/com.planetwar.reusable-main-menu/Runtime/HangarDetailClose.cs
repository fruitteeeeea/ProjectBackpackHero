using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>Package-local replacement for the original detail page close callback.</summary>
    public sealed class HangarDetailClose : MonoBehaviour
    {
        [SerializeField] private HangarView hangar;
        public void Configure(HangarView owner) => hangar = owner;
        public void OnClickClose() => hangar?.HideDetails();
    }
}
