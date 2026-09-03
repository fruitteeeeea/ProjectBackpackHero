using System;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Progression
{
    public readonly struct EnemyMatchProfile
    {
        public readonly int Rank;
        public readonly int RawStage;
        public readonly int EffectiveStage;
        public readonly int ProgressionLevel;
        public readonly DeckPreset DeckPreset;

        public EnemyMatchProfile(int rank, int rawStage, int effectiveStage, int progressionLevel, DeckPreset deckPreset)
        {
            Rank = rank;
            RawStage = rawStage;
            EffectiveStage = effectiveStage;
            ProgressionLevel = progressionLevel;
            DeckPreset = deckPreset;
        }
    }

    public static class RankEnemyDifficultyResolver
    {
        public static bool TryResolve(RankEnemyDifficultyCatalog catalog, int points,
            int currentNodeId, RankDefeatRecoveryState recovery,
            out EnemyMatchProfile profile)
        {
            profile = default;
            if (catalog == null)
            {
                Debug.LogError("敌人段位难度配置缺失。");
                return false;
            }
            if (!catalog.IsValid(out string error))
            {
                Debug.LogError($"敌人段位难度配置无效：{error}");
                return false;
            }

            points = Mathf.Clamp(points, 0, RankProgressionSystem.MaximumPoints);
            RankEnemyDifficultyRank rank = null;
            foreach (RankEnemyDifficultyRank candidate in catalog.Ranks)
            {
                if (candidate != null && points >= candidate.minimumPoints && points <= candidate.maximumPoints)
                {
                    rank = candidate;
                    break;
                }
            }
            if (rank == null) return false;

            int stageCount = rank.stages.Length;
            int count = rank.maximumPoints - rank.minimumPoints + 1;
            int rawStage = count == 1 ? 1 :
                Mathf.Clamp(((points - rank.minimumPoints) * stageCount) / count + 1,
                    1, stageCount);
            int effectiveStage = recovery != null && recovery.nodeId == currentNodeId &&
                recovery.DowngradePending
                ? Mathf.Max(1, rawStage - 1) : rawStage;
            RankEnemyDifficultyStage stage = rank.stages[effectiveStage - 1];
            profile = new EnemyMatchProfile(rank.rank, rawStage, effectiveStage,
                stage.progressionLevel, stage.deckPreset);
            return true;
        }
    }
}
