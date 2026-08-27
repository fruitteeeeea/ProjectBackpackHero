using System;
using System.Collections.Generic;

namespace BackpackHero.Battle
{
    [Serializable]
    public readonly struct BattleResultReward
    {
        public readonly string Label;
        public readonly int Amount;

        public BattleResultReward(string label, int amount)
        {
            Label = label;
            Amount = amount;
        }
    }

    /// <summary>Display-only payload for the end-of-match result panel.</summary>
    public sealed class BattleResultData
    {
        public bool IsVictory { get; }
        public int TotalScore { get; }
        public int ScoreDelta { get; }
        public IReadOnlyList<BattleResultReward> Rewards { get; }

        public BattleResultData(bool isVictory, int totalScore, int scoreDelta,
            IReadOnlyList<BattleResultReward> rewards = null)
        {
            IsVictory = isVictory;
            TotalScore = totalScore;
            ScoreDelta = scoreDelta;
            Rewards = rewards;
        }

        public static BattleResultData CreateDefault(bool isVictory, int totalScore = 0)
        {
            return isVictory
                ? new BattleResultData(true, totalScore, 30, new[]
                {
                    new BattleResultReward("GOLD", 99),
                    new BattleResultReward("DIAMOND", 99)
                })
                : new BattleResultData(false, totalScore, -10, new[]
                {
                    new BattleResultReward("GOLD", 99)
                });
        }
    }
}
