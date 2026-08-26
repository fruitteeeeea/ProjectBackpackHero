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
            currentPhase =
                initialPhase == BattlePhase.Combat
                    ? BattlePhase.CombatTransition
                    : initialPhase;

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
            if (phase == BattlePhase.Combat)
            {
                BeginCombatTransition();
                return;
            }

            ApplyPhase(phase, false);
        }

        public void BeginCombatTransition()
        {
            if (currentPhase == BattlePhase.Combat ||
                currentPhase ==
                BattlePhase.CombatTransition)
            {
                return;
            }

            ApplyPhase(
                BattlePhase.CombatTransition,
                false);
        }

        public bool CompleteCombatTransition()
        {
            if (currentPhase !=
                BattlePhase.CombatTransition)
            {
                return false;
            }

            ApplyPhase(BattlePhase.Combat, false);
            return true;
        }

        public void TogglePhase()
        {
            RestoreAllBackpackHealth();
            SetPhase(
                currentPhase == BattlePhase.Preparation
                    ? BattlePhase.Combat
                    : BattlePhase.Preparation);
        }

        private static void RestoreAllBackpackHealth()
        {
            foreach (BattleBackpackTarget2D backpack in
                     FindObjectsByType<BattleBackpackTarget2D>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                backpack.Health?.ResetHealth();
            }
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

            if (currentPhase == BattlePhase.Preparation ||
                currentPhase == BattlePhase.Result)
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
