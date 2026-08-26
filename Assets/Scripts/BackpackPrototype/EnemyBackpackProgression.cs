using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackPrototype
{
    public static class EnemyBackpackProgression
    {
        public static int CalculateDefaultLevel(
            IReadOnlyList<ItemData> deck,
            Func<ItemData, int> getPlayerLevel)
        {
            if (deck == null || getPlayerLevel == null)
            {
                return PlayerItemSystem.DefaultLevel;
            }

            Dictionary<int, int> counts = new();
            foreach (ItemData item in deck)
            {
                if (item == null)
                {
                    continue;
                }

                int level = Mathf.Clamp(
                    getPlayerLevel(item),
                    PlayerItemSystem.DefaultLevel,
                    PlayerItemSystem.MaximumLevel);
                counts[level] = counts.TryGetValue(level, out int count)
                    ? count + 1
                    : 1;
            }

            int modeLevel = PlayerItemSystem.DefaultLevel;
            int modeCount = 0;
            foreach (KeyValuePair<int, int> entry in counts)
            {
                if (entry.Value > modeCount ||
                    (entry.Value == modeCount && entry.Key < modeLevel))
                {
                    modeLevel = entry.Key;
                    modeCount = entry.Value;
                }
            }

            return Mathf.Max(PlayerItemSystem.DefaultLevel, modeLevel - 1);
        }
    }
}