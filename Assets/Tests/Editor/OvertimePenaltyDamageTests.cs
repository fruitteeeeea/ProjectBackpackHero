using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackPrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OvertimePenaltyDamageTests
{
    private readonly List<Object> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        TrackingOvertimePenaltyAttack.LastContext = null;

        foreach (Object createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void MotherShipPenalty_UsesSeventyFivePercentOfTargetMaximumHealth()
    {
        EnterCombat();
        Fighter2D target = CreateFighter(100);
        target.Health.DecreaseHealth(60f);
        BackpackFighterSpawner spawner = CreateSpawner(BattleFaction.Enemy);
        GameObject projectileObject = new("Overtime Penalty Projectile");
        createdObjects.Add(projectileObject);
        TrackingOvertimePenaltyAttack projectile =
            projectileObject.AddComponent<TrackingOvertimePenaltyAttack>();

        Assert.That(
            spawner.FireOvertimePenalty(
                projectile,
                target,
                ProjectileVisualSource.OvertimePenalty),
            Is.True);
        Assert.That(TrackingOvertimePenaltyAttack.LastContext, Is.Not.Null);
        Assert.That(
            TrackingOvertimePenaltyAttack.LastContext.Value.Damage,
            Is.EqualTo(75f));
    }

    private Fighter2D CreateFighter(int maximumHealth)
    {
        GameObject fighterObject = new("Target Fighter");
        createdObjects.Add(fighterObject);
        fighterObject.SetActive(false);
        fighterObject.AddComponent<SpriteRenderer>();
        fighterObject.AddComponent<DirectionalMover2D>();
        fighterObject.AddComponent<Health>();
        fighterObject.AddComponent<FactionMember>();
        Fighter2D fighter = fighterObject.AddComponent<Fighter2D>();
        FighterDefinition definition = ScriptableObject.CreateInstance<FighterDefinition>();
        createdObjects.Add(definition);
        SerializedObject serializedDefinition = new(definition);
        serializedDefinition.FindProperty("maximumHealth").intValue = maximumHealth;
        serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

        fighterObject.SetActive(true);
        fighter.Initialize(definition, BattleFaction.Player, Color.white);
        return fighter;
    }

    private BackpackFighterSpawner CreateSpawner(BattleFaction faction)
    {
        GameObject spawnerObject = new("Enemy Mother Ship");
        createdObjects.Add(spawnerObject);
        FactionMember factionMember =
            spawnerObject.AddComponent<FactionMember>();
        factionMember.SetFaction(faction);
        return spawnerObject.AddComponent<BackpackFighterSpawner>();
    }

    private static void EnterCombat()
    {
        BattleFlowController battle = BattleFlowController.EnsureInstance();
        battle.SetPhase(BattlePhase.Combat);
        if (battle.Phase == BattlePhase.CombatTransition)
        {
            Assert.That(battle.CompleteCombatTransition(), Is.True);
        }
    }
}

public sealed class TrackingOvertimePenaltyAttack : BattleAttack2D
{
    public static BattleAttackLaunchContext? LastContext { get; set; }

    public override void Initialize(BattleAttackLaunchContext context)
    {
        LastContext = context;
    }
}