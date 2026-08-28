using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackHero.Config;
using BackpackHero.Debugging;
using BackpackHero.Editor;
using NUnit.Framework;
using UnityEditor;

namespace BackpackHero.Tests.Editor
{
    public sealed class GameConfigMigrationTests
    {
        [SetUp]
        public void SetUp() => GameConfigService.ResetForTests();

        [Test]
        public void BundledTables_LoadAllMigratedRecords()
        {
            Assert.That(GameConfigService.IsReady, Is.True, GameConfigService.Error);
            Assert.That(GameConfigService.UsesNativeLubanRuntime, Is.True,
                "GameConfigService must load Luban-generated C# tables, not compatibility CSV bytes.");
            Assert.That(GameConfigService.TryGetItem("aircraft_first", out ItemConfig item), Is.True);
            Assert.That(item.Cooldown, Is.EqualTo(2f));
            Assert.That(item.FighterId, Is.EqualTo("fighter_normal"));
            Assert.That(GameConfigService.TryGetFighter("fighter_sniper", out FighterConfig fighter), Is.True);
            Assert.That(fighter.ProjectileDamage, Is.EqualTo(2.5f));
            Assert.That(GameConfigService.Level.BackpackRoundHealthMultipliers, Has.Length.EqualTo(6));
            Assert.That(
                GameConfigService.Level.EnemyStages,
                Has.Length.EqualTo(
                    LevelDifficultySettings.LevelCount *
                    LevelDifficultySettings.StageCount));
            foreach (EnemyStrengthMultipliers strength in
                     GameConfigService.Level.EnemyStages)
            {
                Assert.That(strength.Health, Is.EqualTo(1f));
                Assert.That(strength.Damage, Is.EqualTo(1f));
            }
        }

        [Test]
        public void GameDataCatalog_UsesTenLevelNeutralDifficultyAsDefault()
        {
            GameDataCatalog catalog =
                UnityEngine.Resources.Load<GameDataCatalog>("GameDataCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.LevelDifficulty, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(catalog.LevelDifficulty),
                Is.EqualTo(
                    "Assets/GameData/Gameplay/LevelDifficultyDefault.asset"));
        }

        [Test]
        public void TableAndUnityResourceBridges_AreConsistent()
        {
            Assert.That(GameConfigValidation.Validate(out List<string> errors), Is.True,
                string.Join("\n", errors));
        }
    }
}
