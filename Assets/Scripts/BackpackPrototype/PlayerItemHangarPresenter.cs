using System.Collections.Generic;
using System.Linq;
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

            var deck = new List<HangarItemSnapshot>();
            foreach (ItemData item in system.GetDeckItems())
                deck.Add(item != null ? BuildSnapshot(item) : default);

            // Preserve catalog order inside each category, but always expose aircraft before
            // equipment in the collection.  Progression is currently uniform (all items are
            // unlocked and max level), so it deliberately does not affect the sort order.
            var collection = system.GetAllItems()
                .Where(item => item != null && !system.IsEquipped(item))
                .OrderBy(item => item.ItemType == ItemType.Aircraft ? 0 : 1)
                .Select(BuildSnapshot)
                .ToList();

            hangar.BindDeckAndCollection(
                deck,
                collection,
                system.Gold,
                system.Diamond,
                HandleDeckReplacement);
        }

        private void HandleDeckReplacement(int slot, HangarItemSnapshot snapshot)
        {
            if (system == null || string.IsNullOrEmpty(snapshot.ItemId)) return;
            ItemData item = null;
            foreach (ItemData candidate in system.GetAllItems())
                if (candidate != null && candidate.ItemId == snapshot.ItemId) { item = candidate; break; }
            system.TryEquipDeckSlot(slot, item);
        }

        private HangarItemSnapshot BuildSnapshot(ItemData item)
        {
            int level = system.GetLevel(item);
            return new HangarItemSnapshot(
                item.ItemType == ItemType.Aircraft ? HangarItemKind.Aircraft : HangarItemKind.Equipment,
                item.ItemName, item.Description, item.Icon, item.Icon, system.IsUnlocked(item), level,
                system.GetFragments(item), item.UpgradeFragmentCost, item.UpgradeGoldCost,
                item.CooldownDuration, item.SpawnCount, null, item.BackgroundColor,
                item.UnlockRequirementText, BuildDetailAttributes(item), item.ItemId);
        }

        private static HangarDetailAttribute[] BuildDetailAttributes(ItemData item)
        {
            if (item.ItemType == ItemType.Equipment) return BuildEquipmentDetailAttributes(item);
            if (item.FighterDefinition == null) return null;
            FighterDefinition fighter = item.FighterDefinition;
            // The migrated UICardInfo prefab serializes its six visible rows in this order:
            // Attack, HP, Attack Speed, Firing Range, CD, Speed.  This differs from the source
            // PlanetWar textAttList order, so values must follow the actual prefab layout.
            return new[]
            {
                new HangarDetailAttribute("Attack", GetDisplayedAircraftDamage(fighter).ToString("0.#")),
                new HangarDetailAttribute("HP", fighter.MaximumHealth.ToString()),
                new HangarDetailAttribute("Attack Speed", fighter.AttackInterval.ToString("0.##") + "s"),
                new HangarDetailAttribute("Firing Range", fighter.AttackRange.ToString("0.#")),
                new HangarDetailAttribute("CD", item.Cd.ToString("0.##") + "s"),
                new HangarDetailAttribute("Speed", fighter.BaseSpeed.ToString("0.#"))
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

    }
}
