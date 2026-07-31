using UnityEngine;

namespace BackpackHero.Background
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ConstellationLink : MonoBehaviour
    {
        [SerializeField] private Transform startNode;
        [SerializeField] private Transform endNode;
        private LineRenderer lineRenderer;

        public void Configure(Transform start, Transform end, Material material)
        {
            startNode = start;
            endNode = end;
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.sharedMaterial = material;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.widthMultiplier = .35f;
            lineRenderer.numCapVertices = 6;
            lineRenderer.sortingLayerName = "BackgroundLines";
            lineRenderer.startColor = new Color(.66f, .7f, .75f, .22f);
            lineRenderer.endColor = new Color(.66f, .7f, .75f, .22f);
        }

        private void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            if (lineRenderer != null && lineRenderer.sharedMaterial == null)
            {
                Configure(startNode, endNode,
                    Resources.Load<Material>("Background/ConstellationAlphaMask"));
            }
        }

        private void LateUpdate()
        {
            if (startNode == null || endNode == null)
            {
                return;
            }

            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            lineRenderer.SetPosition(0, startNode.position);
            lineRenderer.SetPosition(1, endNode.position);
        }
    }
}
