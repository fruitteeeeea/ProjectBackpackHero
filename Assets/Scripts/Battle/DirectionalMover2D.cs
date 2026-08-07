using BackpackHero.Debugging;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 为2D飞行物提供基础的方向、速度和直线移动能力。
    ///
    /// 素材默认朝向必须为本地Y轴正方向，也就是“朝上”。
    /// </summary>
    public sealed class DirectionalMover2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)]
        private float baseSpeed = 5f;

        [SerializeField, Min(0f)]
        private float speedMultiplier = 1f;
        
        [SerializeField]
        private Vector2 direction = Vector2.up;
        private FactionMember factionMember;
        private bool isPaused;
        
        /// <summary>
        /// 基础移动速度，不包含飞机的爆发速度倍率。
        /// </summary>
        public float BaseSpeed => baseSpeed;
        public bool IsPaused =>
            isPaused;
        
        
        /// <summary>
        /// 当前实际移动速度。
        /// </summary>
        public float CurrentSpeed =>
            baseSpeed * speedMultiplier *
            GamePacingDebugRuntime.GetAircraftSpeedMultiplier(
                factionMember != null
                    ? factionMember.Faction
                    : BattleFaction.Player) *
            StyleTendencyDebugRuntime.GetAircraftFlightSpeedMultiplier();

        /// <summary>
        /// 当前速度倍率。
        /// </summary>
        public float SpeedMultiplier => speedMultiplier;
        
        /// <summary>
        /// 当前标准化后的世界空间飞行方向。
        /// </summary>
        public Vector2 Direction => direction;

        private void Awake()
        {
            factionMember = GetComponent<FactionMember>();
            SetDirection(direction);
        }

        private void Update()
        {
            Move(Time.deltaTime);
        }

        /// <summary>
        /// 生成飞行物后调用，用于设置初始方向。
        /// </summary>
        public void Initialize(Vector2 initialDirection)
        {
            SetDirection(initialDirection);
        }

        /// <summary>
        /// 改变飞行方向，同时更新物体的视觉朝向。
        /// 后续Battle Curve也会通过这个入口控制方向。
        /// </summary>
        public void SetDirection(Vector2 newDirection)
        {
            if (newDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                Debug.LogWarning(
                    $"{name} 收到了零方向，将继续使用当前方向。",
                    this);

                return;
            }

            direction = newDirection.normalized;
            transform.up = direction;
        }

        /// <summary>
        /// 修改基础速度。
        /// </summary>
        public void SetBaseSpeed(float newSpeed)
        {
            baseSpeed = Mathf.Max(0f, newSpeed);
        }

        private void Move(float deltaTime)
        {
            if (isPaused)
            {
                return;
            }

            Vector2 movement =
                direction * CurrentSpeed * deltaTime;

            transform.position += (Vector3)movement;
        }
        
        /// <summary>
        /// 暂停或恢复直线移动。
        /// 暂停不会清除当前方向和速度。
        /// </summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }
        
        /// <summary>
        /// 设置速度倍率。
        /// 飞机可以用它实现初始爆发，普通子弹保持为1。
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0f, multiplier);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            baseSpeed = Mathf.Max(0f, baseSpeed);
            speedMultiplier = Mathf.Max(0f, speedMultiplier);

            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                direction.Normalize();
            }
            else
            {
                direction = Vector2.up;
            }
        }
#endif
    }
}
