using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    /// <summary>
    /// 在拖拽物品期间显示其名称、等级和描述。
    /// 视觉层级保留在预制体中，便于直接调整排版。
    /// </summary>
    public sealed class ItemInfoPanel : MonoBehaviour
    {
        [SerializeField]
        private PlayerBackpackSystem playerBackpackSystem;

        [SerializeField]
        private GameObject panelVisual;

        [SerializeField]
        private Text itemNameLabel;

        [SerializeField]
        private Text levelLabel;

        [SerializeField]
        private Text descriptionLabel;

        public bool IsShowing =>
            panelVisual != null && panelVisual.activeSelf;

        private PlayerBackpackSystem System =>
            playerBackpackSystem != null
                ? playerBackpackSystem
                : playerBackpackSystem =
                    GetComponentInParent<PlayerBackpackSystem>(true);

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            SubscribeToSystem();
            RefreshPresentation(System != null
                ? System.SelectedItem
                : null);
        }

        private void Start()
        {
            SubscribeToSystem();
            RefreshPresentation(System != null
                ? System.SelectedItem
                : null);
        }

        private void OnDisable()
        {
            if (playerBackpackSystem != null)
            {
                playerBackpackSystem.SelectedItemChanged -=
                    RefreshPresentation;
            }
        }

        public void RefreshPresentation(ItemView selectedItem)
        {
            if (selectedItem == null ||
                !selectedItem.IsDragging ||
                selectedItem.Instance?.Data == null)
            {
                SetVisible(false);
                return;
            }

            ItemInstance instance = selectedItem.Instance;
            ItemData data = instance.Data;

            if (itemNameLabel != null)
            {
                itemNameLabel.text = data.ItemName;
            }

            if (levelLabel != null)
            {
                levelLabel.text = $"Lv.{instance.Level}";
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = data.Description;
            }

            SetVisible(true);
        }

        private void SubscribeToSystem()
        {
            PlayerBackpackSystem system = System;
            if (system == null)
            {
                return;
            }

            system.SelectedItemChanged -= RefreshPresentation;
            system.SelectedItemChanged += RefreshPresentation;
        }

        private void SetVisible(bool visible)
        {
            if (panelVisual != null)
            {
                panelVisual.SetActive(visible);
            }
        }
    }
}
