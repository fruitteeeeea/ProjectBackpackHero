using System.Collections.Generic;
using BackpackHero.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class FighterCombatTargetingArcTests
{
    private readonly List<Object> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
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
    public void PositionWithinTargetingArc_AllowsDoubleArcBoundary()
    {
        FighterCombat2D combat = CreateCombat(30f);
        Vector2 targetPosition = Quaternion.Euler(0f, 0f, -30f) * Vector2.up;

        Assert.That(
            combat.IsPositionWithinTargetingArc(targetPosition, 2f),
            Is.True);
    }

    [Test]
    public void PositionWithinTargetingArc_RejectsPositionOutsideDoubleArc()
    {
        FighterCombat2D combat = CreateCombat(30f);
        Vector2 targetPosition = Quaternion.Euler(0f, 0f, -30.1f) * Vector2.up;

        Assert.That(
            combat.IsPositionWithinTargetingArc(targetPosition, 2f),
            Is.False);
    }

    [Test]
    public void PositionWithinTargetingArc_ClampsExpandedArcAtFullCircle()
    {
        FighterCombat2D combat = CreateCombat(180f);

        Assert.That(
            combat.IsPositionWithinTargetingArc(Vector2.down, 2f),
            Is.True);
    }

    private FighterCombat2D CreateCombat(float targetingArcAngle)
    {
        GameObject fighterObject = new("Fighter");
        createdObjects.Add(fighterObject);
        fighterObject.SetActive(false);
        fighterObject.AddComponent<SpriteRenderer>();
        fighterObject.AddComponent<DirectionalMover2D>();
        fighterObject.AddComponent<Health>();
        fighterObject.AddComponent<FactionMember>();
        Fighter2D fighter = fighterObject.AddComponent<Fighter2D>();
        FighterCombat2D combat = fighterObject.AddComponent<FighterCombat2D>();
        FighterDefinition definition = ScriptableObject.CreateInstance<FighterDefinition>();
        createdObjects.Add(definition);
        SerializedObject serializedDefinition = new(definition);
        serializedDefinition.FindProperty("targetingArcAngle").floatValue = targetingArcAngle;
        serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

        fighterObject.SetActive(true);
        fighter.Initialize(definition, BattleFaction.Enemy, Color.white);
        fighterObject.GetComponent<DirectionalMover2D>().Initialize(Vector2.up);
        return combat;
    }
}