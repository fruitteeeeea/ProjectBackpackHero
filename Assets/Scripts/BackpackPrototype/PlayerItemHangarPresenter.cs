using System.Collections.Generic;
using System.Text;
using BackpackHero.Battle;
using BackpackHero.Debugging;
using PlanetWar.ReusableMainMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackPrototype
{
    [DefaultExecutionOrder(200)]
    public sealed class PlayerItemHangarPresenter : MonoBehaviour
    {
        private PlayerItemSystem system;
        private HangarView hangar;

        private void Awake()
        {
            system = PlayerItemSystem.Instance ?? FindFirstObjectByType<PlayerItemSystem>();
            hangar = FindFirstObjectByType<HangarView>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            if (system != null) system.Changed += Refresh;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            Refresh();
        }

        private void OnDisable()
        {
            if (system != null) system.Changed -= Refresh;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            hangar = null;
            Refresh();
        }

        public void Refresh()
        {
            if (system == null) system = PlayerItemSystem.Instance;
            if (hangar == null) hangar = FindFirstObjectByType<HangarView>(FindObjectsInactive.Include);
            if (system == null || hangar == null) return;

            var snapshots = new List<HangarItemSnapshot>();
            foreach (ItemData item in system.GetAllItems()) snapshots.Add(BuildSnapshot(item));
            hangar.Bind(snapshots, system.Gold, system.Diamond);
        }

        private HangarItemSnapshot BuildSnapshot(ItemData item)
        {
            int level = system.GetLevel(item);
            string stats = item.ItemType == ItemType.Aircraft ? BuildAircraftStats(item) : BuildEquipmentStats(item);
            return new HangarItemSnapshot(
                item.ItemType == ItemType.Aircraft ? HangarItemKind.Aircraft : HangarItemKind.Equipment,
                item.ItemName, string.IsNullOrEmpty(stats) ? item.Description : item.Description + "\n\n" + stats, item.Icon, item.Icon, system.IsUnlocked(item), level,
                system.GetFragments(item), item.UpgradeFragmentCost, item.UpgradeGoldCost,
                item.CooldownDuration, item.SpawnCount, stats, item.BackgroundColor,
                item.UnlockRequirementText, BuildDetailAttributes(item));
        }

        private static HangarDetailAttribute[] BuildDetailAttributes(ItemData item)
        {
            if (item.ItemType == ItemType.Equipment) return BuildEquipmentDetailAttributes(item);
            if (item.FighterDefinition == null) return null;
            FighterDefinition fighter = item.FighterDefinition;
            // The migrated UICardInfo prefab serializes its six visible rows in this order:
            // Attack, HP, Attack Speed, Firing Range, CD, Cost.  This differs from the source
            // PlanetWar textAttList order, so values must follow the actual prefab layout.
            return new[]
            {
                new HangarDetailAttribute("攻击", GetDisplayedAircraftDamage(fighter).ToString("0.#")),
                new HangarDetailAttribute("生命", fighter.MaximumHealth.ToString()),
                new HangarDetailAttribute("攻击间隔", fighter.AttackInterval.ToString("0.##") + "s"),
                new HangarDetailAttribute("射程", fighter.AttackRange.ToString("0.#")),
                new HangarDetailAttribute("冷却", item.Cd.ToString("0.##") + "s"),
                new HangarDetailAttribute("价格", item.Price.ToString())
            };
        }

        // FighterCombat2D applies the two gameplay damage multipliers before emitting the
        // damage event, and FighterDamageFloatingText then applies this display-only magic
        // number. Showing the same product keeps the Hangar attack value equal to battle text.
        private static float GetDisplayedAircraftDamage(FighterDefinition fighter)
        {
            return fighter.ProjectileDamage *
                   GamePacingDebugRuntime.GetProjectileDamageMultiplier(BattleFaction.Player) *
                   LevelDifficultyRuntime.GetProjectileDamageMultiplier(BattleFaction.Player) *
                   GamePacingDebugRuntime.GetDamageFloatingTextMagicNumber();
        }

        private static HangarDetailAttribute[] BuildEquipmentDetailAttributes(ItemData item)
        {
            float range = 0f, damage = 0f, interval = 0f;
            bool hasStats = false;
            foreach (EquipmentEffectDefinition effect in item.EquipmentEffects)
            {
                if (effect == null || !effect.TryGetHangarStats(out EquipmentHangarStats stats)) continue;
                hasStats = true;
                range = Mathf.Max(range, stats.Range);
                damage += stats.Damage;
                interval = interval <= 0f ? stats.Interval : Mathf.Min(interval, stats.Interval);
            }
            return new[]
            {
                new HangarDetailAttribute("冷却", item.Cd.ToString("0.##") + "s"),
                new HangarDetailAttribute("作用范围", range.ToString("0.#"), hasStats && range > 0f),
                new HangarDetailAttribute("伤害", damage.ToString("0.#"), hasStats && damage > 0f),
                new HangarDetailAttribute("效果间隔", interval.ToString("0.##") + "s", hasStats && interval > 0f)
            };
        }

        private static string BuildAircraftStats(ItemData item)
        {
            var fighter = item.FighterDefinition;
            if (fighter == null) return "Aircraft configuration unavailable.";
            return $"Type: Aircraft  Shape: {BuildShape(item)}\n" +
                   $"Cooldown {item.CooldownDuration:0.##}s  Spawn {item.SpawnCount}\n" +
                   $"HP {fighter.MaximumHealth}  Damage {GetDisplayedAircraftDamage(fighter):0.#}\n" +
                   $"Attack {fighter.AttackInterval:0.##}s  Speed {fighter.BaseSpeed:0.#}\n" +
                   $"Range {fighter.AttackRange:0.#}";
        }

        private static string BuildEquipmentStats(ItemData item)
        {
            var builder = new StringBuilder($"Type: Equipment  Shape: {BuildShape(item)}\nCooldown {item.Cd:0.##}s");
            foreach (EquipmentEffectDefinition effect in item.EquipmentEffects)
            {
                if (effect is ProjectileEquipmentEffectDefinition projectile)
                {
                    string projectileName = projectile.ProjectilePrefab != null
                        ? projectile.ProjectilePrefab.name
                        : "None";
                    builder.Append($"\nEffect: {effect.name}  Projectile: {projectileName}, {projectile.Cooldown:0.##}s");
                }
                else if (effect != null) builder.Append($"\nEffect: {effect.name}");
            }
            return builder.ToString();
        }

        private static string BuildShape(ItemData item)
        {
            if (item.ShapeOffsets.Count == 0) return "None";
            var builder = new StringBuilder();
            foreach (Vector2Int cell in item.ShapeOffsets)
            {
                if (builder.Length > 0) builder.Append(' ');
                builder.Append($"({cell.x},{cell.y})");
            }
            return builder.ToString();
        }
    }
}
