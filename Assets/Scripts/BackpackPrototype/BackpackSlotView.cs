using BackpackHero.Debugging;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    public sealed class BackpackSlotView : MonoBehaviour
    {
        [Header("Slot Data")]
        [SerializeField]
        private Vector2Int cell;

        [Header("References")]
        [SerializeField]
        private Image background;

        [SerializeField]
        private Text label;

        [Header("Preview Colors")]
        [SerializeField]
        private Color defaultColor = Color.white;

        [SerializeField]
        private Color legalPreviewColor =
            new Color(0.95f, 0.78f, 0.18f, 1f);

        [SerializeField]
        private Color illegalPreviewColor =
            new Color(0.86f, 0.22f, 0.18f, 1f);

        private bool occupied;

        public Vector2Int Cell => cell;

        private void Awake()
        {
            CacheBackground();
            ClearPreview();
        }

        public void Initialize(Vector2Int slotCell)
        {
            cell = slotCell;

            CacheBackground();
            ClearPreview();

            if (label != null)
            {
                label.text = $"({cell.x},{cell.y})";
            }
        }

        public void SetPreview(bool canPlace)
        {
            CacheBackground();

            if (background == null)
            {
                return;
            }

            BackpackVisualSettings settings =
                BackpackVisualDebugRuntime.CurrentSettings;
            background.enabled = true;
            background.color = settings.OverridesEnabled
                ? (canPlace ? settings.LegalPreviewColor :
                    settings.IllegalPreviewColor)
                : (canPlace ? legalPreviewColor : illegalPreviewColor);
        }

        /// <summary>
        /// Controls whether a placed item covers this slot's background.
        /// The slot remains active so its coordinate and input behavior are unchanged.
        /// </summary>
        public void SetOccupied(bool isOccupied)
        {
            occupied = isOccupied;
            ClearPreview();
        }

        public void ClearPreview()
        {
            CacheBackground();

            if (background == null)
            {
                return;
            }

            background.color = defaultColor;
            background.enabled = !occupied;
        }

        private void CacheBackground()
        {
            if (background == null)
            {
                background = GetComponent<Image>();
            }
        }
    }
}
