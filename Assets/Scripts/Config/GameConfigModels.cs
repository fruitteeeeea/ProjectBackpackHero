using System;
using System.Collections.Generic;
using System.Globalization;
using BackpackHero.Battle;
using BackpackPrototype;

namespace BackpackHero.Config
{
    /// <summary>Generated-config shaped records. These are intentionally Unity-object-free.</summary>
    public sealed class ItemConfig
    {
        public string Id { get; internal set; }
        public ItemType ItemType { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public string UnlockRequirementText { get; internal set; }
        public int Cost { get; internal set; }
        public int Debris { get; internal set; }
        public int UpgradeFragmentsLevel1 { get; internal set; }
        public int UpgradeFragmentsLevel9 { get; internal set; }
        public float AircraftHealthMultiplierLevel1 { get; internal set; }
        public float AircraftHealthMultiplierLevel10 { get; internal set; }
        public float AircraftDamageMultiplierLevel1 { get; internal set; }
        public float AircraftDamageMultiplierLevel10 { get; internal set; }
        public float EquipmentIntervalMultiplierLevel1 { get; internal set; }
        public float EquipmentIntervalMultiplierLevel10 { get; internal set; }
        public float Cooldown { get; internal set; }
        public int Count { get; internal set; }
        public float EquipmentItemModifier { get; internal set; }
        public bool ApplyEquipmentItemModifierToProjectileStats { get; internal set; }
        public int Price { get; internal set; }
        public float AircraftCooldownReductionLevel2 { get; internal set; }
        public float AircraftCooldownReductionLevel3 { get; internal set; }
        public float EquipmentEffectIntervalReductionLevel2 { get; internal set; }
        public float EquipmentEffectIntervalReductionLevel3 { get; internal set; }
        public string FighterId { get; internal set; }
        public string ShapeId { get; internal set; }
    }

    public sealed class FighterConfig
    {
        public string Id { get; internal set; }
        public string DisplayName { get; internal set; }
        public float BaseSpeed { get; internal set; }
        public int MaximumHealth { get; internal set; }
        public float AttackRange { get; internal set; }
        public float TargetingArcAngle { get; internal set; }
        public int TargetingPriority { get; internal set; }
        public FighterTargetSelectionMode TargetingMode { get; internal set; }
        public float AttackInterval { get; internal set; }
        public float ProjectileDamage { get; internal set; }
        public float ProjectileSpeed { get; internal set; }
    }

    public sealed class LevelConfig
    {
        public float PlayerAircraftHealthMultiplier { get; internal set; }
        public float PlayerAircraftDamageMultiplier { get; internal set; }
        public float[] BackpackRoundHealthMultipliers { get; internal set; }
        public EnemyStrengthMultipliers[] EnemyStages { get; internal set; }
    }

    internal static class GameConfigValue
    {
        public static int Int(IReadOnlyDictionary<string, string> row, string key) =>
            int.Parse(row[key], NumberStyles.Integer, CultureInfo.InvariantCulture);

        public static float Float(IReadOnlyDictionary<string, string> row, string key) =>
            float.Parse(row[key], NumberStyles.Float, CultureInfo.InvariantCulture);

        public static bool Bool(IReadOnlyDictionary<string, string> row, string key) =>
            row[key] == "1" || bool.Parse(row[key]);

        public static string String(IReadOnlyDictionary<string, string> row, string key) => row[key]?.Trim();
    }
}
