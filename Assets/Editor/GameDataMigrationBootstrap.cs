using BackpackHero.Battle;
using BackpackHero.Debugging;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>一次性把历史 Resources 调试资产转为正式 GameData，并建立运行时目录清单。</summary>
    [InitializeOnLoad]
    internal static class GameDataMigrationBootstrap
    {
        private const string CatalogPath = "Assets/Resources/GameDataCatalog.asset";

        static GameDataMigrationBootstrap() => EditorApplication.delayCall += EnsureMigrated;

        private static void EnsureMigrated()
        {
            EnsureFolder("Assets/GameData");
            EnsureFolder("Assets/GameData/Gameplay");
            EnsureFolder("Assets/GameData/Visual");
            GamePacingDebugSettings pacing = Migrate<GamePacingDebugSettings>(
                "Assets/Resources/GamePacingDebugSettings.asset", "Assets/GameData/Gameplay/GamePacing.asset");
            StyleTendencyDebugSettings tendency = Migrate<StyleTendencyDebugSettings>(
                "Assets/Resources/StyleTendency/Arcade.asset", "Assets/GameData/Gameplay/StyleTendency.asset");
            // Strategy 是现有项目中可由设计师选择的第二份正式配置，不能在迁移时丢失。
            Migrate<StyleTendencyDebugSettings>(
                "Assets/Resources/StyleTendency/Strategy.asset", "Assets/GameData/Gameplay/StyleTendencyStrategy.asset");
            Migrate<LevelDifficultySettings>(
                "Assets/Resources/LevelDifficultySettings.asset", "Assets/GameData/Gameplay/LevelDifficulty.asset");
            LevelDifficultySettings difficulty =
                EnsureDefaultLevelDifficulty(
                    "Assets/GameData/Gameplay/LevelDifficultyDefault.asset");
            AircraftVisualDebugSettings aircraft = Migrate<AircraftVisualDebugSettings>(
                "Assets/Resources/AircraftVisual/AircraftVisualDebugSettings.asset", "Assets/GameData/Visual/AircraftVisual.asset");
            BackpackVisualDebugSettings backpack = Migrate<BackpackVisualDebugSettings>(
                "Assets/Resources/BackpackVisual/BackpackVisualDebugSettings.asset", "Assets/GameData/Visual/BackpackVisual.asset");
            FloatingDamageTextDebugSettings floating = Migrate<FloatingDamageTextDebugSettings>(
                "Assets/Resources/FloatingDamageText/FloatingDamageTextDebugSettings.asset", "Assets/GameData/Visual/FloatingDamageText.asset");
            ArtAssetDebugSettings art = Migrate<ArtAssetDebugSettings>(
                null, "Assets/GameData/Visual/ArtAsset.asset");
            FunctionBlockSettings functionBlock = Migrate<FunctionBlockSettings>(
                null, "Assets/GameData/Gameplay/FunctionBlock.asset");
            EnsureFolder("Assets/Resources");
            GameDataCatalog catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.SetDefaults(pacing, tendency, difficulty, aircraft, backpack, floating, art, functionBlock);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            DeleteLegacy("Assets/Resources/GamePacingDebugSettings.asset");
            DeleteLegacy("Assets/Resources/StyleTendency/Arcade.asset");
            DeleteLegacy("Assets/Resources/StyleTendency/Strategy.asset");
            DeleteLegacy("Assets/Resources/LevelDifficultySettings.asset");
            DeleteLegacy("Assets/Resources/AircraftVisual/AircraftVisualDebugSettings.asset");
            DeleteLegacy("Assets/Resources/BackpackVisual/BackpackVisualDebugSettings.asset");
            DeleteLegacy("Assets/Resources/FloatingDamageText/FloatingDamageTextDebugSettings.asset");
            AssetDatabase.SaveAssets();
        }

        private static T Migrate<T>(string legacyPath, string targetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(targetPath);
            if (existing != null) return existing;
            T target = ScriptableObject.CreateInstance<T>();
            T legacy = string.IsNullOrEmpty(legacyPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(legacyPath);
            if (legacy != null) EditorUtility.CopySerialized(legacy, target);
            AssetDatabase.CreateAsset(target, targetPath);
            return target;
        }

        private static LevelDifficultySettings EnsureDefaultLevelDifficulty(
            string path)
        {
            LevelDifficultySettings existing =
                AssetDatabase.LoadAssetAtPath<LevelDifficultySettings>(path);
            if (existing != null) return existing;

            LevelDifficultySettings settings =
                ScriptableObject.CreateInstance<LevelDifficultySettings>();
            for (int level = 1;
                 level <= LevelDifficultySettings.LevelCount;
                 level++)
            {
                for (int stage = 0;
                     stage < LevelDifficultySettings.StageCount;
                     stage++)
                {
                    settings.SetEnemyStrength(level, stage, 1f, 1f);
                }
            }

            AssetDatabase.CreateAsset(settings, path);
            return settings;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }

        private static void DeleteLegacy(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
        }
    }
}
