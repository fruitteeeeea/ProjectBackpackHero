using System;
using System.Collections.Generic;
using BackpackHero.Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace BackpackPrototype
{
    [CreateAssetMenu(menuName = "Backpack Prototype/Item Data")]
    public sealed class ItemData : ScriptableObject
    {
        public const float MinimumEquipmentItemModifier = 0.1f;
        [Header("Player Progress")]
        [SerializeField, FormerlySerializedAs("itemId")] private string id;
        // Kept serialized solely so existing ItemData assets retain their layout. PlayerItemSystem
        // intentionally ignores it: this prototype exposes the entire catalog from the start.
        [SerializeField, HideInInspector] private bool initiallyUnlocked = true;
        [SerializeField, TextArea(1, 2)] private string unlockRequirementText = "Unlocked by default";
        [SerializeField, Min(0), FormerlySerializedAs("upgradeGoldCost")] private int cost;
        [SerializeField, Min(0), FormerlySerializedAs("upgradeFragmentCost")] private int debris;

        [SerializeField, FormerlySerializedAs("itemName")] private string name = "Item";
        [SerializeField, TextArea(2, 5), FormerlySerializedAs("description")] private string desc = "Item description";
        [SerializeField] private Sprite icon;
        [SerializeField] private Color backgroundColor = new Color32(255, 204, 128, 255);
        [SerializeField] private ItemType itemType = ItemType.Equipment;
        [SerializeField] private Color equipmentColor = Color.white;

        [Header("Equipment Effects")]
        [SerializeField] private List<EquipmentEffectDefinition> equipmentEffects = new();
        [SerializeField, FormerlySerializedAs("cooldownDuration")] private float cd = 3f;

        [Header("Entity Detail")]
        [Tooltip("Only displayed in the Aircraft detail page; does not affect battle resources.")]
        [SerializeField, Min(0)] private int price;

        [Header("Aircraft Spawn")]
        [SerializeField, Min(1), FormerlySerializedAs("spawnCount")] private int count = 1;
        [InspectorName("装备物品修正")]
        [Tooltip("生成的飞机从临近装备获得的攻击修正。装备攻击冷却除以此值，伤害和速度乘以此值。")]
        [SerializeField, Min(MinimumEquipmentItemModifier)]
        private float equipmentItemModifier = 1f;
        [SerializeField] private ItemShapeData shape;

        [Header("Battle")]
        [SerializeField] private FighterDefinition fighterDefinition;

        public string Id => id;
        public string Name => name;
        public string Desc => desc;
        public int Cost => cost;
        public int Debris => debris;
        public float Cd => cd;
        public int Count => Mathf.Max(1, count);
        public float EquipmentItemModifier =>
            itemType == ItemType.Aircraft
                ? Mathf.Max(MinimumEquipmentItemModifier, equipmentItemModifier)
                : 1f;
        public int Price => price;
        public bool InitiallyUnlocked => true;
        public string UnlockRequirementText => unlockRequirementText;
        public Sprite Icon => icon;
        public Color BackgroundColor => backgroundColor;
        public ItemType ItemType => itemType;
        public Color EquipmentColor => equipmentColor;
        public IReadOnlyList<EquipmentEffectDefinition> EquipmentEffects =>
            itemType == ItemType.Equipment && equipmentEffects != null
                ? equipmentEffects : Array.Empty<EquipmentEffectDefinition>();
        public ItemShapeData Shape => shape;
        public FighterDefinition FighterDefinition => fighterDefinition;
        public IReadOnlyList<Vector2Int> ShapeOffsets => shape != null ? shape.ShapeOffsets : Array.Empty<Vector2Int>();
        public bool CanEnterCooldown => cd > 0f;

        // Compatibility aliases keep existing gameplay callers and save contracts intact.
        public string ItemName => Name;
        public string ItemId => Id;
        public int UpgradeGoldCost => Cost;
        public int UpgradeFragmentCost => Debris;
        public string Description => Desc;
        public float CooldownDuration => Cd;
        public int SpawnCount => Count;

        public void InitializeForTests(string testName, ItemType testItemType, float testCooldownDuration, ItemShapeData testShape)
        {
            name = testName;
            itemType = testItemType;
            cd = testCooldownDuration;
            shape = testShape;
            OnValidate();
        }

        public void SetEquipmentEffectsForTests(params EquipmentEffectDefinition[] effects)
        {
            equipmentEffects ??= new List<EquipmentEffectDefinition>();
            equipmentEffects.Clear();
            if (effects == null) return;
            foreach (EquipmentEffectDefinition effect in effects)
                if (effect != null) equipmentEffects.Add(effect);
        }

        public void SetEquipmentItemModifierForTests(float value)
        {
            equipmentItemModifier = value;
            OnValidate();
        }

        public void ConfigurePlayerProgressForTests(string testItemId, int goldCost = 0, int fragmentCost = 0)
        {
            id = testItemId;
            cost = goldCost;
            debris = fragmentCost;
            OnValidate();
        }

        private void OnValidate()
        {
            id = id?.Trim();
            cost = Mathf.Max(0, cost);
            debris = Mathf.Max(0, debris);
            price = Mathf.Max(0, price);
            cd = Mathf.Max(0.01f, cd);
            count = itemType == ItemType.Equipment ? 1 : Mathf.Max(1, count);
            equipmentItemModifier = Mathf.Max(
                MinimumEquipmentItemModifier,
                equipmentItemModifier);
        }
    }
}
