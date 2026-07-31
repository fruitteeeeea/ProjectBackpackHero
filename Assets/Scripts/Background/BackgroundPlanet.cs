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
        private const float DefaultDriftRadius = .8f;
        private const float DefaultDriftSpeed = .6f;
        private const float DefaultSpringStrength = 8f;
        private const float DefaultDamping = 3.2f;
        private const float DefaultMaxDistance = .8f;
        private const float DefaultMaxSpeed = 8f;

        [SerializeField, Min(0f)] private float driftRadius = DefaultDriftRadius;
        [SerializeField, Min(0f)] private float driftSpeed = DefaultDriftSpeed;
        [SerializeField, Min(0f)] private float springStrength = DefaultSpringStrength;
        [SerializeField, Min(0f)] private float damping = DefaultDamping;
        [SerializeField, Min(0f)] private float maxDistance = DefaultMaxDistance;
        [SerializeField, Min(0f)] private float maxSpeed = DefaultMaxSpeed;

        private Vector3 anchorPosition;
        private Vector2 velocity;
        private float noiseSeed;
        private float nextInteractionTime;

        public Vector2 WorldPosition => transform.position;
        public Vector3 AnchorPosition => anchorPosition;
        public static BackgroundPlanetPhysicsSettings DefaultPhysicsSettings => new()
        {
            DriftRadius = DefaultDriftRadius,
            DriftSpeed = DefaultDriftSpeed,
            SpringStrength = DefaultSpringStrength,
            Damping = DefaultDamping,
            MaxDistance = DefaultMaxDistance,
            MaxSpeed = DefaultMaxSpeed,
        };
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

        public bool TryTriggerRandomMotion(
            float minimumImpulse,
            float maximumImpulse,
            float cooldown,
            float rearmSpeed)
        {
            if (Time.time < nextInteractionTime || velocity.magnitude > Mathf.Max(0f, rearmSpeed))
            {
                return false;
            }

            var direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = Vector2.right;
            }

            var impulse = Random.Range(
                Mathf.Max(0f, minimumImpulse),
                Mathf.Max(minimumImpulse, maximumImpulse));
            velocity += direction * impulse;
            velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
            nextInteractionTime = Time.time + Mathf.Max(0f, cooldown);
            return true;
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
