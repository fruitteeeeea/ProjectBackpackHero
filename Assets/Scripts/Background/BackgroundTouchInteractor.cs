using UnityEngine;
using UnityEngine.InputSystem;

namespace BackpackHero.Background
{
    [System.Serializable]
    public struct BackgroundTouchPhysicsSettings
    {
        public float InfluenceRadius;
        public float MinimumRandomImpulse;
        public float MaximumRandomImpulse;
        public float InteractionCooldown;
        public float RearmSpeed;
    }

    public sealed class BackgroundTouchInteractor : MonoBehaviour
    {
        private const float DefaultInfluenceRadius = 3f;
        private const float DefaultMinimumRandomImpulse = 3.5f;
        private const float DefaultMaximumRandomImpulse = 4f;
        private const float DefaultInteractionCooldown = .35f;
        private const float DefaultRearmSpeed = .12f;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private BackgroundPlanet[] planets;
        [SerializeField, Min(0f)] private float influenceRadius = DefaultInfluenceRadius;
        [SerializeField, Min(0f)] private float minimumRandomImpulse = DefaultMinimumRandomImpulse;
        [SerializeField, Min(0f)] private float maximumRandomImpulse = DefaultMaximumRandomImpulse;
        [SerializeField, Min(0f)] private float interactionCooldown = DefaultInteractionCooldown;
        [SerializeField, Min(0f)] private float rearmSpeed = DefaultRearmSpeed;
        [SerializeField] private float interactionPlaneZ = 2f;

        public void Configure(Camera camera, BackgroundPlanet[] configuredPlanets, float planeZ)
        {
            targetCamera = camera;
            planets = configuredPlanets;
            interactionPlaneZ = planeZ;
        }

        public static BackgroundTouchPhysicsSettings DefaultPhysicsSettings => new()
        {
            InfluenceRadius = DefaultInfluenceRadius,
            MinimumRandomImpulse = DefaultMinimumRandomImpulse,
            MaximumRandomImpulse = DefaultMaximumRandomImpulse,
            InteractionCooldown = DefaultInteractionCooldown,
            RearmSpeed = DefaultRearmSpeed,
        };

        public BackgroundTouchPhysicsSettings PhysicsSettings => new()
        {
            InfluenceRadius = influenceRadius,
            MinimumRandomImpulse = minimumRandomImpulse,
            MaximumRandomImpulse = maximumRandomImpulse,
            InteractionCooldown = interactionCooldown,
            RearmSpeed = rearmSpeed,
        };

        public void SetPhysicsSettings(BackgroundTouchPhysicsSettings settings)
        {
            influenceRadius = Mathf.Max(0f, settings.InfluenceRadius);
            minimumRandomImpulse = Mathf.Max(0f, settings.MinimumRandomImpulse);
            maximumRandomImpulse = Mathf.Max(minimumRandomImpulse, settings.MaximumRandomImpulse);
            interactionCooldown = Mathf.Max(0f, settings.InteractionCooldown);
            rearmSpeed = Mathf.Max(0f, settings.RearmSpeed);
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
                    if (!touch.press.wasPressedThisFrame)
                    {
                        continue;
                    }

                    hasTouch = true;
                    TryTriggerAtScreenPoint(touch.position.ReadValue());
                }
            }

            var mouse = Mouse.current;
            if (!hasTouch && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                TryTriggerAtScreenPoint(mouse.position.ReadValue());
            }
        }

        private void TryTriggerAtScreenPoint(Vector2 screenPosition)
        {
            var worldPosition = ScreenToWorld(screenPosition);

            foreach (var planet in planets)
            {
                if (planet == null)
                {
                    continue;
                }

                if (Vector2.Distance(planet.WorldPosition, worldPosition) > influenceRadius)
                {
                    continue;
                }

                planet.TryTriggerRandomMotion(
                    minimumRandomImpulse,
                    maximumRandomImpulse,
                    interactionCooldown,
                    rearmSpeed);
            }
        }

        private Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            var distance = interactionPlaneZ - targetCamera.transform.position.z;
            return targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance));
        }

    }
}
