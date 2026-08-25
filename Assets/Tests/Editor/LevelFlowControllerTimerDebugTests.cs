using System.Reflection;
using BackpackHero.Battle;
using NUnit.Framework;
using UnityEngine;

public sealed class LevelFlowControllerTimerDebugTests
{
    private GameObject battleObject;
    private GameObject flowObject;

    [TearDown]
    public void TearDown()
    {
        if (flowObject != null) Object.DestroyImmediate(flowObject);
        if (battleObject != null) Object.DestroyImmediate(battleObject);
    }

    [Test]
    public void AdjustRoundTimerForDebug_OnlyWorksDuringCombat()
    {
        LevelFlowController flow = CreateFlow();

        Assert.That(flow.AdjustRoundTimerForDebug(5f), Is.False);

        EnterCombat();
        SetTimer(flow, 10f, false);

        Assert.That(flow.AdjustRoundTimerForDebug(5f), Is.True);
        Assert.That(flow.RemainingRoundTime, Is.EqualTo(15f));
    }

    [Test]
    public void AdjustRoundTimerForDebug_WhenNormalTimerCrossesZero_StartsOvertime()
    {
        LevelFlowController flow = CreateFlow();
        EnterCombat();
        SetTimer(flow, 3f, false);

        Assert.That(flow.AdjustRoundTimerForDebug(-5f), Is.True);
        Assert.That(flow.IsOvertime, Is.True);
        Assert.That(flow.RemainingRoundTime,
            Is.EqualTo(LevelFlowController.OvertimeDurationSeconds));
    }

    [Test]
    public void ResetForDebugMatch_ResetsRoundScoreAndPhase()
    {
        LevelFlowController flow = CreateFlow();
        SetMatchState(flow, 3, 2, 1, true);
        EnterCombat();

        flow.ResetForDebugMatch();

        Assert.That(flow.Round, Is.EqualTo(1));
        Assert.That(flow.PlayerWins, Is.EqualTo(0));
        Assert.That(flow.EnemyWins, Is.EqualTo(0));
        Assert.That(flow.IsMatchComplete, Is.False);
        Assert.That(BattleFlowController.CurrentPhase,
            Is.EqualTo(BattlePhase.Preparation));
    }
    private LevelFlowController CreateFlow()
    {
        battleObject = BattleFlowController.EnsureInstance().gameObject;
        flowObject = new GameObject("Level Flow Timer Debug Test");
        return flowObject.AddComponent<LevelFlowController>();
    }

    private static void SetMatchState(
        LevelFlowController flow,
        int round,
        int playerWins,
        int enemyWins,
        bool isMatchComplete)
    {
        const BindingFlags Flags = BindingFlags.Instance |
            BindingFlags.NonPublic;
        typeof(LevelFlowController).GetField(
            "currentRound", Flags)?.SetValue(flow, round);
        typeof(LevelFlowController).GetField(
            "playerWins", Flags)?.SetValue(flow, playerWins);
        typeof(LevelFlowController).GetField(
            "enemyWins", Flags)?.SetValue(flow, enemyWins);
        typeof(LevelFlowController).GetField(
            "isMatchComplete", Flags)?.SetValue(
            flow,
            isMatchComplete);
    }
    private static void SetTimer(
        LevelFlowController flow,
        float remainingTime,
        bool overtime)
    {
        const BindingFlags Flags = BindingFlags.Instance |
            BindingFlags.NonPublic;
        typeof(LevelFlowController).GetField(
            "remainingRoundTime", Flags)?.SetValue(flow, remainingTime);
        typeof(LevelFlowController).GetField(
            "isOvertime", Flags)?.SetValue(flow, overtime);
    }

    private static void EnterCombat()
    {
        BattleFlowController battle = BattleFlowController.EnsureInstance();
        battle.SetPhase(BattlePhase.Combat);
        Assert.That(battle.CompleteCombatTransition(), Is.True);
    }
}
