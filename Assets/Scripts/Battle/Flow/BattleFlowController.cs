using System;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 对局阶段的唯一状态源。业务组件只监听阶段，不依赖调试面板。
    /// </summary>
    public sealed class BattleFlowController : MonoBehaviour
    {
        [SerializeField]
        private BattlePhase initialPhase = BattlePhase.Preparation;

        [SerializeField]
        private bool persistBetweenScenes = true;

        public static BattleFlowController Instance { get; private set; }

        public static BattlePhase CurrentPhase =>
            Instance != null
                ? Instance.currentPhase
                : BattlePhase.Preparation;

        public static bool IsCombatPhase =>
            CurrentPhase == BattlePhase.Combat;

        public static event Action<BattlePhase> PhaseChanged;

        [SerializeField]
        private BattlePhase currentPhase;

        public BattlePhase Phase => currentPhase;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
            PhaseChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureGlobalInstance()
        {
            EnsureInstance();
        }

        public static BattleFlowController EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            BattleFlowController sceneController =
                FindAnyObjectByType<BattleFlowController>(
                    FindObjectsInactive.Include);

            if (sceneController != null)
            {
                Instance = sceneController;
                return Instance;
            }

            GameObject controller =
                new GameObject("BattleFlowController");
            return controller.AddComponent<
                BattleFlowController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            currentPhase = initialPhase;

            if (persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            ApplyPhase(currentPhase, true);
        }

        public void SetPhase(BattlePhase phase)
        {
            ApplyPhase(phase, false);
        }

        public void TogglePhase()
        {
            SetPhase(
                currentPhase == BattlePhase.Preparation
                    ? BattlePhase.Combat
                    : BattlePhase.Preparation);
        }

        private void ApplyPhase(
            BattlePhase phase,
            bool forceNotification)
        {
            if (!forceNotification &&
                currentPhase == phase)
            {
                return;
            }

            currentPhase = phase;

            if (currentPhase == BattlePhase.Preparation)
            {
                FadeOutRemainingBattleObjects();
            }

            PhaseChanged?.Invoke(currentPhase);
        }

        private static void FadeOutRemainingBattleObjects()
        {
            foreach (Fighter2D fighter in
                     FindObjectsByType<Fighter2D>(
                         FindObjectsInactive.Exclude))
            {
                BattleFadeOut2D.Begin(fighter.gameObject);
            }

            foreach (Projectile2D projectile in
                     FindObjectsByType<Projectile2D>(
                         FindObjectsInactive.Exclude))
            {
                BattleFadeOut2D.Begin(projectile.gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
