using UnityEngine;

namespace BackpackHero.Battle
{
    public enum FighterTargetSelectionMode
    {
        Standard,
        FarthestFighter,
    }

    [CreateAssetMenu(
        fileName = "New Fighter",
        menuName = "Battle/Fighter Definition")]
    public sealed class FighterDefinition :
        ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string displayName = "Fighter";

        [Header("Presentation")]
        [SerializeField]
        private Sprite sprite;

        [SerializeField]
        private Color baseColor = Color.white;

        [Header("Movement")]
        [SerializeField, Min(0f)]
        private float baseSpeed = 3f;

        [Header("Combat")]
        [SerializeField, Min(1)]
        private int maximumHealth = 10;

        [Tooltip("飞机开始攻击目标的最大距离，单位为世界单位。")]
        [SerializeField, Min(0.1f)]
        private float attackRange = 2f;

        [Tooltip(
            "飞机前方索敌扇形的完整夹角。" +
            "30表示左右各15度。")]
        [SerializeField, Range(1f, 360f)]
        private float targetingArcAngle = 30f;

        [Tooltip("索敌时的目标优先级；数值越高越会被其他飞机优先锁定。")]
        [SerializeField, Min(0)]
        private int targetingPriority;

        [Tooltip("标准：按优先级、朝向和距离选择；最远敌机：选择索敌范围内最远的敌方飞机。")]
        [SerializeField]
        private FighterTargetSelectionMode targetingMode;
        
        [Tooltip("两次发射之间的时间间隔，单位为秒。")]
        [SerializeField, Min(0.1f)]
        private float attackInterval = 0.5f;

        [Tooltip("每颗子弹造成的伤害。")]
        [SerializeField, Min(0f)]
        private float projectileDamage = 1f;

        [Tooltip("子弹每秒移动的世界单位数。")]
        [SerializeField, Min(0.1f)]
        private float projectileSpeed = 8f;

        [Header("Default Attack Override")]
        [Tooltip("留空时使用 Fighter Prefab 的默认攻击预制体。")]
        [SerializeField]
        private BattleAttack2D defaultAttackPrefab;

        [Tooltip("留空时使用 Fighter Prefab 的默认发射模式。")]
        [SerializeField]
        private ProjectileFirePattern defaultFirePattern;

        [Header("Special Abilities")]
        [Tooltip("飞机死亡时立即触发其 ProjectileImpactEffect2D。")]
        [SerializeField]
        private BattleAttack2D deathExplosionAttackPrefab;

        
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public Color BaseColor => baseColor;
        public float BaseSpeed => baseSpeed;
        public int MaximumHealth => maximumHealth;
        
        public float AttackRange =>
            attackRange;

        public float TargetingArcAngle =>
            targetingArcAngle;

        public int TargetingPriority =>
            targetingPriority;

        public FighterTargetSelectionMode TargetingMode =>
            targetingMode;
        
        public float AttackInterval =>
            attackInterval;

        public float ProjectileDamage =>
            projectileDamage;

        public float ProjectileSpeed =>
            projectileSpeed;

        public BattleAttack2D DefaultAttackPrefab =>
            defaultAttackPrefab;

        public ProjectileFirePattern DefaultFirePattern =>
            defaultFirePattern;

        public BattleAttack2D DeathExplosionAttackPrefab =>
            deathExplosionAttackPrefab;

#if UNITY_EDITOR
        private void OnValidate()
        {
            baseSpeed =
                Mathf.Max(0f, baseSpeed);

            maximumHealth =
                Mathf.Max(1, maximumHealth);

            attackRange =
                Mathf.Max(0.1f, attackRange);

            targetingArcAngle =
                Mathf.Clamp(
                    targetingArcAngle,
                    1f,
                    360f);

            targetingPriority =
                Mathf.Max(0, targetingPriority);
            
            attackInterval =
                Mathf.Max(0.1f, attackInterval);

            projectileDamage =
                Mathf.Max(0f, projectileDamage);

            projectileSpeed =
                Mathf.Max(0.1f, projectileSpeed);
        }
#endif
    }
}
