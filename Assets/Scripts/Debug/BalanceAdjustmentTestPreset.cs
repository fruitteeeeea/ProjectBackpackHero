using System;
using System.Collections.Generic;
using BackpackPrototype;
using BackpackHero.Config;
using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>仅用于编辑器复现对局的测试方案，绝不作为正式 GameData 加载。</summary>
    [CreateAssetMenu(fileName = "BalanceAdjustmentTestPreset", menuName = "Debug/Balance Adjustment Test Preset")]
    public sealed class BalanceAdjustmentTestPreset : ScriptableObject
    {
        [Serializable]
        public struct LayoutEntry
        {
            public ItemData Item;
            public Vector2Int AnchorCell;
            [Range(ItemInstance.DefaultLevel, ItemInstance.MaximumLevel)] public int Level;

            public LayoutEntry(ItemData item, Vector2Int anchorCell, int level)
            {
                Item = item;
                AnchorCell = anchorCell;
                Level = Mathf.Clamp(level, ItemInstance.DefaultLevel, ItemInstance.MaximumLevel);
            }
        }

        [SerializeField, Range(0f, 2f)] private float gameSpeed = 1f;
        [SerializeField] private BalanceProfile balanceProfile = BalanceProfile.Proposed;
        [SerializeField] private RandomFlightCurveMode randomFlightMode;
        [SerializeField] private bool autoAddConfiguredItems;
        [SerializeField, Range(1, 10)] private int playerProgressionLevel = 1;
        [SerializeField, Range(1, 10)] private int enemyProgressionLevel = 1;
        [SerializeField] private DeckPreset enemySourcePreset;
        [SerializeField] private List<ItemData> playerDeck = new();
        [SerializeField] private List<ItemData> enemyDeck = new();
        [SerializeField] private List<LayoutEntry> playerLayout = new();
        [SerializeField] private List<LayoutEntry> enemyLayout = new();

        public float GameSpeed => gameSpeed;
        public BalanceProfile BalanceProfile => balanceProfile;
        public RandomFlightCurveMode RandomFlightMode => randomFlightMode;
        public bool AutoAddConfiguredItems => autoAddConfiguredItems;
        public int PlayerProgressionLevel => playerProgressionLevel;
        public int EnemyProgressionLevel => enemyProgressionLevel;
        public DeckPreset EnemySourcePreset => enemySourcePreset;
        public IReadOnlyList<ItemData> PlayerDeck => playerDeck;
        public IReadOnlyList<ItemData> EnemyDeck => enemyDeck;
        public IReadOnlyList<LayoutEntry> PlayerLayout => playerLayout;
        public IReadOnlyList<LayoutEntry> EnemyLayout => enemyLayout;

        public void SetBalanceProfile(BalanceProfile profile) =>
            balanceProfile = profile;

        public void CopyFrom(BalanceAdjustmentTestPreset source)
        {
            if (source == null) return;
            gameSpeed = source.gameSpeed;
            balanceProfile = source.balanceProfile;
            randomFlightMode = source.randomFlightMode;
            autoAddConfiguredItems = source.autoAddConfiguredItems;
            playerProgressionLevel = source.playerProgressionLevel;
            enemyProgressionLevel = source.enemyProgressionLevel;
            enemySourcePreset = source.enemySourcePreset;
            playerDeck = new List<ItemData>(source.playerDeck);
            enemyDeck = new List<ItemData>(source.enemyDeck);
            playerLayout = new List<LayoutEntry>(source.playerLayout);
            enemyLayout = new List<LayoutEntry>(source.enemyLayout);
        }

        public bool ContentEquals(BalanceAdjustmentTestPreset other)
        {
            return other != null &&
                Mathf.Approximately(gameSpeed, other.gameSpeed) &&
                balanceProfile == other.balanceProfile &&
                randomFlightMode == other.randomFlightMode &&
                autoAddConfiguredItems == other.autoAddConfiguredItems &&
                playerProgressionLevel == other.playerProgressionLevel &&
                enemyProgressionLevel == other.enemyProgressionLevel &&
                enemySourcePreset == other.enemySourcePreset &&
                SameItems(playerDeck, other.playerDeck) &&
                SameItems(enemyDeck, other.enemyDeck) &&
                SameLayout(playerLayout, other.playerLayout) &&
                SameLayout(enemyLayout, other.enemyLayout);
        }

        private static bool SameItems(List<ItemData> left, List<ItemData> right)
        {
            if (left.Count != right.Count) return false;
            for (int index = 0; index < left.Count; index++)
                if (left[index] != right[index]) return false;
            return true;
        }

        private static bool SameLayout(List<LayoutEntry> left, List<LayoutEntry> right)
        {
            if (left.Count != right.Count) return false;
            for (int index = 0; index < left.Count; index++)
                if (left[index].Item != right[index].Item || left[index].AnchorCell != right[index].AnchorCell || left[index].Level != right[index].Level) return false;
            return true;
        }

        public void SetRuntimeState(float speed, BalanceProfile profile,
            RandomFlightCurveMode flightMode,
            bool autoAddItems,
            int playerLevel, int enemyLevel, DeckPreset enemyPreset,
            IEnumerable<ItemData> playerDeckItems, IEnumerable<ItemData> enemyDeckItems,
            IEnumerable<LayoutEntry> playerLayoutItems, IEnumerable<LayoutEntry> enemyLayoutItems)
        {
            gameSpeed = Mathf.Clamp(speed, 0f, 2f);
            balanceProfile = profile;
            randomFlightMode = flightMode;
            autoAddConfiguredItems = autoAddItems;
            playerProgressionLevel = Mathf.Clamp(playerLevel, 1, 10);
            enemyProgressionLevel = Mathf.Clamp(enemyLevel, 1, 10);
            enemySourcePreset = enemyPreset;
            playerDeck = playerDeckItems != null ? new List<ItemData>(playerDeckItems) : new List<ItemData>();
            enemyDeck = enemyDeckItems != null ? new List<ItemData>(enemyDeckItems) : new List<ItemData>();
            playerLayout = playerLayoutItems != null ? new List<LayoutEntry>(playerLayoutItems) : new List<LayoutEntry>();
            enemyLayout = enemyLayoutItems != null ? new List<LayoutEntry>(enemyLayoutItems) : new List<LayoutEntry>();
        }

        /// <summary>
        /// Compatibility overload for existing debug presets and tests. New callers
        /// should pass an explicit profile; old callers retain the release default.
        /// </summary>
        public void SetRuntimeState(float speed,
            RandomFlightCurveMode flightMode, bool autoAddItems,
            int playerLevel, int enemyLevel, DeckPreset enemyPreset,
            IEnumerable<ItemData> playerDeckItems,
            IEnumerable<ItemData> enemyDeckItems,
            IEnumerable<LayoutEntry> playerLayoutItems,
            IEnumerable<LayoutEntry> enemyLayoutItems) =>
            SetRuntimeState(speed, BalanceProfile.Proposed, flightMode,
                autoAddItems, playerLevel, enemyLevel, enemyPreset,
                playerDeckItems, enemyDeckItems, playerLayoutItems,
                enemyLayoutItems);
    }
}
