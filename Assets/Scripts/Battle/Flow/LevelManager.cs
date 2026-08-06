using System;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 当前关卡的最小状态源，供关卡流程和界面继续扩展。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class LevelManager : MonoBehaviour
    {
        [SerializeField, Min(1)]
        private int initialLevel = 1;

        private int currentLevel = 1;

        public static LevelManager Instance { get; private set; }
        public static int CurrentLevel =>
            Instance != null ? Instance.currentLevel : 1;

        public static event Action<int> LevelChanged;

        public int Level => currentLevel;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
            LevelChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureGlobalInstanceBeforeSceneLoad()
        {
            EnsureInstance();
        }

        public static LevelManager EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            LevelManager sceneManager =
                FindAnyObjectByType<LevelManager>(
                    FindObjectsInactive.Include);
            if (sceneManager != null)
            {
                Instance = sceneManager;
                return sceneManager;
            }

            GameObject manager = new GameObject("LevelManager");
            return manager.AddComponent<LevelManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            currentLevel = Mathf.Max(1, initialLevel);
            DontDestroyOnLoad(gameObject);
        }

        public void SetLevel(int level)
        {
            int nextLevel = Mathf.Max(1, level);
            if (currentLevel == nextLevel)
            {
                return;
            }

            currentLevel = nextLevel;
            LevelChanged?.Invoke(currentLevel);
        }

        public void AdvanceLevel()
        {
            SetLevel(currentLevel + 1);
        }
    }
}
