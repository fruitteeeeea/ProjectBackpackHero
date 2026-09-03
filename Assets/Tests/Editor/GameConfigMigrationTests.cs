using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackHero.Config;
using BackpackHero.Debugging;
using BackpackHero.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
            Assert.That(fighter.ProjectileDamage, Is.EqualTo(2.2f));
            Assert.That(fighter.ProjectileLifetimeOverride, Is.EqualTo(6f));
            Assert.That(GameConfigService.TryGetFighter("fighter_normal", out FighterConfig normalFighter), Is.True);
            Assert.That(normalFighter.ProjectileLifetimeOverride, Is.EqualTo(-1f));
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
        public void BalanceProfiles_SwitchBetweenFrozenLegacyAndProposedValues()
        {
            Assert.That(GameConfigService.TryGetItem("aircraft_charge", out ItemConfig proposed), Is.True);
            Assert.That(proposed.Cooldown, Is.EqualTo(2.3f));
            Assert.That(GameConfigService.TryGetFighter("fighter_charge", out FighterConfig proposedFighter), Is.True);
            Assert.That(proposedFighter.ProjectileDamage, Is.EqualTo(.65f));
            Assert.That(proposedFighter.AttackInterval, Is.EqualTo(.35f));
            Assert.That(GameConfigService.TryGetFighter("fighter_laser", out FighterConfig proposedLaser), Is.True);
            Assert.That(proposedLaser.AttackRange, Is.EqualTo(3.5f));
            Assert.That(proposedLaser.ProjectileDamage, Is.EqualTo(.9f));

            Assert.That(GameConfigService.TrySetBalanceProfile(BalanceProfile.Legacy), Is.True);
            Assert.That(GameConfigService.TryGetItem("aircraft_charge", out ItemConfig legacy), Is.True);
            Assert.That(legacy.Cooldown, Is.EqualTo(1.8f));
            Assert.That(GameConfigService.TryGetFighter("fighter_charge", out FighterConfig legacyFighter), Is.True);
            Assert.That(legacyFighter.ProjectileDamage, Is.EqualTo(.8f));
            Assert.That(GameConfigService.TryGetFighter("fighter_laser", out FighterConfig legacyLaser), Is.True);
            Assert.That(legacyLaser.AttackRange, Is.EqualTo(2.5f));
            Assert.That(GameConfigService.GetEquipmentEffectConfig("equipment_arc_coil").MaximumChainTargets, Is.EqualTo(3));

            Assert.That(GameConfigService.TrySetBalanceProfile(BalanceProfile.Proposed), Is.True);
            Assert.That(GameConfigService.GetEquipmentEffectConfig("equipment_arc_coil").MaximumChainTargets, Is.EqualTo(2));
            Assert.That(GameConfigService.GetEquipmentEffectConfig("equipment_rapid_cannon").DamageMultiplier, Is.EqualTo(.7f));
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
        public void BaseProjectile_UsesTwoSecondLifetime()
        {
            GameObject projectilePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Battle/Projectiles/Projectile.prefab");

            Assert.That(projectilePrefab, Is.Not.Null);
            Assert.That(
                projectilePrefab.GetComponent<LifetimeAndScreenBounds2D>()
                    .Lifetime,
                Is.EqualTo(2f));
        }

        [Test]
        public void TableAndUnityResourceBridges_AreConsistent()
        {
            Assert.That(GameConfigValidation.Validate(out List<string> errors), Is.True,
                string.Join("\n", errors));
        }
    }
}
