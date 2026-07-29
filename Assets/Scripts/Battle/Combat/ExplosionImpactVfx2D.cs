using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>短暂扩张并淡出的爆炸环形视觉。</summary>
    public sealed class ExplosionImpactVfx2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Min(0.01f)] private float duration = 0.35f;
        [SerializeField, Min(0.01f)] private float finalScale = 2.4f;

        private float elapsed;
        private Color initialColor;

        private void Awake()
        {
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            if (visual != null)
            {
                initialColor = visual.color;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, finalScale, progress);

            if (visual != null)
            {
                Color color = initialColor;
                color.a *= 1f - progress;
                visual.color = color;
            }

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
