using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>使用LineRenderer显示一段命中瞬间位置快照的闪电连线。</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LightningLinkVfx2D : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField, Min(0.01f)] private float duration = 0.15f;

        private float elapsed;

        private void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }
        }

        public void Configure(Vector2 start, Vector2 end)
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
