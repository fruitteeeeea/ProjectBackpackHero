using System;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    [Serializable]
    public sealed class RankInfoReward
    {
        public Sprite icon;
        public int count;
        public string displayName;
        public bool isPack;
    }

    [Serializable]
    public sealed class RankInfoUnlockItem
    {
        public Sprite icon;
        public string displayName;
        public bool isEquipment;
    }

    [Serializable]
    public sealed class RankInfoEntry
    {
        public int id;
        public int level;
        public int score;
        public int type;
        public string enName;
        public int[] unlockIds;
        public RankInfoUnlockItem[] unlocks;
        public RankInfoReward[] rewards;
        public Sprite iconSprite;
        public Sprite lockedIconSprite;
    }
}
