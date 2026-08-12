using TMPro;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    /// <summary>Hosts the serialized Hangar visual page and its intentionally data-free item placeholder.</summary>
    [DisallowMultipleComponent]
    public sealed class HangarView : MonoBehaviour
    {
        [SerializeField] private GameObject placeholderMask;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text diamondText;
        [SerializeField] private TMP_Text battleGoldText;
        [SerializeField] private TMP_Text battleDiamondText;

        public void Show()
        {
            gameObject.SetActive(true);
            if (placeholderMask != null) placeholderMask.SetActive(false);
            if (goldText != null && battleGoldText != null) goldText.text = battleGoldText.text;
            if (diamondText != null && battleDiamondText != null) diamondText.text = battleDiamondText.text;
        }

        public void ShowPlaceholder()
        {
            if (placeholderMask != null) placeholderMask.SetActive(true);
        }

        public void HidePlaceholder()
        {
            if (placeholderMask != null) placeholderMask.SetActive(false);
        }
    }
}
