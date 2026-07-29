using MoreMountains.Feedbacks;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 集中触发飞机的 FEEL 反馈。
    /// MMF_Player 由 Prefab Inspector 手动添加和配置，未配置时安全跳过。
    /// </summary>
    public sealed class FighterFeedbacks : MonoBehaviour
    {
        [Header("MMF Players")]
        [SerializeField]
        private MMF_Player attackFeedback;

        [SerializeField]
        private MMF_Player hitFeedback;

        [SerializeField]
        private MMF_Player deathFeedback;

        [Header("Death")]
        [SerializeField, Min(0f)]
        private float deathCleanupDelay = 0.55f;

        public float DeathCleanupDelay => deathCleanupDelay;

        public void PlayAttack()
        {
            attackFeedback?.PlayFeedbacks();
        }

        public void PlayHit()
        {
            hitFeedback?.PlayFeedbacks();
        }

        public void PlayDeath()
        {
            deathFeedback?.PlayFeedbacks();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            deathCleanupDelay = Mathf.Max(0f, deathCleanupDelay);
        }
#endif
    }
}
