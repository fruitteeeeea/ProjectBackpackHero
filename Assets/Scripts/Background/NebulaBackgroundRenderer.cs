using UnityEngine;
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
        private static readonly int SpeedId = Shader.PropertyToID("_Speed");

        [SerializeField] private Camera targetCamera;
        [SerializeField, Min(0.31f)] private float cameraDistance = DefaultDistance;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private Color overtimeNebulaColor = new(0.42f, 0.02f, 0.16f, 1f);
        [SerializeField, Min(0.01f)] private float overtimeTransitionDuration = 1f;

        private Material runtimeMaterial;
        private Color normalNebulaColor;
        private float normalSpeed;

        private void OnEnable()
        {
            ResolveReferences();
            CacheRuntimeMaterial();
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
            if (runtimeMaterial == null || !runtimeMaterial.HasProperty(ColorBId) ||
                !runtimeMaterial.HasProperty(SpeedId))
            {
                return;
            }

            normalNebulaColor = runtimeMaterial.GetColor(ColorBId);
            normalSpeed = runtimeMaterial.GetFloat(SpeedId);
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
            float transition = Mathf.Clamp01(Time.deltaTime /
                Mathf.Max(0.01f, overtimeTransitionDuration));
            runtimeMaterial.SetColor(ColorBId, Color.Lerp(
                runtimeMaterial.GetColor(ColorBId),
                overtime ? overtimeNebulaColor : normalNebulaColor,
                transition));
            runtimeMaterial.SetFloat(SpeedId, Mathf.Lerp(
                runtimeMaterial.GetFloat(SpeedId),
                overtime ? normalSpeed * LevelFlowController.OvertimeCooldownSpeedMultiplier : normalSpeed,
                transition));
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
