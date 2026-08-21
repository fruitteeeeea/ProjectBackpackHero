using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>
    /// Read-only strength summary for the items currently placed in a backpack.
    /// </summary>
    public readonly struct BackpackStrengthScore
    {
        public BackpackStrengthScore(
            int aircraftCount,
            int equipmentCount,
            float aircraftScore,
            float equipmentScore)
        {
            AircraftCount = aircraftCount;
            EquipmentCount = equipmentCount;
            AircraftScore = aircraftScore;
            EquipmentScore = equipmentScore;
        }

        public int AircraftCount { get; }
        public int EquipmentCount { get; }
        public float AircraftScore { get; }
        public float EquipmentScore { get; }
        public float TotalScore => AircraftScore + EquipmentScore;
    }

    /// <summary>
    /// Calculates design-time backpack strength without changing backpack state.
    /// </summary>
    public static class BackpackStrengthCalculator
    {
        public const float AircraftBaseScore = 10f;
        public const float EquipmentBaseScore = 2f;
        public const float EquipmentAdjacentAircraftScore = 8f;

        public static BackpackStrengthScore Calculate(
            BackpackController backpack)
        {
            if (backpack == null)
            {
                return default;
            }

            int aircraftCount = 0;
            int equipmentCount = 0;
            float aircraftScore = 0f;
            float equipmentScore = 0f;

            foreach (ItemInstance item in backpack.Items)
            {
                if (item?.Data == null)
                {
                    continue;
                }

                float levelMultiplier = GetLevelMultiplier(item.Level);
                if (item.Data.ItemType == ItemType.Aircraft)
                {
                    aircraftCount++;
                    aircraftScore += AircraftBaseScore * levelMultiplier;
                    continue;
                }

                if (item.Data.ItemType == ItemType.Equipment)
                {
                    equipmentCount++;
                    equipmentScore +=
                        (EquipmentBaseScore +
                         backpack.GetAdjacentAircraftItems(item).Count *
                         EquipmentAdjacentAircraftScore) *
                        levelMultiplier;
                }
            }

            return new BackpackStrengthScore(
                aircraftCount,
                equipmentCount,
                aircraftScore,
                equipmentScore);
        }

        private static float GetLevelMultiplier(int level)
        {
            return Mathf.Clamp(
                level,
                ItemInstance.DefaultLevel,
                ItemInstance.MaximumLevel) switch
            {
                2 => 1.25f,
                3 => 1.5f,
                _ => 1f,
            };
        }
    }
}
