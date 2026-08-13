using UnityEngine;
using UnityEngine.UI;

namespace BackpackHero.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleResultPresenter : MonoBehaviour
    {
        [SerializeField] private OriginalSettlementPanelAdapter resultPanel;
        [SerializeField] private string panelResourcePath =
            "PlanetWar/OriginalSettlement/Prefab/UIGameSettle";
        public bool IsShowing => resultPanel != null && resultPanel.gameObject.activeSelf;

        private void Awake()
        {
            EnsurePanel();
        }

        public void Show(BattleResultData data)
        {
            if (!EnsurePanel())
            {
                Debug.LogError("Battle result panel is not assigned.", this);
                return;
            }
            resultPanel.Show(data);
        }

        private bool EnsurePanel()
        {
            if (resultPanel != null) return true;

            OriginalSettlementPanelAdapter prefab =
                Resources.Load<OriginalSettlementPanelAdapter>(
                panelResourcePath);
            if (prefab == null) return false;

            GameObject host = new GameObject("Battle Result Overlay",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = host.GetComponent<Canvas>();
            // Keep this result layer independent of post processing used by
            // the battle camera, just like the backpack overlay UI.
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // The original UIGameSettle was authored under the UI Scene's
            // portrait 720 x 1280 Canvas.  Preserve that authoring space so
            // every copied RectTransform retains its original proportion.
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            resultPanel = Instantiate(prefab, host.transform);
            resultPanel.gameObject.SetActive(false);
            return true;
        }
    }
}
