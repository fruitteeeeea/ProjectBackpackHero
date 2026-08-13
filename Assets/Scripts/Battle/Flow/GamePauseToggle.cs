using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 通过时间缩放暂停或继续本局游戏，并同步按钮文案。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GamePauseToggle : MonoBehaviour
    {
        private static readonly Vector2 MainMenuReferenceResolution =
            new Vector2(720f, 1280f);

        private const float BattleButtonWidth = 254f;
        private const float BattleButtonHeight = 103f;
        private const float BattleButtonPositionY = -183f;
        private const float BattleButtonLabelOffsetY = 3f;
        private const float BattleButtonFontSize = 38f;

        [SerializeField]
        private Text buttonLabel;

        [SerializeField]
        private GameObject quitGameOverlay;

        [SerializeField]
        private string mainMenuSceneName = "MainMenuDemo";

        [SerializeField]
        private Sprite battleButtonSprite;

        [SerializeField]
        private TMP_FontAsset battleButtonFont;

        [SerializeField]
        private Material battleButtonFontMaterial;

        private void OnEnable()
        {
            EnsureQuitGameOverlay();
            if (quitGameOverlay != null)
            {
                quitGameOverlay.SetActive(false);
            }
        }

        public void TogglePause()
        {
            EnsureQuitGameOverlay();
            if (quitGameOverlay == null)
            {
                return;
            }

            quitGameOverlay.SetActive(true);
            quitGameOverlay.transform.SetAsLastSibling();
        }

        public void QuitGame()
        {
            LevelFlowController.EnsureInstance()
                ?.ResetForLevel(LevelManager.CurrentLevel);
            SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
        }

        public void CloseQuitGameOverlay()
        {
            if (quitGameOverlay != null)
            {
                quitGameOverlay.SetActive(false);
            }
        }

        private void EnsureQuitGameOverlay()
        {
            if (quitGameOverlay != null)
            {
                return;
            }

            RectTransform hudRoot = transform as RectTransform;
            if (hudRoot == null)
            {
                return;
            }

            quitGameOverlay = new GameObject(
                "QuitGameOverlay",
                typeof(RectTransform));
            RectTransform overlayRect =
                quitGameOverlay.GetComponent<RectTransform>();
            overlayRect.SetParent(hudRoot, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image dimmer = quitGameOverlay.AddComponent<Image>();
            dimmer.color = new Color(0f, 0f, 0f, 0.9f);
            dimmer.raycastTarget = true;

            Button overlayCloseButton = quitGameOverlay.AddComponent<Button>();
            overlayCloseButton.targetGraphic = dimmer;
            overlayCloseButton.transition = Selectable.Transition.None;
            overlayCloseButton.onClick.AddListener(CloseQuitGameOverlay);

            float menuToHudScale = GetMainMenuToHudScale();

            GameObject buttonObject = new GameObject(
                "QuitGameButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(overlayRect, false);
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(
                0f,
                BattleButtonPositionY * menuToHudScale);
            buttonRect.sizeDelta = new Vector2(
                BattleButtonWidth * menuToHudScale,
                BattleButtonHeight * menuToHudScale);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.sprite = battleButtonSprite;
            buttonImage.type = Image.Type.Simple;
            buttonImage.color = Color.white;

            Button quitButton = buttonObject.GetComponent<Button>();
            quitButton.targetGraphic = buttonImage;
            quitButton.onClick.AddListener(QuitGame);

            GameObject labelObject = new GameObject(
                "QuitGameText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(buttonRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.anchoredPosition = new Vector2(
                0f,
                BattleButtonLabelOffsetY * menuToHudScale);
            labelRect.sizeDelta = new Vector2(0f, -6f);

            TextMeshProUGUI label =
                labelObject.GetComponent<TextMeshProUGUI>();
            label.font = battleButtonFont;
            label.fontSharedMaterial = battleButtonFontMaterial;
            label.text = "Quit Game";
            label.fontSize = BattleButtonFontSize * menuToHudScale;
            label.enableKerning = true;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            quitGameOverlay.SetActive(false);
        }

        private float GetMainMenuToHudScale()
        {
            CanvasScaler hudScaler = GetComponent<CanvasScaler>();
            if (hudScaler == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return 1f;
            }

            float mainMenuScale = Screen.width / MainMenuReferenceResolution.x;
            float hudScale = GetCanvasScale(
                hudScaler.referenceResolution,
                hudScaler.matchWidthOrHeight);
            return hudScale > 0f ? mainMenuScale / hudScale : 1f;
        }

        private static float GetCanvasScale(
            Vector2 referenceResolution,
            float matchWidthOrHeight)
        {
            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
            {
                return 1f;
            }

            float widthScale = Screen.width / referenceResolution.x;
            float heightScale = Screen.height / referenceResolution.y;
            return Mathf.Exp(Mathf.Lerp(
                Mathf.Log(widthScale),
                Mathf.Log(heightScale),
                matchWidthOrHeight));
        }
    }
}
