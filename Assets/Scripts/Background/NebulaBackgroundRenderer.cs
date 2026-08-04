using UnityEngine;

namespace BackpackHero.Background
{
    /// <summary>
    /// Keeps a sprite-backed procedural background fitted to the active camera's view.
    /// The shader derives its UVs from screen coordinates, so the image remains seamless
    /// while this renderer follows the camera.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class NebulaBackgroundRenderer : MonoBehaviour
    {
        private const float DefaultDistance = 13f;
        private const float CoveragePadding = 1.06f;

        [SerializeField] private Camera targetCamera;
        [SerializeField, Min(0.31f)] private float cameraDistance = DefaultDistance;
        [SerializeField] private SpriteRenderer backgroundRenderer;

        private void OnEnable()
        {
            ResolveReferences();
            FitToCamera();
        }

        private void OnValidate()
        {
            cameraDistance = Mathf.Max(0.31f, cameraDistance);
            ResolveReferences();
            FitToCamera();
        }

        private void LateUpdate()
        {
            FitToCamera();
        }

        private void ResolveReferences()
        {
            backgroundRenderer ??= GetComponent<SpriteRenderer>();
            targetCamera ??= Camera.main;
        }

        private void FitToCamera()
        {
            if (targetCamera == null || backgroundRenderer == null || backgroundRenderer.sprite == null)
            {
                return;
            }

            var cameraTransform = targetCamera.transform;
            transform.SetPositionAndRotation(
                cameraTransform.position + cameraTransform.forward * cameraDistance,
                cameraTransform.rotation);

            var viewHeight = targetCamera.orthographic
                ? targetCamera.orthographicSize * 2f
                : 2f * cameraDistance * Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * .5f);
            var viewWidth = viewHeight * targetCamera.aspect;
            var spriteSize = backgroundRenderer.sprite.bounds.size;
            if (spriteSize.x <= Mathf.Epsilon || spriteSize.y <= Mathf.Epsilon)
            {
                return;
            }

            transform.localScale = new Vector3(
                viewWidth * CoveragePadding / spriteSize.x,
                viewHeight * CoveragePadding / spriteSize.y,
                1f);
        }
    }
}
