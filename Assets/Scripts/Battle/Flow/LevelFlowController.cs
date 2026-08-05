using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 管理回合显示与背包胜负结算；战斗阶段本身仍由
    /// BattleFlowController 维护。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class LevelFlowController : MonoBehaviour
    {
        private static readonly Color RoundColor =
            new Color32(0xFD, 0xD8, 0x35, 0xFF);
        private static readonly Color WinColor =
            new Color32(0x21, 0x96, 0xF3, 0xFF);
        private static readonly Color LoseColor =
            new Color32(0xE5, 0x39, 0x35, 0xFF);

        [SerializeField, Min(0.1f)]
        private float bannerDuration = 2f;

        private BattleBackpackTarget2D playerTarget;
        private BattleBackpackTarget2D enemyTarget;
        private Canvas overlayCanvas;
        private Image bannerBackground;
        private TextMeshProUGUI bannerLabel;
        private Coroutine bannerRoutine;
        private int currentRound = 1;
        private bool hasCompletedRound;
        private bool pendingPlayerDeath;
        private bool pendingEnemyDeath;
        private bool resolutionQueued;
        private bool showingResult;

        public static LevelFlowController Instance { get; private set; }
        public static int CurrentRound =>
            Instance != null ? Instance.currentRound : 1;

        public int Round => currentRound;
        public bool IsShowingBanner => bannerRoutine != null;

        public static event Action<int> RoundStarted;
        public static event Action<string> ResultShown;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
            RoundStarted = null;
            ResultShown = null;
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
        }

        /// <summary>
        /// 从准备阶段开始当前回合，或在上回合结算后开始下一回合。
        /// </summary>
        public bool RequestStartRound()
        {
            if (BattleFlowController.CurrentPhase !=
                BattlePhase.Preparation || showingResult)
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
            ShowResultBanner(
                playerWins ? "Player Win" : "Player Lose",
                playerWins ? WinColor : LoseColor);
        }

        private void ResetBackpackHealth()
        {
            playerTarget?.Health?.ResetHealth();
            enemyTarget?.Health?.ResetHealth();
        }

        private void ShowRoundBanner()
        {
            ShowBanner($"Round {currentRound}", RoundColor,
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
            if (bannerBackground != null)
            {
                bannerBackground.color = color;
                bannerBackground.gameObject.SetActive(true);
            }
            if (bannerLabel != null)
            {
                bannerLabel.text = text;
                bannerLabel.gameObject.SetActive(true);
            }

            bannerRoutine = StartCoroutine(HideBannerAfterDelay());
        }

        private IEnumerator HideBannerAfterDelay()
        {
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0.1f, bannerDuration));

            if (bannerBackground != null)
            {
                bannerBackground.gameObject.SetActive(false);
            }

            bannerRoutine = null;
            bool finishedResult = showingResult;
            showingResult = false;

            if (finishedResult)
            {
                BattleFlowController.EnsureInstance()
                    ?.SetPhase(BattlePhase.Preparation);
            }

        }

        private void BeginCombat()
        {
            RoundStarted?.Invoke(currentRound);
            BattleFlowController.EnsureInstance()
                ?.SetPhase(BattlePhase.Combat);
        }

        private void EnsureBanner()
        {
            if (bannerBackground != null && bannerLabel != null)
            {
                return;
            }

            Canvas overlay = null;
            foreach (Canvas canvas in FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include))
            {
                if (canvas.name == "LevelFlowOverlay")
                {
                    overlay = canvas;
                    break;
                }
            }

            if (overlay == null)
            {
                GameObject overlayObject = new GameObject(
                    "LevelFlowOverlay",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler));
                overlay = overlayObject.GetComponent<Canvas>();
                overlay.renderMode = RenderMode.ScreenSpaceOverlay;
                overlay.overrideSorting = true;
                overlay.sortingOrder = 1000;

                CanvasScaler scaler =
                    overlayObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution =
                    new Vector2(1206f, 2622f);
                scaler.screenMatchMode =
                    CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            overlayCanvas = overlay;

            GameObject backgroundObject = new GameObject(
                "LevelFlowBanner",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            backgroundObject.transform.SetParent(
                overlayCanvas.transform,
                false);
            bannerBackground = backgroundObject.GetComponent<Image>();
            bannerBackground.raycastTarget = false;
            RectTransform backgroundRect =
                bannerBackground.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.offsetMin = new Vector2(0f, -110f);
            backgroundRect.offsetMax = new Vector2(0f, 110f);

            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(backgroundObject.transform,
                false);
            bannerLabel = labelObject.GetComponent<TextMeshProUGUI>();
            bannerLabel.raycastTarget = false;
            bannerLabel.alignment = TextAlignmentOptions.Center;
            bannerLabel.font = TMP_Settings.defaultFontAsset;
            bannerLabel.fontSize = 92f;
            bannerLabel.fontStyle = FontStyles.Bold;
            bannerLabel.color = Color.white;
            bannerLabel.outlineWidth = 0f;
            RectTransform labelRect = bannerLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            backgroundObject.SetActive(false);
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
