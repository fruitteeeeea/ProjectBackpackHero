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
            "爆炸飞机", 3.8f, 2,
            4.5f, 10, 0.5f, 30f, 0.5f, 0.7f, 8f,
            "ItemShape_1x2.asset");
        AssertAircraft(
            "Aircraft_Sniper.asset",
            "Fighter_05_Sniper.asset",
            "狙击飞机", 3.8f, 1,
            1f, 7, 6.5f, 45f, 1f, 2.2f, 12f,
            "ItemShape_L_MissingTopLeft.asset");
        AssertAircraft(
            "Aircraft_Laser.asset",
            "Fighter_06_Laser.asset",
            "激光飞机", 2.8f, 1,
            3f, 8, 3.5f, 45f, .75f, .9f, 12f,
            "ItemShape_1x2.asset");
        AssertAircraft(
            "Aircraft_Shotgun.asset",
            "Fighter_07_Shotgun.asset",
            "霰弹飞机", 2.8f, 1,
            2.5f, 16, 1.5f, 40f, 1f, .65f, 8f,
            "ItemShape_2x1.asset");

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
        Assert.That(laser.TargetingMode,
            Is.EqualTo(FighterTargetSelectionMode.FarthestFighter));
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
        ItemData shieldItem = Load<ItemData>(
            "Assets/Data/Backpack/Items/Aircraft_Shield.asset");
        ProjectileEquipmentEffectDefinition rapidCannon =
            Load<ProjectileEquipmentEffectDefinition>(
                "Assets/Data/Backpack/EquipmentEffects/" +
                "EquipmentEffect_ProjectileNormal.asset");

        Assert.That(lAircraft.SpawnCount, Is.EqualTo(2));
        Assert.That(chargeFighter.ProjectileDamage,
            Is.EqualTo(0.65f));
        Assert.That(shieldFighter.TargetingPriority,
            Is.EqualTo(1));
        Assert.That(shieldItem.Icon,
            Is.EqualTo(shieldFighter.Sprite));
        Assert.That(rapidCannon.Cooldown,
            Is.EqualTo(0.6f));
        Assert.That(lAircraft.EquipmentItemModifier,
            Is.EqualTo(1f));
    }

    [Test]
    public void AircraftItems_DefaultEquipmentProjectileStatModifiersAreDisabled()
    {
        string[] paths =
        {
            "Aircraft_Charge.asset",
            "Aircraft_Explosive.asset",
            "Aircraft_First.asset",
            "Aircraft_L.asset",
            "Aircraft_Laser.asset",
            "Aircraft_Shield.asset",
            "Aircraft_Shotgun.asset",
            "Aircraft_Sniper.asset",
        };

        foreach (string path in paths)
        {
            ItemData aircraft = Load<ItemData>(
                "Assets/Data/Backpack/Items/" + path);
            Assert.That(aircraft.ApplyEquipmentItemModifierToProjectileStats,
                Is.False, path);
        }
    }

    [Test]
    public void AircraftEquipmentModifier_ClampsToMinimum()
    {
        ItemData aircraft = ScriptableObject.CreateInstance<ItemData>();
        try
        {
            aircraft.InitializeForTests("Aircraft", ItemType.Aircraft, 1f, null);
            aircraft.SetEquipmentItemModifierForTests(0f);

            Assert.That(aircraft.EquipmentItemModifier,
                Is.EqualTo(ItemData.MinimumEquipmentItemModifier));
        }
        finally
        {
            Object.DestroyImmediate(aircraft);
        }
    }

    [Test]
    public void AircraftEquipmentProjectileStatModifier_IsDisabledByDefaultAndCanBeEnabled()
    {
        ItemData aircraft = ScriptableObject.CreateInstance<ItemData>();
        try
        {
            aircraft.InitializeForTests("Aircraft", ItemType.Aircraft, 1f, null);

            Assert.That(aircraft.ApplyEquipmentItemModifierToProjectileStats,
                Is.False);

            aircraft.SetApplyEquipmentItemModifierToProjectileStatsForTests(true);

            Assert.That(aircraft.ApplyEquipmentItemModifierToProjectileStats,
                Is.True);
        }
        finally
        {
            Object.DestroyImmediate(aircraft);
        }
    }

    [Test]
    public void ItemAssets_UseDefaultLevelCooldownReductions()
    {
        string[] aircraftPaths =
        {
            "Aircraft_Charge.asset",
            "Aircraft_Explosive.asset",
            "Aircraft_First.asset",
            "Aircraft_L.asset",
            "Aircraft_Laser.asset",
            "Aircraft_Shield.asset",
            "Aircraft_Shotgun.asset",
            "Aircraft_Sniper.asset",
        };

        string[] equipmentPaths =
        {
            "Equipment_1x2.asset",
            "Equipment_ArcCoil.asset",
            "Equipment_First.asset",
            "Equipment_LaserLink.asset",
            "Equipment_RapidCannon.asset",
            "Equipment_WaveEmitter.asset",
        };

        foreach (string path in aircraftPaths)
        {
            ItemData aircraft = Load<ItemData>(
                "Assets/Data/Backpack/Items/" + path);
            Assert.That(
                aircraft.GetAircraftCooldownReductionForLevel(2),
                Is.EqualTo(0.2f),
                path);
            Assert.That(
                aircraft.GetAircraftCooldownReductionForLevel(3),
                Is.EqualTo(0.4f),
                path);
        }

        foreach (string path in equipmentPaths)
        {
            ItemData equipment = Load<ItemData>(
                "Assets/Data/Backpack/Items/" + path);
            Assert.That(
                equipment.GetEquipmentEffectIntervalReductionForLevel(2),
                Is.EqualTo(0.1f),
                path);
            Assert.That(
                equipment.GetEquipmentEffectIntervalReductionForLevel(3),
                Is.EqualTo(0.2f),
                path);
        }
    }

    [Test]
    public void ItemAssets_UseDefaultOutOfMatchProgressionCurves()
    {
        string[] paths =
        {
            "Aircraft_Charge.asset", "Aircraft_Explosive.asset",
            "Aircraft_First.asset", "Aircraft_L.asset", "Aircraft_Laser.asset",
            "Aircraft_Shield.asset", "Aircraft_Shotgun.asset", "Aircraft_Sniper.asset",
            "Equipment_1x2.asset", "Equipment_ArcCoil.asset", "Equipment_First.asset",
            "Equipment_LaserLink.asset", "Equipment_RapidCannon.asset", "Equipment_WaveEmitter.asset",
        };

        foreach (string path in paths)
        {
            ItemData item = Load<ItemData>("Assets/Data/Backpack/Items/" + path);
            Assert.That(item.GetUpgradeFragmentCost(1), Is.EqualTo(2), path);
            Assert.That(item.GetUpgradeFragmentCost(3), Is.EqualTo(26), path);
            Assert.That(item.GetUpgradeFragmentCost(9), Is.EqualTo(100), path);
            if (item.ItemType == ItemType.Aircraft)
            {
                Assert.That(item.GetAircraftHealthMultiplierForProgressionLevel(10), Is.EqualTo(1.4f), path);
                Assert.That(item.GetAircraftDamageMultiplierForProgressionLevel(10), Is.EqualTo(1.4f), path);
            }
            else
            {
                Assert.That(item.GetEquipmentIntervalMultiplierForProgressionLevel(10), Is.EqualTo(0.8f), path);
            }
        }
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
            "Equipment_LaserLink.asset",
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
    public void RebalancedAircraftAssets_SelectMatchingVisualShells()
    {
        ItemData laser = Load<ItemData>(
            "Assets/Data/Backpack/Items/Aircraft_Laser.asset");
        ItemData shotgun = Load<ItemData>(
            "Assets/Data/Backpack/Items/Aircraft_Shotgun.asset");
        ItemView fallback = Load<ItemView>(
            "Assets/Prefabs/BackpackUI/Items/Item_1x1.prefab");
        ItemView vertical = Load<ItemView>(
            "Assets/Prefabs/BackpackUI/Items/Item_1x2.prefab");
        ItemView horizontal = Load<ItemView>(
            "Assets/Prefabs/BackpackUI/Items/Item_2x1.prefab");

        Assert.That(ItemViewPrefabSelector.Select(
                laser, fallback, vertical, horizontal,
                null, null, null, null),
            Is.SameAs(vertical));
        Assert.That(ItemViewPrefabSelector.Select(
                shotgun, fallback, vertical, horizontal,
                null, null, null, null),
            Is.SameAs(horizontal));
    }

    [Test]
    public void EquipmentAssets_UseConfiguredBarShapesAndVisualShells()
    {
        ItemShapeData vertical = Load<ItemShapeData>(
            "Assets/Data/Backpack/ItemShapes/ItemShape_1x2.asset");
        ItemShapeData horizontal = Load<ItemShapeData>(
            "Assets/Data/Backpack/ItemShapes/ItemShape_2x1.asset");
        ItemView fallback = Load<ItemView>(
            "Assets/Prefabs/BackpackUI/Items/Item_1x1.prefab");
        ItemView verticalPrefab = Load<ItemView>(
            "Assets/Prefabs/BackpackUI/Items/Item_1x2.prefab");
        ItemView horizontalPrefab = Load<ItemView>(
            "Assets/Prefabs/BackpackUI/Items/Item_2x1.prefab");

        Assert.That(vertical.ShapeOffsets, Is.EqualTo(new[]
        {
            new Vector2Int(0, 0), new Vector2Int(0, 1),
        }));
        Assert.That(horizontal.ShapeOffsets, Is.EqualTo(new[]
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
        }));

        AssertEquipmentBarShape("Equipment_First.asset", horizontal,
            horizontalPrefab, fallback, verticalPrefab, horizontalPrefab);
        AssertEquipmentBarShape("Equipment_RapidCannon.asset", vertical,
            verticalPrefab, fallback, verticalPrefab, horizontalPrefab);
        AssertEquipmentBarShape("Equipment_ArcCoil.asset", vertical,
            verticalPrefab, fallback, verticalPrefab, horizontalPrefab);
        AssertEquipmentBarShape("Equipment_1x2.asset", vertical,
            verticalPrefab, fallback, verticalPrefab, horizontalPrefab);
        AssertEquipmentBarShape("Equipment_LaserLink.asset", vertical,
            verticalPrefab, fallback, verticalPrefab, horizontalPrefab);
        AssertEquipmentBarShape("Equipment_WaveEmitter.asset", vertical,
            verticalPrefab, fallback, verticalPrefab, horizontalPrefab);
    }

    [Test]
    public void ItemAssets_ProvideShortDisplayNamesAndDescriptions()
    {
        (string path, string name, string description)[] expectedItems =
        {
            ("Aircraft_Charge.asset", "冲锋飞机", "高速突击，牺牲耐久换取机动。"),
            ("Aircraft_First.asset", "初号飞机", "均衡的前线战机，提供稳定火力。"),
            ("Aircraft_L.asset", "双机编队", "紧凑编队一次部署两架均衡战机。"),
            ("Aircraft_Shield.asset", "护盾飞机", "高耐久防御机，负责稳固战线。"),
            ("Equipment_1x2.asset", "Arc Launcher", "Adds arcing shots to adjacent fighters."),
            ("Equipment_ArcCoil.asset", "Arc Coil", "Adds chain-lightning shots to adjacent fighters."),
            ("Equipment_First.asset", "Blast Module", "Adds explosive shots to adjacent fighters."),
            ("Equipment_RapidCannon.asset", "Rapid Cannon", "Adds rapid straight shots to adjacent fighters."),
            ("Equipment_WaveEmitter.asset", "Wave Emitter", "Adds weaving wave shots to adjacent fighters."),
            ("Equipment_LaserLink.asset", "Laser Link Module", "Fires lasers at up to 3 farthest linked allies every 3 seconds."),
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
            explosivePrefab.GetComponent<
                ExplosiveProjectileImpact2D>().AreaDamageMultiplier,
            Is.EqualTo(0.5f));
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

    [Test]
    public void TargetSelection_FarthestModePrefersGreaterDistance()
    {
        MethodInfo method = typeof(FighterCombat2D).GetMethod(
            "IsFartherFighterCandidate",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        Assert.That((bool)method.Invoke(null, new object[] { 9f, 4f }),
            Is.True);
        Assert.That((bool)method.Invoke(null, new object[] { 4f, 9f }),
            Is.False);
    }

    [Test]
    public void LaserLinkEquipmentAsset_UsesConfiguredLaserEffect()
    {
        ItemData equipment = Load<ItemData>(
            "Assets/Data/Backpack/Items/Equipment_LaserLink.asset");
        LaserLinkEquipmentEffectDefinition effect = Load<
            LaserLinkEquipmentEffectDefinition>(
            "Assets/Data/Backpack/EquipmentEffects/" +
            "EquipmentEffect_LaserLink.asset");

        Assert.That(equipment.ItemId, Is.EqualTo("equipment_laser_link"));
        Assert.That(equipment.CooldownDuration, Is.EqualTo(3f));
        Assert.That(equipment.Shape.ShapeOffsets.Count, Is.EqualTo(2));
        Assert.That(equipment.EquipmentEffects, Is.EqualTo(new[] { effect }));
        Assert.That(effect.Cooldown, Is.EqualTo(3f));
        Assert.That(effect.LaserAttackPrefab, Is.TypeOf<LaserBeamAttack2D>());
    }

    [Test]
    public void PlayerItemCatalog_ContainsLaserLinkEquipment()
    {
        PlayerItemCatalog catalog = Load<PlayerItemCatalog>(
            "Assets/Resources/PlayerItemCatalog.asset");

        Assert.That(catalog.Items, Has.Member(
            Load<ItemData>(
                "Assets/Data/Backpack/Items/Equipment_LaserLink.asset")));
        Assert.That(catalog.IsValid(out string error), Is.True, error);
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

    private static void AssertEquipmentBarShape(
        string itemFile,
        ItemShapeData expectedShape,
        ItemView expectedPrefab,
        ItemView fallback,
        ItemView verticalPrefab,
        ItemView horizontalPrefab)
    {
        ItemData equipment = Load<ItemData>(
            "Assets/Data/Backpack/Items/" + itemFile);

        Assert.That(equipment.ItemType, Is.EqualTo(ItemType.Equipment),
            itemFile);
        Assert.That(equipment.Shape, Is.SameAs(expectedShape), itemFile);
        Assert.That(ItemViewPrefabSelector.Select(
                equipment, fallback, verticalPrefab, horizontalPrefab,
                null, null, null, null),
            Is.SameAs(expectedPrefab), itemFile);
    }
}
