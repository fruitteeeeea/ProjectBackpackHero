using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 控制一颗战斗子弹的运行时初始化。
    /// 实际移动由DirectionalMover2D负责，
    /// 实际伤害由HitBox2D负责。
    /// </summary>
    [RequireComponent(typeof(DirectionalMover2D))]
    [RequireComponent(typeof(HitBox2D))]
    public sealed class Projectile2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private DirectionalMover2D mover;

        [SerializeField]
        private HitBox2D hitBox;

        public BattleFaction Faction =>
            hitBox != null
                ? hitBox.Faction
                : BattleFaction.Player;

        private void Awake()
        {
            FindReferences();
        }

        /// <summary>
        /// 子弹生成后立即调用。
        /// </summary>
        public void Initialize(
            BattleFaction faction,
            float damage,
            float speed,
            Vector2 direction)
        {
            FindReferences();

            if (mover == null || hitBox == null)
            {
                Debug.LogError(
                    $"{name}无法初始化：缺少Mover或HitBox。",
                    this);

                return;
            }

            Vector2 safeDirection = direction;

            if (safeDirection.sqrMagnitude <=
                Mathf.Epsilon)
            {
                safeDirection = transform.up;
            }

            if (safeDirection.sqrMagnitude <=
                Mathf.Epsilon)
            {
                safeDirection = Vector2.up;
            }

            hitBox.Initialize(
                faction,
                damage);

            mover.SetBaseSpeed(
                Mathf.Max(0f, speed));

            mover.SetSpeedMultiplier(1f);

            mover.Initialize(
                safeDirection);

            gameObject.name =
                $"{faction} Projectile";
        }

        private void FindReferences()
        {
            if (mover == null)
            {
                mover =
                    GetComponent<DirectionalMover2D>();
            }

            if (hitBox == null)
            {
                hitBox =
                    GetComponent<HitBox2D>();
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            FindReferences();
        }

        private void OnValidate()
        {
            FindReferences();
        }
#endif
    }
}