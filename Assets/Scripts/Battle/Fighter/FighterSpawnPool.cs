using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BackpackHero.Battle
{
    [CreateAssetMenu(
        fileName = "New Fighter Spawn Pool",
        menuName = "Battle/Fighter Spawn Pool")]
    public sealed class FighterSpawnPool : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private FighterDefinition definition;

            [Tooltip("相对生成权重。设为0时不会被随机选中。")]
            [SerializeField, Min(0f)]
            private float weight = 1f;

            public FighterDefinition Definition =>
                definition;

            public float Weight =>
                Mathf.Max(0f, weight);
        }

        [SerializeField]
        private Entry[] entries = Array.Empty<Entry>();

        public int EntryCount =>
            entries?.Length ?? 0;

        /// <summary>
        /// 按照各个条目的相对权重随机选择一种飞机。
        /// </summary>
        public bool TryGetRandom(
            out FighterDefinition definition)
        {
            definition = null;

            if (entries == null ||
                entries.Length == 0)
            {
                Debug.LogWarning(
                    $"{name}没有配置任何飞机。",
                    this);

                return false;
            }

            float totalWeight = 0f;

            foreach (Entry entry in entries)
            {
                if (!IsUsable(entry))
                {
                    continue;
                }

                totalWeight += entry.Weight;
            }

            if (totalWeight <= Mathf.Epsilon)
            {
                Debug.LogWarning(
                    $"{name}中没有可生成的飞机。" +
                    "请检查Definition和Weight。",
                    this);

                return false;
            }

            float randomValue =
                Random.value * totalWeight;

            foreach (Entry entry in entries)
            {
                if (!IsUsable(entry))
                {
                    continue;
                }

                randomValue -= entry.Weight;

                if (randomValue <= 0f)
                {
                    definition = entry.Definition;
                    return true;
                }
            }

            // 浮点误差保险：返回最后一个可用条目。
            for (int index = entries.Length - 1;
                 index >= 0;
                 index--)
            {
                Entry entry = entries[index];

                if (!IsUsable(entry))
                {
                    continue;
                }

                definition = entry.Definition;
                return true;
            }

            return false;
        }

        public FighterDefinition GetDefinition(
            int index)
        {
            if (entries == null ||
                index < 0 ||
                index >= entries.Length)
            {
                return null;
            }

            return entries[index]?.Definition;
        }

        private static bool IsUsable(Entry entry)
        {
            return entry != null &&
                   entry.Definition != null &&
                   entry.Weight > 0f;
        }
    }
}