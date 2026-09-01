using System;
using System.Collections;
using System.Linq;
using BackpackHero.Audio;
using BackpackPrototype;
using BackpackHero.Progression;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
#endif

namespace BackpackHero.Battle
{
    /// <summary>
    /// 管理回合显示与背包胜负结算；战斗阶段本身仍由
    /// BattleFlowController 维护。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class LevelFlowController : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly ProfilerMarker ResolveTargetsMarker =
            new("SampleScenePerf.LevelFlowController.ResolveTargets");
#endif

        public const int WinsRequired = 2;
        public const int MaximumRounds = 3;
        public const float RoundDurationSeconds = 60f;
        public const float OvertimeDurationSeconds = 30f;
        public const float OvertimeCooldownSpeedMultiplier = 1.5f;
        public const float OvertimeAircraftHealthMultiplier = 0.8f;
        public const float OvertimeProjectileDamageMultiplier = 1.5f;
        private const float ResultBannerDurationSeconds = 2f;
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
        private float remainingRoundTime;
        private bool isOvertime;

        public static LevelFlowController Instance { get; private set; }
        /// <summary>每次创建或重启整场对局时递增，用于运行时可靠地只应用一次初始布局。</summary>
        public static int MatchInitializationVersion { get; private set; } = 1;
        public static int CurrentRound =>
            Instance != null ? Instance.currentRound : 1;

        public int Round => currentRound;
        public int PlayerWins => playerWins;
        public int EnemyWins => enemyWins;
        public bool IsMatchComplete => isMatchComplete;
        public bool IsShowingBanner => bannerRoutine != null;
        public float RemainingRoundTime => remainingRoundTime;
        public bool IsOvertime => isOvertime;
        public bool IsRoundTimerRunning =>
            BattleFlowController.IsCombatPhase && !showingResult;

        /// <summary>
        /// Adjusts the active round timer for runtime debugging. Crossing zero applies
        /// the same transition or resolution that the normal timer would apply.
        /// </summary>
        public bool AdjustRoundTimerForDebug(float seconds)
        {
            if (!IsRoundTimerRunning || Mathf.Approximately(seconds, 0f))
            {
                return false;
            }

            remainingRoundTime += seconds;
            if (remainingRoundTime > 0f)
            {
                RoundTimerStateChanged?.Invoke();
                return true;
            }

            if (!isOvertime)
            {
                isOvertime = true;
                remainingRoundTime = OvertimeDurationSeconds;
                RoundTimerStateChanged?.Invoke();
                return true;
            }

            remainingRoundTime = 0f;
            RoundTimerStateChanged?.Invoke();
            ResolveOvertimeByHealth();
            return true;
        }

        /// <summary>以正式结算流程立即结束整场对局，仅供运行时调试调用。</summary>
        public bool ForceDebugMatchResult(bool playerWon)
        {
            if (!BattleFlowController.IsCombatPhase || showingResult || isMatchComplete)
            {
                return false;
            }

            playerWins = playerWon ? WinsRequired - 1 : 0;
            enemyWins = playerWon ? 0 : WinsRequired - 1;
            RecordDeath(player: !playerWon);
            return true;
        }

        public static event Action<int> RoundStarted;
        public static event Action<string> ResultShown;
        public static event Action MatchStateChanged;
        /// <summary>仅在首次对局创建或明确重启后发出，不包含开战和结算。</summary>
        public static event Action MatchInitialized;
        public static event Action RoundTimerStateChanged;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
            RoundStarted = null;
            ResultShown = null;
            MatchStateChanged = null;
            MatchInitialized = null;
            MatchInitializationVersion = 1;
            RoundTimerStateChanged = null;
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
            MatchInitialized?.Invoke();
            BattleFlowController.PhaseChanged +=
                HandleBattlePhaseChanged;
        }

        private void Update()
        {
            ResolveTargets();
            EnsureBanner();
            TickRoundTimer();
        }

        /// <summary>
        /// 从准备阶段开始当前回合，或在上回合结算后开始下一回合。
        /// </summary>
        public bool RequestStartRound()
        {
            // 调试 AI 正在以真实商店/背包状态连续操作时，不能让任何入口
            // （包括正式准备 UI 和调试面板）提前切进战斗。
            if (PlayerBackpackSystem.IsAnyDebugAutoOperationRunning ||
                PlayerBackpackDebugBridge.Active?.EnemyTarget?.IsDebugFastOperationRunning == true)
            {
                return false;
            }

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
            LevelManager.EnsureInstance()?.SetLevel(level);
            ResetMatchState();
            MatchInitializationVersion++;
            BattleFlowController.EnsureInstance()?.SetPhase(BattlePhase.Preparation);
            MatchInitialized?.Invoke();
            MatchStateChanged?.Invoke();
        }

        /// <summary>
        /// 离开战斗场景前清理常驻的对局状态。
        /// 不广播新对局事件，避免仍在当前场景中的背包重新应用初始布局。
        /// </summary>
        public void PrepareForMenuExit()
        {
            LevelManager.EnsureInstance()?.SetLevel(1);
            ResetMatchState();
            BattleFlowController.EnsureInstance()?.SetPhase(BattlePhase.Preparation);
            MatchStateChanged?.Invoke();
        }

        public void ResetForDebugMatch()
        {
            ResetMatchState();
            MatchInitializationVersion++;
            BattleFlowController.EnsureInstance()
                ?.SetPhase(BattlePhase.Preparation);
            MatchInitialized?.Invoke();
            MatchStateChanged?.Invoke();
        }

        private void ResetMatchState()
        {
            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
                bannerRoutine = null;
            }

            SetBannerVisible(false);
            currentRound = 1;
            playerWins = 0;
            enemyWins = 0;
            hasCompletedRound = false;
            pendingPlayerDeath = false;
            pendingEnemyDeath = false;
            resolutionQueued = false;
            showingResult = false;
            isMatchComplete = false;
            ResetRoundTimer();
            ResetBackpackHealth();
        }
        private void HandleBattlePhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.Combat && !showingResult)
            {
                StartRoundTimer();
                ShowRoundBanner();
                return;
            }

            ResetRoundTimer();
        }

        private void StartRoundTimer()
        {
            remainingRoundTime = RoundDurationSeconds;
            isOvertime = false;
            RoundTimerStateChanged?.Invoke();
        }

        private void ResetRoundTimer()
        {
            remainingRoundTime = RoundDurationSeconds;
            isOvertime = false;
            RoundTimerStateChanged?.Invoke();
        }

        private void TickRoundTimer()
        {
            if (!IsRoundTimerRunning || remainingRoundTime <= 0f)
            {
                return;
            }

            remainingRoundTime = Mathf.Max(
                0f,
                remainingRoundTime - Time.deltaTime);
            RoundTimerStateChanged?.Invoke();

            if (remainingRoundTime > 0f)
            {
                return;
            }

            if (!isOvertime)
            {
                isOvertime = true;
                remainingRoundTime = OvertimeDurationSeconds;
                RoundTimerStateChanged?.Invoke();
                return;
            }

            ResolveOvertimeByHealth();
        }

        private void ResolveOvertimeByHealth()
        {
            float playerHealth = playerTarget?.Health?.CurrentHealth ?? 0f;
            float enemyHealth = enemyTarget?.Health?.CurrentHealth ?? 0f;
            // Overtime ends as soon as its clock expires.  Clear the state before
            // opening the result banner so all overtime-bound visuals recover now.
            isOvertime = false;
            remainingRoundTime = RoundDurationSeconds;
            RoundTimerStateChanged?.Invoke();
            ResolveRound(playerHealth >= enemyHealth);
        }

        private void ResolveTargets()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (ResolveTargetsMarker.Auto())
            {
#endif
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
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
            ResolveRound(playerWins);
        }

        private void ResolveRound(bool playerWon)
        {
            if (hasCompletedRound || showingResult ||
                !BattleFlowController.IsCombatPhase)
            {
                return;
            }

            hasCompletedRound = true;
            if (playerWon)
            {
                playerWins++;
            }
            else
            {
                enemyWins++;
            }

            isMatchComplete = playerWins >= WinsRequired ||
                enemyWins >= WinsRequired || currentRound >= MaximumRounds;
            MatchStateChanged?.Invoke();

            string result = isMatchComplete
                ? (playerWins > enemyWins ? "关卡胜利" : "关卡失败") +
                  $" {playerWins}:{enemyWins}"
                : playerWon ? "Player Win" : "Player Lose";
            bool playerWonMatch = isMatchComplete &&
                playerWins > enemyWins;
            BattleFlowController.EnsureInstance()
                ?.SetPhase(BattlePhase.Result);

            Color resultColor = playerWon
                ? bannerView != null
                    ? bannerView.WinColor
                    : Color.blue
                : bannerView != null
                    ? bannerView.LoseColor
                    : Color.red;
            ShowResultBanner(result, resultColor, () =>
                CompleteRoundResolution(playerWonMatch));
        }

        private void CompleteRoundResolution(bool playerWonMatch)
        {
            if (!isMatchComplete)
            {
                BattleFlowController.EnsureInstance()
                    ?.SetPhase(BattlePhase.Preparation);
                return;
            }

            if (playerWonMatch)
            {
                GameSfxService.Instance?.Play(GameSfxId.Victory);
            }

            BattleResultPresenter presenter =
                FindAnyObjectByType<BattleResultPresenter>(
                    FindObjectsInactive.Include);
            RankSettlement settlement = RankProgressionSystem.Instance
                .SettleMatch(playerWonMatch);
            string advisory = BuildDefeatAdvisory(settlement);
            presenter?.Show(new BattleResultData(
                settlement.Victory,
                settlement.ScoreBefore,
                settlement.ScoreDelta,
                settlement.Rewards,
                advisory));
        }

        private static string BuildDefeatAdvisory(RankSettlement settlement)
        {
            if (settlement.Victory) return null;
            EnemyBackpackSystem enemy = FindAnyObjectByType<EnemyBackpackSystem>(
                FindObjectsInactive.Include);
            string enemyText = null;
            if (enemy != null && enemy.HasMatchProfile)
            {
                EnemyMatchProfile profile = enemy.MatchProfile;
                string deck = string.Join(" / ", profile.DeckPreset.Slots
                    .Where(item => item != null).Select(item => item.Name));
                enemyText = $"敌方：段位 {profile.Rank} · 阶段 {profile.RawStage} · 养成 Lv{profile.ProgressionLevel}\n卡组：{deck}";
            }
            string upgradeText = !string.IsNullOrEmpty(settlement.RecommendedItemName)
                ? $"推荐升级：{settlement.RecommendedItemName}（还差 {settlement.RecommendedFragmentsNeeded} 碎片）"
                : null;
            string protectionText = settlement.DowngradeUnlocked
                ? "下局敌人将降低一个小阶段。"
                : null;
            return string.Join("\n", new[] { enemyText, upgradeText, protectionText }
                .Where(text => !string.IsNullOrEmpty(text)));
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

        private void ShowResultBanner(
            string text,
            Color color,
            Action onCompleted)
        {
            ShowBanner(text, color, isResult: true,
                ResultBannerDurationSeconds, onCompleted);
            ResultShown?.Invoke(text);
        }

        private void ShowBanner(
            string text,
            Color color,
            bool isResult,
            float duration = -1f,
            Action onCompleted = null)
        {
            EnsureBanner();
            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
            }

            showingResult = isResult;
            bannerView?.SetContent(text, color);

            float lifecycleDuration = duration >= 0f
                ? duration
                : DisplayDuration;
            bannerRoutine = StartCoroutine(PlayBannerLifecycle(
                lifecycleDuration, onCompleted));
        }

        private IEnumerator PlayBannerLifecycle(
            float totalDuration,
            Action onCompleted)
        {
            totalDuration = Mathf.Max(0f, totalDuration);
            float flickerInterval = Mathf.Min(
                FlickerInterval, totalDuration / 6f);
            float steadyDuration = Mathf.Max(
                0f,
                totalDuration - flickerInterval * 6f);

            SetBannerVisible(false);
            yield return new WaitForSecondsRealtime(
                flickerInterval);
            SetBannerVisible(true);
            yield return new WaitForSecondsRealtime(
                flickerInterval);
            SetBannerVisible(false);
            yield return new WaitForSecondsRealtime(
                flickerInterval);
            SetBannerVisible(true);

            yield return new WaitForSecondsRealtime(steadyDuration);

            yield return new WaitForSecondsRealtime(
                flickerInterval);
            SetBannerVisible(false);
            yield return new WaitForSecondsRealtime(
                flickerInterval);
            SetBannerVisible(true);
            yield return new WaitForSecondsRealtime(
                flickerInterval);
            SetBannerVisible(false);

            bannerRoutine = null;
            showingResult = false;
            onCompleted?.Invoke();
        }

        private void SetBannerVisible(bool visible)
        {
            bannerView?.SetVisible(visible);
        }

        private float DisplayDuration =>
            bannerView != null
                ? bannerView.DisplayDuration
                : ResultBannerDurationSeconds;

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
