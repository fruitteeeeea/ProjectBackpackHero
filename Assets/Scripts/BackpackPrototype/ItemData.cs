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

        [Header("Equipment Effects")]
        [SerializeField]
        private List<EquipmentEffectDefinition> equipmentEffects =
            new();

        [SerializeField]
        private float cooldownDuration = 3f;

        [Header("Aircraft Spawn")]
        [Tooltip("每次该飞机生成时生成的数量。")]
        [SerializeField, Min(1)]
        private int spawnCount = 1;

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
        public IReadOnlyList<EquipmentEffectDefinition>
            EquipmentEffects =>
            itemType == ItemType.Equipment &&
            equipmentEffects != null
                ? equipmentEffects
                : Array.Empty<EquipmentEffectDefinition>();
        public float CooldownDuration => cooldownDuration;
        public int SpawnCount => Mathf.Max(1, spawnCount);
        public ItemShapeData Shape => shape;
        public FighterDefinition FighterDefinition =>
            fighterDefinition;

        public IReadOnlyList<Vector2Int> ShapeOffsets =>
            shape != null
                ? shape.ShapeOffsets
                : Array.Empty<Vector2Int>();

        public bool CanEnterCooldown =>
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

        public void SetEquipmentEffectsForTests(
            params EquipmentEffectDefinition[] effects)
        {
            equipmentEffects ??=
                new List<EquipmentEffectDefinition>();
            equipmentEffects.Clear();

            if (effects != null)
            {
                foreach (EquipmentEffectDefinition effect in effects)
                {
                    if (effect != null)
                    {
                        equipmentEffects.Add(effect);
                    }
                }
            }
        }
        
        private void OnValidate()
        {
            cooldownDuration = Mathf.Max(0.01f, cooldownDuration);

            if (itemType == ItemType.Equipment)
            {
                spawnCount = 1;
                return;
            }

            spawnCount = Mathf.Max(1, spawnCount);
        }
    }
}
