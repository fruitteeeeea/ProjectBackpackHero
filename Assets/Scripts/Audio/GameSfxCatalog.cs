using System;
using BackpackHero.Battle;
using UnityEngine;

namespace BackpackHero.Audio
{
    [CreateAssetMenu(fileName = "GameSfxCatalog", menuName = "Backpack Hero/Audio/SFX Catalog")]
    public sealed class GameSfxCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class EquipmentProjectileOverride
        {
            public BattleAttack2D ProjectilePrefab;
            public AudioClip Clip;
        }

        [Header("UI and Backpack")]
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip backpackItem;

        [Header("Battle")]
        [SerializeField] private AudioClip fighterHit;
        [SerializeField] private AudioClip defaultProjectile;
        [SerializeField] private AudioClip equipmentProjectile;
        [SerializeField] private AudioClip laserProjectile;
        [SerializeField] private AudioClip spreadProjectile;
        [SerializeField] private AudioClip victory;
        [SerializeField] private EquipmentProjectileOverride[] equipmentProjectileOverrides = Array.Empty<EquipmentProjectileOverride>();

        public AudioClip UiClick => uiClick;
        public AudioClip BackpackItem => backpackItem;
        public AudioClip FighterHit => fighterHit;
        public AudioClip DefaultProjectile => defaultProjectile;
        public AudioClip EquipmentProjectile => equipmentProjectile;
        public AudioClip LaserProjectile => laserProjectile;
        public AudioClip SpreadProjectile => spreadProjectile;
        public AudioClip Victory => victory;

        public AudioClip FindEquipmentProjectileOverride(BattleAttack2D projectilePrefab)
        {
            if (projectilePrefab == null || equipmentProjectileOverrides == null) return null;
            foreach (EquipmentProjectileOverride entry in equipmentProjectileOverrides)
                if (entry != null && entry.ProjectilePrefab == projectilePrefab && entry.Clip != null) return entry.Clip;
            return null;
        }
    }
}
