using System;
using System.Collections.Generic;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Progression
{
    [Serializable]
    public sealed class RankEnemyDifficultyStage
    {
        [Range(PlayerItemSystem.DefaultLevel, PlayerItemSystem.MaximumLevel)]
        public int progressionLevel = PlayerItemSystem.DefaultLevel;
        public DeckPreset deckPreset;
    }

    [Serializable]
    public sealed class RankEnemyDifficultyRank
    {
        [Range(1, 8)] public int rank;
        public int minimumPoints;
        public int maximumPoints;
        public RankEnemyDifficultyStage[] stages = new RankEnemyDifficultyStage[3];
    }

    /// <summary>Data-only mapping from ranked score bands to enemy decks and persistent card levels.</summary>
    [CreateAssetMenu(fileName = "RankEnemyDifficultyCatalog", menuName = "Backpack Hero/Rank Enemy Difficulty Catalog")]
    public sealed class RankEnemyDifficultyCatalog : ScriptableObject
    {
        public const int RankCount = 8;
        public const int StagesPerRank = 3;
        [SerializeField] private RankEnemyDifficultyRank[] ranks = new RankEnemyDifficultyRank[RankCount];

        public IReadOnlyList<RankEnemyDifficultyRank> Ranks => ranks ?? Array.Empty<RankEnemyDifficultyRank>();

        public RankEnemyDifficultyRank FindRank(int rank)
        {
            if (ranks == null) return null;
            for (int index = 0; index < ranks.Length; index++)
                if (ranks[index] != null && ranks[index].rank == rank) return ranks[index];
            return null;
        }

        public bool IsValid(out string error)
        {
            if (ranks == null || ranks.Length != RankCount)
            {
                error = $"敌人难度配置必须包含 {RankCount} 个段位。";
                return false;
            }

            int previousMaximum = -1;
            for (int rank = 1; rank <= RankCount; rank++)
            {
                RankEnemyDifficultyRank entry = FindRank(rank);
                if (entry == null || entry.minimumPoints > entry.maximumPoints ||
                    (rank > 1 && entry.minimumPoints != previousMaximum + 1))
                {
                    error = $"段位 {rank} 的积分范围无效或不连续。";
                    return false;
                }
                if (entry.stages == null || entry.stages.Length < 1 ||
                    entry.stages.Length > StagesPerRank)
                {
                    error = $"段位 {rank} 必须配置 1 至 {StagesPerRank} 个小阶段。";
                    return false;
                }
                for (int stage = 0; stage < entry.stages.Length; stage++)
                {
                    RankEnemyDifficultyStage value = entry.stages[stage];
                    string deckError = null;
                    bool validStage = value != null &&
                        value.progressionLevel >= PlayerItemSystem.DefaultLevel &&
                        value.progressionLevel <= PlayerItemSystem.MaximumLevel &&
                        value.deckPreset != null &&
                        IsFullDeck(value.deckPreset, out deckError);
                    if (!validStage)
                    {
                        error = $"段位 {rank}，阶段 {stage + 1} 无效：{deckError ?? "养成等级或卡组缺失"}";
                        return false;
                    }
                }
                previousMaximum = entry.maximumPoints;
            }
            if (previousMaximum != RankProgressionSystem.MaximumPoints)
            {
                error = $"最后一个段位必须结束于 {RankProgressionSystem.MaximumPoints} 分。";
                return false;
            }
            error = null;
            return true;
        }

        private static bool IsFullDeck(DeckPreset preset, out string error)
        {
            if (!preset.IsValid(out error)) return false;
            foreach (ItemData item in preset.Slots)
                if (item == null) { error = "敌方预设必须填满 3 架飞机和 2 件装备。"; return false; }
            return true;
        }
    }
}
