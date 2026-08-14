using TMPro;
using UnityEngine;

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
        private TMP_Text itemNameLabel;

        [SerializeField]
        private TMP_Text levelLabel;

        [SerializeField]
        private TMP_Text descriptionLabel;

        public bool IsShowing =>
            panelVisual != null && panelVisual.activeSelf;

        private PlayerBackpackSystem System =>
            playerBackpackSystem != null
                ? playerBackpackSystem
                : playerBackpackSystem =
                    GetComponentInParent<PlayerBackpackSystem>(true);

        private void Awake()
        {
            ResolveTextReferences();
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

        private void ResolveTextReferences()
        {
            if (panelVisual == null) return;

            foreach (TMP_Text text in panelVisual.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null) continue;
                switch (text.gameObject.name)
                {
                    // Nested prefab references can remain pointed at their source asset after
                    // a parent prefab is instantiated. Always use the text objects below this
                    // runtime visual, rather than retaining a stale serialized reference.
                    case "ItemNameText": itemNameLabel = text; break;
                    case "LevelText": levelLabel = text; break;
                    case "DescriptionText": descriptionLabel = text; break;
                }
            }
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
