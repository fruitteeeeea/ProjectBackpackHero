using System;
using UnityEngine;

namespace PlanetWar.ReusableMainMenu
{
    [Serializable]
    public sealed class RankEntry
    {
        public string displayName;
        public int score;
        public Sprite countryFlag;
        public bool isCurrentPlayer;
    }
}
