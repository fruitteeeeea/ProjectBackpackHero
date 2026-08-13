using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    public enum HangarItemKind { Aircraft, Equipment }
    public readonly struct HangarItemSnapshot
    {
        public HangarItemSnapshot(HangarItemKind kind, string name, string description, Sprite icon, Sprite lockedIcon, bool unlocked, int level, int fragments, int requiredFragments, int goldCost, float cooldown, int spawnCount, string stats, Color backgroundColor, string unlockRequirementText, string[] detailValues = null)
        { Kind = kind; Name = name; Description = description; Icon = icon; LockedIcon = lockedIcon; Unlocked = unlocked; Level = level; Fragments = fragments; RequiredFragments = requiredFragments; GoldCost = goldCost; Cooldown = cooldown; SpawnCount = spawnCount; Stats = stats; BackgroundColor = backgroundColor; UnlockRequirementText = unlockRequirementText; DetailValues = detailValues; }
        public HangarItemKind Kind { get; } public string Name { get; } public string Description { get; } public Sprite Icon { get; } public Sprite LockedIcon { get; } public bool Unlocked { get; } public int Level { get; } public int Fragments { get; } public int RequiredFragments { get; } public int GoldCost { get; } public float Cooldown { get; } public int SpawnCount { get; } public string Stats { get; } public Color BackgroundColor { get; } public string UnlockRequirementText { get; } public string[] DetailValues { get; }
        public bool IsMaxLevel => Level >= 2;
    }
}
