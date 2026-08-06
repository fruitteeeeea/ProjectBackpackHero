using System;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 当前正式关卡状态源。第一版固定支持五个关卡。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class LevelManager : MonoBehaviour
    {
        public const int MaximumLevel = 5;
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
            // 正式流程永远从第一关开始；调试入口使用 SetLevel 显式切换。
            currentLevel = 1;
            DontDestroyOnLoad(gameObject);
        }

        public void SetLevel(int level)
        {
            int nextLevel = Mathf.Clamp(level, 1, MaximumLevel);
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
