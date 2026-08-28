using System;
using PlanetWar.ReusableMainMenu;
using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>Runtime-safe source of truth for the main-menu feature gates.</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class FunctionBlockRuntime : MonoBehaviour
    {
        private FunctionBlockSettings defaultSettings;
        private bool blockMilestone = true;
        private bool blockPack = true;

        public static FunctionBlockRuntime Instance { get; private set; }
        public static event Action<bool, bool> StateChanged;
        public static bool IsMilestoneBlocked => Instance == null || Instance.blockMilestone;
        public static bool IsPackBlocked => Instance == null || Instance.blockPack;
        public FunctionBlockSettings DefaultSettings => defaultSettings;
        public bool BlockMilestone => blockMilestone;
        public bool BlockPack => blockPack;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntime()
        {
            if (Instance != null) return;
            GameObject host = new(nameof(FunctionBlockRuntime));
            DontDestroyOnLoad(host);
            host.AddComponent<FunctionBlockRuntime>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            defaultSettings = GameDataCatalog.Load()?.FunctionBlock;
            MainMenuActionRelay.ActionFilter = AllowMenuAction;
            RestoreRuntimeDefaults();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (MainMenuActionRelay.ActionFilter == AllowMenuAction)
                MainMenuActionRelay.ActionFilter = null;
            Instance = null;
        }

        public void SetValues(bool milestoneBlocked, bool packBlocked)
        {
            bool changed = blockMilestone != milestoneBlocked || blockPack != packBlocked;
            blockMilestone = milestoneBlocked;
            blockPack = packBlocked;
            if (changed) StateChanged?.Invoke(blockMilestone, blockPack);
        }

        public void SetDefaultSettings(FunctionBlockSettings settings)
        {
            if (settings == null || defaultSettings == settings) return;
            defaultSettings = settings;
            RestoreRuntimeDefaults();
        }

        public void RestoreRuntimeDefaults()
        {
            SetValues(defaultSettings?.BlockMilestone ?? true, defaultSettings?.BlockPack ?? true);
        }

        private bool AllowMenuAction(MainMenuAction action) =>
            action != MainMenuAction.Rank || !blockMilestone;
    }
}
