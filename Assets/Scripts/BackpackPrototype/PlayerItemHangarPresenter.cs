using System.Collections.Generic;
using System.Text;
using BackpackHero.Battle;
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
                item.UnlockRequirementText, BuildDetailValues(item));
        }

        private static string[] BuildDetailValues(ItemData item)
        {
            if (item.ItemType != ItemType.Aircraft || item.FighterDefinition == null) return null;
            FighterDefinition fighter = item.FighterDefinition;
            // Matches the retained UICardInfo detail slots: HP, attack, third utility slot,
            // range, attack interval, and cooldown. The third slot has no target-project price,
            // so it deliberately displays the meaningful aircraft spawn count instead.
            return new[]
            {
                fighter.MaximumHealth.ToString(),
                fighter.ProjectileDamage.ToString("0.#"),
                item.SpawnCount.ToString(),
                fighter.AttackRange.ToString("0.#"),
                fighter.AttackInterval.ToString("0.##") + "s",
                item.CooldownDuration.ToString("0.##") + "s"
            };
        }

        private static string BuildAircraftStats(ItemData item)
        {
            var fighter = item.FighterDefinition;
            if (fighter == null) return "Aircraft configuration unavailable.";
            return $"Type: Aircraft  Shape: {BuildShape(item)}\n" +
                   $"Cooldown {item.CooldownDuration:0.##}s  Spawn {item.SpawnCount}\n" +
                   $"HP {fighter.MaximumHealth}  Damage {fighter.ProjectileDamage:0.#}\n" +
                   $"Attack {fighter.AttackInterval:0.##}s  Speed {fighter.BaseSpeed:0.#}\n" +
                   $"Range {fighter.AttackRange:0.#}";
        }

        private static string BuildEquipmentStats(ItemData item)
        {
            var builder = new StringBuilder($"Type: Equipment  Shape: {BuildShape(item)}\nCooldown {item.CooldownDuration:0.##}s");
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
