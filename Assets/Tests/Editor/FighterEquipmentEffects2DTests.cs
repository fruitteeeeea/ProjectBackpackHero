using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackPrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class FighterEquipmentEffects2DTests
{
    [Test]
    public void ExplosiveEquipmentAsset_UsesConfiguredExplosiveProjectile()
    {
        ItemData equipment =
            AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Data/Backpack/Items/" +
                "Equipment_First.asset");
        GameObject explosivePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Battle/Projectiles/" +
                "Projectile_Explosive.prefab");

        Assert.That(equipment, Is.Not.Null);
        Assert.That(explosivePrefab, Is.Not.Null);
        Assert.That(equipment.EquipmentEffects.Count,
            Is.EqualTo(1));

        ProjectileEquipmentEffectDefinition effect =
            equipment.EquipmentEffects[0] as
            ProjectileEquipmentEffectDefinition;

        Assert.That(effect, Is.Not.Null);
        Assert.That(effect.ProjectilePrefab, Is.SameAs(
            explosivePrefab.GetComponent<BattleAttack2D>()));
        Assert.That(effect.Cooldown, Is.EqualTo(0.8f));
    }

    [Test]
    public void ProjectileEffect_ClampsCooldownToSharedInterval()
    {
        ProjectileEquipmentEffectDefinition effect =
            ScriptableObject.CreateInstance<
                ProjectileEquipmentEffectDefinition>();

        try
        {
            effect.InitializeForTests(null, 0.01f);

            Assert.That(
                effect.Cooldown,
                Is.EqualTo(
                    ProjectileEquipmentEffectDefinition
                        .MinimumCooldown));
        }
        finally
        {
            Object.DestroyImmediate(effect);
        }
    }

    [Test]
    public void ItemData_OnlyExposesEffectsForEquipment()
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        ProjectileEquipmentEffectDefinition effect =
            ScriptableObject.CreateInstance<
                ProjectileEquipmentEffectDefinition>();

        try
        {
            item.SetEquipmentEffectsForTests(effect);
            item.InitializeForTests(
                "Aircraft",
                ItemType.Aircraft,
                1f,
                null);

            Assert.That(item.EquipmentEffects, Is.Empty);

            item.InitializeForTests(
                "Equipment",
                ItemType.Equipment,
                -1f,
                null);

            Assert.That(item.EquipmentEffects, Is.EqualTo(
                new[] { effect }));
        }
        finally
        {
            Object.DestroyImmediate(item);
            Object.DestroyImmediate(effect);
        }
    }

    [Test]
    public void ProjectileEffects_FireImmediatelyThenRoundRobinAtSharedInterval()
    {
        var owner = new GameObject("Equipment Effects");
        var firstPrefab = new GameObject("First Projectile");
        var secondPrefab = new GameObject("Second Projectile");
        ProjectileEquipmentEffectDefinition firstEffect =
            ScriptableObject.CreateInstance<
                ProjectileEquipmentEffectDefinition>();
        ProjectileEquipmentEffectDefinition secondEffect =
            ScriptableObject.CreateInstance<
                ProjectileEquipmentEffectDefinition>();

        try
        {
            BattleAttack2D firstAttack =
                firstPrefab.AddComponent<
                    EquipmentEffectTestAttack>();
            BattleAttack2D secondAttack =
                secondPrefab.AddComponent<
                    EquipmentEffectTestAttack>();
            firstEffect.InitializeForTests(firstAttack, 0.8f);
            secondEffect.InitializeForTests(secondAttack, 0.8f);

            FighterEquipmentEffects2D controller =
                owner.AddComponent<FighterEquipmentEffects2D>();
            var shots = new List<BattleAttack2D>();
            controller.ProjectileShotRequested += shots.Add;
            controller.Configure(new EquipmentEffectDefinition[]
            {
                firstEffect,
                secondEffect,
            });

            Assert.That(controller.ProjectileEffectCount, Is.EqualTo(2));
            Assert.That(controller.Tick(0f), Is.True);
            Assert.That(shots, Is.EqualTo(new[] { firstAttack }));

            Assert.That(controller.Tick(0.09f), Is.False);
            Assert.That(controller.Tick(0.01f), Is.True);
            Assert.That(shots, Is.EqualTo(
                new[] { firstAttack, secondAttack }));

            Assert.That(controller.Tick(0.69f), Is.False);
            Assert.That(controller.Tick(0.01f), Is.True);
            Assert.That(shots, Is.EqualTo(
                new[] { firstAttack, secondAttack, firstAttack }));
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(firstPrefab);
            Object.DestroyImmediate(secondPrefab);
            Object.DestroyImmediate(firstEffect);
            Object.DestroyImmediate(secondEffect);
        }
    }

    [Test]
    public void ProjectileEffects_ApplyAircraftEquipmentModifierToCooldownAndShot()
    {
        var owner = new GameObject("Equipment Effects");
        var prefab = new GameObject("Projectile");
        ProjectileEquipmentEffectDefinition effect =
            ScriptableObject.CreateInstance<ProjectileEquipmentEffectDefinition>();

        try
        {
            BattleAttack2D attack = prefab.AddComponent<EquipmentEffectTestAttack>();
            effect.InitializeForTests(attack, 0.8f);
            FighterEquipmentEffects2D controller =
                owner.AddComponent<FighterEquipmentEffects2D>();
            float emittedModifier = 0f;
            controller.ProjectileShotRequestedWithSource +=
                (_, _, modifier) => emittedModifier = modifier;
            controller.Configure(
                new[] { (Effect: (EquipmentEffectDefinition)effect, Item: (ItemInstance)null) },
                0.5f);

            Assert.That(controller.Tick(0f), Is.True);
            Assert.That(emittedModifier, Is.EqualTo(0.5f));
            Assert.That(controller.Tick(1.59f), Is.False);
            Assert.That(controller.Tick(0.01f), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(effect);
        }
    }

    [Test]
    public void ProjectileEffects_ApplyEquipmentLevelIntervalReduction()
    {
        var owner = new GameObject("Equipment Effects");
        var prefab = new GameObject("Projectile");
        ProjectileEquipmentEffectDefinition effect =
            ScriptableObject.CreateInstance<ProjectileEquipmentEffectDefinition>();
        ItemData equipmentData = ScriptableObject.CreateInstance<ItemData>();

        try
        {
            BattleAttack2D attack = prefab.AddComponent<EquipmentEffectTestAttack>();
            effect.InitializeForTests(attack, 0.8f);
            equipmentData.InitializeForTests(
                "Equipment",
                ItemType.Equipment,
                3f,
                null);
            equipmentData.SetEquipmentEffectsForTests(effect);

            ItemInstance equipment = new ItemInstance(
                "equipment-lv2",
                equipmentData,
                Vector2Int.zero,
                2);
            FighterEquipmentEffects2D controller =
                owner.AddComponent<FighterEquipmentEffects2D>();

            controller.Configure(
                new[]
                {
                    (Effect: (EquipmentEffectDefinition)effect,
                     Item: equipment),
                },
                1f);

            Assert.That(controller.Tick(0f), Is.True);
            Assert.That(controller.Tick(0.69f), Is.False);
            Assert.That(controller.Tick(0.01f), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(equipmentData);
        }
    }

    [Test]
    public void ItemInstance_ScalesEquipmentEffectCooldownByLevel()
    {
        ProjectileEquipmentEffectDefinition projectile =
            ScriptableObject.CreateInstance<ProjectileEquipmentEffectDefinition>();
        LaserLinkEquipmentEffectDefinition laser =
            ScriptableObject.CreateInstance<LaserLinkEquipmentEffectDefinition>();
        ItemData equipmentData = ScriptableObject.CreateInstance<ItemData>();

        try
        {
            projectile.InitializeForTests(null, 0.8f);
            laser.InitializeForTests(null, 3f);
            equipmentData.InitializeForTests(
                "Equipment",
                ItemType.Equipment,
                3f,
                null);

            ItemInstance levelTwo = new ItemInstance(
                "equipment-lv2",
                equipmentData,
                Vector2Int.zero,
                2);
            ItemInstance levelThree = new ItemInstance(
                "equipment-lv3",
                equipmentData,
                Vector2Int.zero,
                3);

            Assert.That(
                levelTwo.GetEquipmentEffectCooldown(projectile),
                Is.EqualTo(0.7f));
            Assert.That(
                levelThree.GetEquipmentEffectCooldown(projectile),
                Is.EqualTo(0.6f));
            Assert.That(
                levelTwo.GetEquipmentEffectCooldown(laser),
                Is.EqualTo(2.9f));
            Assert.That(
                levelThree.GetEquipmentEffectCooldown(laser),
                Is.EqualTo(2.8f));
        }
        finally
        {
            Object.DestroyImmediate(projectile);
            Object.DestroyImmediate(laser);
            Object.DestroyImmediate(equipmentData);
        }
    }
}

public sealed class EquipmentEffectTestAttack : BattleAttack2D
{
    public override void Initialize(BattleAttackLaunchContext context)
    {
    }
}
