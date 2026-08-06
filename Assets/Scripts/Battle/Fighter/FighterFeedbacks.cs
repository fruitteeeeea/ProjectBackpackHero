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

        [Header("Death VFX")]
        [SerializeField]
        private GameObject explosionParticlesPrefab;

        [SerializeField]
        private ParticleSystem deathFlashParticlesPrefab;

        [SerializeField]
        private Color playerExplosionColor =
            new Color(0.55f, 0.85f, 1f, 1f);

        [SerializeField]
        private Color enemyExplosionColor =
            new Color(1f, 0.55f, 0.55f, 1f);

        private FighterDamageFloatingText floatingDamageText;

        [Header("Death")]
        [SerializeField, Min(0f)]
        private float deathCleanupDelay = 0.55f;

        public float DeathCleanupDelay => deathCleanupDelay;

        public void PlayAttack()
        {
            attackFeedback?.PlayFeedbacks();
        }

        public void PlayHit(
            float damage,
            BattleFaction faction)
        {
            hitFeedback?.PlayFeedbacks(
                transform.position,
                damage);

            floatingDamageText?.Play(damage, faction);
        }

        public void PlayDeath(BattleFaction faction)
        {
            deathFeedback?.PlayFeedbacks();

            Color factionColor =
                faction == BattleFaction.Player
                    ? playerExplosionColor
                    : enemyExplosionColor;

            PlayTintedParticles(
                explosionParticlesPrefab,
                factionColor);

            PlayTintedParticles(
                deathFlashParticlesPrefab,
                factionColor);
        }

        private void PlayTintedParticles(
            GameObject particlesPrefab,
            Color color)
        {
            if (particlesPrefab == null)
            {
                return;
            }

            GameObject particles = Instantiate(
                particlesPrefab,
                transform.position,
                particlesPrefab.transform.rotation);

            foreach (ParticleSystem particleSystem in
                     particles.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main =
                    particleSystem.main;

                main.startColor = color;
            }
        }

        private void PlayTintedParticles(
            ParticleSystem particlesPrefab,
            Color color)
        {
            if (particlesPrefab == null)
            {
                return;
            }

            ParticleSystem particles = Instantiate(
                particlesPrefab,
                transform.position,
                particlesPrefab.transform.rotation);

            ParticleSystem.MainModule main = particles.main;
            main.startColor = color;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            deathCleanupDelay = Mathf.Max(0f, deathCleanupDelay);
        }
#endif

        private void Awake()
        {
            floatingDamageText =
                GetComponent<FighterDamageFloatingText>();
        }
    }
}
