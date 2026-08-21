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
            int aircraftLevel1Count,
            int aircraftLevel2Count,
            int aircraftLevel3Count,
            int equipmentLevel1Count,
            int equipmentLevel2Count,
            int equipmentLevel3Count,
            float aircraftScore,
            float equipmentScore)
        {
            AircraftCount = aircraftCount;
            EquipmentCount = equipmentCount;
            AircraftLevel1Count = aircraftLevel1Count;
            AircraftLevel2Count = aircraftLevel2Count;
            AircraftLevel3Count = aircraftLevel3Count;
            EquipmentLevel1Count = equipmentLevel1Count;
            EquipmentLevel2Count = equipmentLevel2Count;
            EquipmentLevel3Count = equipmentLevel3Count;
            AircraftScore = aircraftScore;
            EquipmentScore = equipmentScore;
        }

        public int AircraftCount { get; }
        public int EquipmentCount { get; }
        public int AircraftLevel1Count { get; }
        public int AircraftLevel2Count { get; }
        public int AircraftLevel3Count { get; }
        public int EquipmentLevel1Count { get; }
        public int EquipmentLevel2Count { get; }
        public int EquipmentLevel3Count { get; }
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
            int aircraftLevel1Count = 0;
            int aircraftLevel2Count = 0;
            int aircraftLevel3Count = 0;
            int equipmentLevel1Count = 0;
            int equipmentLevel2Count = 0;
            int equipmentLevel3Count = 0;
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
                    IncrementLevelCount(
                        item.Level,
                        ref aircraftLevel1Count,
                        ref aircraftLevel2Count,
                        ref aircraftLevel3Count);
                    aircraftScore += AircraftBaseScore * levelMultiplier;
                    continue;
                }

                if (item.Data.ItemType == ItemType.Equipment)
                {
                    equipmentCount++;
                    IncrementLevelCount(
                        item.Level,
                        ref equipmentLevel1Count,
                        ref equipmentLevel2Count,
                        ref equipmentLevel3Count);
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
                aircraftLevel1Count,
                aircraftLevel2Count,
                aircraftLevel3Count,
                equipmentLevel1Count,
                equipmentLevel2Count,
                equipmentLevel3Count,
                aircraftScore,
                equipmentScore);
        }

        private static void IncrementLevelCount(
            int level,
            ref int level1Count,
            ref int level2Count,
            ref int level3Count)
        {
            switch (Mathf.Clamp(
                        level,
                        ItemInstance.DefaultLevel,
                        ItemInstance.MaximumLevel))
            {
                case 2:
                    level2Count++;
                    break;
                case 3:
                    level3Count++;
                    break;
                default:
                    level1Count++;
                    break;
            }
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
