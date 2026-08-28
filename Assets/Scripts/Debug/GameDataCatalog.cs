using BackpackHero.Battle;
using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>运行时默认 GameData 的唯一入口。正式资产位于 Assets/GameData；
    /// 此目录仅作为 Player 可加载的引用清单。</summary>
    [CreateAssetMenu(fileName = "GameDataCatalog", menuName = "Backpack Hero/Game Data Catalog")]
    public sealed class GameDataCatalog : ScriptableObject
    {
        private const string ResourcePath = "GameDataCatalog";
        private static GameDataCatalog cached;

        [SerializeField] private GamePacingDebugSettings gamePacing;
        [SerializeField] private StyleTendencyDebugSettings styleTendency;
        [SerializeField] private LevelDifficultySettings levelDifficulty;
        [SerializeField] private AircraftVisualDebugSettings aircraftVisual;
        [SerializeField] private BackpackVisualDebugSettings backpackVisual;
        [SerializeField] private FloatingDamageTextDebugSettings floatingDamageText;
        [SerializeField] private ArtAssetDebugSettings artAsset;
        [SerializeField] private FunctionBlockSettings functionBlock;

        public GamePacingDebugSettings GamePacing => gamePacing;
        public StyleTendencyDebugSettings StyleTendency => styleTendency;
        public LevelDifficultySettings LevelDifficulty => levelDifficulty;
        public AircraftVisualDebugSettings AircraftVisual => aircraftVisual;
        public BackpackVisualDebugSettings BackpackVisual => backpackVisual;
        public FloatingDamageTextDebugSettings FloatingDamageText => floatingDamageText;
        public ArtAssetDebugSettings ArtAsset => artAsset;
        public FunctionBlockSettings FunctionBlock => functionBlock;

        public static GameDataCatalog Load()
        {
            if (cached == null) cached = Resources.Load<GameDataCatalog>(ResourcePath);
            return cached;
        }

        public void SetDefaults(
            GamePacingDebugSettings pacing, StyleTendencyDebugSettings tendency,
            LevelDifficultySettings difficulty, AircraftVisualDebugSettings aircraft,
            BackpackVisualDebugSettings backpack, FloatingDamageTextDebugSettings floating,
            ArtAssetDebugSettings art, FunctionBlockSettings functionBlockSettings)
        {
            gamePacing = pacing;
            styleTendency = tendency;
            levelDifficulty = difficulty;
            aircraftVisual = aircraft;
            backpackVisual = backpack;
            floatingDamageText = floating;
            artAsset = art;
            functionBlock = functionBlockSettings;
        }
    }
}
