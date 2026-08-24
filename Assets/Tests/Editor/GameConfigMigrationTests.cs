using System.Collections.Generic;
using BackpackHero.Config;
using BackpackHero.Editor;
using NUnit.Framework;

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
            Assert.That(GameConfigService.Level.EnemyStages, Has.Length.EqualTo(15));
        }

        [Test]
        public void TableAndUnityResourceBridges_AreConsistent()
        {
            Assert.That(GameConfigValidation.Validate(out List<string> errors), Is.True,
                string.Join("\n", errors));
        }
    }
}
