using System;
using System.Collections.Generic;
using BackpackHero.Battle;
using UnityEngine;

namespace BackpackPrototype
{
    [CreateAssetMenu(
        menuName = "Backpack Prototype/Item Data")]
    public sealed class ItemData : ScriptableObject
    {
        [SerializeField]
        private string itemName = "Item";

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private Color backgroundColor =
            new Color32(255, 204, 128, 255);

        [SerializeField]
        private ItemType itemType = ItemType.Equipment;

        [SerializeField]
        private Color equipmentColor = Color.white;

        [SerializeField]
        private float cooldownDuration = -1f;

        [SerializeField]
        private ItemShapeData shape;

        [Header("Battle")]
        [SerializeField]
        private FighterDefinition fighterDefinition;

        public string ItemName => itemName;
        public Sprite Icon => icon;
        public Color BackgroundColor => backgroundColor;
        public ItemType ItemType => itemType;
        public Color EquipmentColor => equipmentColor;
        public float CooldownDuration => cooldownDuration;
        public ItemShapeData Shape => shape;
        public FighterDefinition FighterDefinition =>
            fighterDefinition;

        public IReadOnlyList<Vector2Int> ShapeOffsets =>
            shape != null
                ? shape.ShapeOffsets
                : Array.Empty<Vector2Int>();

        public bool CanEnterCooldown =>
            itemType == ItemType.Aircraft &&
            cooldownDuration > 0f;

        public void InitializeForTests(
            string testName,
            ItemType testItemType,
            float testCooldownDuration,
            ItemShapeData testShape)
        {
            itemName = testName;
            itemType = testItemType;
            cooldownDuration = testCooldownDuration;
            shape = testShape;

            OnValidate();
        }
        
        private void OnValidate()
        {
            if (itemType == ItemType.Equipment)
            {
                cooldownDuration = -1f;
                return;
            }

            cooldownDuration =
                Mathf.Max(0.01f, cooldownDuration);
        }
    }
}
