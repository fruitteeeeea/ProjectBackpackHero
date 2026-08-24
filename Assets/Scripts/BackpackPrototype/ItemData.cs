using System;
using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackHero.Config;
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

        [Header("Out-of-Match Progression")]
        [Tooltip("Lv.1 -> Lv.2 的碎片消耗。")]
        [SerializeField, Min(0)] private int upgradeFragmentsLevel1 = 2;
        [Tooltip("Lv.9 -> Lv.10 的碎片消耗。中间等级按线性曲线向下取整。")]
        [SerializeField, Min(0)] private int upgradeFragmentsLevel9 = 100;
        [Tooltip("飞机 Lv.1/Lv.10 的生命倍率。")]
        [SerializeField] private Vector2 aircraftHealthMultiplierRange = new(1f, 1.4f);
        [Tooltip("飞机 Lv.1/Lv.10 的伤害倍率。")]
        [SerializeField] private Vector2 aircraftDamageMultiplierRange = new(1f, 1.4f);
        [Tooltip("装备 Lv.1/Lv.10 的射击间隔倍率。")]
        [SerializeField] private Vector2 equipmentIntervalMultiplierRange = new(1f, 0.8f);

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
        [InspectorName("装备物品修正系数")]
        [Tooltip("生成的飞机从临近装备获得的攻击修正。装备攻击冷却除以此值；可选地影响投射物伤害和速度。")]
        [SerializeField, Min(MinimumEquipmentItemModifier)]
        private float equipmentItemModifier = 1f;
        [Tooltip("是否将装备物品修正系数应用到装备投射物的伤害和速度。默认关闭。")]
        [SerializeField] private bool applyEquipmentItemModifierToProjectileStats;
        [SerializeField] private ItemShapeData shape;

        [Header("Level Cooldown Reductions")]
        [Tooltip("飞机物品在 Lv2 时从部署冷却中减去的秒数。")]
        [SerializeField, Min(0f)] private float aircraftCooldownReductionLevel2 = 0.2f;
        [Tooltip("飞机物品在 Lv3 时从部署冷却中减去的秒数。")]
        [SerializeField, Min(0f)] private float aircraftCooldownReductionLevel3 = 0.4f;
        [Tooltip("装备效果在 Lv2 时减少的内置射击间隔比例（0-1）。")]
        [SerializeField, Min(0f)] private float equipmentEffectIntervalReductionLevel2 = 0.1f;
        [Tooltip("装备效果在 Lv3 时减少的内置射击间隔比例（0-1）。")]
        [SerializeField, Min(0f)] private float equipmentEffectIntervalReductionLevel3 = 0.2f;

        [Header("Battle")]
        [SerializeField] private FighterDefinition fighterDefinition;

        private ItemConfig Config => GameConfigService.TryGetItem(id, out ItemConfig config) ? config : null;

        public string Id => id;
        public string Name => Config?.Name ?? name;
        public string Desc => Config?.Description ?? desc;
        public int Cost => Mathf.Max(0, Config?.Cost ?? cost);
        public int Debris => Mathf.Max(0, Config?.Debris ?? debris);
        public int UpgradeFragmentsLevel1 => Mathf.Max(0, Config?.UpgradeFragmentsLevel1 ?? upgradeFragmentsLevel1);
        public int UpgradeFragmentsLevel9 => Mathf.Max(0, Config?.UpgradeFragmentsLevel9 ?? upgradeFragmentsLevel9);
        public float Cd => Mathf.Max(0.01f, Config?.Cooldown ?? cd);
        public int Count => Mathf.Max(1, Config?.Count ?? count);
        public float EquipmentItemModifier =>
            ItemType == ItemType.Aircraft
                ? Mathf.Max(MinimumEquipmentItemModifier, Config?.EquipmentItemModifier ?? equipmentItemModifier)
                : 1f;
        public bool ApplyEquipmentItemModifierToProjectileStats =>
            ItemType == ItemType.Aircraft &&
            (Config?.ApplyEquipmentItemModifierToProjectileStats ?? applyEquipmentItemModifierToProjectileStats);
        public float AircraftCooldownReductionLevel2 =>
            Mathf.Max(0f, Config?.AircraftCooldownReductionLevel2 ?? aircraftCooldownReductionLevel2);
        public float AircraftCooldownReductionLevel3 =>
            Mathf.Max(0f, Config?.AircraftCooldownReductionLevel3 ?? aircraftCooldownReductionLevel3);
        public float EquipmentEffectIntervalReductionLevel2 =>
            Mathf.Clamp01(Config?.EquipmentEffectIntervalReductionLevel2 ?? equipmentEffectIntervalReductionLevel2);
        public float EquipmentEffectIntervalReductionLevel3 =>
            Mathf.Clamp01(Config?.EquipmentEffectIntervalReductionLevel3 ?? equipmentEffectIntervalReductionLevel3);
        public int Price => Mathf.Max(0, Config?.Price ?? price);
        public bool InitiallyUnlocked => true;
        public string UnlockRequirementText => Config?.UnlockRequirementText ?? unlockRequirementText;
        public Sprite Icon => icon;
        public Color BackgroundColor => backgroundColor;
        public ItemType ItemType => Config?.ItemType ?? itemType;
        public Color EquipmentColor => equipmentColor;
        public IReadOnlyList<EquipmentEffectDefinition> EquipmentEffects =>
            ItemType == ItemType.Equipment && equipmentEffects != null
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

        public int GetUpgradeFragmentCost(int fromLevel)
        {
            const int firstUpgradeLevel = 1;
            const int lastUpgradeLevel = PlayerItemSystem.MaximumLevel - 1;
            int level = Mathf.Clamp(fromLevel, firstUpgradeLevel, lastUpgradeLevel);
            float progress = (level - firstUpgradeLevel) /
                (float)(lastUpgradeLevel - firstUpgradeLevel);
            return Mathf.FloorToInt(Mathf.Lerp(
                UpgradeFragmentsLevel1, UpgradeFragmentsLevel9, progress));
        }

        public float GetAircraftHealthMultiplierForProgressionLevel(int level) =>
            ItemType == ItemType.Aircraft
                ? GetProgressionMultiplier(new Vector2(
                    Config?.AircraftHealthMultiplierLevel1 ?? aircraftHealthMultiplierRange.x,
                    Config?.AircraftHealthMultiplierLevel10 ?? aircraftHealthMultiplierRange.y), level)
                : 1f;

        public float GetAircraftDamageMultiplierForProgressionLevel(int level) =>
            ItemType == ItemType.Aircraft
                ? GetProgressionMultiplier(new Vector2(
                    Config?.AircraftDamageMultiplierLevel1 ?? aircraftDamageMultiplierRange.x,
                    Config?.AircraftDamageMultiplierLevel10 ?? aircraftDamageMultiplierRange.y), level)
                : 1f;

        public float GetEquipmentIntervalMultiplierForProgressionLevel(int level) =>
            ItemType == ItemType.Equipment
                ? GetProgressionMultiplier(new Vector2(
                    Config?.EquipmentIntervalMultiplierLevel1 ?? equipmentIntervalMultiplierRange.x,
                    Config?.EquipmentIntervalMultiplierLevel10 ?? equipmentIntervalMultiplierRange.y), level)
                : 1f;

        public float GetAircraftCooldownReductionForLevel(int level)
        {
            if (level <= 1) return 0f;
            return level == 2
                ? AircraftCooldownReductionLevel2
                : AircraftCooldownReductionLevel3;
        }

        /// <summary>
        /// Gets the proportion removed from an equipment effect's base interval
        /// by its in-match level.
        /// </summary>
        public float GetEquipmentEffectIntervalReductionForLevel(int level)
        {
            if (level <= 1) return 0f;
            return level == 2
                ? EquipmentEffectIntervalReductionLevel2
                : EquipmentEffectIntervalReductionLevel3;
        }

        public float GetAircraftCooldownDurationForLevel(int level)
        {
            return Mathf.Max(
                0.01f,
                Cd - GetAircraftCooldownReductionForLevel(level));
        }

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

        public void SetApplyEquipmentItemModifierToProjectileStatsForTests(
            bool value) =>
            applyEquipmentItemModifierToProjectileStats = value;

        public void SetLevelCooldownReductionsForTests(
            float aircraftLevel2,
            float aircraftLevel3,
            float equipmentLevel2,
            float equipmentLevel3)
        {
            aircraftCooldownReductionLevel2 = aircraftLevel2;
            aircraftCooldownReductionLevel3 = aircraftLevel3;
            equipmentEffectIntervalReductionLevel2 = equipmentLevel2;
            equipmentEffectIntervalReductionLevel3 = equipmentLevel3;
            OnValidate();
        }

        public void ConfigurePlayerProgressForTests(string testItemId, int goldCost = 0, int fragmentCost = 0)
        {
            id = testItemId;
            cost = goldCost;
            debris = fragmentCost;
            OnValidate();
        }

        public void SetOutOfMatchProgressionForTests(
            int levelOneFragments, int levelNineFragments,
            Vector2 aircraftHealthRange, Vector2 aircraftDamageRange,
            Vector2 equipmentIntervalRange)
        {
            upgradeFragmentsLevel1 = levelOneFragments;
            upgradeFragmentsLevel9 = levelNineFragments;
            aircraftHealthMultiplierRange = aircraftHealthRange;
            aircraftDamageMultiplierRange = aircraftDamageRange;
            equipmentIntervalMultiplierRange = equipmentIntervalRange;
            OnValidate();
        }

        private void OnValidate()
        {
            id = id?.Trim();
            cost = Mathf.Max(0, cost);
            debris = Mathf.Max(0, debris);
            upgradeFragmentsLevel1 = Mathf.Max(0, upgradeFragmentsLevel1);
            upgradeFragmentsLevel9 = Mathf.Max(upgradeFragmentsLevel1, upgradeFragmentsLevel9);
            aircraftHealthMultiplierRange = ClampMultiplierRange(aircraftHealthMultiplierRange);
            aircraftDamageMultiplierRange = ClampMultiplierRange(aircraftDamageMultiplierRange);
            equipmentIntervalMultiplierRange = ClampMultiplierRange(equipmentIntervalMultiplierRange);
            price = Mathf.Max(0, price);
            cd = Mathf.Max(0.01f, cd);
            count = itemType == ItemType.Equipment ? 1 : Mathf.Max(1, count);
            equipmentItemModifier = Mathf.Max(
                MinimumEquipmentItemModifier,
                equipmentItemModifier);
            aircraftCooldownReductionLevel2 = Mathf.Max(0f, aircraftCooldownReductionLevel2);
            aircraftCooldownReductionLevel3 = Mathf.Max(0f, aircraftCooldownReductionLevel3);
            equipmentEffectIntervalReductionLevel2 = Mathf.Clamp01(equipmentEffectIntervalReductionLevel2);
            equipmentEffectIntervalReductionLevel3 = Mathf.Clamp01(equipmentEffectIntervalReductionLevel3);
        }

        private static float GetProgressionMultiplier(Vector2 range, int level)
        {
            float progress = (Mathf.Clamp(level, PlayerItemSystem.DefaultLevel,
                PlayerItemSystem.MaximumLevel) - PlayerItemSystem.DefaultLevel) /
                (float)(PlayerItemSystem.MaximumLevel - PlayerItemSystem.DefaultLevel);
            return Mathf.Lerp(range.x, range.y, progress);
        }

        private static Vector2 ClampMultiplierRange(Vector2 range) =>
            new(Mathf.Max(0.01f, range.x), Mathf.Max(0.01f, range.y));
    }
}
