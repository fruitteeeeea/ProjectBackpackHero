using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>Authoritative card-pack tuning migrated from PlanetWar's tbpack/tbconfig tables.</summary>
    [CreateAssetMenu(fileName = "PackCatalog", menuName = "Backpack Hero/Pack Catalog")]
    public sealed class PackCatalog : ScriptableObject
    {
        [SerializeField] int secondsPerDiamond = 60;
        [SerializeField] List<PackDefinition> definitions = new();

        public int SecondsPerDiamond => secondsPerDiamond;
        public IReadOnlyList<PackDefinition> Definitions => definitions;
        public PackDefinition GetDefinition(PackId id) => definitions.FirstOrDefault(x => x != null && x.Id == id);
    }

    [Serializable]
    public sealed class PackDefinition
    {
        [SerializeField] PackId id;
        [SerializeField] int visualIndex;
        [SerializeField] string name;
        [SerializeField] int goldMin;
        [SerializeField] int goldMax;
        [SerializeField] int diamondMin;
        [SerializeField] int diamondMax;
        [SerializeField] int fragmentMin;
        [SerializeField] int fragmentMax;
        [SerializeField] int openSeconds;
        [SerializeField] int weight;
        [SerializeField] string spineSkin;

        public PackId Id => id;
        public int VisualIndex => visualIndex;
        public string Name => name;
        public int GoldMin => goldMin;
        public int GoldMax => goldMax;
        public int DiamondMin => diamondMin;
        public int DiamondMax => diamondMax;
        public int FragmentMin => fragmentMin;
        public int FragmentMax => fragmentMax;
        public int OpenSeconds => openSeconds;
        public int Weight => weight;
        public string SpineSkin => spineSkin;
    }
}
