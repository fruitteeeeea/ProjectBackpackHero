using BackpackHero.Battle;
using BackpackPrototype;
using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class ItemBalanceConfigurationTests
{
    [Test]
    public void NewAircraftAssets_MatchConfiguredItemsAndCombatProfiles()
    {
        AssertAircraft(
            "Aircraft_Explosive.asset",
            "Fighter_04_Explosive.asset",
            "爆炸飞机", 2f, 2,
            4.5f, 8, 0.5f, 30f, 0.5f, 0.8f, 8f,
            "ItemShape_1x2.asset");
        AssertAircraft(
            "Aircraft_Sniper.asset",
            "Fighter_05_Sniper.asset",
            "狙击飞机", 3.5f, 1,
            1f, 6, 6.5f, 45f, 1f, 2.5f, 12f,
            "ItemShape_L_MissingTopLeft.asset");
        AssertAircraft(
            "Aircraft_Laser.asset",
            "Fighter_06_Laser.asset",
            "激光飞机", 2f, 3,
            3f, 8, 6.5f, 90f, 1f, 0.5f, 12f,
            "ItemShape_L_MissingTopRight.asset");
        AssertAircraft(
            "Aircraft_Shotgun.asset",
            "Fighter_07_Shotgun.asset",
            "霰弹飞机", 2.5f, 1,
            2.5f, 16, 1.5f, 40f, 1f, 0.8f, 8f,
            "ItemShape_L_MissingBottomRight.asset");

        FighterDefinition explosive = Load<FighterDefinition>(
            "Assets/Settings/Battle/Fighters/Fighter_04_Explosive.asset");
        FighterDefinition laser = Load<FighterDefinition>(
            "Assets/Settings/Battle/Fighters/Fighter_06_Laser.asset");
        FighterDefinition shotgun = Load<FighterDefinition>(
            "Assets/Settings/Battle/Fighters/Fighter_07_Shotgun.asset");

        Assert.That(explosive.DeathExplosionAttackPrefab,
            Is.TypeOf<Projectile2D>());
        Assert.That(laser.DefaultAttackPrefab,
            Is.TypeOf<LaserBeamAttack2D>());
        Assert.That(laser.LinkedLaserAttackPrefab,
            Is.TypeOf<LaserBeamAttack2D>());
        Assert.That(shotgun.DefaultFirePattern,
            Is.TypeOf<SpreadProjectileFirePattern>());
    }
    [Test]
    public void ItemAssets_ContainApprovedBalanceValues()
    {
        ItemData lAircraft = Load<ItemData>(
            "Assets/Data/Backpack/Items/Aircraft_L.asset");
        FighterDefinition chargeFighter = Load<FighterDefinition>(
            "Assets/Settings/Battle/Fighters/Fighter_02_Charge.asset");
        FighterDefinition shieldFighter = Load<FighterDefinition>(
            "Assets/Settings/Battle/Fighters/Fighter_03_Shield.asset");
        ProjectileEquipmentEffectDefinition rapidCannon =
            Load<ProjectileEquipmentEffectDefinition>(
                "Assets/Data/Backpack/EquipmentEffects/" +
                "EquipmentEffect_ProjectileNormal.asset");

        Assert.That(lAircraft.SpawnCount, Is.EqualTo(2));
        Assert.That(chargeFighter.ProjectileDamage,
            Is.EqualTo(0.8f));
        Assert.That(shieldFighter.TargetingPriority,
            Is.EqualTo(1));
        Assert.That(rapidCannon.Cooldown,
            Is.EqualTo(0.6f));
    }

    [Test]
    public void ItemAssets_UseConfiguredAircraftAndEquipmentColors()
    {
        ItemData waveEmitter = Load<ItemData>(
            "Assets/Data/Backpack/Items/" +
            "Equipment_WaveEmitter.asset");

        string[] aircraftPaths =
        {
            "Aircraft_Charge.asset",
            "Aircraft_First.asset",
            "Aircraft_L.asset",
            "Aircraft_Shield.asset",
        };
        string[] equipmentPaths =
        {
            "Equipment_1x2.asset",
            "Equipment_ArcCoil.asset",
            "Equipment_First.asset",
            "Equipment_RapidCannon.asset",
            "Equipment_WaveEmitter.asset",
        };
        Color aircraftOrange =
            new Color(1f, 0.8f, 0.5019608f, 1f);

        foreach (string path in aircraftPaths)
        {
            ItemData aircraft = Load<ItemData>(
                "Assets/Data/Backpack/Items/" + path);
            Assert.That(aircraft.BackgroundColor,
                Is.EqualTo(aircraftOrange), path);
        }

        foreach (string path in equipmentPaths)
        {
            ItemData equipment = Load<ItemData>(
                "Assets/Data/Backpack/Items/" + path);
            Assert.That(equipment.BackgroundColor,
                Is.EqualTo(waveEmitter.BackgroundColor),
                path);
            Assert.That(equipment.EquipmentColor,
                Is.EqualTo(waveEmitter.EquipmentColor),
                path);
        }
    }

    [Test, Ignore("Superseded by ItemAssets_ProvideShortDisplayNamesAndDescriptions.")]
    public void ItemAssets_ProvideDescriptionPlaceholders()
    {
        string[] itemPaths =
        {
            "Aircraft_Charge.asset",
            "Aircraft_First.asset",
            "Aircraft_L.asset",
            "Aircraft_Shield.asset",
            "Equipment_1x2.asset",
            "Equipment_ArcCoil.asset",
            "Equipment_First.asset",
            "Equipment_RapidCannon.asset",
            "Equipment_WaveEmitter.asset",
        };

        foreach (string path in itemPaths)
        {
            ItemData item = Load<ItemData>(
                "Assets/Data/Backpack/Items/" + path);

            Assert.That(item.Description,
                Is.EqualTo("暂无描述"), path);
        }
    }

    [Test]
    public void ItemAssets_ProvideShortDisplayNamesAndDescriptions()
    {
        (string path, string name, string description)[] expectedItems =
        {
            ("Aircraft_Charge.asset", "Dash Fighter", "A fast striker that trades durability for speed."),
            ("Aircraft_First.asset", "Scout Fighter", "A balanced fighter for reliable frontline damage."),
            ("Aircraft_L.asset", "Twin Fighter", "Deploys two balanced fighters in a compact formation."),
            ("Aircraft_Shield.asset", "Guardian Fighter", "A durable defender built to hold the line."),
            ("Equipment_1x2.asset", "Arc Launcher", "Adds arcing shots to adjacent fighters."),
            ("Equipment_ArcCoil.asset", "Arc Coil", "Adds chain-lightning shots to adjacent fighters."),
            ("Equipment_First.asset", "Blast Module", "Adds explosive shots to adjacent fighters."),
            ("Equipment_RapidCannon.asset", "Rapid Cannon", "Adds rapid straight shots to adjacent fighters."),
            ("Equipment_WaveEmitter.asset", "Wave Emitter", "Adds weaving wave shots to adjacent fighters."),
        };

        foreach ((string path, string name, string description) expected in expectedItems)
        {
            ItemData item = Load<ItemData>("Assets/Data/Backpack/Items/" + expected.path);
            Assert.That(item.ItemName, Is.EqualTo(expected.name), expected.path);
            Assert.That(item.Description, Is.EqualTo(expected.description), expected.path);
            Assert.That(item.Description.Length, Is.LessThanOrEqualTo(64), expected.path);
        }
    }

    [Test]
    public void ProjectilePrefabs_ContainApprovedBalanceValues()
    {
        GameObject explosivePrefab = Load<GameObject>(
            "Assets/Prefabs/Battle/Projectiles/" +
            "Projectile_Explosive.prefab");
        GameObject lightningPrefab = Load<GameObject>(
            "Assets/Prefabs/Battle/Projectiles/" +
            "Projectile_Lightning.prefab");

        Assert.That(
            explosivePrefab.GetComponent<
                ExplosiveProjectileImpact2D>().Radius,
            Is.EqualTo(0.8f));
        Assert.That(
            lightningPrefab.GetComponent<
                ChainLightningProjectileImpact2D>()
                .ChainDamageMultiplier,
            Is.EqualTo(0.5f));
    }

    [Test]
    public void TargetSelection_HigherPriorityBeatsAlignmentAndDistance()
    {
        MethodInfo method = typeof(FighterCombat2D).GetMethod(
            "IsPreferredFighterCandidate",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        Assert.That((bool)method.Invoke(null, new object[]
        {
            1, 0.1f, 100f,
            0, 1f, 1f,
        }), Is.True);
        Assert.That((bool)method.Invoke(null, new object[]
        {
            0, 1f, 10f,
            0, 0.9f, 1f,
        }), Is.True);
        Assert.That((bool)method.Invoke(null, new object[]
        {
            0, 1f, 10f,
            0, 1f, 1f,
        }), Is.False);
    }

    private static T Load<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.That(asset, Is.Not.Null, path);
        return asset;
    }

    private static void AssertAircraft(
        string itemFile,
        string fighterFile,
        string displayName,
        float cooldown,
        int spawnCount,
        float speed,
        int health,
        float range,
        float arc,
        float interval,
        float damage,
        float projectileSpeed,
        string shapeFile)
    {
        ItemData item = Load<ItemData>(
            "Assets/Data/Backpack/Items/" + itemFile);
        FighterDefinition fighter = Load<FighterDefinition>(
            "Assets/Settings/Battle/Fighters/" + fighterFile);
        ItemShapeData shape = Load<ItemShapeData>(
            "Assets/Data/Backpack/ItemShapes/" + shapeFile);

        Assert.That(item.ItemName, Is.EqualTo(displayName));
        Assert.That(item.CooldownDuration, Is.EqualTo(cooldown));
        Assert.That(item.SpawnCount, Is.EqualTo(spawnCount));
        Assert.That(item.Shape, Is.SameAs(shape));
        Assert.That(item.FighterDefinition, Is.SameAs(fighter));
        Assert.That(item.Icon, Is.SameAs(fighter.Sprite));
        Assert.That(fighter.BaseSpeed, Is.EqualTo(speed));
        Assert.That(fighter.MaximumHealth, Is.EqualTo(health));
        Assert.That(fighter.AttackRange, Is.EqualTo(range));
        Assert.That(fighter.TargetingArcAngle, Is.EqualTo(arc));
        Assert.That(fighter.AttackInterval, Is.EqualTo(interval));
        Assert.That(fighter.ProjectileDamage, Is.EqualTo(damage));
        Assert.That(fighter.ProjectileSpeed, Is.EqualTo(projectileSpeed));
        Assert.That(fighter.TargetingPriority, Is.Zero);
    }
}
