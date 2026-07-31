using UnityEngine;

namespace BackpackHero.Background
{
    [System.Serializable]
    public struct BackgroundPlanetPhysicsSettings
    {
        public float DriftRadius;
        public float DriftSpeed;
        public float SpringStrength;
        public float Damping;
        public float MaxDistance;
        public float MaxSpeed;
    }

    public sealed class BackgroundPlanet : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float driftRadius = 0.22f;
        [SerializeField, Min(0f)] private float driftSpeed = 0.12f;
        [SerializeField, Min(0f)] private float springStrength = 7f;
        [SerializeField, Min(0f)] private float damping = 5f;
        [SerializeField, Min(0f)] private float maxDistance = 0.6f;
        [SerializeField, Min(0f)] private float maxSpeed = 2f;

        private Vector3 anchorPosition;
        private Vector2 velocity;
        private float noiseSeed;

        public Vector2 WorldPosition => transform.position;
        public Vector3 AnchorPosition => anchorPosition;
        public BackgroundPlanetPhysicsSettings PhysicsSettings => new()
        {
            DriftRadius = driftRadius,
            DriftSpeed = driftSpeed,
            SpringStrength = springStrength,
            Damping = damping,
            MaxDistance = maxDistance,
            MaxSpeed = maxSpeed,
        };

        public void SetPhysicsSettings(BackgroundPlanetPhysicsSettings settings)
        {
            driftRadius = Mathf.Max(0f, settings.DriftRadius);
            driftSpeed = Mathf.Max(0f, settings.DriftSpeed);
            springStrength = Mathf.Max(0f, settings.SpringStrength);
            damping = Mathf.Max(0f, settings.Damping);
            maxDistance = Mathf.Max(0f, settings.MaxDistance);
            maxSpeed = Mathf.Max(0f, settings.MaxSpeed);
            ClampToActivityRange();
        }

        public void SetAnchor(Vector3 anchor, float seed)
        {
            anchorPosition = anchor;
            noiseSeed = seed;
            velocity = Vector2.zero;
            transform.position = anchor;
        }

        public void ApplyPointerDrag(Vector2 worldDelta, float weight, float followStrength, float impulseStrength)
        {
            if (weight <= 0f || worldDelta.sqrMagnitude <= 0f)
            {
                return;
            }

            var appliedDelta = worldDelta * Mathf.Clamp01(weight);
            transform.position += (Vector3)(appliedDelta * Mathf.Max(0f, followStrength));
            velocity += appliedDelta * Mathf.Max(0f, impulseStrength);
            velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
            ClampToActivityRange();
        }

        private void Awake()
        {
            anchorPosition = transform.position;
            noiseSeed = Mathf.Abs(transform.position.x * 17.31f + transform.position.y * 41.17f);
        }

        private void Update()
        {
            var time = Time.time * driftSpeed;
            var drift = new Vector2(
                (Mathf.PerlinNoise(noiseSeed, time) - .5f) * 2f,
                (Mathf.PerlinNoise(noiseSeed + 19.17f, time) - .5f) * 2f) * driftRadius;
            var displacement = (Vector2)(transform.position - anchorPosition);
            velocity += (drift - displacement) * springStrength * Time.deltaTime;
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, damping * Time.deltaTime);
            velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
            transform.position += (Vector3)(velocity * Time.deltaTime);
            ClampToActivityRange();
        }

        private void ClampToActivityRange()
        {
            var offset = (Vector2)(transform.position - anchorPosition);
            if (offset.magnitude <= maxDistance)
            {
                return;
            }

            var normal = offset.normalized;
            transform.position = anchorPosition + (Vector3)(normal * maxDistance);
            var outwardVelocity = Vector2.Dot(velocity, normal);
            if (outwardVelocity > 0f)
            {
                velocity -= normal * outwardVelocity;
            }
        }
    }
}
