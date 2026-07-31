using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>
    /// 可持久化的战斗节奏倍率。该资产只保存正式节奏参数；
    /// 游戏速度属于临时调试状态，因此不保存在这里。
    /// </summary>
    [CreateAssetMenu(
        fileName = "GamePacingDebugSettings",
        menuName = "Debug/Game Pacing Settings")]
    public sealed class GamePacingDebugSettings : ScriptableObject
    {
        [Header("Global Multipliers")]
        [SerializeField, Min(0f)]
        private float aircraftSpeedMultiplier = 1f;

        [SerializeField, Min(0f)]
        private float projectileDamageMultiplier = 1f;

        [SerializeField, Min(0f)]
        private float aircraftHealthMultiplier = 1f;

        [SerializeField, Min(0f)]
        private float playerBackpackHealthMultiplier = 1f;

        [SerializeField, Min(0f)]
        private float enemyBackpackHealthMultiplier = 1f;

        [SerializeField, Min(0f)]
        private float playerOverallStrengthMultiplier = 1f;

        [SerializeField, Min(0f)]
        private float enemyOverallStrengthMultiplier = 1f;

        public float AircraftSpeedMultiplier => aircraftSpeedMultiplier;
        public float ProjectileDamageMultiplier => projectileDamageMultiplier;
        public float AircraftHealthMultiplier => aircraftHealthMultiplier;
        public float PlayerBackpackHealthMultiplier => playerBackpackHealthMultiplier;
        public float EnemyBackpackHealthMultiplier => enemyBackpackHealthMultiplier;
        public float PlayerOverallStrengthMultiplier =>
            playerOverallStrengthMultiplier;
        public float EnemyOverallStrengthMultiplier =>
            enemyOverallStrengthMultiplier;

        public void SetValues(GamePacingMultipliers values)
        {
            aircraftSpeedMultiplier = values.AircraftSpeed;
            projectileDamageMultiplier = values.ProjectileDamage;
            aircraftHealthMultiplier = values.AircraftHealth;
            playerBackpackHealthMultiplier = values.PlayerBackpackHealth;
            enemyBackpackHealthMultiplier = values.EnemyBackpackHealth;
            playerOverallStrengthMultiplier = values.PlayerOverallStrength;
            enemyOverallStrengthMultiplier = values.EnemyOverallStrength;
        }

        public GamePacingMultipliers GetValues()
        {
            return new GamePacingMultipliers(
                aircraftSpeedMultiplier,
                projectileDamageMultiplier,
                aircraftHealthMultiplier,
                playerBackpackHealthMultiplier,
                enemyBackpackHealthMultiplier,
                playerOverallStrengthMultiplier,
                enemyOverallStrengthMultiplier);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetValues(GetValues());
        }
#endif
    }
}
