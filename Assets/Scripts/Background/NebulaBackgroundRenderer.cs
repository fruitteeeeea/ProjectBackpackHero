using UnityEngine;
using UnityEngine.Serialization;
using BackpackHero.Battle;

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
        private static readonly int ColorBId = Shader.PropertyToID("_ColorB");

        [SerializeField] private Camera targetCamera;
        [SerializeField, Min(0.31f)] private float cameraDistance = DefaultDistance;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private Color overtimeNebulaColor = new(0.42f, 0.02f, 0.16f, 1f);
        [FormerlySerializedAs("overtimeTransitionDuration")]
        [SerializeField, Min(0.01f)] private float overtimeColorTransitionDuration = 1f;
        [SerializeField, Min(0.01f)] private float normalTransitionDuration = 1f;

        private Material runtimeMaterial;
        private Color normalNebulaColor;
        private bool hasOvertimeVisualState;
        private bool overtimeVisualState;
        private Color colorTransitionStart;
        private float colorTransitionElapsed;

        private void OnEnable()
        {
            ResolveReferences();
            CacheRuntimeMaterial();
            hasOvertimeVisualState = false;
            LevelFlowController.RoundTimerStateChanged += RefreshOvertimeVisual;
            RefreshOvertimeVisual();
            FitToCamera();
        }

        private void OnDisable()
        {
            LevelFlowController.RoundTimerStateChanged -= RefreshOvertimeVisual;
            if (Application.isPlaying && runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

        private void OnValidate()
        {
            cameraDistance = Mathf.Max(0.31f, cameraDistance);
            overtimeColorTransitionDuration = Mathf.Max(0.01f, overtimeColorTransitionDuration);
            normalTransitionDuration = Mathf.Max(0.01f, normalTransitionDuration);
            ResolveReferences();
            FitToCamera();
        }

        private void LateUpdate()
        {
            FitToCamera();
            UpdateOvertimeVisual();
        }

        private void ResolveReferences()
        {
            backgroundRenderer ??= GetComponent<SpriteRenderer>();
            targetCamera ??= Camera.main;
        }

        private void CacheRuntimeMaterial()
        {
            if (!Application.isPlaying || runtimeMaterial != null ||
                backgroundRenderer == null)
            {
                return;
            }

            runtimeMaterial = backgroundRenderer.material;
            if (runtimeMaterial == null || !runtimeMaterial.HasProperty(ColorBId))
            {
                return;
            }

            normalNebulaColor = runtimeMaterial.GetColor(ColorBId);
        }

        private void RefreshOvertimeVisual()
        {
            CacheRuntimeMaterial();
        }

        private void UpdateOvertimeVisual()
        {
            if (!Application.isPlaying || runtimeMaterial == null)
            {
                return;
            }

            bool overtime = LevelFlowController.Instance?.IsOvertime == true;
            if (!hasOvertimeVisualState || overtimeVisualState != overtime)
            {
                BeginOvertimeVisualTransition(overtime);
            }

            float duration = overtime
                ? overtimeColorTransitionDuration
                : normalTransitionDuration;
            colorTransitionElapsed += Time.deltaTime;
            runtimeMaterial.SetColor(ColorBId, Color.Lerp(
                colorTransitionStart,
                overtime ? overtimeNebulaColor : normalNebulaColor,
                Mathf.Clamp01(colorTransitionElapsed / duration)));
        }

        private void BeginOvertimeVisualTransition(bool overtime)
        {
            hasOvertimeVisualState = true;
            overtimeVisualState = overtime;
            colorTransitionStart = runtimeMaterial.GetColor(ColorBId);
            colorTransitionElapsed = 0f;
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
