using UnityEngine;
using UnityEngine.InputSystem;

namespace BackpackHero.Battle
{
    /// <summary>
    /// BattlePrototype中的鼠标瞄准与子弹发射测试台。
    /// 素材的本地Y轴正方向视为枪口方向。
    /// </summary>
    [RequireComponent(
        typeof(ProjectileFireModeController2D))]
    public sealed class TestShooter2D : MonoBehaviour
    {
        public const float MinimumManualTriggerInterval =
            0.02f;

        [Header("References")]
        [SerializeField]
        private Transform firePoint;

        [SerializeField]
        private Projectile2D projectilePrefab;

        [SerializeField]
        private ProjectileFireModeController2D
            fireModeController;

        [Header("Input")]
        [SerializeField, Min(MinimumManualTriggerInterval)]
        private float manualTriggerInterval = 0.2f;

        [Header("Projectile")]
        [SerializeField]
        private BattleFaction faction = BattleFaction.Player;

        [SerializeField, Min(0f)]
        private float projectileDamage = 1f;

        [SerializeField, Min(0f)]
        private float projectileSpeed = 8f;

        [SerializeField, Min(0f)]
        private float projectileLifetime = 5f;

        private Camera aimCamera;
        private bool isFiring;
        private bool hasReportedMissingCamera;
        private bool hasReportedMissingFirePoint;
        private bool hasReportedMissingProjectilePrefab;
        private bool hasReportedMissingLifetime;

        private readonly TestShooterManualTriggerTimer
            manualTriggerTimer = new();

        public Transform FirePoint => firePoint;
        public Projectile2D ProjectilePrefab => projectilePrefab;
        public BattleFaction Faction => faction;
        public float ProjectileDamage => projectileDamage;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public float ManualTriggerInterval =>
            manualTriggerInterval;

        public bool IsFiring => isFiring;
        public Vector2 AimDirection => transform.up;
        public ProjectileFireModeController2D
            FireModeController => fireModeController;

        private void Awake()
        {
            FindReferences();
        }

        private void OnEnable()
        {
            FindReferences();

            if (fireModeController != null)
            {
                fireModeController.ShotRequested -=
                    SpawnProjectile;

                fireModeController.ShotRequested +=
                    SpawnProjectile;
            }

            isFiring = false;
            manualTriggerTimer.Reset();
            TestShooterDebugRuntimeBridge.Register(this);
        }

        private void OnDisable()
        {
            if (fireModeController != null)
            {
                fireModeController.ShotRequested -=
                    SpawnProjectile;
            }

            isFiring = false;
            manualTriggerTimer.Reset();
            TestShooterDebugRuntimeBridge.Unregister(this);
        }

        private void Update()
        {
            UpdateAim();
            UpdateShooting(Time.deltaTime);
        }

        public void SetProjectilePrefab(Projectile2D prefab)
        {
            projectilePrefab = prefab;
            hasReportedMissingProjectilePrefab = false;
            hasReportedMissingLifetime = false;
        }

        public void SetFaction(BattleFaction newFaction)
        {
            faction = newFaction;
        }

        public void SetProjectileDamage(float damage)
        {
            projectileDamage = Mathf.Max(0f, damage);
        }

        public void SetProjectileSpeed(float speed)
        {
            projectileSpeed = Mathf.Max(0f, speed);
        }

        public void SetProjectileLifetime(float lifetime)
        {
            projectileLifetime = Mathf.Max(0f, lifetime);
        }

        public void SetManualTriggerInterval(float interval)
        {
            manualTriggerInterval =
                Mathf.Max(
                    MinimumManualTriggerInterval,
                    interval);
        }

        /// <summary>
        /// 将射手朝向一个世界坐标。目标与射手重合时保留上一有效朝向。
        /// </summary>
        public bool AimAtWorldPosition(Vector2 worldPosition)
        {
            Vector2 direction =
                worldPosition - (Vector2)transform.position;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            transform.up = direction.normalized;
            return true;
        }

        /// <summary>
        /// 主动触发当前挂载的全部发射模式。
        /// </summary>
        public int TriggerAllFireModes()
        {
            FindReferences();

            if (fireModeController == null)
            {
                Debug.LogError(
                    $"{name}无法发射：缺少发射模式控制器。",
                    this);

                return 0;
            }

            return fireModeController.TriggerAll(
                AimDirection);
        }

        public int AddManualFireMode(
            ProjectileFirePattern pattern)
        {
            FindReferences();

            return fireModeController != null
                ? fireModeController.AddMode(
                    pattern,
                    ProjectileFireModeController2D
                        .ManualInterval)
                : -1;
        }

        public bool RemoveFireModeAt(int index)
        {
            FindReferences();

            return fireModeController != null &&
                   fireModeController.RemoveModeAt(index);
        }

        public bool SetFireModePattern(
            int index,
            ProjectileFirePattern pattern)
        {
            FindReferences();

            return fireModeController != null &&
                   fireModeController.SetModePattern(
                       index,
                       pattern);
        }

        private void SpawnProjectile(
            Vector2 fireDirection)
        {
            if (firePoint == null)
            {
                ReportMissingFirePoint();
                return;
            }

            if (projectilePrefab == null)
            {
                ReportMissingProjectilePrefab();
                return;
            }

            Projectile2D projectile =
                Instantiate(
                    projectilePrefab,
                    firePoint.position,
                    Quaternion.FromToRotation(
                        Vector2.up,
                        fireDirection));

            projectile.Initialize(
                faction,
                projectileDamage,
                projectileSpeed,
                fireDirection);

            LifetimeAndScreenBounds2D lifetime =
                projectile.GetComponent<
                    LifetimeAndScreenBounds2D>();

            if (lifetime != null)
            {
                lifetime.SetLifetime(
                    projectileLifetime);
            }
            else if (!hasReportedMissingLifetime)
            {
                hasReportedMissingLifetime = true;

                Debug.LogWarning(
                    $"{projectilePrefab.name}没有" +
                    $"{nameof(LifetimeAndScreenBounds2D)}，" +
                    "调试面板中的子弹寿命不会生效。",
                    projectilePrefab);
            }
        }

        private void UpdateAim()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null)
            {
                return;
            }

            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            if (aimCamera == null)
            {
                if (!hasReportedMissingCamera)
                {
                    hasReportedMissingCamera = true;
                    Debug.LogWarning(
                        $"{name}无法瞄准：场景中没有Main Camera。",
                        this);
                }

                return;
            }

            hasReportedMissingCamera = false;

            Vector3 screenPosition =
                mouse.position.ReadValue();

            screenPosition.z =
                Mathf.Abs(
                    aimCamera.transform.position.z -
                    transform.position.z);

            Vector3 mouseWorldPosition =
                aimCamera.ScreenToWorldPoint(
                    screenPosition);

            AimAtWorldPosition(mouseWorldPosition);
        }

        private void UpdateShooting(float deltaTime)
        {
            Mouse mouse = Mouse.current;

            if (mouse == null)
            {
                isFiring = false;
                manualTriggerTimer.Reset();
                return;
            }

            isFiring =
                mouse.leftButton.isPressed;

            if (manualTriggerTimer.Tick(
                    deltaTime,
                    isFiring,
                    mouse.leftButton.wasPressedThisFrame,
                    manualTriggerInterval))
            {
                TriggerAllFireModes();
            }
        }

        private void FindReferences()
        {
            if (fireModeController == null)
            {
                fireModeController =
                    GetComponent<
                        ProjectileFireModeController2D>();
            }
        }

        private void ReportMissingFirePoint()
        {
            if (hasReportedMissingFirePoint)
            {
                return;
            }

            hasReportedMissingFirePoint = true;
            Debug.LogWarning(
                $"{name}无法发射：没有配置Fire Point。",
                this);
        }

        private void ReportMissingProjectilePrefab()
        {
            if (hasReportedMissingProjectilePrefab)
            {
                return;
            }

            hasReportedMissingProjectilePrefab = true;
            Debug.LogWarning(
                $"{name}无法发射：没有选择Projectile Prefab。",
                this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetProjectileDamage(projectileDamage);
            SetProjectileSpeed(projectileSpeed);
            SetProjectileLifetime(projectileLifetime);
            SetManualTriggerInterval(
                manualTriggerInterval);

            FindReferences();
        }
#endif
    }

    /// <summary>
    /// TestShooter输入层的按住重复触发计时器。
    /// 不改变任何发射模式自身的冷却。
    /// </summary>
    public sealed class TestShooterManualTriggerTimer
    {
        private float remainingTime;

        public float RemainingTime => remainingTime;

        public void Reset()
        {
            remainingTime = 0f;
        }

        public bool Tick(
            float deltaTime,
            bool isPressed,
            bool wasPressedThisFrame,
            float triggerInterval)
        {
            if (!isPressed)
            {
                Reset();
                return false;
            }

            if (wasPressedThisFrame)
            {
                remainingTime = 0f;
            }

            remainingTime -=
                Mathf.Max(0f, deltaTime);

            if (remainingTime > 0f)
            {
                return false;
            }

            remainingTime =
                Mathf.Max(
                    TestShooter2D
                        .MinimumManualTriggerInterval,
                    triggerInterval);

            return true;
        }
    }
}
