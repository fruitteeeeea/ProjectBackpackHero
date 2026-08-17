using System;
using System.Collections;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 管理回合显示与背包胜负结算；战斗阶段本身仍由
    /// BattleFlowController 维护。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class LevelFlowController : MonoBehaviour
    {
        public const int WinsRequired = 2;
        public const int MaximumRounds = 3;
        private BattleBackpackTarget2D playerTarget;
        private BattleBackpackTarget2D enemyTarget;
        private LevelFlowBannerView bannerView;
        private Coroutine bannerRoutine;
        private int currentRound = 1;
        private bool hasCompletedRound;
        private bool pendingPlayerDeath;
        private bool pendingEnemyDeath;
        private bool resolutionQueued;
        private bool showingResult;
        private int playerWins;
        private int enemyWins;
        private bool isMatchComplete;

        public static LevelFlowController Instance { get; private set; }
        public static int CurrentRound =>
            Instance != null ? Instance.currentRound : 1;

        public int Round => currentRound;
        public int PlayerWins => playerWins;
        public int EnemyWins => enemyWins;
        public bool IsMatchComplete => isMatchComplete;
        public bool IsShowingBanner => bannerRoutine != null;

        public static event Action<int> RoundStarted;
        public static event Action<string> ResultShown;
        public static event Action MatchStateChanged;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
            RoundStarted = null;
            ResultShown = null;
            MatchStateChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureGlobalInstanceBeforeSceneLoad()
        {
            EnsureInstance();
        }

        public static LevelFlowController EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            LevelFlowController sceneController =
                FindAnyObjectByType<LevelFlowController>(
                    FindObjectsInactive.Include);
            if (sceneController != null)
            {
                Instance = sceneController;
                return sceneController;
            }

            GameObject controller =
                new GameObject("LevelFlowController");
            return controller.AddComponent<LevelFlowController>();
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
        }

        private void Start()
        {
            BattleFlowController.EnsureInstance()
                ?.SetPhase(BattlePhase.Preparation);
            BattleFlowController.PhaseChanged +=
                HandleBattlePhaseChanged;
        }

        private void Update()
        {
            ResolveTargets();
            EnsureBanner();
        }

        /// <summary>
        /// 从准备阶段开始当前回合，或在上回合结算后开始下一回合。
        /// </summary>
        public bool RequestStartRound()
        {
            if (BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation || showingResult ||
                isMatchComplete || currentRound > MaximumRounds)
            {
                return false;
            }

            if (hasCompletedRound)
            {
                currentRound++;
                hasCompletedRound = false;
                ResetBackpackHealth();
                BeginCombat();
                return true;
            }

            BeginCombat();
            return true;
        }

        /// <summary>以指定关卡开始全新对局，供正式入口和调试面板复用。</summary>
        public void ResetForLevel(int level)
        {
            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
                bannerRoutine = null;
            }
            SetBannerVisible(false);

            LevelManager.EnsureInstance()?.SetLevel(level);
            currentRound = 1;
            playerWins = 0;
            enemyWins = 0;
            hasCompletedRound = false;
            pendingPlayerDeath = false;
            pendingEnemyDeath = false;
            resolutionQueued = false;
            showingResult = false;
            isMatchComplete = false;
            ResetBackpackHealth();
            BattleFlowController.EnsureInstance()?.SetPhase(BattlePhase.Preparation);
            MatchStateChanged?.Invoke();
        }

        private void HandleBattlePhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.Combat && !showingResult)
            {
                ShowRoundBanner();
            }
        }

        private void ResolveTargets()
        {
            BattleBackpackTarget2D foundPlayer = null;
            BattleBackpackTarget2D foundEnemy = null;
            foreach (BattleBackpackTarget2D target in
                     FindObjectsByType<BattleBackpackTarget2D>(
                         FindObjectsInactive.Include))
            {
                if (target.Faction == BattleFaction.Enemy)
                {
                    foundEnemy ??= target;
                }
                else
                {
                    foundPlayer ??= target;
                }
            }

            BindTarget(ref playerTarget, foundPlayer,
                HandlePlayerDied);
            BindTarget(ref enemyTarget, foundEnemy,
                HandleEnemyDied);
        }

        private static void BindTarget(
            ref BattleBackpackTarget2D current,
            BattleBackpackTarget2D next,
            Action diedHandler)
        {
            if (current == next)
            {
                return;
            }

            if (current?.Health != null)
            {
                current.Health.Died -= diedHandler;
            }

            current = next;

            if (current?.Health != null)
            {
                current.Health.Died += diedHandler;
            }
        }

        private void HandlePlayerDied()
        {
            RecordDeath(player: true);
        }

        private void HandleEnemyDied()
        {
            RecordDeath(player: false);
        }

        private void RecordDeath(bool player)
        {
            if (!BattleFlowController.IsCombatPhase ||
                showingResult)
            {
                return;
            }

            if (player)
            {
                pendingPlayerDeath = true;
            }
            else
            {
                pendingEnemyDeath = true;
            }

            if (!resolutionQueued)
            {
                resolutionQueued = true;
                StartCoroutine(ResolveAtEndOfFrame());
            }
        }

        private IEnumerator ResolveAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();

            resolutionQueued = false;
            if (!BattleFlowController.IsCombatPhase)
            {
                pendingPlayerDeath = false;
                pendingEnemyDeath = false;
                yield break;
            }

            bool playerWins = pendingEnemyDeath;
            bool playerLoses = pendingPlayerDeath;
            pendingPlayerDeath = false;
            pendingEnemyDeath = false;
            if (!playerWins && !playerLoses)
            {
                yield break;
            }
            hasCompletedRound = true;
            if (playerWins)
            {
                this.playerWins++;
            }
            else
            {
                enemyWins++;
            }

            isMatchComplete = this.playerWins >= WinsRequired ||
                enemyWins >= WinsRequired || currentRound >= MaximumRounds;
            MatchStateChanged?.Invoke();

            string result = isMatchComplete
                ? (this.playerWins > enemyWins ? "关卡胜利" : "关卡失败") +
                  $" {this.playerWins}:{enemyWins}"
                : playerWins ? "Player Win" : "Player Lose";
            bool playerWonMatch = isMatchComplete &&
                this.playerWins > enemyWins;
            if (isMatchComplete)
            {
                HideBannerImmediately();
                // End the combat before opening the result overlay. This
                // invokes the existing fade-out path for aircraft/projectiles
                // and resets every backpack item's cooldown state.
                BattleFlowController.EnsureInstance()
                    ?.SetPhase(BattlePhase.Preparation);
            }
            if (isMatchComplete)
            {
                BattleResultPresenter presenter =
                    FindAnyObjectByType<BattleResultPresenter>(
                        FindObjectsInactive.Include);
                presenter?.Show(
                    BattleResultData.CreateDefault(playerWonMatch));
            }
            else
            {
                ShowResultBanner(
                    result,
                    playerWins
                        ? bannerView != null
                            ? bannerView.WinColor
                            : Color.blue
                        : bannerView != null
                            ? bannerView.LoseColor
                            : Color.red);
            }
        }

        private void ResetBackpackHealth()
        {
            playerTarget?.Health?.ResetHealth();
            enemyTarget?.Health?.ResetHealth();
        }

        private void ShowRoundBanner()
        {
            ShowBanner($"Round {currentRound}",
                bannerView != null
                    ? bannerView.RoundColor
                    : Color.yellow,
                isResult: false);
        }

        private void ShowResultBanner(string text, Color color)
        {
            ShowBanner(text, color, isResult: true);
            ResultShown?.Invoke(text);
        }

        private void ShowBanner(string text, Color color, bool isResult)
        {
            EnsureBanner();
            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
            }

            showingResult = isResult;
            bannerView?.SetContent(text, color);

            bannerRoutine = StartCoroutine(PlayBannerLifecycle());
        }

        private IEnumerator PlayBannerLifecycle()
        {
            float totalDuration = Mathf.Max(
                FlickerInterval * 6f,
                DisplayDuration);
            float steadyDuration = Mathf.Max(
                0f,
                totalDuration - FlickerInterval * 6f);

            SetBannerVisible(false);
            yield return new WaitForSecondsRealtime(
                FlickerInterval);
            SetBannerVisible(true);
            yield return new WaitForSecondsRealtime(
                FlickerInterval);
            SetBannerVisible(false);
            yield return new WaitForSecondsRealtime(
                FlickerInterval);
            SetBannerVisible(true);

            yield return new WaitForSecondsRealtime(steadyDuration);

            yield return new WaitForSecondsRealtime(
                FlickerInterval);
            SetBannerVisible(false);
            yield return new WaitForSecondsRealtime(
                FlickerInterval);
            SetBannerVisible(true);
            yield return new WaitForSecondsRealtime(
                FlickerInterval);
            SetBannerVisible(false);

            bannerRoutine = null;
            bool finishedResult = showingResult;
            showingResult = false;

            // 最终结算也要退出战斗阶段，避免背包继续生成飞机；
            // IsMatchComplete 会锁住下一回合，保留关卡结算状态。
            if (finishedResult)
            {
                BattleFlowController.EnsureInstance()
                    ?.SetPhase(BattlePhase.Preparation);
            }
        }

        private void SetBannerVisible(bool visible)
        {
            bannerView?.SetVisible(visible);
        }

        private void HideBannerImmediately()
        {
            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
                bannerRoutine = null;
            }
            showingResult = false;
            bannerView?.Hide();
        }

        private float DisplayDuration =>
            bannerView != null
                ? bannerView.DisplayDuration
                : 2f;

        private float FlickerInterval =>
            bannerView != null
                ? bannerView.FlickerInterval
                : 0.05f;

        private void BeginCombat()
        {
            RoundStarted?.Invoke(currentRound);
            MatchStateChanged?.Invoke();
            BattleFlowController.EnsureInstance()
                ?.SetPhase(BattlePhase.Combat);
        }

        private void EnsureBanner()
        {
            if (bannerView != null)
            {
                return;
            }

            bannerView = FindAnyObjectByType<LevelFlowBannerView>(
                FindObjectsInactive.Include);
        }

        private void OnDestroy()
        {
            BattleFlowController.PhaseChanged -=
                HandleBattlePhaseChanged;
            BindTarget(ref playerTarget, null, HandlePlayerDied);
            BindTarget(ref enemyTarget, null, HandleEnemyDied);
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
