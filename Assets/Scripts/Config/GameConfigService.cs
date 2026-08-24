using System;
using System.Collections.Generic;
using System.Globalization;
using BackpackHero.Battle;
using Luban.SimpleJSON;
using UnityEngine;
using LubanItemConfig = BackpackHero.Config.BackpackHero.Config.ItemConfig;
using LubanFighterConfig = BackpackHero.Config.BackpackHero.Config.FighterConfig;
using LubanLevelConfig = BackpackHero.Config.BackpackHero.Config.LevelConfig;

namespace BackpackHero.Config
{
    /// <summary>
    /// Runtime entry point for the official Luban-generated C# tables and JSON payloads.
    /// Public records keep Unity-facing callers independent from generated namespace details.
    /// </summary>
    public static class GameConfigService
    {
        private static readonly Dictionary<string, ItemConfig> Items = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, FighterConfig> Fighters = new(StringComparer.Ordinal);
        private static bool attempted;
        private static string error;
        private static Tables tables;

        public static bool IsReady { get { EnsureLoaded(); return error == null; } }
        public static string Error { get { EnsureLoaded(); return error; } }
        public static bool UsesNativeLubanRuntime { get { EnsureLoaded(); return error == null && tables != null; } }
        public static LevelConfig Level { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeAtStartup() => EnsureLoaded();

        public static bool TryGetItem(string id, out ItemConfig config)
        {
            EnsureLoaded();
            return Items.TryGetValue(id ?? string.Empty, out config);
        }

        public static bool TryGetFighter(string id, out FighterConfig config)
        {
            EnsureLoaded();
            return Fighters.TryGetValue(id ?? string.Empty, out config);
        }

        public static void ResetForTests()
        {
            attempted = false;
            error = null;
            tables = null;
            Level = null;
            Items.Clear();
            Fighters.Clear();
        }

        private static void EnsureLoaded()
        {
            if (attempted) return;
            attempted = true;
            try
            {
                tables = new Tables(LoadJson);
                LoadItems(tables.ItemConfigs.DataList);
                LoadFighters(tables.FighterConfigs.DataList);
                LoadLevel(tables.LevelConfigs.Data);
            }
            catch (Exception exception)
            {
                error = "Game configuration failed to load: " + exception.Message;
                Debug.LogError(error);
                Items.Clear();
                Fighters.Clear();
                Level = null;
                tables = null;
            }
        }

        private static JSONNode LoadJson(string fileName)
        {
            string resourcePath = "Configs/Luban/" + fileName;
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null) throw new InvalidOperationException("Missing Resources/" + resourcePath + ".json");
            JSONNode node = JSON.Parse(asset.text);
            if (node == null || node.IsNull) throw new InvalidOperationException("Invalid JSON in Resources/" + resourcePath + ".json");
            return node;
        }

        private static void LoadItems(IReadOnlyList<LubanItemConfig> rows)
        {
            foreach (LubanItemConfig row in rows)
            {
                ItemConfig config = new()
                {
                    Id = row.Id, ItemType = (BackpackPrototype.ItemType)row.ItemType,
                    Name = row.Name, Description = row.Description, UnlockRequirementText = row.UnlockRequirementText,
                    Cost = row.Cost, Debris = row.Debris, UpgradeFragmentsLevel1 = row.UpgradeFragmentsLevel1, UpgradeFragmentsLevel9 = row.UpgradeFragmentsLevel9,
                    AircraftHealthMultiplierLevel1 = row.AircraftHealthMultiplierLevel1, AircraftHealthMultiplierLevel10 = row.AircraftHealthMultiplierLevel10,
                    AircraftDamageMultiplierLevel1 = row.AircraftDamageMultiplierLevel1, AircraftDamageMultiplierLevel10 = row.AircraftDamageMultiplierLevel10,
                    EquipmentIntervalMultiplierLevel1 = row.EquipmentIntervalMultiplierLevel1, EquipmentIntervalMultiplierLevel10 = row.EquipmentIntervalMultiplierLevel10,
                    Cooldown = row.Cooldown, Count = row.Count, EquipmentItemModifier = row.EquipmentItemModifier,
                    ApplyEquipmentItemModifierToProjectileStats = row.ApplyEquipmentItemModifierToProjectileStats,
                    Price = row.Price, AircraftCooldownReductionLevel2 = row.AircraftCooldownReductionLevel2, AircraftCooldownReductionLevel3 = row.AircraftCooldownReductionLevel3,
                    EquipmentEffectIntervalReductionLevel2 = row.EquipmentEffectIntervalReductionLevel2, EquipmentEffectIntervalReductionLevel3 = row.EquipmentEffectIntervalReductionLevel3,
                    FighterId = row.FighterId, ShapeId = row.ShapeId
                };
                AddUnique(Items, config.Id, config, "item");
            }
            if (Items.Count == 0) throw new InvalidOperationException("ItemConfig has no rows.");
        }

        private static void LoadFighters(IReadOnlyList<LubanFighterConfig> rows)
        {
            foreach (LubanFighterConfig row in rows)
            {
                FighterConfig config = new()
                {
                    Id = row.Id, DisplayName = row.DisplayName, BaseSpeed = row.BaseSpeed, MaximumHealth = row.MaximumHealth,
                    AttackRange = row.AttackRange, TargetingArcAngle = row.TargetingArcAngle, TargetingPriority = row.TargetingPriority,
                    TargetingMode = (FighterTargetSelectionMode)row.TargetingMode, AttackInterval = row.AttackInterval,
                    ProjectileDamage = row.ProjectileDamage, ProjectileSpeed = row.ProjectileSpeed
                };
                AddUnique(Fighters, config.Id, config, "fighter");
            }
            if (Fighters.Count == 0) throw new InvalidOperationException("FighterConfig has no rows.");
        }

        private static void LoadLevel(LubanLevelConfig row)
        {
            if (row == null) throw new InvalidOperationException("LevelConfig has no row.");
            float[] rounds = ParseFloatList(row.BackpackRoundHealthMultipliers, 6, "backpack round multipliers");
            string[] stageStrings = row.EnemyStages.Split('|');
            if (stageStrings.Length != 15) throw new InvalidOperationException("enemyStages must contain 15 health:damage pairs.");
            EnemyStrengthMultipliers[] stages = new EnemyStrengthMultipliers[15];
            for (int i = 0; i < stages.Length; i++)
            {
                string[] pair = stageStrings[i].Split(':');
                if (pair.Length != 2) throw new InvalidOperationException("Invalid enemy stage at index " + i + ".");
                stages[i] = new EnemyStrengthMultipliers(ParseFloat(pair[0]), ParseFloat(pair[1]));
            }
            Level = new LevelConfig
            {
                PlayerAircraftHealthMultiplier = row.PlayerAircraftHealthMultiplier,
                PlayerAircraftDamageMultiplier = row.PlayerAircraftDamageMultiplier,
                BackpackRoundHealthMultipliers = rounds, EnemyStages = stages
            };
        }

        private static float[] ParseFloatList(string value, int count, string name)
        {
            string[] values = value.Split('|');
            if (values.Length != count) throw new InvalidOperationException(name + " must contain " + count + " values.");
            float[] result = new float[count];
            for (int i = 0; i < count; i++) result[i] = ParseFloat(values[i]);
            return result;
        }

        private static float ParseFloat(string value) => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static void AddUnique<T>(Dictionary<string, T> map, string id, T config, string tableName)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("An " + tableName + " ID is empty.");
            if (!map.TryAdd(id, config)) throw new InvalidOperationException("Duplicate " + tableName + " ID: " + id);
        }
    }

}
