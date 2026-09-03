using BackpackHero.Battle;

namespace BackpackHero.Config
{
    /// <summary>Named, immutable balance datasets used for controlled comparisons.</summary>
    public enum BalanceProfile
    {
        Legacy,
        Proposed,
    }

    public readonly struct EquipmentEffectBalanceConfig
    {
        public EquipmentEffectBalanceConfig(float interval, float damageMultiplier,
            float explosionRadius = 0f, float areaDamageMultiplier = 0f,
            int maximumChainTargets = 0, float chainDamageMultiplier = 0f,
            int maximumLinkedTargets = 0)
        {
            Interval = interval;
            DamageMultiplier = damageMultiplier;
            ExplosionRadius = explosionRadius;
            AreaDamageMultiplier = areaDamageMultiplier;
            MaximumChainTargets = maximumChainTargets;
            ChainDamageMultiplier = chainDamageMultiplier;
            MaximumLinkedTargets = maximumLinkedTargets;
        }

        public float Interval { get; }
        public float DamageMultiplier { get; }
        public float ExplosionRadius { get; }
        public float AreaDamageMultiplier { get; }
        public int MaximumChainTargets { get; }
        public float ChainDamageMultiplier { get; }
        public int MaximumLinkedTargets { get; }
    }

    internal static class BalanceProfileData
    {
        public static void Apply(BalanceProfile profile, ItemConfig item)
        {
            if (profile != BalanceProfile.Legacy || item == null) return;
            switch (item.Id)
            {
                case "aircraft_charge": item.Cooldown = 1.8f; break;
                case "aircraft_shield": item.Cooldown = 3.5f; break;
                case "aircraft_explosive": item.Cooldown = 2f; break;
                case "aircraft_sniper": item.Cooldown = 4.5f; item.EquipmentItemModifier = .2f; break;
                case "aircraft_laser": item.Cooldown = 2f; break;
                case "aircraft_shotgun": item.Cooldown = 3.5f; break;
                case "aircraft_l": item.Cooldown = 4f; break;
                case "equipment_first": item.Cooldown = 4f; break;
                case "equipment_rapid_cannon": item.Cooldown = 2.5f; break;
                case "equipment_laser_link": item.Cooldown = 3f; break;
                case "equipment_1x2": item.Cooldown = 2.5f; break;
                case "equipment_arc_coil": item.Cooldown = 3.5f; break;
            }
        }

        public static void Apply(BalanceProfile profile, FighterConfig fighter)
        {
            if (profile != BalanceProfile.Legacy || fighter == null) return;
            switch (fighter.Id)
            {
                case "fighter_charge": fighter.MaximumHealth = 6; fighter.AttackInterval = .25f; fighter.ProjectileDamage = .8f; break;
                case "fighter_shield": fighter.AttackInterval = 1f; fighter.ProjectileDamage = 2f; break;
                case "fighter_explosive": fighter.MaximumHealth = 12; fighter.ProjectileDamage = .8f; break;
                case "fighter_sniper": fighter.MaximumHealth = 6; fighter.ProjectileDamage = 2.5f; break;
                case "fighter_laser": fighter.AttackInterval = 1f; fighter.ProjectileDamage = .5f; break;
                case "fighter_shotgun": fighter.ProjectileDamage = .8f; break;
            }
        }

        public static EquipmentEffectBalanceConfig GetEquipmentEffect(
            BalanceProfile profile, string itemId)
        {
            bool legacy = profile == BalanceProfile.Legacy;
            return itemId switch
            {
                "equipment_first" => legacy
                    ? new EquipmentEffectBalanceConfig(.8f, 1f, .8f, .5f)
                    : new EquipmentEffectBalanceConfig(.9f, .75f, .8f, .4f),
                "equipment_rapid_cannon" => legacy
                    ? new EquipmentEffectBalanceConfig(.6f, 1f)
                    : new EquipmentEffectBalanceConfig(.55f, .65f),
                "equipment_wave_emitter" => legacy
                    ? new EquipmentEffectBalanceConfig(.8f, 1f)
                    : new EquipmentEffectBalanceConfig(.75f, .8f),
                "equipment_laser_link" => legacy
                    ? new EquipmentEffectBalanceConfig(3f, 1f, maximumLinkedTargets: 3)
                    : new EquipmentEffectBalanceConfig(2.5f, .45f, maximumLinkedTargets: 2),
                "equipment_1x2" => legacy
                    ? new EquipmentEffectBalanceConfig(.8f, 1f)
                    : new EquipmentEffectBalanceConfig(.8f, .9f),
                "equipment_arc_coil" => legacy
                    ? new EquipmentEffectBalanceConfig(.8f, 1f, maximumChainTargets: 3, chainDamageMultiplier: .5f)
                    : new EquipmentEffectBalanceConfig(.9f, .55f, maximumChainTargets: 2, chainDamageMultiplier: .4f),
                _ => new EquipmentEffectBalanceConfig(0f, 1f),
            };
        }

        public static EquipmentEffectBalanceConfig GetAircraftDeathExplosion(
            BalanceProfile profile, string itemId) => itemId == "aircraft_explosive"
            ? profile == BalanceProfile.Legacy
                ? new EquipmentEffectBalanceConfig(0f, 1f, .8f, .5f)
                : new EquipmentEffectBalanceConfig(0f, 1f, .8f, .4f)
            : new EquipmentEffectBalanceConfig(0f, 1f);
    }
}
