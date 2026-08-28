using System.Collections;
using System.Reflection;
using BackpackHero.Battle;
using NUnit.Framework;
using UnityEngine;

public sealed class LevelFlowControllerTimerDebugTests
{
    private GameObject battleObject;
    private GameObject flowObject;
    private GameObject levelManagerObject;

    [TearDown]
    public void TearDown()
    {
        if (flowObject != null) Object.DestroyImmediate(flowObject);
        if (battleObject != null) Object.DestroyImmediate(battleObject);
        if (levelManagerObject != null)
        {
            Object.DestroyImmediate(levelManagerObject);
            typeof(LevelManager).GetProperty("Instance",
                BindingFlags.Static | BindingFlags.Public)
                ?.SetValue(null, null);
        }
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

    [Test]
    public void PrepareForMenuExit_ResetsStateWithoutInitializingMatch()
    {
        LevelFlowController flow = CreateFlow();
        levelManagerObject = LevelManager.EnsureInstance().gameObject;
        LevelManager.EnsureInstance().SetLevel(3);
        SetMatchState(flow, 3, 2, 1, true);
        SetTimer(flow, 10f, true);
        EnterCombat();

        int initializationVersion =
            LevelFlowController.MatchInitializationVersion;
        int initializationCount = 0;
        LevelFlowController.MatchInitialized += CountInitialization;
        try
        {
            flow.PrepareForMenuExit();
        }
        finally
        {
            LevelFlowController.MatchInitialized -= CountInitialization;
        }

        Assert.That(flow.Round, Is.EqualTo(1));
        Assert.That(flow.PlayerWins, Is.EqualTo(0));
        Assert.That(flow.EnemyWins, Is.EqualTo(0));
        Assert.That(flow.IsMatchComplete, Is.False);
        Assert.That(flow.IsRoundTimerRunning, Is.False);
        Assert.That(LevelManager.CurrentLevel, Is.EqualTo(1));
        Assert.That(BattleFlowController.CurrentPhase,
            Is.EqualTo(BattlePhase.Preparation));
        Assert.That(LevelFlowController.MatchInitializationVersion,
            Is.EqualTo(initializationVersion));
        Assert.That(initializationCount, Is.EqualTo(0));

        int resetVersion = LevelFlowController.MatchInitializationVersion;
        LevelFlowController.MatchInitialized += CountInitialization;
        try
        {
            flow.ResetForLevel(2);
        }
        finally
        {
            LevelFlowController.MatchInitialized -= CountInitialization;
        }

        Assert.That(LevelManager.CurrentLevel, Is.EqualTo(2));
        Assert.That(LevelFlowController.MatchInitializationVersion,
            Is.EqualTo(resetVersion + 1));
        Assert.That(initializationCount, Is.EqualTo(1));

        void CountInitialization() => initializationCount++;
    }
    [Test]
    public void ResultPhase_StopsCombatWithoutEnteringPreparation()
    {
        LevelFlowController flow = CreateFlow();
        EnterCombat();

        BattleFlowController.EnsureInstance().SetPhase(
            BattlePhase.Result);

        Assert.That(BattleFlowController.CurrentPhase,
            Is.EqualTo(BattlePhase.Result));
        Assert.That(BattleFlowController.IsCombatPhase, Is.False);
        Assert.That(flow.IsRoundTimerRunning, Is.False);
    }

    [Test]
    public void ResultBannerLifecycle_UsesFixedTwoSecondDuration()
    {
        LevelFlowController flow = CreateFlow();
        MethodInfo method = typeof(LevelFlowController).GetMethod(
            "PlayBannerLifecycle", BindingFlags.Instance |
            BindingFlags.NonPublic);

        FieldInfo durationField = typeof(LevelFlowController).GetField(
            "ResultBannerDurationSeconds", BindingFlags.Static |
            BindingFlags.NonPublic);
        float duration = (float)durationField?.GetValue(null);
        IEnumerator lifecycle = (IEnumerator)method?.Invoke(flow,
            new object[] { duration, null });
        float totalWait = 0f;
        while (lifecycle.MoveNext())
        {
            if (lifecycle.Current is WaitForSecondsRealtime wait)
            {
                totalWait += wait.waitTime;
            }
        }

        Assert.That(totalWait, Is.EqualTo(2f).Within(0.0001f));
    }

    [Test]
    public void CompleteRoundResolution_NonFinalRoundEntersPreparation()
    {
        CreateFlow();
        BattleFlowController.EnsureInstance().SetPhase(
            BattlePhase.Result);
        MethodInfo method = typeof(LevelFlowController).GetMethod(
            "CompleteRoundResolution", BindingFlags.Instance |
            BindingFlags.NonPublic);

        method?.Invoke(LevelFlowController.Instance,
            new object[] { false });

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
