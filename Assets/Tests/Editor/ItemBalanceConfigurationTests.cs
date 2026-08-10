using BackpackHero.Battle;
using BackpackPrototype;
using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class ItemBalanceConfigurationTests
{
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

    [Test]
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
}
