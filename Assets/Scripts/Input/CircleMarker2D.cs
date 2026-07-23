using UnityEngine;

namespace BackpackHero.Input
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CircleMarker2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color color = Color.white;
        [SerializeField, Min(0.01f)] private float diameter = 1f;

        public Color Color
        {
            get => color;
            set
            {
                color = value;
                RefreshVisual();
            }
        }

        public float Diameter
        {
            get => diameter;
            set
            {
                diameter = Mathf.Max(0.01f, value);
                RefreshVisual();
            }
        }

        private void OnEnable()
        {
            EnsureSpriteRenderer();
            RefreshVisual();
        }

        private void OnValidate()
        {
            diameter = Mathf.Max(0.01f, diameter);
            EnsureSpriteRenderer();
            RefreshVisual();
        }

        private void EnsureSpriteRenderer()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            }

            spriteRenderer.sortingOrder = 1;
        }

        private void RefreshVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.color = color;
            transform.localScale = new Vector3(diameter, diameter, 1f);
        }
    }
}
