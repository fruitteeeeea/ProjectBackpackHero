using UnityEngine;

namespace BackpackHero.Battle
{
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
        
        [Tooltip("两次发射之间的时间间隔，单位为秒。")]
        [SerializeField, Min(0.1f)]
        private float attackInterval = 0.5f;

        [Tooltip("每颗子弹造成的伤害。")]
        [SerializeField, Min(0f)]
        private float projectileDamage = 1f;

        [Tooltip("子弹每秒移动的世界单位数。")]
        [SerializeField, Min(0.1f)]
        private float projectileSpeed = 8f;
        
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public Color BaseColor => baseColor;
        public float BaseSpeed => baseSpeed;
        public int MaximumHealth => maximumHealth;
        
        public float AttackRange =>
            attackRange;

        public float TargetingArcAngle =>
            targetingArcAngle;
        
        public float AttackInterval =>
            attackInterval;

        public float ProjectileDamage =>
            projectileDamage;

        public float ProjectileSpeed =>
            projectileSpeed;

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