using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    public readonly struct HangarDetailAttribute
    {
        public HangarDetailAttribute(string label, string value, bool visible = true)
            : this(label, value, string.Empty, visible) { }
        public HangarDetailAttribute(string label, string value, string additionalValue,
            bool visible = true)
        {
            Label = label;
            Value = value;
            AdditionalValue = additionalValue;
            Visible = visible;
        }
        public string Label { get; }
        public string Value { get; }
        public string AdditionalValue { get; }
        public bool Visible { get; }
    }
    public enum HangarItemKind { Aircraft, Equipment }
    public readonly struct HangarItemSnapshot
    {
        public HangarItemSnapshot(HangarItemKind kind, string name, string description, Sprite icon, Sprite lockedIcon, bool unlocked, int level, int fragments, int requiredFragments, int goldCost, float cooldown, int spawnCount, string stats, Color backgroundColor, string unlockRequirementText, HangarDetailAttribute[] detailAttributes = null, string itemId = null, int maximumLevel = 10, int unlockRank = 1, IReadOnlyList<Vector2Int> shapeOffsets = null)
        { Kind = kind; Name = name; Description = description; Icon = icon; LockedIcon = lockedIcon; Unlocked = unlocked; Level = level; Fragments = fragments; RequiredFragments = requiredFragments; GoldCost = goldCost; Cooldown = cooldown; SpawnCount = spawnCount; Stats = stats; BackgroundColor = backgroundColor; UnlockRequirementText = unlockRequirementText; DetailAttributes = detailAttributes; ItemId = itemId; MaximumLevel = maximumLevel; UnlockRank = unlockRank; ShapeOffsets = shapeOffsets != null ? CopyShapeOffsets(shapeOffsets) : Array.Empty<Vector2Int>(); }
        public HangarItemKind Kind { get; } public string Name { get; } public string Description { get; } public Sprite Icon { get; } public Sprite LockedIcon { get; } public bool Unlocked { get; } public int Level { get; } public int Fragments { get; } public int RequiredFragments { get; } public int GoldCost { get; } public float Cooldown { get; } public int SpawnCount { get; } public string Stats { get; } public Color BackgroundColor { get; } public string UnlockRequirementText { get; } public HangarDetailAttribute[] DetailAttributes { get; } public string ItemId { get; } public int UnlockRank { get; }
        public IReadOnlyList<Vector2Int> ShapeOffsets { get; }
        public int MaximumLevel { get; }
        public bool IsMaxLevel => Level >= (MaximumLevel > 0 ? MaximumLevel : 10);

        private static Vector2Int[] CopyShapeOffsets(IReadOnlyList<Vector2Int> source)
        {
            var copy = new Vector2Int[source.Count];
            for (int index = 0; index < source.Count; index++) copy[index] = source[index];
            return copy;
        }
    }
}
