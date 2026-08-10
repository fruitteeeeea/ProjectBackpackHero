using System;
using UnityEngine;

namespace BackpackHero.Battle
{
    [Serializable]
    public struct EnemyStrengthMultipliers
    {
        [SerializeField, Min(0.01f)] private float health;
        [SerializeField, Min(0.01f)] private float damage;

        public EnemyStrengthMultipliers(float healthMultiplier, float damageMultiplier)
        {
            health = Mathf.Max(0.01f, healthMultiplier);
            damage = Mathf.Max(0.01f, damageMultiplier);
        }

        public float Health => health;
        public float Damage => damage;
    }

    /// <summary>五关三回合档位的正式难度配置。</summary>
    [CreateAssetMenu(fileName = "LevelDifficultySettings", menuName = "Battle/Level Difficulty Settings")]
    public sealed class LevelDifficultySettings : ScriptableObject
    {
        public const int LevelCount = 5;
        public const int StageCount = 3;
        public const int BackpackRoundMultiplierCount = 6;

        [SerializeField, Min(0.01f)] private float playerAircraftHealthMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float playerAircraftDamageMultiplier = 1f;
        [SerializeField] private float[] backpackRoundHealthMultipliers = CreateDefaultBackpackRoundHealthMultipliers();
        [SerializeField] private EnemyStrengthMultipliers[] enemyStages = CreateDefaultStages();

        public float PlayerAircraftHealthMultiplier => playerAircraftHealthMultiplier;
        public float PlayerAircraftDamageMultiplier => playerAircraftDamageMultiplier;

        public float GetBackpackRoundHealthMultiplier(int round)
        {
            EnsureBackpackRoundMultiplierArray();
            int index = Mathf.Clamp(
                round,
                1,
                BackpackRoundMultiplierCount) - 1;
            return backpackRoundHealthMultipliers[index];
        }

        public EnemyStrengthMultipliers GetEnemyStrength(int level, int round)
        {
            EnsureStageArray();
            int levelIndex = Mathf.Clamp(level, 1, LevelCount) - 1;
            return enemyStages[levelIndex * StageCount + GetStageIndex(round)];
        }

        public void SetPlayerMultipliers(float healthMultiplier, float damageMultiplier)
        {
            playerAircraftHealthMultiplier = Mathf.Max(0.01f, healthMultiplier);
            playerAircraftDamageMultiplier = Mathf.Max(0.01f, damageMultiplier);
        }

        public void SetBackpackRoundHealthMultiplier(
            int round,
            float multiplier)
        {
            EnsureBackpackRoundMultiplierArray();
            int index = Mathf.Clamp(
                round,
                1,
                BackpackRoundMultiplierCount) - 1;
            backpackRoundHealthMultipliers[index] =
                Mathf.Max(0.01f, multiplier);
        }

        public void SetEnemyStrength(int level, int stageIndex, float healthMultiplier, float damageMultiplier)
        {
            EnsureStageArray();
            int levelIndex = Mathf.Clamp(level, 1, LevelCount) - 1;
            int clampedStage = Mathf.Clamp(stageIndex, 0, StageCount - 1);
            enemyStages[levelIndex * StageCount + clampedStage] = new EnemyStrengthMultipliers(healthMultiplier, damageMultiplier);
        }

        public void CopyFrom(LevelDifficultySettings source)
        {
            if (source == null) return;
            source.EnsureStageArray();
            source.EnsureBackpackRoundMultiplierArray();
            playerAircraftHealthMultiplier = source.playerAircraftHealthMultiplier;
            playerAircraftDamageMultiplier = source.playerAircraftDamageMultiplier;
            backpackRoundHealthMultipliers =
                (float[])source.backpackRoundHealthMultipliers.Clone();
            enemyStages = (EnemyStrengthMultipliers[])source.enemyStages.Clone();
        }

        public bool ContentEquals(LevelDifficultySettings other)
        {
            if (other == null) return false;
            EnsureStageArray();
            EnsureBackpackRoundMultiplierArray();
            other.EnsureStageArray();
            other.EnsureBackpackRoundMultiplierArray();
            if (!Mathf.Approximately(playerAircraftHealthMultiplier, other.playerAircraftHealthMultiplier) ||
                !Mathf.Approximately(playerAircraftDamageMultiplier, other.playerAircraftDamageMultiplier)) return false;
            for (int i = 0;
                 i < backpackRoundHealthMultipliers.Length;
                 i++)
            {
                if (!Mathf.Approximately(
                        backpackRoundHealthMultipliers[i],
                        other.backpackRoundHealthMultipliers[i]))
                {
                    return false;
                }
            }
            for (int i = 0; i < enemyStages.Length; i++)
            {
                if (!Mathf.Approximately(enemyStages[i].Health, other.enemyStages[i].Health) ||
                    !Mathf.Approximately(enemyStages[i].Damage, other.enemyStages[i].Damage)) return false;
            }
            return true;
        }

        public static int GetStageIndex(int round) => round <= 1 ? 0 : round == 2 ? 1 : 2;

        private void EnsureStageArray()
        {
            if (enemyStages != null && enemyStages.Length == LevelCount * StageCount) return;
            EnemyStrengthMultipliers[] defaults = CreateDefaultStages();
            if (enemyStages != null)
                Array.Copy(enemyStages, defaults, Mathf.Min(enemyStages.Length, defaults.Length));
            enemyStages = defaults;
        }

        private void EnsureBackpackRoundMultiplierArray()
        {
            if (backpackRoundHealthMultipliers != null &&
                backpackRoundHealthMultipliers.Length ==
                BackpackRoundMultiplierCount)
            {
                for (int index = 0;
                     index < backpackRoundHealthMultipliers.Length;
                     index++)
                {
                    backpackRoundHealthMultipliers[index] =
                        Mathf.Max(
                            0.01f,
                            backpackRoundHealthMultipliers[index]);
                }

                return;
            }

            float[] defaults =
                CreateDefaultBackpackRoundHealthMultipliers();
            if (backpackRoundHealthMultipliers != null)
            {
                Array.Copy(
                    backpackRoundHealthMultipliers,
                    defaults,
                    Mathf.Min(
                        backpackRoundHealthMultipliers.Length,
                        defaults.Length));
            }

            backpackRoundHealthMultipliers = defaults;
        }

        private static float[] CreateDefaultBackpackRoundHealthMultipliers()
        {
            return new[] { .75f, .8f, .85f, .9f, .95f, 1f };
        }

        private static EnemyStrengthMultipliers[] CreateDefaultStages()
        {
            float[,] values = { { .79f, .79f }, { .82f, .81f }, { .84f, .84f }, { .82f, .82f }, { .85f, .84f }, { .87f, .87f }, { .85f, .85f }, { .87f, .87f }, { .89f, .89f }, { .87f, .88f }, { .90f, .90f }, { .92f, .92f }, { .90f, .90f }, { .92f, .93f }, { .95f, .95f } };
            EnemyStrengthMultipliers[] result = new EnemyStrengthMultipliers[LevelCount * StageCount];
            for (int i = 0; i < result.Length; i++) result[i] = new EnemyStrengthMultipliers(values[i, 0], values[i, 1]);
            return result;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            playerAircraftHealthMultiplier = Mathf.Max(.01f, playerAircraftHealthMultiplier);
            playerAircraftDamageMultiplier = Mathf.Max(.01f, playerAircraftDamageMultiplier);
            EnsureBackpackRoundMultiplierArray();
            EnsureStageArray();
        }
#endif
    }
}
