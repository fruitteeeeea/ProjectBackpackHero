using UnityEngine;
using UnityEngine.InputSystem;

namespace BackpackHero.Background
{
    [System.Serializable]
    public struct BackgroundTouchPhysicsSettings
    {
        public float InfluenceRadius;
        public float FollowStrength;
        public float ImpulseStrength;
        public float MaxAppliedDelta;
    }

    public sealed class BackgroundTouchInteractor : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private BackgroundPlanet[] planets;
        [SerializeField, Min(0f)] private float influenceRadius = 1.4f;
        [SerializeField, Min(0f)] private float followStrength = .65f;
        [SerializeField, Min(0f)] private float impulseStrength = 2f;
        [SerializeField, Min(0f)] private float maxAppliedDelta = .4f;
        [SerializeField] private float interactionPlaneZ = 2f;

        public void Configure(Camera camera, BackgroundPlanet[] configuredPlanets, float planeZ)
        {
            targetCamera = camera;
            planets = configuredPlanets;
            interactionPlaneZ = planeZ;
        }

        public BackgroundTouchPhysicsSettings PhysicsSettings => new()
        {
            InfluenceRadius = influenceRadius,
            FollowStrength = followStrength,
            ImpulseStrength = impulseStrength,
            MaxAppliedDelta = maxAppliedDelta,
        };

        public void SetPhysicsSettings(BackgroundTouchPhysicsSettings settings)
        {
            influenceRadius = Mathf.Max(0f, settings.InfluenceRadius);
            followStrength = Mathf.Max(0f, settings.FollowStrength);
            impulseStrength = Mathf.Max(0f, settings.ImpulseStrength);
            maxAppliedDelta = Mathf.Max(0f, settings.MaxAppliedDelta);
        }

        private void Update()
        {
            if (targetCamera == null || planets == null || planets.Length == 0)
            {
                return;
            }

            var hasTouch = false;
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    if (!touch.press.isPressed)
                    {
                        continue;
                    }

                    hasTouch = true;
                    ApplyScreenSegment(touch.position.ReadValue() - touch.delta.ReadValue(), touch.position.ReadValue());
                }
            }

            var mouse = Mouse.current;
            if (!hasTouch && mouse != null && mouse.leftButton.isPressed)
            {
                ApplyScreenSegment(mouse.position.ReadValue() - mouse.delta.ReadValue(), mouse.position.ReadValue());
            }
        }

        private void ApplyScreenSegment(Vector2 previousScreenPosition, Vector2 currentScreenPosition)
        {
            var previous = ScreenToWorld(previousScreenPosition);
            var current = ScreenToWorld(currentScreenPosition);
            var delta = Vector2.ClampMagnitude(current - previous, maxAppliedDelta);
            if (delta.sqrMagnitude <= 0f)
            {
                return;
            }

            foreach (var planet in planets)
            {
                if (planet == null)
                {
                    continue;
                }

                var distance = DistanceToSegment(planet.WorldPosition, previous, current);
                if (distance >= influenceRadius)
                {
                    continue;
                }

                var weight = Mathf.SmoothStep(0f, 1f, 1f - distance / influenceRadius);
                planet.ApplyPointerDrag(delta, weight, followStrength, impulseStrength);
            }
        }

        private Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            var distance = interactionPlaneZ - targetCamera.transform.position.z;
            return targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance));
        }

        public static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, start);
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + segment * t);
        }
    }
}
