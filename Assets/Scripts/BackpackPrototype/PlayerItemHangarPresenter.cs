using System.Collections.Generic;
using System.Linq;
using BackpackHero.Battle;
using BackpackHero.Debugging;
using BackpackHero.Progression;
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

        private void Start()
        {
            // The menu sample's Hangar page is initially inactive. Refresh again after the
            // scene's Start phase so its serialized card hierarchy is always available on the
            // first visit, including when the presenter was created before the scene loaded.
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

            var collection = OrderCollectionItems(
                    system.GetAllItems(),
                    system.IsEquipped,
                    system.IsUnlocked,
                    GetUnlockRank)
                .Select(BuildSnapshot)
                .ToList();

            hangar.BindDeckAndCollection(
                deck,
                collection,
                system.Gold,
                system.Diamond,
                HandleDeckReplacement,
                HandleUpgrade);
        }

        private void HandleDeckReplacement(int slot, HangarItemSnapshot snapshot)
        {
            if (system == null || string.IsNullOrEmpty(snapshot.ItemId)) return;
            ItemData item = null;
            foreach (ItemData candidate in system.GetAllItems())
                if (candidate != null && candidate.ItemId == snapshot.ItemId) { item = candidate; break; }
            system.TryEquipDeckSlot(slot, item);
        }

        private void HandleUpgrade(HangarItemSnapshot snapshot)
        {
            if (system == null || string.IsNullOrEmpty(snapshot.ItemId)) return;
            system.TryUpgrade(FindItem(snapshot.ItemId));
        }

        private ItemData FindItem(string itemId)
        {
            foreach (ItemData candidate in system.GetAllItems())
                if (candidate != null && candidate.ItemId == itemId) return candidate;
            return null;
        }

        private static IEnumerable<ItemData> OrderCollectionItems(
            IEnumerable<ItemData> items,
            System.Func<ItemData, bool> isEquipped,
            System.Func<ItemData, bool> isUnlocked,
            System.Func<ItemData, int> getUnlockRank)
        {
            if (items == null) return System.Array.Empty<ItemData>();
            return items
                .Select((item, catalogIndex) => new
                {
                    Item = item,
                    CatalogIndex = catalogIndex,
                    Unlocked = item != null && isUnlocked(item),
                    UnlockRank = item != null ? getUnlockRank(item) : 1
                })
                .Where(entry => entry.Item != null && !isEquipped(entry.Item))
                .OrderBy(entry => entry.Item.ItemType == ItemType.Aircraft ? 0 : 1)
                .ThenBy(entry => entry.Unlocked ? 0 : 1)
                .ThenBy(entry => entry.Unlocked ? 0 : entry.UnlockRank)
                .ThenBy(entry => entry.CatalogIndex)
                .Select(entry => entry.Item)
                .ToArray();
        }

        private static int GetUnlockRank(ItemData item) =>
            RankProgressionSystem.Instance?.GetUnlockRankForItem(item.ItemId) ?? 1;

        private HangarItemSnapshot BuildSnapshot(ItemData item)
        {
            int level = system.GetLevel(item);
            int unlockRank = GetUnlockRank(item);
            string unlockText = unlockRank <= 1
                ? "Available from the start"
                : $"Unlocked at Rank {unlockRank}";
            return new HangarItemSnapshot(
                item.ItemType == ItemType.Aircraft ? HangarItemKind.Aircraft : HangarItemKind.Equipment,
                item.ItemName, item.Description, item.Icon, item.Icon, system.IsUnlocked(item), level,
                system.GetFragments(item), system.GetUpgradeFragmentCost(item), 0,
                item.CooldownDuration, item.SpawnCount, null, item.BackgroundColor,
                unlockText, BuildDetailAttributes(item, level), item.ItemId,
                PlayerItemSystem.MaximumLevel, unlockRank, item.ShapeOffsets);
        }

        private static HangarDetailAttribute[] BuildDetailAttributes(ItemData item, int level)
        {
            if (item.ItemType == ItemType.Equipment) return BuildEquipmentDetailAttributes(item);
            if (item.FighterDefinition == null) return null;
            FighterDefinition fighter = item.FighterDefinition;
            // The migrated UICardInfo prefab serializes its six visible rows in this order:
            // Attack, HP, Attack Speed, Firing Range, CD, Speed.  This differs from the source
            // PlanetWar textAttList order, so values must follow the actual prefab layout.
            return new[]
            {
                CreateAircraftProgressionAttribute("Attack",
                    GetDisplayedAircraftDamage(fighter) * item.GetAircraftDamageMultiplierForProgressionLevel(level),
                    level < PlayerItemSystem.MaximumLevel
                        ? GetDisplayedAircraftDamage(fighter) * item.GetAircraftDamageMultiplierForProgressionLevel(level + 1)
                        : 0f),
                CreateAircraftProgressionAttribute("HP",
                    fighter.MaximumHealth * item.GetAircraftHealthMultiplierForProgressionLevel(level),
                    level < PlayerItemSystem.MaximumLevel
                        ? fighter.MaximumHealth * item.GetAircraftHealthMultiplierForProgressionLevel(level + 1)
                        : 0f),
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
            return new[]
            {
                new HangarDetailAttribute("CD", item.Cd.ToString("0.##") + "s")
            };
        }

        // Mirrors UICardInfo.textAttList/textAddList: current stats stay in val and only
        // a real next-level difference is emitted to the green addVal field.
        private static HangarDetailAttribute CreateAircraftProgressionAttribute(string label,
            float value, float next)
        {
            float increase = next - value;
            return new HangarDetailAttribute(label, value.ToString("0.#"),
                next > 0f && !Mathf.Approximately(increase, 0f)
                    ? increase > 0f ? $"+{increase:0.#}" : increase.ToString("0.#")
                    : string.Empty);
        }

    }
}
