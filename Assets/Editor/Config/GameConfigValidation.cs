using System;
using System.Collections.Generic;
using System.IO;
using BackpackHero.Battle;
using BackpackHero.Config;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.Editor
{
    public static class GameConfigValidation
    {
        [MenuItem("Tools/Backpack Hero/Validate Game Config")]
        public static void ValidateFromMenu()
        {
            if (Validate(out List<string> errors))
            {
                Debug.Log("[GameConfig] Validation passed.");
                return;
            }

            foreach (string error in errors) Debug.LogError("[GameConfig] " + error);
            throw new InvalidOperationException("Game config validation failed with " + errors.Count + " error(s).");
        }

        public static bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            GameConfigService.ResetForTests();
            if (!GameConfigService.IsReady)
            {
                errors.Add(GameConfigService.Error ?? "Configuration service did not become ready.");
                return false;
            }

            ValidateItems(errors);
            ValidateFighters(errors);
            ValidateLevel(errors);
            return errors.Count == 0;
        }

        private static void ValidateItems(List<string> errors)
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Data/Backpack/Items" });
            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null) { errors.Add("Could not load item at " + path); continue; }
                if (!seen.Add(item.Id)) errors.Add("Duplicate ItemData ID: " + item.Id);
                if (!GameConfigService.TryGetItem(item.Id, out ItemConfig config))
                {
                    errors.Add("ItemData '" + path + "' has no ItemConfig row for '" + item.Id + "'.");
                    continue;
                }
                if (config.ItemType != item.ItemType) errors.Add("Item type mismatch for " + item.Id + ".");
                if (item.ItemType == ItemType.Aircraft)
                {
                    if (item.FighterDefinition == null) errors.Add("Aircraft item " + item.Id + " has no FighterDefinition.");
                    else if (!string.Equals(config.FighterId, item.FighterDefinition.Id, StringComparison.Ordinal)) errors.Add("Fighter ID mismatch for " + item.Id + ".");
                }
                else if (!string.IsNullOrEmpty(config.FighterId)) errors.Add("Equipment item " + item.Id + " must not declare fighterId.");

                string shapeId = ToShapeId(item.Shape);
                if (!string.Equals(config.ShapeId, shapeId, StringComparison.Ordinal)) errors.Add("Shape ID mismatch for " + item.Id + ".");
            }
            if (seen.Count != 14) errors.Add("Expected 14 ItemData assets but found " + seen.Count + ".");
        }

        private static void ValidateFighters(List<string> errors)
        {
            string[] guids = AssetDatabase.FindAssets("t:FighterDefinition", new[] { "Assets/Settings/Battle/Fighters" });
            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                FighterDefinition fighter = AssetDatabase.LoadAssetAtPath<FighterDefinition>(path);
                if (fighter == null) { errors.Add("Could not load fighter at " + path); continue; }
                if (!seen.Add(fighter.Id)) errors.Add("Duplicate FighterDefinition ID: " + fighter.Id);
                if (!GameConfigService.TryGetFighter(fighter.Id, out _)) errors.Add("FighterDefinition '" + path + "' has no FighterConfig row.");
            }
            if (seen.Count != 7) errors.Add("Expected 7 FighterDefinition assets but found " + seen.Count + ".");
        }

        private static void ValidateLevel(List<string> errors)
        {
            if (GameConfigService.Level?.BackpackRoundHealthMultipliers?.Length != LevelDifficultySettings.BackpackRoundMultiplierCount)
                errors.Add("LevelConfig must contain six backpack round multipliers.");
            if (GameConfigService.Level?.EnemyStages?.Length != LevelDifficultySettings.LevelCount * LevelDifficultySettings.StageCount)
                errors.Add("LevelConfig must contain " + (LevelDifficultySettings.LevelCount * LevelDifficultySettings.StageCount) + " enemy stages.");
        }

        private static string ToShapeId(ItemShapeData shape)
        {
            if (shape == null) return string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(shape));
            string value = fileName.StartsWith("ItemShape_", StringComparison.Ordinal) ? fileName.Substring("ItemShape_".Length) : fileName;
            System.Text.StringBuilder result = new("shape_");
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsUpper(c) && i > 0 && value[i - 1] != '_') result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            return result.ToString();
        }
    }
}
