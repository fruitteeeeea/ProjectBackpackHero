using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>运行时使用的一组不可变节奏倍率。</summary>
    public readonly struct GamePacingMultipliers
    {
        public const float MinimumMultiplier = 0.5f;
        public const float MaximumMultiplier = 2f;

        public GamePacingMultipliers(
            float aircraftSpeed,
            float projectileDamage,
            float aircraftHealth,
            float playerBackpackHealth,
            float enemyBackpackHealth,
            float playerOverallStrength,
            float enemyOverallStrength)
        {
            AircraftSpeed = Clamp(aircraftSpeed);
            ProjectileDamage = Clamp(projectileDamage);
            AircraftHealth = Clamp(aircraftHealth);
            PlayerBackpackHealth = Clamp(playerBackpackHealth);
            EnemyBackpackHealth = Clamp(enemyBackpackHealth);
            PlayerOverallStrength = Clamp(playerOverallStrength);
            EnemyOverallStrength = Clamp(enemyOverallStrength);
        }

        public float AircraftSpeed { get; }
        public float ProjectileDamage { get; }
        public float AircraftHealth { get; }
        public float PlayerBackpackHealth { get; }
        public float EnemyBackpackHealth { get; }
        public float PlayerOverallStrength { get; }
        public float EnemyOverallStrength { get; }

        public static GamePacingMultipliers Default =>
            new(1f, 1f, 1f, 1f, 1f, 1f, 1f);

        private static float Clamp(float value)
        {
            return Mathf.Clamp(
                value,
                MinimumMultiplier,
                MaximumMultiplier);
        }
    }
}
